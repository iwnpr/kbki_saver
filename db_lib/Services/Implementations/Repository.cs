using cache_lib.Interfaces;
using db_lib.Entities;
using db_lib.Models.DTO;
using db_lib.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QBCH_lib.CommonTypes.Api;
using QBCH_lib.qcb_xml.v2_0.CommonTypes;
using QBCH_lib.qcb_xml.v2_0.Enums;
using QBCH_lib.qcb_xml.v2_0.qcb_answer;
using QBCH_lib.qcb_xml.v2_0.qcb_request;
using QBCHService_lib.Models.DTOs;
using StackExchange.Redis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Xml_service_lib;

namespace db_lib.Services.Implementations
{
    public class Repository(
        qbchContext context,
        ICacheService cacheService,
        ILogger<Repository> logger,
        IXmlService xmlService,
        IBKIRequisitsHandler requisits) : IRepository
    {
        private readonly qbchContext _context = context;
        private readonly ICacheService _cacheService = cacheService;
        private readonly ILogger<Repository> _logger = logger;
        private readonly IXmlService _xmlService = xmlService;
        private readonly List<QBCHRequisite> _bureauList = requisits.GetBureaList();
        private const string OurBureaName = "BKICI";

        /// <summary>
        /// Получить абонента по ОГРН
        /// </summary>
        /// <param name="psrn">ОГРН</param>
        /// <returns>Абонент</returns>
        private async Task<TrAbonent?> GetAbonentByPSRN(string? psrn)
        {
            // Получаем список сертификатов из кэша
            DataHelper.Abonents ??= await _context.TrAbonents.ToListAsync();

            var psrnUpper = psrn?.ToUpper();
            TrAbonent? abonent = DataHelper.Abonents?.FirstOrDefault(x => x.Ogrn == psrnUpper);

            // Если в Кэше данных не нашлось, возможно в таблице есть обновления.  Обновляем Кэш.
            if (abonent is null)
                DataHelper.Abonents = await _context.TrAbonents.ToListAsync();
            else
                return abonent;

            return DataHelper.Abonents?.FirstOrDefault(x => x.Ogrn == psrnUpper);
        }

        /// <summary>
        /// Получить абонента по отпечатку сертификата
        /// </summary>
        /// <param name="thumbprint">Отпечаток сертификата</param>
        /// <returns>Абонент</returns>
        private async Task<TrAbonent?> GetAbonentByThumbprint(string? thumbprint)
        {
            // Получаем список сертификатов из кэша

            var certs = _context.TrAbonentCertificates.Include(x => x.Abonent).ToList();
            DataHelper.AbonentCertificates ??= await _context.TrAbonentCertificates.Include(x => x.Abonent).ToListAsync();

            var thumbprintUpper = thumbprint?.ToUpper();
            TrAbonent? abonent = DataHelper.AbonentCertificates?.FirstOrDefault(x => x.Thumbprint.ToUpper() == thumbprintUpper)?.Abonent;

            // Если в Кэше данных не нашлось, возможно в таблице есть обновления. Обновляем Кэш.
            if (abonent is null)
                DataHelper.AbonentCertificates = await _context.TrAbonentCertificates.Include(x => x.Abonent).ToListAsync();
            else
                return abonent;

            return DataHelper.AbonentCertificates?.FirstOrDefault(x => x.Thumbprint.ToUpper() == thumbprintUpper)?.Abonent;
        }

        private async Task<TdUser?> GetOrCreateUserByPSRNAsync(string? psrn)
        {
            // Получаем список сертификатов из кэша
            DataHelper.Users ??= await _context.TdUsers.ToListAsync();

            var psrnUpper = psrn?.ToUpper();
            TdUser? abonent = DataHelper.Users?.FirstOrDefault(x => x.Ogrn?.ToUpper() == psrnUpper);

            // Если в Кэше данных не нашлось, возможно в таблице есть обновления.  Обновляем Кэш.
            if (abonent is null)
                DataHelper.Users = await _context.TdUsers.ToListAsync();
            else
                return abonent;

            return DataHelper.Users?.FirstOrDefault(x => x.Ogrn?.ToUpper() == psrnUpper);
        }

        /// <summary>
        /// Обертка tryparse возвращающая 0 в случае отсувствия значения
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static int GetIntValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            if (int.TryParse(value, out var result))
            {
                return result;
            }
            else
            {
                throw new Exception("Ошибка считывания значения поля ErrorCode");
            }
        }

