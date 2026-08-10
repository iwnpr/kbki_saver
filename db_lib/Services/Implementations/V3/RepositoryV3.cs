using cache_lib.Interfaces;
using db_lib.Entities;
using db_lib.Models.DTO;
using db_lib.Services.Interfaces.V3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QBCH.Lib.qcb_xml.v3_0;
using QBCH_lib.CommonTypes.Api;
using QBCHService_lib.Models.DTOs;
using StackExchange.Redis;
using System.Globalization;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Xml_service_lib;

namespace db_lib.Services.Implementations.V3;

public class RepositoryV3(QbchContext context, ILogger<RepositoryV3> logger, IXmlService xmlService, ICacheService cacheService, IBKIRequisitsHandler requisits) : IRepositoryV3
{
    private readonly QbchContext _context = context;
    private readonly ILogger<RepositoryV3> _logger = logger;
    private readonly IXmlService _xmlService = xmlService;
    private readonly ICacheService _cacheService = cacheService;
    private readonly List<QBCHRequisite> _bureauList = requisits.GetBureaList();
    private const string OurBureaName = "BKICI";
    private const int ErrorXsdSchemaValidationCode = 9;

    private static DateTime? GetDateTimeValue(string? value, string pattern = "dd.MM.yyyy HH:mm:ss:ffff")
        => DateTime.TryParseExact(value, pattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) ? result : null;

    private string? TryParseXmlBytesToString(byte[]? bytes)
    {
        try
        {
            if(bytes is null)
                return null;

            using var stream = new MemoryStream(bytes);
            using var reader = XmlReader.Create(stream);
            reader.MoveToContent();
            return reader.ReadOuterXml();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка преобразования XML из массива байтов V3");
            return null;
        }
    }

    private T? TryDeserialize<T>(string? value) where T : class
    {
        try
        {
            return _xmlService.Deserialize<T>(value);
        }
        catch(Exception ex)
        {
            _logger.LogCritical(ex, "Ошибка десериализации блока {block} V3", typeof(T).Name);
        }

        return default;
    }

    private async Task<TrAbonent?> GetAbonentByThumbprint(string? thumbprint)
    {
        var upper = thumbprint?.ToUpper();
        if (string.IsNullOrWhiteSpace(upper)) return null;
        return await _context.TrAbonentCertificates.Include(x => x.Abonent)
            .Where(x => x.Thumbprint.ToUpper() == upper)
            .Select(x => x.Abonent)
            .FirstOrDefaultAsync();
    }

    private async Task<TrAbonent?> GetAbonentByPSRN(string? psrn)
    {
        var upper = psrn?.ToUpper();
        if (string.IsNullOrWhiteSpace(upper)) return null;
        return await _context.TrAbonents.FirstOrDefaultAsync(x => x.Ogrn == upper);
    }

