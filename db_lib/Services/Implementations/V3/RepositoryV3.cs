using db_lib.Entities;
using db_lib.Models.DTO;
using db_lib.Services.Interfaces.V3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QBCH.Lib.qcb_xml.v3_0;
using StackExchange.Redis;
using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using Xml_service_lib;

namespace db_lib.Services.Implementations.V3;

public class RepositoryV3(qbchContext context, ILogger<RepositoryV3> logger, IXmlService xmlService) : IRepositoryV3
{
    private readonly qbchContext _context = context;
    private readonly ILogger<RepositoryV3> _logger = logger;
    private readonly IXmlService _xmlService = xmlService;

    private static DateTime? GetDateTimeValue(string? value, string pattern = "dd.MM.yyyy HH:mm:ss:ffff")
        => DateTime.TryParseExact(value, pattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) ? result : null;

    private static string? TryParseXmlBytesToString(byte[]? bytes)
    {
        try
        {
            return bytes is not null
                ? XDocument.Load(new MemoryStream(bytes)).ToString()
                : null; }
        catch
        {
            return null;
        }
    }

    private T? TryDeserialize<T>(string? value) where T : class
    {
        try { return _xmlService.Deserialize<T>(value); }
        catch (Exception ex) { _logger.LogDebug(ex, "V3 deserialize error"); }
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

    private async Task<bool> SaveAsync(string hashKey)
    {
        try { await _context.SaveChangesAsync(); return true; }
        catch (Exception ex) { _logger.LogCritical(ex, "Ошибка записи V3 в БД. Ключ {key}", hashKey); return false; }
    }

    public async Task<bool> CreateDlRequestV3(string hashKey, HashEntry[]? hashset)
    {
        if (hashset is null)
        {
            _logger.LogCritical("не удалось считать данные из Redis");
            return false;
        }

        var requestXml = hashset.FirstOrDefault(x => x.Name == "request_xml").Value.ToString();
        var request = TryDeserialize<ЗапросСведений>(requestXml);
        var errorCode = int.TryParse(hashset.FirstOrDefault(x => x.Name == "error_code").Value.ToString(), out var parsedError) ? parsedError : 0;
        var trAbonent = await GetAbonentByThumbprint(hashset.FirstOrDefault(x => x.Name == "request_certificate_thumbprint").Value.ToString());

        var dlrequest = new TeDlrequest
        {
            ResponseGuid = hashset.FirstOrDefault(x => x.Name == "response_guid").Value.ToString()!,
            AbonentId = trAbonent?.KeyId,
            IpAddress = hashset.FirstOrDefault(x => x.Name == "ip_address").Value.ToString(),
            RequestCertificateData = hashset.FirstOrDefault(x => x.Name == "request_certificate_data").Value,
            RequestCertificateThumbprint = hashset.FirstOrDefault(x => x.Name == "request_certificate_thumbprint").Value.ToString(),
            RequestDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "request_date_time").Value.ToString()) ?? DateTime.UtcNow,
            RequestSignedData = hashset.FirstOrDefault(x => x.Name == "request_signed_data").Value,
            RequestXml = TryParseXmlBytesToString(hashset.FirstOrDefault(x => x.Name == "request_xml").Value),
            ValidationDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "validation_date_time").Value.ToString()),
            ErrorCode = errorCode,
            ErrorMessage = hashset.FirstOrDefault(x => x.Name == "error_message").Value,
            RequestId = request?.ИдентификаторЗапроса,
            InformationCode = request is not null ? (int)request.КодСведений : null,
            RequestMode = request is not null ? (int)request.РежимЗапроса : null,
            RequestType = request is not null ? (int)request.ТипЗапроса : null,
            QbchTasksEndDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "qbch_tasks_end_date_time").Value.ToString()),
            QbchTasksResultXml = TryParseXmlBytesToString(hashset.FirstOrDefault(x => x.Name == "qbch_tasks_aggregate_xml").Value),
            ResponseSignedData = hashset.FirstOrDefault(x => x.Name == "response_signed_data").Value,
            ResponseDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "response_date_time").Value.ToString()),
            ResponseXml = TryParseXmlBytesToString(hashset.FirstOrDefault(x => x.Name == "response_xml").Value)
        };

        await _context.TeDlrequests.AddAsync(dlrequest);

        if (request?.Запрос is not null)
        {
            var packageErrorJson = hashset.FirstOrDefault(x => x.Name == "package_error").Value.ToString();
            var packageErrors = string.IsNullOrWhiteSpace(packageErrorJson)
                ? null
                : JsonSerializer.Deserialize<List<PackageError>>(packageErrorJson);

            foreach (var nested in request.Запрос)
            {
                var orderNum = int.TryParse(nested.ПорядковыйНомер, out var parsedOrder) ? parsedOrder : 0;
                var packageError = packageErrors?.FirstOrDefault(e => e.Id == orderNum);

                var teRequest = new TeRequest
                {
                    Dlrequest = dlrequest,
                    OrderNum = orderNum,
                    ErrorCode = packageError?.error_code ?? 0,
                    ErrorMessage = packageError?.error_message,
                    RequestXml = _xmlService.SerializeAsString(nested)?.Trim()
                };

                await _context.TeRequests.AddAsync(teRequest);
                await AddSubject(nested, teRequest);
            }
        }

        return await SaveAsync(hashKey);
    }

    private async Task AddSubject(ЗапросСведенийЗапрос request, TeRequest teRequest)
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

    public async Task<bool> CreateDlAnswerV3(string hashKey, HashEntry[]? hashset)
    {
        if (hashset is null)
        {
            _logger.LogCritical("не удалось считать данные из Redis");
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
            RequestDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "request_date_time").Value.ToString()) ?? DateTime.UtcNow,
            ValidationDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "validation_date_time").Value.ToString()),
            ResponseDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "response_date_time").Value.ToString()) ?? DateTime.UtcNow,
            RequestCertificateThumbprint = hashset.FirstOrDefault(x => x.Name == "request_certificate_thumbprint").Value.ToString(),
            RequestCertificateData = hashset.FirstOrDefault(x => x.Name == "request_certificate_data").Value,
            IpAddress = hashset.FirstOrDefault(x => x.Name == "ip_address").Value.ToString(),
            ResponseXml = responseXml,
            ResponseSignedData = hashset.FirstOrDefault(x => x.Name == "response_signed_data").Value,
            ErrorCode = errorCode,
            ErrorMessage = hashset.FirstOrDefault(x => x.Name == "error_message").Value,
            AbonentId = trAbonent?.KeyId
        };

        await _context.TeDlanswers.AddAsync(dlanswer);

        if (!string.IsNullOrWhiteSpace(responseXml))
        {
            var isReceipt = TryDeserialize<Результат>(responseXml) is not null;
            if (!isReceipt)
            {
                var answer = TryDeserialize<ОтветНаЗапросСведений>(responseXml);
                if (answer is not null)
                {
                    foreach (var info in answer.Сведения ?? [])
                    {
                        // Явно проходим все блоки версии 3.0: титульная часть, КБКИ, обязательства,
                        // сведения о запрете и антифрод-сведения. На этом шаге проверяем корректную
                        // десериализацию и отделяем их от служебных квитанций.
                        _ = info.ТитульнаяЧасть;
                        foreach (var qbki in info.КБКИ ?? [])
                        {
                            _ = qbki.ПризнакНаличияКИSpecified;

                            for (var i = 0; i < (qbki.Items?.Length ?? 0); i++)
                            {
                                var item = qbki.Items![i];
                                var itemType = qbki.ItemsElementName?[i];

                                if (item is ОтветНаЗапросСведенийСведенияКБКИОбязательства obligations)
                                {
                                    _ = obligations.БКИ;
                                }

                                if (item is ОтветНаЗапросСведенийСведенияКБКИУсловияЗапрета denyInfo)
                                {
                                    _ = denyInfo.Условие;
                                }

                                if (item is ОтветНаЗапросСведенийСведенияКБКИСведенияДляПредупреждения antiFraud)
                                {
                                    foreach (var antiFraudBki in antiFraud.БКИ ?? [])
                                    {
                                        foreach (var claim in antiFraudBki.ОбращениеОбязательство ?? [])
                                        {
                                            _ = claim.КодИсточника;
                                            _ = claim.СтадияРассмотрения;
                                            _ = claim.ДатаСтадии;
                                            _ = claim.СуммаЗайма;
                                            _ = claim.ПричинаОтказа;
                                        }
                                    }
                                }

                                _ = itemType;
                            }
                        }
                    }
                }
            }
        }

        return await SaveAsync(hashKey);
    }

    public async Task<bool> CreateDlPutV3(string hashKey, HashEntry[]? hashset)
    {
        if (hashset is null)
        {
            _logger.LogCritical("не удалось считать данные из Redis");
            return false;
        }

        var requestXml = TryParseXmlBytesToString(hashset.FirstOrDefault(x => x.Name == "request_xml").Value);
        var request = TryDeserialize<ПредставлениеСведений>(requestXml);
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
                case ПредставлениеСведенийСведенияДоговор contract:
                    {
                        _ = contract.Субъект;
                        switch (contract.Item)
                        {
                            case ТипДоговор addContract:
                                addCommandsCount++;
                                _ = addContract.УИД;
                                _ = addContract.Представлено;
                                _ = addContract.СреднемесячныйПлатеж?.ЗначениеЦелое;
                                _ = addContract.СреднемесячныйПлатеж?.Валюта;
                                _ = addContract.СреднемесячныйПлатеж?.ДатаРасчета;
                                _ = addContract.ПСК;
                                _ = addContract.ДатаПрекращенияSpecified ? addContract.ДатаПрекращения : (DateTime?)null;
                                break;
                            case ПредставлениеСведенийСведенияДоговорУдалить deleteContract:
                                deleteCommandsCount++;
                                _ = deleteContract.УИД;
                                _ = deleteContract.ДатаРасчетаSpecified ? deleteContract.ДатаРасчета : (DateTime?)null;
                                _ = deleteContract.Причина;
                                break;
                        }
                        break;
                    }
                case ПредставлениеСведенийСведенияОбращениеОбязательство claim:
                    {
                        _ = claim.Субъект;
                        switch (claim.Item)
                        {
                            case ТипОбращениеОбязательство addClaim:
                                addCommandsCount++;
                                _ = addClaim.УИД;
                                _ = addClaim.КодИсточника;
                                _ = addClaim.СтадияРассмотрения;
                                _ = addClaim.ДатаСтадии;
                                _ = addClaim.СуммаЗайма?.ЗначениеДесятичное;
                                _ = addClaim.СуммаЗайма?.Валюта;
                                _ = addClaim.ПричинаОтказа;
                                break;
                            case ПредставлениеСведенийСведенияОбращениеОбязательствоУдалить deleteClaim:
                                deleteCommandsCount++;
                                _ = deleteClaim.УИД;
                                _ = deleteClaim.СтадияРассмотренияSpecified ? deleteClaim.СтадияРассмотрения : (ushort?)null;
                                _ = deleteClaim.Причина;
                                break;
                        }
                        break;
                    }
            }
        }

        var dlput = new TeDlput
        {
            Guid = hashset.FirstOrDefault(x => x.Name == "response_guid").Value.ToString(),
            AbonentId = trAbonent?.KeyId,
            IpAddress = hashset.FirstOrDefault(x => x.Name == "ip_address").Value.ToString(),
            RequestCertificateThumbprint = hashset.FirstOrDefault(x => x.Name == "request_certificate_thumbprint").Value.ToString(),
            RequestDateTime = GetDateTimeValue(hashset.FirstOrDefault(x => x.Name == "request_date_time").Value.ToString()) ?? DateTime.UtcNow,
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

    public Task<bool> CreateDlPutAnswerV3(string hashKey, HashEntry[]? hashset)
    {
        throw new NotImplementedException();
    }
}