        private static int? TryGetIntValue(string? value)
        {
            return int.TryParse(value, out var result) ? result : null;
        }

        /// <summary>
        /// Попытка распросить DateTime из строки по шаблону dd.MM.yyyy HH:mm:ss:ffff
        /// </summary>
        /// <param name="value">Значение</param>
        /// <param name="pattern">Паттерн с дефолтным значением</param>
        /// <returns>DateTime</returns>
        private static DateTime? GetDateTimeValue(byte[]? value, string? pattern = "dd.MM.yyyy HH:mm:ss:ffff")
        {

            if (DateTime.TryParseExact(Encoding.UTF8.GetString(value), pattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out var ValidDateTime))
                return ValidDateTime;

            return null;
        }

        /// <summary>
        /// Попытка распросить DateTime из строки по шаблону dd.MM.yyyy HH:mm:ss:ffff
        /// </summary>
        /// <param name="value">Значение</param>
        /// <param name="pattern">Паттерн с дефолтным значением</param>
        /// <returns>DateTime</returns>
        private static DateTime? GetDateTimeValue(string? value, string? pattern = "dd.MM.yyyy HH:mm:ss:ffff")
        {

            if (DateTime.TryParseExact(value, pattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out var ValidDateTime))
                return ValidDateTime;

            return null;
        }

        private static string? TryParseXmlBytesToString(byte[]? bytes)
        {
            try
            {
                if (bytes is not null)
                    return XDocument.Load(new MemoryStream(bytes)).ToString();
            }
            catch
            {
                return null;
            }

            return null;
        }

        /// <summary>
        /// Получить пользователя по огрн
        /// </summary>
        /// <param name="psrn">Огрн</param>
        /// <returns>Пользователь</returns>
        private async Task<TdUser?> GetOrCreateUser(Запрос Запрос, List<TdUser> users)
        {
            TdUser? user = null;

            if (Запрос.Источник.ИностранныйПредприниматель is not null || Запрос.Источник.ИностранноеЮЛ is not null)
            {
                return await GetOrCreateForeign(Запрос.Источник, users);
            }

            var psrnUpper = Запрос.Источник.ЮридическоеЛицо?.ОГРН.ToUpper() ??
                            Запрос.Источник.ИндивидуальныйПредприниматель?.ОГРН.ToUpper();

            if (users.Any(x => x.Ogrn?.ToUpper() == psrnUpper))
                return users.FirstOrDefault(x => x.Ogrn?.ToUpper() == psrnUpper);

            user = await _context.TdUsers.FirstOrDefaultAsync(x => !string.IsNullOrWhiteSpace(x.Ogrn) && x.Ogrn.ToUpper() == psrnUpper);

            /* Типы пользователей из БД
             * 1	Юридическое лицо
             * 2	Индивидуальный предприниматель
             */
            if (user is null)
            {
                if (Запрос.Источник.ИндивидуальныйПредприниматель is ТипИП ИП)
                {
                    user = CreateUserndIvidual(
                        ИП.ФИО,
                        ИП.ДокументЛичности,
                        DateOnly.FromDateTime(ИП.ДатаРождения),
                        ИП.МестоРождения,
                        ИП?.ИНН,
                        ИП?.ОГРН,
                        2,
                        snils: ИП?.СНИЛС);
                }
                else if (Запрос.Источник.ЮридическоеЛицо is ЗапросСведенийЗапросИсточникЮридическоеЛицо ЮЛ)
                {
                    user = CreateUserLegal(
                        ЮЛ.ПолноеНаименование,
                        ЮЛ?.СокращенноеНаименование,
                        ЮЛ?.ИноеНаименование,
                        ЮЛ?.ИНН,
                        ЮЛ?.ОГРН,
                        1);
                }
            }

            if (user is not null)
                users.Add(user);

            return user;
        }