    private async Task<bool> SaveAsync(string hashKey)
    {
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Ошибка записи в БД V3. Ключ {key}", hashKey);
            return false;
        }
    }

    private async Task<TdUser?> GetOrCreateUserV3(ЗапросСведенийЗапросИсточник источник, List<TdUser> users)
    {
        switch (источник.Item)
        {
            case ЗапросСведенийЗапросИсточникЮридическоеЛицо юл:
                {
                    var ogrnUpper = юл.ОГРН?.ToUpper();
                    var cached = users.FirstOrDefault(x => x.Ogrn?.ToUpper() == ogrnUpper);
                    if (cached is not null) return cached;

                    var existing = await _context.TdUsers
                        .FirstOrDefaultAsync(x => !string.IsNullOrWhiteSpace(x.Ogrn) && x.Ogrn.ToUpper() == ogrnUpper);

                    if (existing is not null)
                    {
                        users.Add(existing);
                        return existing;
                    }

                    var userTypeCodeId = XmlEnumHelper.GetXmlEnumValue(юл.КодВидаПользователя);
                    var user = new TdUser
                    {
                        FullName = юл.ПолноеНаименование,
                        ShortName = юл.СокращенноеНаименование,
                        OtherName = юл.ИноеНаименование?.ToString(),
                        Inn = юл.ИНН,
                        Ogrn = юл.ОГРН,
                        UserType = 1,
                        IsForeign = false,
                        //UserTypeCodeId = userTypeCodeId
                    };
                    users.Add(user);
                    return user;
                }
            case ТипИП ип:
                {
                    var ogrnUpper = ип.ОГРНИП?.ToUpper();
                    var cached = users.FirstOrDefault(x => x.Ogrn?.ToUpper() == ogrnUpper);
                    if (cached is not null) return cached;

                    var existing = await _context.TdUsers
                        .FirstOrDefaultAsync(x => !string.IsNullOrWhiteSpace(x.Ogrn) && x.Ogrn.ToUpper() == ogrnUpper);

                    if (existing is not null)
                    {
                        users.Add(existing);
                        return existing;
                    }

                    var user = new TdUser
                    {
                        FirstName = ип.ФИО?.Имя,
                        LastName = ип.ФИО?.Фамилия,
                        MiddleName = ип.ФИО?.Отчество,
                        BirthDate = ип.ДатаРождения == default ? null : DateOnly.FromDateTime(ип.ДатаРождения),
                        Inn = ип.ИННИП,
                        Ogrn = ип.ОГРНИП,
                        Snils = ип.СНИЛС,
                        DocSeria = ип.ДокументЛичности?.Серия,
                        DocNumber = ип.ДокументЛичности?.Номер,
                        DocIssueDate = ип.ДокументЛичности?.ДатаВыдачи == null || ип.ДокументЛичности.ДатаВыдачи == default ? null : DateOnly.FromDateTime(ип.ДокументЛичности.ДатаВыдачи),
                        DocIssuerName = ип.ДокументЛичности?.НаименованиеОргана,
                        DocIssuerCode = ип.ДокументЛичности?.КодПодразделения,
                        DocType = ип.ДокументЛичности is not null ? XmlEnumHelper.GetXmlEnumValue(ип.ДокументЛичности.КодДУЛ) : null,
                        DocOtherName = ип.ДокументЛичности?.НаименованиеДУЛ,
                        UserType = 2,
                        IsForeign = false
                    };
                    users.Add(user);
                    return user;
                }
            case ЗапросСведенийЗапросИсточникИностранноеЮЛ иностранноеЮЛ:
                {
                    var name = иностранноеЮЛ.ПолноеНаименование;
                    var cached = users.FirstOrDefault(x => x.FullName == name);
                    if (cached is not null) return cached;

                    var existing = await _context.TdUsers.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.FullName == name);

                    if (existing is not null)
                    {
                        users.Add(existing);
                        return existing;
                    }

                    var userTypeCodeId = XmlEnumHelper.GetXmlEnumValue(иностранноеЮЛ.КодВидаПользователя);
                    var user = new TdUser
                    {
                        FullName = иностранноеЮЛ.ПолноеНаименование,
                        ShortName = иностранноеЮЛ.СокращенноеНаименование,
                        OtherName = иностранноеЮЛ.ИноеНаименование,
                        Inn = иностранноеЮЛ.НомерНП,
                        Ogrn = иностранноеЮЛ.РегНомер,
                        UserType = 1,
                        IsForeign = true,
                        //UserTypeCodeId = userTypeCodeId
                    };
                    users.Add(user);
                    return user;
                }
            case ТипИностранныйПредприниматель иностранныйИП:
                {
                    var name = иностранныйИП.ФИО?.Фамилия + иностранныйИП.ФИО?.Имя
                        + иностранныйИП.ДокументЛичности?.Серия + иностранныйИП.ДокументЛичности?.Номер;
                    var cached = users.FirstOrDefault(x => x.FullName == name);
                    if (cached is not null) return cached;

                    var existing = await _context.TdUsers.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.FullName == name);

                    if (existing is not null)
                    {
                        users.Add(existing);
                        return existing;
                    }

                    var user = new TdUser
                    {
                        FirstName = иностранныйИП.ФИО?.Имя,
                        LastName = иностранныйИП.ФИО?.Фамилия,
                        MiddleName = иностранныйИП.ФИО?.Отчество,
                        BirthDate = иностранныйИП.ДатаРождения == default ? null : DateOnly.FromDateTime(иностранныйИП.ДатаРождения),
                        Inn = иностранныйИП.НомерНП,
                        Ogrn = иностранныйИП.РегНомер,
                        DocSeria = иностранныйИП.ДокументЛичности?.Серия,
                        DocNumber = иностранныйИП.ДокументЛичности?.Номер,
                        DocIssueDate = иностранныйИП.ДокументЛичности?.ДатаВыдачи == null || иностранныйИП.ДокументЛичности.ДатаВыдачи == default ? null : DateOnly.FromDateTime(иностранныйИП.ДокументЛичности.ДатаВыдачи),
                        DocIssuerName = иностранныйИП.ДокументЛичности?.НаименованиеОргана,
                        DocIssuerCode = иностранныйИП.ДокументЛичности?.КодПодразделения,
                        DocType = иностранныйИП.ДокументЛичности is not null ? XmlEnumHelper.GetXmlEnumValue(иностранныйИП.ДокументЛичности.КодДУЛ) : null,
                        DocOtherName = иностранныйИП.ДокументЛичности?.НаименованиеДУЛ,
                        FullName = name,
                        UserType = 5,
                        IsForeign = true
                    };
                    users.Add(user);
                    return user;
                }
            default:
                return null;
        }
    }

    public async Task<bool> CreateDlRequestV3(string hashKey, HashEntry[]? hashset, bool checkAlreadySaved = false)
    {
        if (hashset is null)
        {
            _logger.LogCritical("Не удалось считать данные из Redis: {hashset}", hashset);
            return false;
        }

        var errorCode = int.TryParse(hashset.FirstOrDefault(x => x.Name == "error_code").Value.ToString(), out var parsedError) ? parsedError : 0;
        var requestBytes = hashset.FirstOrDefault(x => x.Name == "request_xml").Value;
        var requestXmlData = TryLoadRequestXml(requestBytes);
        var requestXml = requestXmlData?.Xml;

        ЗапросСведений? request = null;

        if (errorCode != ErrorXsdSchemaValidationCode)
        {
            request = TryDeserialize<ЗапросСведений>(requestXml);
        }

        var trAbonent = await GetAbonentByThumbprint(hashset.FirstOrDefault(x => x.Name == "request_certificate_thumbprint").Value.ToString());

        var dlrequest = new TeDlrequest
        {
            ResponseGuid = hashset.FirstOrDefault(x => x.Name == "response_guid").Value.ToString()!,
            AbonentId = trAbonent?.KeyId,
            IpAddress = hashset.FirstOrDefault(x => x.Name == "ip_address").Value.ToString(),
            RequestCertificateData = hashset.FirstOrDefault(x => x.Name == "request_certificate_data").Value,
            RequestCertificateThumbprint = hashset.FirstOrDefault(x => x.Name == "request_certificate_thumbprint").Value.ToString(),
            RequestDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "request_date_time").Value.ToString()) ?? DateTime.Now,
            RequestSignedData = hashset.FirstOrDefault(x => x.Name == "request_signed_data").Value,
            RequestXml = requestXml,
            ValidationDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "validation_date_time").Value.ToString()),
            ErrorCode = errorCode,
            ErrorMessage = hashset.FirstOrDefault(x => x.Name == "error_message").Value,
            RequestId = requestXmlData?.RequestId,
            InformationCode = requestXmlData?.InformationCode,
            RequestMode = requestXmlData?.RequestMode,
            RequestType = requestXmlData?.RequestType,
            QbchTasksEndDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "qbch_tasks_end_date_time").Value.ToString()),
            QbchTasksResultXml = TryParseXmlBytesToString(hashset.FirstOrDefault(x => x.Name == "qbch_tasks_aggregate_xml").Value),
            ResponseSignedData = hashset.FirstOrDefault(x => x.Name == "response_signed_data").Value,
            ResponseDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "response_date_time").Value.ToString()),
            ResponseXml = TryParseXmlBytesToString(hashset.FirstOrDefault(x => x.Name == "response_xml").Value)
        };

        await _context.TeDlrequests.AddAsync(dlrequest);

        if (request is not null && (errorCode == 0 || errorCode == 12))
        {
            var packageErrorJson = hashset.FirstOrDefault(x => x.Name == "package_error").Value.ToString();
            var packageErrors = string.IsNullOrWhiteSpace(packageErrorJson)
                ? null
                : JsonSerializer.Deserialize<List<PackageError>>(packageErrorJson);

            List<TdUser> users = [];

            foreach (var nested in request.Запрос ?? [])
            {
                var orderNum = int.TryParse(nested.ПорядковыйНомер, out var parsedOrder) ? parsedOrder : 0;
                var packageError = packageErrors?.FirstOrDefault(e => e.Id == orderNum);

                TdUser? user = null;
                if (nested.Источник is not null)
                    user = await GetOrCreateUserV3(nested.Источник, users);

                var teRequest = new TeRequest
                {
                    Dlrequest = dlrequest,
                    OrderNum = orderNum,
                    ErrorCode = packageError?.error_code ?? 0,
                    ErrorMessage = packageError?.error_message,
                    User = user?.KeyId == 0 ? user : null,
                    UserId = user?.KeyId == 0 ? null : user?.KeyId,
                    //ObligationAmount = nested.СуммаОбязательства?.Value,
                    //ObligationAmountCurrency = nested.СуммаОбязательства?.Валюта,
                    RequestXml = _xmlService.SerializeAsString(nested)?.Trim()
                };

                await _context.TeRequests.AddAsync(teRequest);
                await AddSubject(nested, teRequest);
                //await AddConsentPurposes(nested, teRequest);
                //await AddRequestPurposes(nested, teRequest);
            }

            await AddTeQBCHTasksV3(request, dlrequest, hashKey);
        }

        return await SaveAsync(hashKey);
    }

    private RequestXmlData? TryLoadRequestXml(byte[]? bytes)
    {
        try
        {
            if (bytes is null)
                return null;

            using var stream = new MemoryStream(bytes);
            using var reader = XmlReader.Create(stream);
            reader.MoveToContent();

            var requestId = reader.GetAttribute("ИдентификаторЗапроса");
            var informationCode = GetNullableIntValue(reader.GetAttribute("КодСведений"));
            var requestMode = GetNullableIntValue(reader.GetAttribute("РежимЗапроса"));
            var requestType = GetNullableIntValue(reader.GetAttribute("ТипЗапроса"));
            var xml = reader.ReadOuterXml();

            return new RequestXmlData(xml, requestId, informationCode, requestMode, requestType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка разбора XML запроса V3");
            return null;
        }
    }

    private sealed record RequestXmlData(string Xml, string? RequestId, int? InformationCode, int? RequestMode, int? RequestType);

    private static int? GetNullableIntValue(string? value) => int.TryParse(value, out var result) ? result : null;

    public async Task<bool> CreateDlAnswerV3(string hashKey, HashEntry[]? hashset, bool checkAlreadySaved = false)
    {
        if (hashset is null)
        {
            _logger.LogCritical("Не удалось считать данные из Redis: {hashset}", hashset);
            return false;
        }

        var responseXml = TryParseXmlBytesToString(hashset.FirstOrDefault(x => x.Name == "response_xml").Value);
        var trAbonent = await GetAbonentByThumbprint(hashset.FirstOrDefault(x => x.Name == "request_certificate_thumbprint").Value.ToString());
        var errorCode = int.TryParse(hashset.FirstOrDefault(x => x.Name == "error_code").Value.ToString(), out var parsedErrorCode)
            ? parsedErrorCode
            : 0;

        var dlanswer = new TeDlanswer
        {
            ResponseGuid = hashset.FirstOrDefault(x => x.Name == "response_guid").Value.ToString(),
            TempGuid = hashset.FirstOrDefault(x => x.Name == "temp_guid").Value.ToString() ?? string.Empty,
            RequestDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "request_date_time").Value.ToString()) ?? DateTime.Now,
            ValidationDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "validation_date_time").Value.ToString()),
            ResponseDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "response_date_time").Value.ToString()) ?? DateTime.Now,
            RequestCertificateData = hashset.FirstOrDefault(x => x.Name == "request_certificate_data").Value,
            RequestCertificateThumbprint = hashset.FirstOrDefault(x => x.Name == "request_certificate_thumbprint").Value.ToString(),
            IpAddress = hashset.FirstOrDefault(x => x.Name == "ip_address").Value.ToString(),
            ResponseXml = responseXml,
            ResponseSignedData = hashset.FirstOrDefault(x => x.Name == "response_signed_data").Value,
            ErrorCode = errorCode,
            ErrorMessage = hashset.FirstOrDefault(x => x.Name == "error_message").Value,
            AbonentId = trAbonent?.KeyId
        };

        await _context.TeDlanswers.AddAsync(dlanswer);
        return await SaveAsync(hashKey);
    }

    public async Task<bool> CreateDlPutV3(string hashKey, HashEntry[]? hashset, bool checkAlreadySaved = false)
    {
        if (hashset is null)
        {
            _logger.LogCritical("Не удалось считать данные из Redis: {hashset}", hashset);
            return false;
        }
        var requestXml = TryParseXmlBytesToString(hashset.FirstOrDefault(x => x.Name == "request_xml").Value);
        var request = TryDeserialize<ПредставлениеСведений>(requestXml);

        if (request is null)
            _logger.LogError("Не удалось считать данные блока {block}", nameof(ПредставлениеСведений));

        var trAbonent = await GetAbonentByThumbprint(hashset.FirstOrDefault(x => x.Name == "request_certificate_thumbprint").Value.ToString());
        var errorCode = int.TryParse(hashset.FirstOrDefault(x => x.Name == "error_code").Value.ToString(), out var parsedErrorCode)
            ? parsedErrorCode
            : 0;

        var addCommandsCount = 0;
        var deleteCommandsCount = 0;

        foreach (var info in request?.Сведения ?? [])
        {
            switch (info.Item)
            {
                case ПредставлениеСведенийСведенияДоговор { Item: ТипДоговор }:
                case ПредставлениеСведенийСведенияОбращениеОбязательство { Item: ТипОбращениеОбязательство }:
                    addCommandsCount++;
                    break;

                case ПредставлениеСведенийСведенияДоговор { Item: ПредставлениеСведенийСведенияДоговорУдалить }:
                case ПредставлениеСведенийСведенияОбращениеОбязательство { Item: ПредставлениеСведенийСведенияОбращениеОбязательствоУдалить }:
                    deleteCommandsCount++;
                    break;
            }
        }

        var dlput = new TeDlput
        {
            Guid = hashset.FirstOrDefault(x => x.Name == "response_guid").Value.ToString(),
            AbonentId = trAbonent?.KeyId,
            IpAddress = hashset.FirstOrDefault(x => x.Name == "ip_address").Value.ToString(),
            RequestCertificateThumbprint = hashset.FirstOrDefault(x => x.Name == "request_certificate_thumbprint").Value.ToString(),
            RequestDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "request_date_time").Value.ToString()) ?? DateTime.Now,
            ValidationDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "validation_date_time").Value.ToString()),
            ResponseDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "response_date_time").Value.ToString()),
            RequestId = hashset.FirstOrDefault(x => x.Name == "request_id").Value.ToString(),
            RequestXml = requestXml,
            RequestSignedData = hashset.FirstOrDefault(x => x.Name == "request_signed_data").Value,
            ResponseXml = TryParseXmlBytesToString(hashset.FirstOrDefault(x => x.Name == "response_xml").Value),
            ResponseSignedData = hashset.FirstOrDefault(x => x.Name == "response_signed_data").Value,
            ErrorCode = errorCode,
            ErrorMessage = hashset.FirstOrDefault(x => x.Name == "error_message").Value,
            AddCommandsCount = addCommandsCount,
            DeleteCommandsCount = deleteCommandsCount
        };

        await _context.TeDlputs.AddAsync(dlput);
        return await SaveAsync(hashKey);
    }

    public async Task<bool> CreateDlPutAnswerV3(string hashKey, HashEntry[]? hashset, bool checkAlreadySaved = false)
    {
        if (hashset is null)
        {
            _logger.LogCritical("Не удалось считать данные из Redis: {hashset}", hashset);
            return false;
        }
        var responseXml = TryParseXmlBytesToString(hashset.FirstOrDefault(x => x.Name == "response_xml").Value);
        var trAbonent = await GetAbonentByThumbprint(hashset.FirstOrDefault(x => x.Name == "request_certificate_thumbprint").Value.ToString());
        var errorCode = int.TryParse(hashset.FirstOrDefault(x => x.Name == "error_code").Value.ToString(), out var parsedErrorCode)
            ? parsedErrorCode
            : 0;

        var dlPutAnswer = new TeDlputanswer
        {
            TempGuid = hashset.FirstOrDefault(x => x.Name == "temp_guid").Value.ToString() ?? string.Empty,
            Guid = hashset.FirstOrDefault(x => x.Name == "response_guid").Value.ToString(),
            RequestDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "request_date_time").Value.ToString()) ?? DateTime.Now,
            ValidationDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "validation_date_time").Value.ToString()),
            ResponseDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "response_date_time").Value.ToString()) ?? DateTime.Now,
            RequestCertificateThumbprint = hashset.FirstOrDefault(x => x.Name == "request_certificate_thumbprint").Value.ToString(),
            IpAddress = hashset.FirstOrDefault(x => x.Name == "ip_address").Value.ToString(),
            ResponseXml = responseXml,
            ResponseSignedData = hashset.FirstOrDefault(x => x.Name == "response_signed_data").Value,
            ErrorCode = errorCode,
            ErrorMessage = hashset.FirstOrDefault(x => x.Name == "error_message").Value,
            AbonentId = trAbonent?.KeyId
        };

        await _context.TeDlputanswers.AddAsync(dlPutAnswer);
        return await SaveAsync(hashKey);
    }

    private async Task AddSubject(Запрос request, TeRequest teRequest)
    {
        if (request.Субъект is null)
            return;

        var normalizedSnils = string.IsNullOrWhiteSpace(request.Субъект.СНИЛС)
            ? null
            : new string(request.Субъект.СНИЛС.Where(char.IsDigit).ToArray());

        var subject = new TeSubject
        {
            Request = teRequest,
            BirthDay = request.Субъект.ДатаРождения == default ? null : DateOnly.FromDateTime(request.Субъект.ДатаРождения),
            Inn = request.Субъект.ИНН?.Value ?? request.Субъект.ИнНомер,
            Snils = normalizedSnils?.Length == 11 ? normalizedSnils : request.Субъект.СНИЛС,
            InnChecked = request.Субъект.ИНН?.ПризнакПроверки == ТипИННФЛсПризнакомПризнакПроверки.Item1,
            InnForeign = !string.IsNullOrWhiteSpace(request.Субъект.ИнНомер)
        };

        await _context.TeSubjects.AddAsync(subject);

        if (request.Субъект.ДокументЛичности is not null)
        {
            var docs = request.Субъект.ДокументЛичности.Select(x => new TeSubjectsDocument
            {
                DocTypeId = XmlEnumHelper.GetXmlEnumValue(x.КодДУЛ),
                DocDateIssue = DateOnly.FromDateTime(x.ДатаВыдачи),
                DocSeries = x.Серия,
                DocNumber = x.Номер,
                CountryCode = int.TryParse(x.Гражданство, out var countryCode) ? countryCode : null,
                Subject = subject
            });
            await _context.TeSubjectsDocuments.AddRangeAsync(docs);
        }

        if (request.Субъект.ФИО is not null)
        {
            var fio = request.Субъект.ФИО.Select(x => new TeSubjectsFullName
            {
                FirstName = x.Имя,
                LastName = x.Фамилия,
                MiddleName = x.Отчество,
                Subject = subject
            });
            await _context.TeSubjectsFullNames.AddRangeAsync(fio);
        }
    }

    private async Task AddTeQBCHTasksV3(ЗапросСведений request, TeDlrequest dlrequest, string redisKey)
    {
        var bureauList = request.ТипЗапроса == СправочникСпособыЗапроса.Item1
            ? _bureauList.Where(x => x.Name == OurBureaName)
            : _bureauList;

        foreach (var bureau in bureauList)
        {
            var qbchKey = $"{redisKey}:{bureau.ogrn}";
            var hashset = await _cacheService.TryGetHashAll(qbchKey);

            if (hashset == null)
            {
                _logger.LogCritical("Ключ {Key} КБКИ {Name} пустой", qbchKey, bureau.Name);
                continue;
            }

            var abonent = await GetAbonentByPSRN(bureau.ogrn);
            if (abonent is null)
            {
                _logger.LogCritical("Абонент по ОГРН {Ogrn} не найден", bureau.ogrn);
                continue;
            }

            var qbchTask = new TeQbchTask
            {
                QbchCorrespondentId = abonent.KeyId,
                Req = dlrequest,
                ResponseId = hashset.FirstOrDefault(x => x.Name == "response_id").Value,
                TaskStartDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "task_start_date_time").Value.ToString()),
                TaskEndDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "task_end_date_time").Value.ToString()),
                TaskResultXml = TryParseXmlBytesToString(hashset.FirstOrDefault(x => x.Name == "task_result_xml").Value)
            };

            await _context.TeQbchTasks.AddAsync(qbchTask);
            await AddTeQBCHDlRequestsV3(qbchTask, qbchKey);
            await AddTeQBCHDlAnswersV3(qbchTask, qbchKey);
            await AddTeResponsesV3(qbchTask, bureau.ogrn, dlrequest, request);
        }
    }

    private async Task AddTeQBCHDlRequestsV3(TeQbchTask qbchTask, string redisKey)
    {
        var redis = _cacheService.GetDatabase();
        var key = $"{redisKey}:dlrequest";
        var length = await redis.ListLengthAsync(key);

        for (long i = length; i > 0; i--)
        {
            var cache = await redis.ListGetByIndexAsync(key, i - 1);
            if (!cache.HasValue) continue;

            using var ms = new MemoryStream(cache!);
            var cachedValue = await JsonSerializer.DeserializeAsync<RedisMessageDTO>(ms);
            await _context.TeQbchDlrequests.AddAsync(new TeQbchDlrequest
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

    private async Task AddTeQBCHDlAnswersV3(TeQbchTask qbchTask, string redisKey)
    {
        var redis = _cacheService.GetDatabase();
        var key = $"{redisKey}:dlanswer";
        var length = await redis.ListLengthAsync(key);

        for (long i = length; i > 0; i--)
        {
            var cache = await redis.ListGetByIndexAsync(key, i - 1);
            if (!cache.HasValue) continue;

            using var ms = new MemoryStream(cache);
            var cachedValue = await JsonSerializer.DeserializeAsync<RedisMessageDTO>(ms);
            await _context.TeQbchDlanswers.AddAsync(new TeQbchDlanswer
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

    private async Task AddTeResponsesV3(TeQbchTask qbchTask, string psrn, TeDlrequest dlrequest, ЗапросСведений запрос)
    {
        if (dlrequest.QbchTasksResultXml is null) return;

        var ответ = TryDeserialize<ОтветНаЗапросСведений>(dlrequest.QbchTasksResultXml);
        if (ответ is null) return;

        var ourBureauOgrn = _bureauList.FirstOrDefault(x => x.Name == OurBureaName)?.ogrn;

        foreach (var item in запрос.Запрос ?? [])
        {
            var orderNum = int.TryParse(item.ПорядковыйНомер, out var o) ? o : 0;
            var сведения = ответ.Сведения?.FirstOrDefault(x => x.ПорядковыйНомер == item.ПорядковыйНомер);

            foreach (var кбки in сведения?.КБКИ?.Where(x => x.ОГРН == psrn) ?? [])
            {
                var teResponse = new TeResponse
                {
                    OrderNum = orderNum,
                    ResponseXml = _xmlService.SerializeAsString(сведения)?.Trim(),
                    QbchTask = qbchTask,
                    AmpResponseType = запрос.КодСведений == СправочникВидыСведений.Item6
                        ? null
                        : MapAmpResponseTypeV3(кбки, ourBureauOgrn),
                    SpResponseType = MapSpResponseTypeV3(кбки),
                    ErrorCode = GetErrorCodeV3(кбки),
                    ErrorMessage = GetErrorMessageV3(кбки)
                };

                await _context.TeResponses.AddAsync(teResponse);
            }
        }
    }

    private static int? MapAmpResponseTypeV3(ОтветНаЗапросСведенийСведенияКБКИ кбки, string? ourPSRN)
    {
        if (кбки.ItemsElementName is null) return null;

        if (кбки.ItemsElementName.Contains(ItemsChoiceType.СубъектНеНайден)) return 1;

        if (кбки.ItemsElementName.Contains(ItemsChoiceType.ОбязательствНет)) return 2;

        if (кбки.ItemsElementName.Contains(ItemsChoiceType.Обязательства))
        {
            var обязательства = кбки.Items?.OfType<ОтветНаЗапросСведенийСведенияКБКИОбязательства>().FirstOrDefault();
            var ourData = обязательства?.БКИ?.Any(x => x.ОГРН == ourPSRN) ?? false;
            var notOurData = обязательства?.БКИ?.Any(x => x.ОГРН != ourPSRN) ?? false;

            if (ourData && notOurData) return 4;
            if (!ourData && notOurData) return 5;
            if (ourData) return 3;
            return 5;
        }

        if (кбки.ItemsElementName.Contains(ItemsChoiceType.Ошибка))
        {
            var ошибка = кбки.Items?.OfType<ТипОшибка>().FirstOrDefault();
            return ошибка?.Код == "18" ? 7 : 6;
        }

        return null;
    }

    private static int? MapSpResponseTypeV3(ОтветНаЗапросСведенийСведенияКБКИ кбки)
    {
        if (кбки.ItemsElementName is null) return null;

        if (кбки.ItemsElementName.Contains(ItemsChoiceType.СубъектНеНайден)) return 1;
        if (кбки.ItemsElementName.Contains(ItemsChoiceType.СведенияОЗапретеНеПредоставляются)) return 2;
        if (кбки.ItemsElementName.Contains(ItemsChoiceType.УсловияЗапрета)) return 3;
        if (кбки.ItemsElementName.Contains(ItemsChoiceType.СведенийОЗапретеНет)) return 4;

        return null;
    }

    private static int GetErrorCodeV3(ОтветНаЗапросСведенийСведенияКБКИ кбки)
    {
        var ошибка = кбки.Items?.OfType<ТипОшибка>().FirstOrDefault();
        return int.TryParse(ошибка?.Код, out var code) ? code : 0;
    }

    private static string? GetErrorMessageV3(ОтветНаЗапросСведенийСведенияКБКИ кбки)
        => кбки.Items?.OfType<ТипОшибка>().FirstOrDefault()?.Value;

    private static int GetIntValue(string? value)
        => int.TryParse(value, out var result) ? result : 0;

    //TODO: до лучших времен. Для новой БД
    //private async Task AddConsentPurposes(ЗапросСведенийЗапрос request, TeRequest teRequest)
    //{
    //    if (request.Согласие?.Цель is null) return;

    //    var purposes = request.Согласие.Цель.Select(x => new TeConsentPurpose
    //    {
    //        Request = teRequest,
    //        PurposeId = int.TryParse(XmlEnumHelper.GetXmlEnumValue(x.КодЦели), out var consentPurposeId) ? consentPurposeId : null
    //    });

    //    await _context.TeConsentPurposes.AddRangeAsync(purposes);
    //}

    //private async Task AddRequestPurposes(ЗапросСведенийЗапрос request, TeRequest teRequest)
    //{
    //    if (request.Цель is null) return;

    //    var purposes = request.Цель.Select(x => new TeRequestPurpose
    //    {
    //        Request = teRequest,
    //        PurposeId = int.TryParse(XmlEnumHelper.GetXmlEnumValue(x.КодЦели), out var requestPurposeId) ? requestPurposeId : null
    //    });

    //    await _context.TeRequestPurposes.AddRangeAsync(purposes);
    //}
}
