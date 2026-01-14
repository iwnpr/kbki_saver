using cache_lib.Interfaces;
using db_lib.DBEntity;
using db_lib.Entity.CommonTypes.Api;
using db_lib.Entity.CommonTypes.Xml;
using db_lib.Entity.qcb_xml.Enums;
using db_lib.Entity.qcb_xml.qcb_answer;
using db_lib.Entity.qcb_xml.qcb_put;
using db_lib.Entity.qcb_xml.qcb_request;
using db_lib.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Globalization;
using System.Xml.Serialization;

namespace db_lib.Services.Implementations
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    /// <param name="db"></param>
    /// <param name="cacheService"></param>
    /// <param name="bKIRequisits"></param>
    /// <param name="logger"></param>
    public class Repository(qbchContext db,
        ICacheService cacheService,
        IBKIRequisitsHandler bKIRequisits,
        IConfiguration config,
        ILogger<Repository> logger,
        ICacheService redisCacheService,
        IBKIRequisitsHandler QBCH) : IRepository
    {
        /// <summary>
        /// Контекст БД
        /// </summary>
        private readonly qbchContext _db = db;
        private readonly ICacheService _cacheService = cacheService;
        private readonly IBKIRequisitsHandler _BKIRequisits = bKIRequisits;
        private readonly IConfiguration _config = config;
        private readonly ILogger<Repository> _logger = logger;
        private readonly ICacheService _redisCacheService = redisCacheService;
        private readonly IBKIRequisitsHandler _QBCH = QBCH;
        private readonly int _dlRequestExpirationMin = config.GetValue<int?>("RedisCache:DlRequestExpirationHours") ?? 480;
        private readonly int _qBCHRequestExpirationMin = config.GetValue<int?>("RedisCache:QBCHRequestExpirationMin") ?? 1;
        private readonly int _dlAnswerExpirationMin = config.GetValue<int?>("RedisCache:DlAnswerExpirationMin") ?? 1;
        private readonly int _dlPutExpirationMin = config.GetValue<int?>("RedisCache:DlPutExpirationHours") ?? 480;
        private readonly int _dlPutAnswerExpirationMin = config.GetValue<int?>("RedisCache:DlPutAnswerExpirationMin") ?? 1;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="thumbprint"></param>
        /// <returns></returns>
        private async Task<TrAbonent?> GetAbonentByThumbprint(string? thumbprint) => await _db.TrAbonents.FirstOrDefaultAsync(abonent => abonent.TrAbonentCertificates.Any(x => x.Thumbprint.ToUpper() == thumbprint!.ToUpper()));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="psrn"></param>
        /// <returns></returns>
        private async Task<TrAbonent> GetAbonentByPSRN(string? psrn) => await _db.TrAbonents.FirstAsync(abonent => abonent.Ogrn == psrn);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ogrn"></param>
        /// <returns></returns>
        private async Task<long?> GetUserIndividualId(string? ogrn) => string.IsNullOrWhiteSpace(ogrn) ? null : (await _db.TdUsersIndividuals.FirstOrDefaultAsync(x => x.Ogrn == ogrn))?.KeyId;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ogrn"></param>
        /// <returns></returns>
        private async Task<long?> GetUserLegalId(string? ogrn) => string.IsNullOrWhiteSpace(ogrn) ? null : (await _db.TdUsersLegals.FirstOrDefaultAsync(x => x.Ogrn == ogrn))?.KeyId;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userType"></param>
        /// <param name="ogrn"></param>
        /// <returns></returns>
        private async Task<long?> GetUserId(int? userType, string? ogrn)
        {
            return userType switch
            {
                1 or 3 or 4 => await GetUserLegalId(ogrn),
                2 or 5 => await GetUserIndividualId(ogrn),
                _ => null
            };
        }

        /* Реализация сохранения 
 * данных в БД
 */

        /// <summary>
        /// Запись запроса dlrequest
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="thumbprint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task SaveDlRequest(string key, string guid)
        {
            try
            {
                var data = await _redisCacheService.TryGetHashAll(key);

                var abonent = await GetAbonentByThumbprint(data.FirstOrDefault(x => x.Name == "Thumbprint").Value);

                var RequestString = data.FirstOrDefault(x => x.Name == "request").Value;
                ЗапросСведенийОПлатежах? request = null;

                if (!string.IsNullOrWhiteSpace(RequestString.ToString()))
                {
                    var serializer = new XmlSerializer(typeof(ЗапросСведенийОПлатежах));
                    request = serializer.Deserialize(new StringReader(RequestString!)) as ЗапросСведенийОПлатежах;
                }

                // Добавление запроса
                var requestId = await AddDlRequest(request, abonent?.KeyId, key, guid);
                var requestId2 = requestId;

                if (request is not null)
                {
                    List<Task> _list =
                     [
                         Task.Run(async () => { await AddSubject(request, requestId); }),
                         Task.Run(async () => { await AddQBCHRequests(requestId2, key); }),
                     ];

                    await Task.WhenAll(_list);

                    await EfSaveChanges();
                    
                    await _db.DisposeAsync();

                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Возникла оишбка записи в БД");
            }

            try
            {
                await _redisCacheService.SetKeyExpiration(key, _dlRequestExpirationMin);
                foreach (var item in _QBCH.GetBureaList())
                {
                    await _redisCacheService.SetKeyExpiration($"{key}:{item.ogrn}", _qBCHRequestExpirationMin);
                }

            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Возникла ошибка изменения времени жазни ключа в Redis");
            }
        }

        /// <summary>
        /// Запись запроса dlanswer
        /// </summary>
        /// <returns></returns>
        public async Task SaveDlAnswer(string key, string guid)
        {
            await CheckReddisHasValues(key, guid);

            try
            {
                var values = await _cacheService.TryGetHashAll(key);

                var abonent = await GetAbonentByThumbprint(values.FirstOrDefault(x => x.Name == "Thumbprint").Value);

                // Добавление запроса
                TeDlanswer TeDlanswer = new()
                {
                    DlanswerId = values.FirstOrDefault(x => x.Name == "guid").Value,
                    RequestDateTime = DateTime.ParseExact(values.FirstOrDefault(x => x.Name == "RequestTime").Value!, "dd.MM.yyyy HH:mm:ss:ffff", CultureInfo.InvariantCulture),
                    ValidationDateTime = DateTime.TryParseExact(values.FirstOrDefault(x => x.Name == "ValidationTime").Value, "dd.MM.yyyy HH:mm:ss:ffff", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ValidateDateTime) ? ValidateDateTime : null,
                    ResponseDateTime = DateTime.ParseExact(values.FirstOrDefault(x => x.Name == "ResponseTime").Value!, "dd.MM.yyyy HH:mm:ss:ffff", CultureInfo.InvariantCulture),
                    ResponseXml = values.FirstOrDefault(x => x.Name == "ResponseXml").Value,
                    AbonentKeyId = abonent?.KeyId,
                    TempGuid = guid,
                    ResponseSignedData = values.FirstOrDefault(x => x.Name == "SignedResponse").Value,
                    RequestCertificateThumbprint = values.FirstOrDefault(x => x.Name == "Thumbprint").Value,
                    ErrorMessage = values.FirstOrDefault(x => x.Name == "Error").Value,
                    ErrorCodeKeyId = (int)values.FirstOrDefault(x => x.Name == "ErrorCode").Value,
                    IpAddress = values.FirstOrDefault(x => x.Name == "IpAddress").Value,
                };

                await _db.TeDlanswers.AddAsync(TeDlanswer);

                await EfSaveChanges();

            }
            catch (Exception ex)
            {
                await _redisCacheService.DeleteKeyExpiration(key);
                _logger.LogError(ex, "Ошибка");
            }

            try
            {
                await _redisCacheService.SetKeyExpiration(key, _dlAnswerExpirationMin);

            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Возникла ошибка изменения времени жазни ключа в Redis");
            }
        }

        /// <summary>
        /// Запись запроса dlputrequest
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="thumbprint"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task SaveDlPut(string key, string? guid)
        {
            try
            {
                var values = await _cacheService.TryGetHashAll(key);
                ПредставлениеСведенийОПлатежах? request = null;

                try
                {
                    var serializer = new XmlSerializer(typeof(ПредставлениеСведенийОПлатежах));
                    request = serializer.Deserialize(new StringReader(values.FirstOrDefault(x => x.Name == "request").Value!)) as ПредставлениеСведенийОПлатежах;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Кривой запрос {request}", values.FirstOrDefault(x => x.Name == "request").Value);
                }

                var abonent = await GetAbonentByThumbprint(values.FirstOrDefault(x => x.Name == "Thumbprint").Value);

                // Добавление запроса
                TeDlput teRequest = new()
                {
                    DlputanswerId = guid,
                    RequestId = values.FirstOrDefault(x => x.Name == "RequestId").Value,
                    RequestDateTime = DateTime.ParseExact(values.FirstOrDefault(x => x.Name == "RequestTime").Value!, "dd.MM.yyyy HH:mm:ss:ffff", CultureInfo.InvariantCulture),
                    ResponseDateTime = DateTime.ParseExact(values.FirstOrDefault(x => x.Name == "ResponseTime").Value!, "dd.MM.yyyy HH:mm:ss:ffff", CultureInfo.InvariantCulture),
                    AbonentKeyId = abonent?.KeyId,
                    RequestSignedData = values.FirstOrDefault(x => x.Name == "SignedRequest").Value,
                    RequestXml = values.FirstOrDefault(x => x.Name == "request").Value,
                    ErrorMessage = values.FirstOrDefault(x => x.Name == "Error").Value,
                    ErrorCodeKeyId = (int)values.FirstOrDefault(x => x.Name == "ErrorCode").Value,
                    ValidationDateTime = DateTime.TryParseExact(values.FirstOrDefault(x => x.Name == "ValidationTime").Value, "dd.MM.yyyy HH:mm:ss:ffff", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ValidationTime) ? ValidationTime : null,
                    ResponseSignedData = values.FirstOrDefault(x => x.Name == "SignedResponse").Value,
                    AddCommandsCount = request?.Договоры.Count(item => item.Item is ДоговорДобавить) ?? 0,
                    DeleteCommandsCount = request?.Договоры.Count(item => item.Item is ДоговорУдалить) ?? 0,
                    IpAddress = values.FirstOrDefault(x => x.Name == "IpAddress").Value,
                    RequestCertificateThumbprint = values.FirstOrDefault(x => x.Name == "Thumbprint").Value,
                    ResponseXml = values.FirstOrDefault(x => x.Name == "ResponseXml").Value
                };

                await _db.TeDlputs.AddAsync(teRequest);
                await EfSaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка");
            }


            try
            {
                await _redisCacheService.SetKeyExpiration(key, _dlPutExpirationMin);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Возникла ошибка изменения времени жазни ключа в Redis");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        public async Task SaveDlPutAnswer(string key, string guid)
        {
            try
            {
                var values = await _cacheService.TryGetHashAll(key);
                var abonent = await GetAbonentByThumbprint(values.FirstOrDefault(x => x.Name == "Thumbprint").Value);

                TeDlputanswer teDlPutAnswer = new()
                {
                    DlputanswerId = values.FirstOrDefault(x => x.Name == "guid").Value,
                    RequestDateTime = DateTime.ParseExact(values.FirstOrDefault(x => x.Name == "RequestTime").Value!, "dd.MM.yyyy HH:mm:ss:ffff", CultureInfo.InvariantCulture),
                    ValidationDateTime = DateTime.TryParseExact(values.FirstOrDefault(x => x.Name == "ValidationTime").Value, "dd.MM.yyyy HH:mm:ss:ffff", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ValidationTime) ? ValidationTime : null,
                    ResponseDateTime = DateTime.ParseExact(values.FirstOrDefault(x => x.Name == "ResponseTime").Value!, "dd.MM.yyyy HH:mm:ss:ffff", CultureInfo.InvariantCulture),
                    ResponseXml = values.FirstOrDefault(x => x.Name == "ResponseXml").Value,
                    ResponseSignedData = values.FirstOrDefault(x => x.Name == "SignedResponse").Value,
                    AbonentKeyId = abonent?.KeyId,
                    TempGuid = guid,
                    RequestCertificateThumbprint = values.FirstOrDefault(x => x.Name == "Thumbprint").Value,
                    ErrorMessage = values.FirstOrDefault(x => x.Name == "Error").Value,
                    ErrorCodeKeyId = (int)values.FirstOrDefault(x => x.Name == "ErrorCode").Value,
                    IpAddress = values.FirstOrDefault(x => x.Name == "IpAddress").Value
                };

                await _db.TeDlputanswers.AddAsync(teDlPutAnswer);
                await EfSaveChanges();
            }
            catch (Exception ex)
            {
                await _redisCacheService.DeleteKeyExpiration(key);
                _logger.LogError(ex, "Ошибка");
            }

            try
            {
                await _redisCacheService.SetKeyExpiration(key, _dlPutAnswerExpirationMin);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Возникла ошибка изменения времени жазни ключа в Redis");
            }
        }

        /* Вспомогательные методы 
         * для сохранения данных в БД
         */

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        private async Task CheckReddisHasValues(string key, string guid)
        {
            int i = 0;
            while (!_cacheService.TryGetHash(key, "ResponseTime", out var result))
            {
                i++;
                if (i > 15)
                {
                    _logger.LogCritical("ResponseTime отсутствует в reddis {key}", key);
                    break;
                }

                await Task.Delay(1000);
            }
        }

        /// <summary>
        /// Добавление всех запросов КБКИ в бд
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        private async Task AddQBCHRequests(long requestId, string key)
        {
            var bureaulist = _BKIRequisits.GetBureaList();

            if (!await _redisCacheService.HashFieldExists(key, "QBCHTotalTime"))
                return;

            var ourBureauOgrn = bureaulist.First(x => x.Name == "BKICI").ogrn;

            //// Создаем лист с сущностями запросов в КБКИ и сразу добавляем туда наш запрос
            List<TeQbchDlrequest> QBCHtoDB = [];

            // Перебираем другие КБКИ и создаем запись в БД
            foreach (var item in bureaulist)
            {
                var values = await _cacheService.TryGetHashAll($"{key}:{item.ogrn}");
                if (values.Length == 0)
                    continue;

                //var QBCHId = (await GetAbonentByPSRN(item.ogrn)).KeyId;
                var responseXml = values.FirstOrDefault(x => x.Name == "ResponseXml").Value;

                QBCHtoDB.Add(new()
                {
                    //QbchKeyId = QBCHId,
                    QbchKeyId = int.Parse(item.Id),
                    DlrequestMainKeyId = requestId,
                    TaskStartDateTime = DateTime.ParseExact(values.First(x => x.Name == "StartTime").Value!, "dd.MM.yyyy HH:mm:ss:ffff", CultureInfo.InvariantCulture),
                    ResponseDateTime = DateTime.TryParseExact(values.FirstOrDefault(x => x.Name == "ResponseTime").Value, "dd.MM.yyyy HH:mm:ss:ffff",
                                                            CultureInfo.InvariantCulture, DateTimeStyles.None, out var ResponseDateTime) ? ResponseDateTime : null,
                    RequestSignedData = values.FirstOrDefault(x => x.Name == "SignedRequest").Value,
                    RequestXml = values.FirstOrDefault(x => x.Name == "RequestXml").Value,
                    ErrorMessage = values.FirstOrDefault(x => x.Name == "ErrorMessage").Value,
                    ErrorCodeKeyId = (int)values.FirstOrDefault(x => x.Name == "ErrorCode").Value,
                    ResponseId = values.FirstOrDefault(x => x.Name == "TicketId").Value,
                    DlrequestResendCount = (int)values.FirstOrDefault(x => x.Name == "TicketResendCount").Value,
                    DlanswerResendCount = (int)values.FirstOrDefault(x => x.Name == "AnswerResendCount").Value,
                    DlanswerStartDateTime = DateTime.TryParseExact(values.FirstOrDefault(x => x.Name == "DlAnswerStartTime").Value,
                                                    "dd.MM.yyyy HH:mm:ss:ffff", CultureInfo.InvariantCulture, DateTimeStyles.None, out var DlanswerStartDateTime) ? DlanswerStartDateTime : null,
                    DlrequestStartDateTime = DateTime.TryParseExact(values.FirstOrDefault(x => x.Name == "DlRequestStartTime").Value,
                                                            "dd.MM.yyyy HH:mm:ss:ffff", CultureInfo.InvariantCulture, DateTimeStyles.None, out var DlrequestStartDateTime) ? DlrequestStartDateTime : null,
                    ResponseSignedData = values.FirstOrDefault(x => x.Name == "SignedResponse").Value,
                    ResponseXml = responseXml,
                    ResponseType = item.ogrn == ourBureauOgrn ? GetResponseType(responseXml, ourBureauOgrn) : null,
                });

            }

            await _db.TeQbchDlrequests.AddRangeAsync(QBCHtoDB);
        }

        /// <summary>
        /// Добавить субъекта в БД
        /// </summary>
        /// <param name="request"></param>
        /// <param name="requestId"></param>
        /// <returns></returns>
        private async Task AddSubject(ЗапросСведенийОПлатежах request, long requestId)
        {
            // Добавление субъектов
            TeSubject subject = new()
            {
                BirthDay = DateOnly.FromDateTime(request.Запрос!.Субъект!.ДатаРождения),
                Inn = request.Запрос.Субъект.ИнНомер,
                Snils = request.Запрос.Субъект.СНИЛС,
                RequestKeyId = requestId
            };
            await _db.TeSubjects.AddAsync(subject);
            await EfSaveChanges();

            var subjectId = subject.KeyId;

            // Документы субъекта
            var docs = request.Запрос?.Субъект?.ДокументЛичности?.Select(x => new TeSubjectsDocument()
            {
                DocTypeKeyId = GetEnumDescription(x.КодДУЛ),
                DocDateIssue = DateOnly.FromDateTime(x.ДатаВыдачи),
                DocSeries = x.Серия,
                DocNumber = x.Номер!,
                CountryCode = int.TryParse(x.Гражданство, out var result) ? result : null,
                SubjectKeyId = subjectId
            });

            if (docs?.Any() ?? false)
            {
                await _db.TeSubjectsDocuments.AddRangeAsync(docs);
                //await EfSaveChanges();
            }

            // ФИО субъекта
            var FIO = request.Запрос?.Субъект?.ФИО?.Select(x => new TeSubjectsFullName()
            {
                FirstName = x.Имя,
                LastName = x.Фамилия,
                MiddleName = x.Отчество,
                SubjectKeyId = subjectId
            });

            if (FIO?.Any() ?? false)
            {
                await _db.TeSubjectsFullNames.AddRangeAsync(FIO);
                //await EfSaveChanges();
            }
        }

        /// <summary>
        /// Добавление запроса
        /// </summary>
        /// <param name="request"></param>
        /// <param name="abonentId"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        private async Task<long> AddDlRequest(ЗапросСведенийОПлатежах? request, int? abonentId, string key, string guid)
        {
            var values = await _cacheService.TryGetHashAll(key);
            var userType = GetUserType(request?.Запрос?.Источник);

            // Добавление запроса
            TeDlrequest teDLRequest = new()
            {
                RequestId = values.FirstOrDefault(x => x.Name == "RequestId").Value,
                DlanswerId = guid,
                RequestDateTime = DateTime.ParseExact(values.FirstOrDefault(x => x.Name == "RequestTime").Value!, "dd.MM.yyyy HH:mm:ss:ffff", CultureInfo.InvariantCulture),
                ResponseDateTime = DateTime.ParseExact(values.FirstOrDefault(x => x.Name == "ResponseTime").Value!, "dd.MM.yyyy HH:mm:ss:ffff", CultureInfo.InvariantCulture),
                AbonentKeyId = abonentId,
                RequestSignedData = values.FirstOrDefault(x => x.Name == "SignedRequest").Value,
                RequestXml = values.FirstOrDefault(x => x.Name == "request").Value,
                ErrorMessage = values.FirstOrDefault(x => x.Name == "Error").Value,
                ErrorCodeKeyId = (int)values.FirstOrDefault(x => x.Name == "ErrorCode").Value,
                ValidationDateTime = DateTime.TryParseExact(values.FirstOrDefault(x => x.Name == "ValidationTime").Value, "dd.MM.yyyy HH:mm:ss:ffff",
                                                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var ValidationDateTime) ? ValidationDateTime : null,
                ResponseSignedData = values.FirstOrDefault(x => x.Name == "SignedResponse").Value,
                RequsetTypeKeyId = request?.ТипЗапроса is null ? null : (int?)request?.ТипЗапроса + 1,
                UserTypeId = userType,
                UserId = await GetUserId(userType, request?.Запрос?.Источник?.Ogrn) ?? (userType.HasValue ? await CreateUser(request) : null),
                IpAddress = values.FirstOrDefault(x => x.Name == "IpAddress").Value,
                RequestCertificateThumbprint = values.FirstOrDefault(x => x.Name == "Thumbprint").Value,
                ResponseXml = values.FirstOrDefault(x => x.Name == "ResponseXml").Value,
                QbchTotalExecutionDateTime = DateTime.TryParseExact(values.FirstOrDefault(x => x.Name == "QBCHTotalTime").Value, "dd.MM.yyyy HH:mm:ss:ffff",
                                                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var QbchTotalExecutionDateTime) ? QbchTotalExecutionDateTime : null
            };

            await _db.TeDlrequests.AddAsync(teDLRequest);
            await EfSaveChanges();
            var keyId = teDLRequest.KeyId;
            teDLRequest.Dispose();
            return keyId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userType"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private async Task<long?> CreateUser(ЗапросСведенийОПлатежах? request)
        {
            long? result = null;

            if (request?.Запрос?.Источник?.ИностранныйПредприниматель is ТипИностранныйПредприниматель foreignIE)
            {
                result = await CreateUserNP(
                    foreignIE.ФИО?.Фамилия,
                    foreignIE.ФИО?.Имя,
                    foreignIE.ФИО?.Отчество,
                    foreignIE.ОГРН,
                    foreignIE.ИНН,
                    foreignIE.ДатаРождения,
                    foreignIE.МестоРождения,
                    foreignIE.ДокументЛичности?.ДатаВыдачи,
                    GetEnumDescription(foreignIE.ДокументЛичности?.КодДУЛ),
                    foreignIE.ДокументЛичности?.КодПодразделения,
                    foreignIE.ДокументЛичности?.НаименованиеДУЛ,
                    foreignIE.ДокументЛичности?.НаименованиеОргана,
                    foreignIE.ДокументЛичности?.Номер,
                    foreignIE.ДокументЛичности?.Серия);
            }
            else if (request?.Запрос?.Источник?.ИндивидуальныйПредприниматель is ТипИП IE)
            {
                result = await CreateUserNP(
                    IE.ФИО?.Фамилия,
                    IE.ФИО?.Имя,
                    IE.ФИО?.Отчество,
                    IE.ОГРН,
                    IE.ИНН,
                    IE.ДатаРождения,
                    IE.МестоРождения,
                    IE.ДокументЛичности?.ДатаВыдачи,
                    GetEnumDescription(IE.ДокументЛичности?.КодДУЛ),
                    IE.ДокументЛичности?.КодПодразделения,
                    IE.ДокументЛичности?.НаименованиеДУЛ,
                    IE.ДокументЛичности?.НаименованиеОргана,
                    IE.ДокументЛичности?.Номер,
                    IE.ДокументЛичности?.Серия,
                    IE.СНИЛС);
            }
            else if (request?.Запрос?.Источник?.ЮридическоеЛицо is ЗапросИсточникЮридическоеЛицо LP)
            {
                result = await CreateUserNP(
                    LP.ИНН,
                    LP.ОГРН,
                    LP.ПолноеНаименование,
                    LP.СокращенноеНаименование,
                    LP.ИноеНаименование,
                    true);
            }
            else if (request?.Запрос?.Источник?.ИностранноеЮЛ is ЗапросИсточникИностранноеЮЛ foreignLP)
            {
                result = await CreateUserNP(
                   foreignLP.ИНН,
                   foreignLP.ОГРН,
                   foreignLP.ПолноеНаименование,
                   foreignLP.СокращенноеНаименование,
                   foreignLP.ИноеНаименование,
                   false);
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inn"></param>
        /// <param name="ogrn"></param>
        /// <param name="fullName"></param>
        /// <param name="shortName"></param>
        /// <param name="otherName"></param>
        /// <param name="isForeign"></param>
        /// <returns></returns>
        private async Task<long?> CreateUserNP(string? inn,
            string? ogrn, string? fullName, string? shortName, string? otherName, bool? isForeign)
        {
            TdUsersLegal user =

                new()
                {
                    IsForeign = isForeign,
                    FullName = fullName,
                    ShortName = shortName,
                    Inn = inn,
                    Ogrn = ogrn,
                    OtherName = otherName
                };

            await _db.TdUsersLegals.AddAsync(user);

            await EfSaveChanges();
            return user.KeyId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lastName"></param>
        /// <param name="firstName"></param>
        /// <param name="middleName"></param>
        /// <param name="ogrn"></param>
        /// <param name="inn"></param>
        /// <param name="dateOfBirth"></param>
        /// <param name="placeOfBirth"></param>
        /// <param name="issueDate"></param>
        /// <param name="docCode"></param>
        /// <param name="issuerCode"></param>
        /// <param name="docName"></param>
        /// <param name="issuerName"></param>
        /// <param name="number"></param>
        /// <param name="series"></param>
        /// <param name="snils"></param>
        /// <returns></returns>
        private async Task<long?> CreateUserNP(string? lastName, string? firstName, string? middleName,
            string? ogrn, string? inn, DateTime dateOfBirth, string? placeOfBirth, DateTime? issueDate,
            string? docCode, string? issuerCode, string? docName,
            string? issuerName, string? number, string? series, string? snils = null)
        {
            TdUsersIndividual user =

                new()
                {
                    Inn = inn,
                    Ogrn = ogrn,
                    Snils = snils,
                    LastName = lastName,
                    FirstName = firstName,
                    MiddleName = middleName,
                    DocTypeKeyId = docCode,
                    BirthDate = DateOnly.FromDateTime(dateOfBirth),
                    BirthPlace = placeOfBirth,
                    DocIssueDate = DateOnly.FromDateTime(issueDate.Value),
                    DocIssuerCode = issuerCode,
                    DocIssuerName = issuerName,
                    DocOtherName = docName,
                    DocSeria = series,
                    DocNumber = number
                };
            await _db.TdUsersIndividuals.AddAsync(user);

            await EfSaveChanges();

            return user.KeyId;
        }


        /// <summary>
        /// Запись данных в БД EF
        /// </summary>
        /// <returns></returns>
        private async Task<bool> EfSaveChanges()
        {
            try
            {
                return await _db.SaveChangesAsync() > 0;
                
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Ошибка записи в БД при помощи EF");
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmlText"></param>
        /// <param name="ourBureauOgrn"></param>
        /// <returns></returns>
        private int? GetResponseType(string? xmlText, string? ourBureauOgrn)
        {
            if (string.IsNullOrWhiteSpace(xmlText))
                return null;
            try
            {
                var serializer = new XmlSerializer(typeof(СведенияОПлатежах));
                using var sr = new StringReader(xmlText);

                if (serializer.Deserialize(sr) is not СведенияОПлатежах result)
                    return null;

                var qbch = result.КБКИ?.FirstOrDefault();

                if (qbch?.СубъектНеНайден != null)
                    return 1;
                else if (qbch?.ОбязательствНет != null)
                    return 2;
                else if (qbch?.Обязательства != null)
                {
                    var ourData = qbch.Обязательства.БКИ?.Any(x => x.ОГРН == ourBureauOgrn) ?? false;
                    var notOurData = qbch.Обязательства.БКИ?.Any(x => x.ОГРН != ourBureauOgrn) ?? false;

                    // Если есть данные и от нас и от кредо
                    if (ourData && notOurData)
                    {
                        return 4;
                    }
                    // Если есть данные от кредо, а наших нет
                    else if (!ourData && notOurData)
                    {
                        return 5;
                    }
                    // Если есть данные только от нас
                    else if (ourData && !notOurData)
                    {
                        return 3;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Возникла ошибка при десериализации");
            }

            return null;
        }

        /// <summary>
        /// Маппинг типа источника
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private int? GetUserType(ЗапросИсточник? source)
        {
            if (source?.ЮридическоеЛицо != null)
                return 1;
            else if (source?.ИндивидуальныйПредприниматель != null)
                return 2;
            else if (source?.ИностранноеЮЛ != null)
                return 3;
            else if (source?.ИностранныйПредприниматель != null)
                return 4;

            return null;
        }

        /// <summary>
        /// Маппинг кода цели
        /// </summary>
        /// <param name="target">Код цели enum</param>
        /// <returns></returns>
        private int GetTargetCode(ТипЦельКодЦели target)
        {
            return target switch
            {
                ТипЦельКодЦели.Item1 => 1,
                ТипЦельКодЦели.Item2 => 2,
                ТипЦельКодЦели.Item3 => 3,
                ТипЦельКодЦели.Item4 => 4,
                ТипЦельКодЦели.Item5 => 5,
                ТипЦельКодЦели.Item6 => 6,
                ТипЦельКодЦели.Item7 => 7,
                ТипЦельКодЦели.Item8 => 8,
                ТипЦельКодЦели.Item9 => 9,
                ТипЦельКодЦели.Item10 => 10,
                ТипЦельКодЦели.Item11 => 11,
                ТипЦельКодЦели.Item12 => 12,
                ТипЦельКодЦели.Item13 => 13,
                ТипЦельКодЦели.Item14 => 14,
                ТипЦельКодЦели.Item15 => 15,
                ТипЦельКодЦели.Item16 => 16,
                ТипЦельКодЦели.Item17 => 17,
                ТипЦельКодЦели.Item18 => 18,
                ТипЦельКодЦели.Item19 => 19,
                ТипЦельКодЦели.Item20 => 20,
                ТипЦельКодЦели.Item21 => 21,
                ТипЦельКодЦели.Item22 => 22,
                ТипЦельКодЦели.Item23 => 23,
                ТипЦельКодЦели.Item24 => 24,
                ТипЦельКодЦели.Item25 => 25,
                ТипЦельКодЦели.Item26 => 26,
                ТипЦельКодЦели.Item27 => 27,
                ТипЦельКодЦели.Item99 => 99,
                _ => 0,
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string? GetEnumDescription(Enum? value)
        {
            var fi = value?.GetType().GetField(value.ToString());

            if (fi?.GetCustomAttributes(typeof(DescriptionAttribute), false) is DescriptionAttribute[] attributes && attributes.Length != 0)
            {
                return attributes.First().Description;
            }

            return value?.ToString();
        }
    }
}