        private async Task<TdUser?> GetOrCreateForeign(ЗапросСведенийЗапросИсточник источник, List<TdUser> users)
        {
            TdUser? user = null;

            var IndividualKey = источник.ИностранныйПредприниматель?.ФИО.Фамилия + источник.ИностранныйПредприниматель?.ФИО.Имя + источник.ИностранныйПредприниматель?.ДокументЛичности.Серия + источник.ИностранныйПредприниматель?.ДокументЛичности.Номер;
            var userName = источник.ИностранноеЮЛ?.ПолноеНаименование ?? IndividualKey;

            user = users.FirstOrDefault(x => x.FullName == userName);

            if (user is not null)
                return user;

            user = await _context.TdUsers.AsNoTracking().FirstOrDefaultAsync(x => x.FullName == userName);

            if (user is null)
            {
                if (источник.ИностранныйПредприниматель is ТипИностранныйПредприниматель ИностранныйИП)
                {
                    user = CreateUserndIvidual(
                        ИностранныйИП.ФИО,
                        ИностранныйИП.ДокументЛичности,
                        DateOnly.FromDateTime(ИностранныйИП.ДатаРождения),
                        ИностранныйИП.МестоРождения,
                        ИностранныйИП?.ИНН,
                        ИностранныйИП?.ОГРН,
                        5, // 5 - Иностранный предприниматель
                        true);
                    user.FullName = IndividualKey;
                }
                else if (источник.ИностранноеЮЛ is ЗапросСведенийЗапросИсточникИностранноеЮЛ иностранноеЮЛ)
                {
                    user = CreateUserLegal(
                        иностранноеЮЛ.ПолноеНаименование,
                        иностранноеЮЛ?.СокращенноеНаименование,
                        иностранноеЮЛ?.ИноеНаименование,
                        иностранноеЮЛ?.ИНН,
                        иностранноеЮЛ?.ОГРН,
                        1,
                        true);
                }
            }

            if (user is not null)
                users.Add(user);

            return user;
        }

        private static TdUser CreateUserndIvidual(
            ТипФИО ФИО,
            ТипДУЛПредпринимателя ДУЛ,
            DateOnly датаРождения,
            string? местоРождения,
            string? tin,
            string? psrn,
            int userType,
            bool isForeign = false,
            string? snils = null)
        {
            return new TdUser()
            {
                BirthDate = датаРождения,
                BirthPlace = местоРождения,
                FirstName = ФИО.Имя,
                LastName = ФИО.Фамилия,
                MiddleName = ФИО.Отчество,
                DocIssueDate = DateOnly.FromDateTime(ДУЛ.ДатаВыдачи),
                DocIssuerCode = ДУЛ.КодПодразделения,
                DocIssuerName = ДУЛ.НаименованиеОргана,
                DocNumber = ДУЛ.Номер,
                DocSeria = ДУЛ.Серия,
                DocOtherName = ДУЛ.НаименованиеДУЛ,
                DocType = MapDocType(ДУЛ.КодДУЛ),
                Inn = tin,
                Ogrn = psrn,
                Snils = snils,
                UserType = userType,
                IsForeign = isForeign
            };
        }

        private static TdUser CreateUserLegal(
            string fullName,
            string? shortName,
            string? otherName,
            string? tin,
            string? psrn,
            int userType,
            bool isForeign = false)
        {
            return new TdUser()
            {
                FullName = fullName,
                ShortName = shortName,
                OtherName = otherName,
                Inn = tin,
                Ogrn = psrn,
                UserType = userType,
                IsForeign = isForeign,
            };
        }

        /// <summary>
        /// Парсинг запросов на отдельные сущности
        /// </summary>
        /// <param name="ЗапросСведений">Пакет</param>
        /// <param name="dlrequest">Основной запрос в БД</param>
        /// <param name="hashset">Сет данных в Redis</param>
        /// <param name="HaskKey">Ключ к Redis</param>
        /// <returns>Task</returns>
        private async Task AddTeRequests(ЗапросСведений ЗапросСведений, TeDlrequest dlrequest, HashEntry[] hashset, string HaskKey)
        {
            var JsonTextValue = hashset.FirstOrDefault(x => x.Name == "package_error").Value.ToString();
            List<PackageError>? packageErrors = null;

            if (!string.IsNullOrWhiteSpace(JsonTextValue))
            {
                packageErrors = JsonSerializer.Deserialize<List<PackageError>>(JsonTextValue);
            }

            //ЗапросСведений template = ЗапросСведений;
            List<Запрос> запросы = ЗапросСведений.Запрос;

            //template.Запрос = [];
            TeRequest teRequest;
            List<TdUser> users = [];

            foreach (var запрос in запросы)
            {
                //template.Запрос = [запрос];
                var packageError = packageErrors?.FirstOrDefault(e => e.Id == запрос.ПорядковыйНомер);

                int error = packageError?.error_code ?? 0;
                var user = await GetOrCreateUser(запрос, users);
                teRequest = new TeRequest()
                {
                    Dlrequest = dlrequest,
                    OrderNum = запрос.ПорядковыйНомер,
                    ErrorCode = error,
                    ErrorMessage = packageError?.error_message,
                    User = user?.KeyId == 0 ? user : null,
                    UserId = user?.KeyId == 0 ? null : user?.KeyId,
                    RequestXml = _xmlService.SerializeAsString(запрос)?.Trim()
                };

                await _context.TeRequests.AddAsync(teRequest);
                ЗапросСведений.Запрос = запросы;
                await AddSubject(запрос, teRequest);
            }
        }

        /// <summary>
        /// Добавить субъекта в БД
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task AddSubject(Запрос request, TeRequest teRequest)
        {
            TeSubject subject = new()
            {
                Request = teRequest,
                BirthDay = DateOnly.FromDateTime(request.Субъект!.ДатаРождения),
                Inn = request.Субъект.ИНН?.Value ?? request.Субъект.ИнНомер,
                Snils = request.Субъект.СНИЛС,
                InnChecked = request.Субъект.ИНН?.ПризнакПроверки == ТипИННФЛсПризнакомПризнакПроверки.Item1,
                InnForeign = !string.IsNullOrWhiteSpace(request.Субъект.ИнНомер)
            };

            await _context.TeSubjects.AddAsync(subject);
            await _context.TeSubjectsDocuments.AddRangeAsync(AddSubjectDocs(request, subject));
            await _context.TeSubjectsFullNames.AddRangeAsync(AddSubjectFIO(request, subject));
        }

        /// <summary>
        /// сформировать список документов для записи в бд
        /// </summary>
        /// <param name="request"></param>
        /// <param name="subject"></param>
        /// <returns></returns>
        private static IEnumerable<TeSubjectsDocument> AddSubjectDocs(Запрос request, TeSubject subject) =>
            request.Субъект?.ДокументЛичности?.Select(x => new TeSubjectsDocument()
            {
                DocTypeId = MapDocType(x.КодДУЛ),
                DocDateIssue = DateOnly.FromDateTime(x.ДатаВыдачи),
                DocSeries = x.Серия,
                DocNumber = x.Номер,
                CountryCode = int.TryParse(x.Гражданство, out var result) ? result : null,
                Subject = subject
            }) ?? [];

        /// <summary>
        /// Сформировать ФИО для записи в БД
        /// </summary>
        /// <param name="request"></param>
        /// <param name="subject"></param>
        /// <returns></returns>
        private static IEnumerable<TeSubjectsFullName> AddSubjectFIO(Запрос request, TeSubject subject) =>
            request.Субъект?.ФИО?.Select(x => new TeSubjectsFullName()
            {
                FirstName = x.Имя,
                LastName = x.Фамилия,
                MiddleName = x.Отчество,
                Subject = subject
            }) ?? [];

        private async Task AddTeQBCHTasks(ЗапросСведений ЗапросСведений, TeDlrequest dlrequest, string redisKey)
        {
            HashEntry[]? hashset = null;
            string qbchKey = string.Empty;

            TeQbchTask qbchTask;

            // Если СпособыЗапроса только наше бюро то оставляем только наше бюро
            var bureauList = ЗапросСведений.ТипЗапроса == СправочникСпособыЗапроса.OurBureau ? _bureauList.Where(x => x.Name == OurBureaName) : _bureauList;

            // Для каждого КБКИ создаем в БД свою таску
            foreach (var bureau in bureauList)
            {
                qbchKey = $"{redisKey}:{bureau.ogrn}";
                hashset = await _cacheService.TryGetHashAll(qbchKey);

                if (hashset == null)
                {
                    _logger.LogCritical("Ключ {Key} КБКИ {Name} пустой", qbchKey, bureau.Name);
                    continue;
                }

                qbchTask = new TeQbchTask()
                {
                    QbchCorrespondentId = (await GetAbonentByPSRN(bureau.ogrn)).KeyId,
                    Req = dlrequest,
                    ResponseId = hashset?.FirstOrDefault(x => x.Name == "response_id").Value,
                    TaskStartDateTime = GetDateTimeValue(hashset?.FirstOrDefault(x => x.Name == "task_start_date_time").Value.ToString()),
                    TaskEndDateTime = GetDateTimeValue(hashset?.FirstOrDefault(x => x.Name == "task_end_date_time").Value.ToString()),
                    TaskResultXml = TryParseXmlBytesToString(hashset?.FirstOrDefault(x => x.Name == "task_result_xml").Value)
                };
                await _context.TeQbchTasks.AddAsync(qbchTask);

                // Каждая таска под катом дергает запросы в другие КБКИ(* Функционал АПИ). Все эти запросы логируются в 2 листа redis.
                // Достаем эти листы и суем их в БД.
                await AddTeQBCHDlRequests(qbchTask, qbchKey);
                await AddTeQBCHDlAnswers(qbchTask, qbchKey);

                // Допом, когда мы получаем конечный ответ от других КБКИ(читай от внутреннего нашего сервиса).
                // Мы забираем его и раскладываем в таблицу te_responses потому что надо.
                await AddTeResponses(qbchTask, bureau.ogrn, dlrequest, ЗапросСведений);
            }
        }

        /// <summary>
        /// Добавление всех попыток dlrequest в БД
        /// </summary>
        /// <param name="qbchTask">Задча в рамках которой были отправлены запросы</param>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        private async Task AddTeQBCHDlRequests(TeQbchTask qbchTask, string redisKey)
        {
            var redis = _cacheService.GetDatabase();
            var key = $"{redisKey}:dlrequest";
            var length = await redis.ListLengthAsync(key);

            for (long i = length; i > 0; i--)
            {
                var cache = await redis.ListGetByIndexAsync(key, i-1);

                if (!cache.HasValue)
                    continue;

                using var ms = new MemoryStream(cache!);

                var cachedValue = await JsonSerializer.DeserializeAsync<RedisMessageDTO>(ms);
                await _context.TeQbchDlrequests.AddAsync(new TeQbchDlrequest()
                {
                    ErrorCode = GetIntValue(cachedValue?.ErrorCode),
                    ErrorMessage = cachedValue?.ErrorMessage,
                    QbchTask = qbchTask,
                    RequestData = cachedValue?.RequestData,
                    RequestXml = TryParseXmlBytesToString(cachedValue?.RequestXml),
                    HttpResponseCode = GetIntValue(cachedValue?.HttpResponseCode),
                    ResponseData = cachedValue?.ResponseData,
                    ResponseXml = TryParseXmlBytesToString(cachedValue?.ResponseXml),
                    RequestDateTime = GetDateTimeValue(cachedValue?.RequestDateTime),
                    ResponseDateTime = GetDateTimeValue(cachedValue?.ResponseDateTime)
                });
            }
        }

        private async Task AddTeQBCHDlAnswers(TeQbchTask qbchTask, string redisKey)
        {
            var redis = _cacheService.GetDatabase();
            var key = $"{redisKey}:dlanswer";
            var length = await redis.ListLengthAsync(key);

            for (long i = length; i > 0; i--)
            {
                var cache = await redis.ListGetByIndexAsync(key, i-1);

                if (!cache.HasValue)
                    continue;

                using var ms = new MemoryStream(cache);
                var cachedValue = await JsonSerializer.DeserializeAsync<RedisMessageDTO>(ms);
                await _context.TeQbchDlanswers.AddAsync(new TeQbchDlanswer()
                {
                    ErrorCode = GetIntValue(cachedValue?.ErrorCode),
                    ErrorMessage = cachedValue?.ErrorMessage,
                    QbchTask = qbchTask,
                    HttpResponseCode = GetIntValue(cachedValue?.HttpResponseCode),
                    ResponseData = cachedValue?.ResponseData,
                    ResponseXml = TryParseXmlBytesToString(cachedValue?.ResponseXml),
                    RequestDateTime = GetDateTimeValue(cachedValue?.RequestDateTime),
                    ResponseDateTime = GetDateTimeValue(cachedValue?.ResponseDateTime)
                });
            }
        }

        private async Task AddTeResponses(TeQbchTask qbchTask, string psrn, TeDlrequest taskResult, ЗапросСведений ЗапросСведений)
        {
            if (taskResult is null)
                return;

            var ответ = _xmlService.Deserialize<ОтветНаЗапросСведений>(taskResult.QbchTasksResultXml);

            if (ответ is ОтветНаЗапросСведений ответНаЗапрос)
            {
                foreach (var запрос in ЗапросСведений?.Запрос ?? [])
                {
                    var Сведения = ответНаЗапрос.Сведения.FirstOrDefault(x => x.ПорядковыйНомер == запрос.ПорядковыйНомер);

                    foreach (var кбки in Сведения?.КБКИ.Where(x => x.ОГРН == psrn) ?? [])
                    {
                        var teResponse = new TeResponse()
                        {
                            OrderNum = запрос.ПорядковыйНомер,
                            ResponseXml = _xmlService.SerializeAsString(Сведения)?.Trim(),
                            QbchTask = qbchTask,
                            AmpResponseType = ЗапросСведений?.КодСведений == СправочникВидыСведений.SP6 ? null : MapAmpResponseType(кбки, _bureauList.FirstOrDefault(x => x.Name == OurBureaName)?.ogrn),
                            SpResponseType = MapSpResponseType(кбки),
                            ErrorCode = GetIntValue(кбки.Ошибка?.Код),
                            ErrorMessage = кбки.Ошибка?.Value
                        };

                        await _context.TeResponses.AddAsync(teResponse);
                    }
                }
            }
        }

        /// <summary>
        /// Попытка сериализации
        /// </summary>
        /// <typeparam name="T">Объект в который нужно сериализовать</typeparam>
        /// <param name="value">Значение</param>
        /// <returns>Результат</returns>
        private T? TryDeserialize<T>(string? value) where T : class
        {
            try
            {
                return _xmlService.Deserialize<T>(value);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Ошибка десериализации");
            }

            return default;
        }

        public async Task<bool> CreateDlRequest(string HaskKey, HashEntry[]? hashset)
        {
            // Читаем хэш из редиса
            //HashEntry[]? hashset = await _cacheService.TryGetHashAll(HaskKey);

            // Провекра что хэш считан
            if (hashset is null)
            {
                _logger.LogCritical("не удалось считать данные из Redis");
                return false;
            }

            var ЗапросСведений = TryDeserialize<ЗапросСведений>(hashset.FirstOrDefault(x => x.Name == "request_xml").Value.ToString());
            var requestlist = ЗапросСведений?.Запрос.Select(x => x.ПорядковыйНомер);
            TrAbonent? trAbonent = await GetAbonentByThumbprint(hashset.FirstOrDefault(x => x.Name == "request_certificate_thumbprint").Value.ToString());
            var errorCode = GetIntValue(hashset.FirstOrDefault(x => x.Name == "error_code").Value);

            TeDlrequest dlrequest = new()
            {
                ResponseGuid = hashset.FirstOrDefault(x => x.Name == "response_guid").Value.ToString()!,
                AbonentId = trAbonent?.KeyId,
                IpAddress = hashset.FirstOrDefault(x => x.Name == "ip_address").Value.ToString(),

                RequestCertificateData = hashset.FirstOrDefault(x => x.Name == "request_certificate_data").Value,
                RequestCertificateThumbprint = hashset.FirstOrDefault(x => x.Name == "request_certificate_thumbprint").Value.ToString(),
                RequestDateTime = DateTime.ParseExact(hashset.FirstOrDefault(x => x.Name == "request_date_time").Value!, "dd.MM.yyyy HH:mm:ss:ffff", CultureInfo.InvariantCulture, DateTimeStyles.None),
                RequestSignedData = hashset.FirstOrDefault(x => x.Name == "request_signed_data").Value,
                RequestXml = TryParseXmlBytesToString(hashset.FirstOrDefault(x => x.Name == "request_xml").Value),

                ValidationDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "validation_date_time").Value.ToString()),
                ErrorCode = errorCode,
                ErrorMessage = hashset.FirstOrDefault(x => x.Name == "error_message").Value,

                RequestId = ЗапросСведений?.ИдентификаторЗапроса,
                InformationCode = ЗапросСведений is not null ? (int)ЗапросСведений.КодСведений : null,
                RequestMode = ЗапросСведений is not null ? (int)ЗапросСведений.РежимЗапроса : null,
                RequestType = ЗапросСведений is not null ? (int)ЗапросСведений.ТипЗапроса : null,

                QbchTasksEndDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "qbch_tasks_end_date_time").Value.ToString()),
                QbchTasksResultXml = TryParseXmlBytesToString(hashset.FirstOrDefault(x => x.Name == "qbch_tasks_aggregate_xml").Value),

                ResponseSignedData = hashset.FirstOrDefault(x => x.Name == "response_signed_data").Value,
                ResponseDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "response_date_time").Value.ToString()),
                ResponseXml = TryParseXmlBytesToString(hashset.FirstOrDefault(x => x.Name == "response_xml").Value)
            };

            await _context.TeDlrequests.AddAsync(dlrequest);

            // Дробление запросов в таблицу te_request
            if (ЗапросСведений is not null && (errorCode == 0 || errorCode == 12))
            {
                await AddTeRequests(ЗапросСведений, dlrequest, hashset, HaskKey);
                await AddTeQBCHTasks(ЗапросСведений, dlrequest, HaskKey);
            }

            return await TrySaveChangesAsync(HaskKey);
        }

        private async Task<bool> TrySaveChangesAsync(string HaskKey)
        {
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Ошибка записи в БД. Ключ {key}", HaskKey);
                return false;
            }
        }


        public async Task<bool> CreateDlAnswer(string HaskKey, HashEntry[]? hashset)
        {
            // Провекра что хэш считан
            if (hashset is null)
            {
                _logger.LogCritical("не удалось считать данные из Redis");
                return false;
            }

            TrAbonent? trAbonent = await GetAbonentByThumbprint(hashset.FirstOrDefault(x => x.Name == "request_certificate_thumbprint").Value.ToString());

            TeDlanswer dlanswer = new()
            {
                ResponseGuid = hashset.FirstOrDefault(x => x.Name == "response_guid").Value.ToString()!,
                IpAddress = hashset.FirstOrDefault(x => x.Name == "ip_address").Value.ToString(),
                RequestCertificateThumbprint = hashset.FirstOrDefault(x => x.Name == "request_certificate_thumbprint").Value.ToString(),
                RequestCertificateData = hashset.FirstOrDefault(x => x.Name == "request_certificate_data").Value,
                AbonentId = trAbonent?.KeyId,
                RequestDateTime = DateTime.ParseExact(hashset.FirstOrDefault(x => x.Name == "request_date_time").Value!, "dd.MM.yyyy HH:mm:ss:ffff", CultureInfo.InvariantCulture, DateTimeStyles.None),
                ValidationDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "validation_date_time").Value.ToString()),
                ResponseDateTime = (DateTime)GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "response_date_time").Value.ToString())!,
                ErrorMessage = hashset.FirstOrDefault(x => x.Name == "error_message").Value,
                ErrorCode = GetIntValue(hashset.FirstOrDefault(x => x.Name == "error_code").Value),
                ResponseXml = TryParseXmlBytesToString(hashset.FirstOrDefault(x => x.Name == "response_xml").Value),
                ResponseSignedData = hashset.FirstOrDefault(x => x.Name == "response_signed_data").Value,
                TempGuid = hashset.FirstOrDefault(x => x.Name == "temp_guid").Value!
            };

            await _context.TeDlanswers.AddAsync(dlanswer);
            await _context.SaveChangesAsync();
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Ошибка записи в БД. Ключ {key}", HaskKey);
                return false;
            }
        }

        public Task CreateDlPut(string HaskKey)
        {
            throw new NotImplementedException();
        }

        public Task CreateDlPutAnswer(string HaskKey)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmlText"></param>
        /// <param name="ourBureauOgrn"></param>
        /// <returns></returns>
        private static int? MapAmpResponseType(КБКИ? qbch, string? ourPSRN)
        {
            if (qbch is null)
                return null;

            if (qbch?.СубъектНеНайден != null)
                return 1;
            else if (qbch?.ОбязательствНет != null)
                return 2;
            else if (qbch?.Обязательства != null)
            {
                var ourData = qbch.Обязательства.БКИ?.Any(x => x.ОГРН == ourPSRN) ?? false;
                var notOurData = qbch.Обязательства.БКИ?.Any(x => x.ОГРН != ourPSRN) ?? false;

                // Если есть данные и от нас и от кредо
                if (ourData && notOurData)
                    return 4;
                // Если есть данные от кредо, а наших нет
                else if (!ourData && notOurData)
                    return 5;
                // Если есть данные только от нас
                else if (ourData && !notOurData)
                    return 3;
                else if (!ourData && !notOurData)
                    return 5;
            }
            else if (qbch?.Ошибка is not null)
            {
                return qbch.Ошибка.Код switch
                {
                    "18" => 7,
                    _ => (int?)6,
                };
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private static int? MapSpResponseType(КБКИ? qbch)
        {
            if (qbch is null)
                return null;

            if (qbch?.СубъектНеНайден != null)
                return 1;
            else if (qbch?.СведенияОЗапретеНеПредоставляются != null)
                return 2;
            else if (qbch?.УсловияЗапрета != null)
                return 3;
            else if (qbch?.СведенийОЗапретеНет != null)
                return 4;

            return null;
        }

        /// <summary>
        /// Маппинг кода цели
        /// </summary>
        /// <param name="target">Код цели enum</param>
        /// <returns></returns>
        private static int GetTargetCode(ТипЦельКодЦели target)
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
        /// Маппинг типов документов
        /// </summary>
        /// <param name="item">Код документа enum</param>
        /// <returns>Код строка</returns>
        private static string MapDocType(СправочникДУЛ? item)
        {
            return item switch
            {
                СправочникДУЛ.Item21 => "21",
                СправочникДУЛ.Item221 => "22.1",
                СправочникДУЛ.Item222 => "22.2",
                СправочникДУЛ.Item223 => "22.3",
                СправочникДУЛ.Item23 => "23",
                СправочникДУЛ.Item24 => "24",
                СправочникДУЛ.Item25 => "25",
                СправочникДУЛ.Item26 => "26",
                СправочникДУЛ.Item27 => "27",
                СправочникДУЛ.Item28 => "28",
                СправочникДУЛ.Item31 => "31",
                СправочникДУЛ.Item32 => "32",
                СправочникДУЛ.Item35 => "35",
                СправочникДУЛ.Item37 => "37",
                СправочникДУЛ.Item38 => "38",
                СправочникДУЛ.Item39 => "39",
                СправочникДУЛ.Item999 => "999",
                _ => throw new Exception("Код документа не найден"),
            };
        }
    }
}
