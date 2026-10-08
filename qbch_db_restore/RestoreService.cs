using db_lib.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Xml.Linq;

namespace qbch_db_restore;

internal enum RestoreStatus
{
    /// <summary>te_dlrequests по response_guid не найден</summary>
    NotFound,

    /// <summary>по response_guid найдено больше одной строки — нужен ручной разбор</summary>
    Ambiguous,

    /// <summary>request_xml пуст или не разбирается — восстанавливать нечем</summary>
    NoRequestXml,

    /// <summary>всё уже на месте, изменений нет</summary>
    NothingToDo,

    /// <summary>изменения подготовлены и откатаны (режим без --apply)</summary>
    Planned,

    /// <summary>изменения записаны</summary>
    Restored,

    /// <summary>ошибка, транзакция откатана</summary>
    Failed
}

internal sealed record RestoreResult(
    string ResponseGuid,
    RestoreStatus Status,
    long? DlrequestKeyId,
    bool HeaderUpdated,
    IReadOnlyDictionary<string, int> AddedRows,
    IReadOnlyList<string> Warnings,
    string? Message)
{
    public int Added(string table) => AddedRows.TryGetValue(table, out var count) ? count : 0;
}

/// <summary>
/// Данные, которых нет в справочниках БД. Валится весь response_guid целиком,
/// чтобы восстановление не теряло часть данных молча.
/// </summary>
internal sealed class RestoreValidationException(string message) : Exception(message);

/// <summary>
/// Справочники, на которые ссылаются восстанавливаемые таблицы.
/// Читаются один раз за прогон, чтобы падать с понятным текстом, а не с ошибкой FK из PostgreSQL.
/// </summary>
internal sealed record Dictionaries(
    HashSet<int> InformationCodes,
    HashSet<int> RequestModes,
    HashSet<int> RequestTypes,
    HashSet<int> ErrorCodes,
    HashSet<int> AmpResponseTypes,
    HashSet<int> SpResponseTypes,
    HashSet<string> DocumentTypes)
{
    public static async Task<Dictionaries> LoadAsync(QbchContext context, CancellationToken ct) => new(
        (await context.TrInformationCodes.Select(x => x.KeyId).ToListAsync(ct)).ToHashSet(),
        (await context.TrRequestModes.Select(x => x.KeyId).ToListAsync(ct)).ToHashSet(),
        (await context.TrDlrequestTypes.Select(x => x.KeyId).ToListAsync(ct)).ToHashSet(),
        (await context.TrErrorCodes.Select(x => x.KeyId).ToListAsync(ct)).ToHashSet(),
        (await context.TrAmpResponseTypes.Select(x => x.KeyId).ToListAsync(ct)).ToHashSet(),
        (await context.TrSpResponseTypes.Select(x => x.KeyId).ToListAsync(ct)).ToHashSet(),
        (await context.TrDocumentTypes.Select(x => x.KeyId).ToListAsync(ct)).ToHashSet(StringComparer.Ordinal));
}

/// <summary>
/// Медианные смещения таймингов одной задачи относительно меток te_dlrequests, в секундах.
/// </summary>
internal sealed record TimingProfile(
    double? StartAfterValidation,
    double? StartAfterRequest,
    double Duration,
    int Samples,
    /// <summary>Доля здоровых задач этого КБКИ, у которых заполнен response_id.</summary>
    double ResponseIdShare);

/// <summary>
/// Калибровка таймингов по здоровым записям этой же БД: для каждого КБКИ считаются медианы
/// «старт после валидации», «старт после прихода запроса» и «длительность задачи».
///
/// task_start_date_time и task_end_date_time хранились только в Redis, поэтому их значения
/// вычисляются, а не восстанавливаются. Опора — фактические метки самой строки te_dlrequests
/// (validation_date_time, request_date_time, qbch_tasks_end_date_time), медианы задают лишь
/// разброс внутри этого интервала.
/// </summary>
internal sealed class TimingCalibration
{
    private readonly Dictionary<int, TimingProfile> _byCorrespondent;

    private TimingCalibration(Dictionary<int, TimingProfile> byCorrespondent, TimingProfile? overall)
    {
        _byCorrespondent = byCorrespondent;
        Overall = overall;
    }

    public TimingProfile? Overall { get; }

    public IReadOnlyDictionary<int, TimingProfile> ByCorrespondent => _byCorrespondent;

    public TimingProfile? For(int correspondentId)
        => _byCorrespondent.TryGetValue(correspondentId, out var profile) ? profile : Overall;

    public static async Task<TimingCalibration> LoadAsync(QbchContext context, int sampleSize, CancellationToken ct)
    {
        var rows = await context.TeQbchTasks
            .AsNoTracking()
            .Where(x => x.TaskStartDateTime != null && x.TaskEndDateTime != null)
            .OrderByDescending(x => x.KeyId)
            .Take(sampleSize)
            .Select(x => new Sample(
                x.QbchCorrespondentId,
                x.TaskStartDateTime!.Value,
                x.TaskEndDateTime!.Value,
                x.Req.ValidationDateTime,
                x.Req.RequestDateTime,
                x.ResponseId != null))
            .ToListAsync(ct);

        var byCorrespondent = rows
            .GroupBy(x => x.CorrespondentId)
            .ToDictionary(x => x.Key, x => Build(x.ToList())!)
            .Where(x => x.Value is not null)
            .ToDictionary(x => x.Key, x => x.Value);

        return new TimingCalibration(byCorrespondent, Build(rows));
    }

    private static TimingProfile? Build(List<Sample> samples)
    {
        if (samples.Count == 0)
            return null;

        var duration = Median(samples.Select(x => (x.End - x.Start).TotalSeconds));

        return duration is null
            ? null
            : new TimingProfile(
                Median(samples.Where(x => x.Validation is not null).Select(x => (x.Start - x.Validation!.Value).TotalSeconds)),
                Median(samples.Select(x => (x.Start - x.Request).TotalSeconds)),
                duration.Value,
                samples.Count,
                (double)samples.Count(x => x.HasResponseId) / samples.Count);
    }

    private static double? Median(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(x => x).ToList();

        if (sorted.Count == 0)
            return null;

        return sorted.Count % 2 == 1
            ? sorted[sorted.Count / 2]
            : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2;
    }

    private sealed record Sample(int CorrespondentId, DateTime Start, DateTime End, DateTime? Validation, DateTime Request, bool HasResponseId);
}

/// <summary>
/// Восстановление потомков te_dlrequests для случая, когда ЗапросСведений не десериализовался
/// и блок сохранения дочерних сущностей в RepositoryV3.CreateDlRequestV3 был пропущен целиком.
///
/// Источники данных — только колонки БД: te_dlrequests.request_xml и te_dlrequests.qbch_tasks_result_xml.
/// Не восстанавливаются (хранились только в Redis и уже истекли):
/// te_requests.error_code/error_message (package_error), te_qbch_tasks.task_start_date_time/
/// task_end_date_time/task_result_xml, te_qbch_dlrequests и te_qbch_dlanswers целиком.
/// </summary>
internal sealed class RestoreService(
    Func<QbchContext> contextFactory,
    Dictionaries dictionaries,
    TimingCalibration calibration,
    ExchangeCalibration exchange,
    string? ourBureauOgrn,
    bool apply)
{
    private static readonly Dictionary<string, string> TableByEntity = new()
    {
        [nameof(TdUser)] = "td_users",
        [nameof(TeRequest)] = "te_requests",
        [nameof(TeSubject)] = "te_subjects",
        [nameof(TeSubjectsDocument)] = "te_subjects_documents",
        [nameof(TeSubjectsFullName)] = "te_subjects_full_name",
        [nameof(TeQbchTask)] = "te_qbch_tasks",
        [nameof(TeResponse)] = "te_responses",
        [nameof(TeQbchDlrequest)] = "te_qbch_dlrequests",
        [nameof(TeQbchDlanswer)] = "te_qbch_dlanswers"
    };

    /// <summary>
    /// Один response_guid — один DbContext и одна транзакция: сбой на одном не влияет на остальные.
    /// </summary>
    public async Task<RestoreResult> RestoreAsync(string responseGuid, CancellationToken ct)
    {
        var warnings = new List<string>();

        await using var context = contextFactory();
        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        try
        {
            var found = await context.TeDlrequests
                .Where(x => x.ResponseGuid == responseGuid)
                .Take(2)
                .ToListAsync(ct);

            if (found.Count == 0)
                return Result(responseGuid, RestoreStatus.NotFound, null, false, context, warnings);

            if (found.Count > 1)
                return Result(responseGuid, RestoreStatus.Ambiguous, null, false, context, warnings,
                    "по response_guid найдено несколько строк te_dlrequests");

            var dlrequest = found[0];
            var root = XmlMapper.Parse(dlrequest.RequestXml);

            if (root is null)
                return Result(responseGuid, RestoreStatus.NoRequestXml, dlrequest.KeyId, false, context, warnings,
                    "request_xml пуст или не разбирается");

            var inserted = dlrequest.Inserted ?? dlrequest.RequestDateTime;
            var packages = XmlMapper.Nodes(root, "Запрос").ToList();
            var headerUpdated = FillHeader(dlrequest, root, warnings);

            var requestCount = await context.TeRequests.CountAsync(x => x.DlrequestId == dlrequest.KeyId, ct);

            if (requestCount > 0)
                warnings.Add($"te_requests уже заполнены ({requestCount}) — пакеты не восстанавливались");
            else if (packages.Count == 0)
                warnings.Add("в request_xml нет блоков Запрос");
            else
                await AddRequestsAsync(context, dlrequest, packages, inserted, ct);

            var taskCount = await context.TeQbchTasks.CountAsync(x => x.ReqId == dlrequest.KeyId, ct);

            if (taskCount > 0)
                warnings.Add($"te_qbch_tasks уже заполнены ({taskCount}) — задачи и ответы не восстанавливались");
            else if (string.IsNullOrWhiteSpace(dlrequest.QbchTasksResultXml))
                warnings.Add("qbch_tasks_result_xml пуст — te_qbch_tasks и te_responses восстановить нечем");
            else
                await AddTasksAndResponsesAsync(context, dlrequest, root, packages, inserted, warnings, ct);

            if (!context.ChangeTracker.HasChanges())
            {
                await transaction.RollbackAsync(ct);
                return Result(responseGuid, RestoreStatus.NothingToDo, dlrequest.KeyId, false, context, warnings);
            }

            if (!apply)
            {
                var planned = Result(responseGuid, RestoreStatus.Planned, dlrequest.KeyId, headerUpdated, context, warnings);
                await transaction.RollbackAsync(ct);
                return planned;
            }

            var result = Result(responseGuid, RestoreStatus.Restored, dlrequest.KeyId, headerUpdated, context, warnings);
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return result;
        }
        catch (Exception ex)
        {
            await SafeRollbackAsync(transaction, ct);

            return new RestoreResult(responseGuid, RestoreStatus.Failed, null, false,
                new Dictionary<string, int>(), warnings, Describe(ex));
        }
    }

    /// <summary>
    /// Атрибуты корня ЗапросСведений, которые в прод-версии брались только из десериализованного объекта.
    /// </summary>
    private bool FillHeader(TeDlrequest dlrequest, XElement root, List<string> warnings)
    {
        var updated = false;

        // Пустая строка в колонке — такой же «не заполнено», как и NULL.
        if (string.IsNullOrWhiteSpace(dlrequest.RequestId))
        {
            var requestId = XmlMapper.Attr(root, "ИдентификаторЗапроса");

            if (requestId is not null)
            {
                dlrequest.RequestId = requestId;
                updated = true;
            }
            else
            {
                warnings.Add("в корне request_xml нет атрибута ИдентификаторЗапроса — request_id оставлен пустым");
            }
        }

        if (dlrequest.InformationCode is null)
        {
            var value = ResolveCode(root, "КодСведений", dictionaries.InformationCodes, "information_code", warnings);

            if (value is not null)
            {
                dlrequest.InformationCode = value;
                updated = true;
            }
        }

        if (dlrequest.RequestMode is null)
        {
            var value = ResolveCode(root, "РежимЗапроса", dictionaries.RequestModes, "request_mode", warnings);

            if (value is not null)
            {
                dlrequest.RequestMode = value;
                updated = true;
            }
        }

        if (dlrequest.RequestType is null)
        {
            var value = ResolveCode(root, "ТипЗапроса", dictionaries.RequestTypes, "request_type", warnings);

            if (value is not null)
            {
                dlrequest.RequestType = value;
                updated = true;
            }
        }

        return updated;
    }

    private static int? ResolveCode(XElement root, string attribute, HashSet<int> dictionary, string column, List<string> warnings)
    {
        var raw = XmlMapper.Attr(root, attribute);

        if (raw is null)
        {
            warnings.Add($"в корне request_xml нет атрибута {attribute} — {column} оставлен NULL");
            return null;
        }

        if (!int.TryParse(raw, out var value))
        {
            warnings.Add($"{attribute}='{raw}' не число — {column} оставлен NULL");
            return null;
        }

        if (!dictionary.Contains(value))
        {
            warnings.Add($"{attribute}={value} отсутствует в справочнике — {column} оставлен NULL");
            return null;
        }

        return value;
    }

    private async Task AddRequestsAsync(
        QbchContext context,
        TeDlrequest dlrequest,
        List<XElement> packages,
        DateTime? inserted,
        CancellationToken ct)
    {
        // Кэш пользователей в рамках одного dlrequest — как в RepositoryV3.CreateDlRequestV3.
        var users = new List<TdUser>();

        foreach (var package in packages)
        {
            var orderNum = XmlMapper.AttrInt(package, "ПорядковыйНомер") ?? 0;
            var user = await ResolveUserAsync(context, package, users, inserted, ct);

            var request = new TeRequest
            {
                Dlrequest = dlrequest,
                Inserted = inserted,
                OrderNum = orderNum,
                // package_error хранился только в Redis и уже истёк, поэтому код ошибки неизвестен.
                ErrorCode = 0,
                ErrorMessage = null,
                User = user?.KeyId == 0 ? user : null,
                UserId = user?.KeyId == 0 ? null : user?.KeyId,
                RequestXml = package.ToString()
            };

            await context.TeRequests.AddAsync(request, ct);

            var subject = XmlMapper.BuildSubject(package);

            if (subject is null)
                continue;

            ValidateDocuments(subject.Value.Documents, orderNum);

            subject.Value.Subject.Request = request;
            subject.Value.Subject.Inserted = inserted;

            foreach (var document in subject.Value.Documents)
                document.Inserted = inserted;

            foreach (var fullName in subject.Value.FullNames)
                fullName.Inserted = inserted;

            await context.TeSubjects.AddAsync(subject.Value.Subject, ct);
            await context.TeSubjectsDocuments.AddRangeAsync(subject.Value.Documents, ct);
            await context.TeSubjectsFullNames.AddRangeAsync(subject.Value.FullNames, ct);
        }
    }

    private void ValidateDocuments(List<TeSubjectsDocument> documents, int orderNum)
    {
        foreach (var document in documents)
        {
            if (string.IsNullOrWhiteSpace(document.DocTypeId))
                throw new RestoreValidationException($"пакет {orderNum}: у ДокументЛичности нет атрибута КодДУЛ");

            if (!dictionaries.DocumentTypes.Contains(document.DocTypeId))
                throw new RestoreValidationException($"пакет {orderNum}: КодДУЛ '{document.DocTypeId}' отсутствует в tr_document_types");

            if (string.IsNullOrWhiteSpace(document.DocNumber))
                throw new RestoreValidationException($"пакет {orderNum}: у ДокументЛичности (КодДУЛ {document.DocTypeId}) нет Номера");
        }
    }

    private async Task<TdUser?> ResolveUserAsync(
        QbchContext context,
        XElement package,
        List<TdUser> users,
        DateTime? inserted,
        CancellationToken ct)
    {
        var built = XmlMapper.BuildUser(package);

        if (built is null)
            return null;

        var (match, value, user) = built.Value;

        if (!string.IsNullOrWhiteSpace(value))
        {
            TdUser? existing;

            if (match == UserMatch.Ogrn)
            {
                var upper = value.ToUpperInvariant();

                existing = users.FirstOrDefault(x => string.Equals(x.Ogrn, value, StringComparison.OrdinalIgnoreCase))
                    ?? await context.TdUsers.FirstOrDefaultAsync(x => x.Ogrn != null && x.Ogrn.ToUpper() == upper, ct);
            }
            else
            {
                existing = users.FirstOrDefault(x => x.FullName == value)
                    ?? await context.TdUsers.FirstOrDefaultAsync(x => x.FullName == value, ct);
            }

            if (existing is not null)
            {
                if (!users.Contains(existing))
                    users.Add(existing);

                return existing;
            }
        }

        if (user.DocType is not null && !dictionaries.DocumentTypes.Contains(user.DocType))
            throw new RestoreValidationException($"КодДУЛ источника '{user.DocType}' отсутствует в tr_document_types");

        user.Inserted = inserted;
        users.Add(user);

        return user;
    }

    private async Task AddTasksAndResponsesAsync(
        QbchContext context,
        TeDlrequest dlrequest,
        XElement root,
        List<XElement> packages,
        DateTime? inserted,
        List<string> warnings,
        CancellationToken ct)
    {
        var informationCode = XmlMapper.Attr(root, "КодСведений");
        var ответ = XmlMapper.Parse(dlrequest.QbchTasksResultXml);

        if (ответ is null)
        {
            warnings.Add("qbch_tasks_result_xml не разбирается — te_qbch_tasks и te_responses не восстановлены");
            return;
        }

        var сведения = XmlMapper.Nodes(ответ, "Сведения").ToList();
        var кбкиБлоки = сведения.SelectMany(x => XmlMapper.Nodes(x, "КБКИ")).ToList();

        var ogrns = кбкиБлоки
            .Select(x => XmlMapper.Attr(x, "ОГРН"))
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (ogrns.Count == 0)
        {
            warnings.Add("в qbch_tasks_result_xml нет блоков КБКИ с ОГРН — te_qbch_tasks и te_responses не восстановлены");
            return;
        }

        var tasks = new List<(TeQbchTask Task, string Ogrn)>();

        foreach (var ogrn in ogrns)
        {
            var abonent = await context.TrAbonents.FirstOrDefaultAsync(x => x.Ogrn == ogrn, ct);

            if (abonent is null)
            {
                warnings.Add($"абонент по ОГРН {ogrn} не найден — задача и её ответы пропущены");
                continue;
            }

            var (start, end) = EstimateTimings(dlrequest, abonent.KeyId);

            if (start is null)
                warnings.Add($"нет калибровочных данных по бюро {ogrn} — тайминги задачи оставлены NULL");

            if (XmlMapper.HasError(ответ, ogrn))
                warnings.Add($"ответ бюро {ogrn} содержит Ошибку — task_result_xml собран из агрегата, бюро могло не ответить вовсе");

            var task = new TeQbchTask
            {
                Req = dlrequest,
                Inserted = inserted,
                QbchCorrespondentId = abonent.KeyId,
                // У своего бюро данные собираются внутри и response_id не заполняется, хотя его блок
                // в агрегате несёт наш собственный ИдентификаторОтвета. Решаем по здоровым записям,
                // а не по ОГРН из конфига.
                ResponseId = calibration.For(abonent.KeyId)?.ResponseIdShare == 0
                    ? null
                    : кбкиБлоки
                        .Where(x => XmlMapper.Attr(x, "ОГРН") == ogrn)
                        .Select(x => XmlMapper.Attr(x, "ИдентификаторОтвета"))
                        .FirstOrDefault(x => x is not null),
                TaskStartDateTime = start,
                TaskEndDateTime = end,
                TaskResultXml = XmlMapper.BuildTaskResultXml(ответ, ogrn)
            };

            tasks.Add((task, ogrn));
            await context.TeQbchTasks.AddAsync(task, ct);

            foreach (var package in packages)
            {
                var номер = XmlMapper.Attr(package, "ПорядковыйНомер");
                var блок = сведения.FirstOrDefault(x => XmlMapper.Attr(x, "ПорядковыйНомер") == номер);

                if (блок is null)
                    continue;

                foreach (var кбки in XmlMapper.Nodes(блок, "КБКИ").Where(x => XmlMapper.Attr(x, "ОГРН") == ogrn))
                {
                    var errorCode = XmlMapper.GetErrorCode(кбки);

                    if (!dictionaries.ErrorCodes.Contains(errorCode))
                        throw new RestoreValidationException(
                            $"пакет {номер}, КБКИ {ogrn}: код ошибки {errorCode} отсутствует в tr_error_codes");

                    await context.TeResponses.AddAsync(new TeResponse
                    {
                        QbchTask = task,
                        Inserted = inserted,
                        OrderNum = int.TryParse(номер, out var orderNum) ? orderNum : 0,
                        ResponseXml = блок.ToString(),
                        AmpResponseType = informationCode == "6"
                            ? null
                            : Check(XmlMapper.MapAmpResponseType(кбки, ourBureauOgrn), dictionaries.AmpResponseTypes, "tr_amp_response_types", номер, ogrn, warnings),
                        SpResponseType = Check(XmlMapper.MapSpResponseType(кбки), dictionaries.SpResponseTypes, "tr_sp_response_types", номер, ogrn, warnings),
                        ErrorCode = errorCode,
                        ErrorMessage = XmlMapper.GetErrorMessage(кбки)
                    }, ct);
                }
            }
        }

        AlignTimings(tasks.Select(x => x.Task).ToList(), dlrequest);

        foreach (var (task, ogrn) in tasks)
            await AddExchangeAsync(context, task, ogrn, root, inserted, warnings, ct);
    }

    /// <summary>
    /// Обмен с одним бюро: te_qbch_dlrequests (наш запрос и Результат бюро) и te_qbch_dlanswers
    /// (опросы «ответ не готов» и финальный ответ). Конверты берутся из образцов реального обмена,
    /// идентификаторы — из восстанавливаемой записи, тайминги — из таймингов задачи.
    ///
    /// Подписанные CMS-блобы (request_data, response_data) остаются NULL: подпись бюро принадлежит
    /// бюро, а свежая наша подпись не была бы артефактом того обмена.
    /// </summary>
    private async Task AddExchangeAsync(
        QbchContext context,
        TeQbchTask task,
        string ogrn,
        XElement root,
        DateTime? inserted,
        List<string> warnings,
        CancellationToken ct)
    {
        var profile = exchange.For(task.QbchCorrespondentId);

        // У своего бюро данные собираются внутри, без HTTP: строк обмена у него и не было.
        if (profile is null)
            return;

        foreach (var code in new[] { profile.DlrequestErrorCode, profile.PendingErrorCode, profile.FinalErrorCode })
        {
            if (!dictionaries.ErrorCodes.Contains(code))
                throw new RestoreValidationException($"КБКИ {ogrn}: код ошибки обмена {code} отсутствует в tr_error_codes");
        }

        var requestId = XmlMapper.Attr(root, "ИдентификаторЗапроса");
        var requestDate = XmlMapper.Attr(root, "ДатаЗапроса");

        var requestXml = XmlMapper.BuildForwardedRequest(profile.RequestTemplate, root, requestId);
        var resultXml = XmlMapper.StampResult(profile.ResultTemplate, ogrn, task.ResponseId, requestId, requestDate);

        if (requestXml is null || resultXml is null)
            warnings.Add($"нет образца обмена с бюро {ogrn} — строки te_qbch_dlrequests восстановлены без XML");

        // Нет ни одной здоровой строки te_qbch_dlanswers по этому бюро — значит обмен так не ведётся
        // (или таблица на контуре пуста). Создавать строки «на всякий случай» нельзя: в исходной
        // записи их тоже не было.
        var restoreAnswers = profile.AnswerSamples > 0;

        if (!restoreAnswers)
            warnings.Add($"нет здоровых строк te_qbch_dlanswers по бюро {ogrn} — строки опросов и финального ответа не создаются");

        var requestSent = task.TaskStartDateTime;
        var requestAnswered = requestSent?.AddSeconds(profile.DlrequestLatency);

        await context.TeQbchDlrequests.AddAsync(new TeQbchDlrequest
        {
            QbchTask = task,
            Inserted = inserted,
            RequestDateTime = requestSent,
            ResponseDateTime = requestAnswered,
            RequestXml = requestXml,
            ResponseXml = resultXml,
            ErrorCode = profile.DlrequestErrorCode,
            HttpResponseCode = profile.DlrequestHttpCode,
            RequestData = null,
            ResponseData = null
        }, ct);

        if (!restoreAnswers)
            return;

        var pendingXml = XmlMapper.StampResult(profile.PendingTemplate, ogrn, task.ResponseId, requestId, requestDate);
        var slots = profile.PendingAnswers + 1;

        for (var i = 1; i <= profile.PendingAnswers; i++)
        {
            var moment = Interpolate(requestAnswered, task.TaskEndDateTime, i, slots);

            await context.TeQbchDlanswers.AddAsync(new TeQbchDlanswer
            {
                QbchTask = task,
                Inserted = inserted,
                RequestDateTime = moment?.AddSeconds(-profile.AnswerLatency),
                ResponseDateTime = moment,
                ResponseXml = pendingXml,
                ErrorCode = profile.PendingErrorCode,
                ErrorMessage = profile.PendingErrorMessage,
                HttpResponseCode = profile.PendingHttpCode,
                ResponseData = null
            }, ct);
        }

        await context.TeQbchDlanswers.AddAsync(new TeQbchDlanswer
        {
            QbchTask = task,
            Inserted = inserted,
            RequestDateTime = task.TaskEndDateTime?.AddSeconds(-profile.AnswerLatency),
            ResponseDateTime = task.TaskEndDateTime,
            ResponseXml = task.TaskResultXml,
            ErrorCode = profile.FinalErrorCode,
            HttpResponseCode = profile.FinalHttpCode,
            ResponseData = null
        }, ct);
    }

    private static DateTime? Interpolate(DateTime? from, DateTime? to, int index, int count)
    {
        if (from is null)
            return null;

        if (to is null || to <= from)
            return from;

        return from.Value.AddSeconds((to.Value - from.Value).TotalSeconds * index / count);
    }

    /// <summary>
    /// Вычисленные тайминги задачи: якорь — фактическая метка te_dlrequests, смещение — медиана по бюро.
    /// </summary>
    private (DateTime? Start, DateTime? End) EstimateTimings(TeDlrequest dlrequest, int correspondentId)
    {
        var profile = calibration.For(correspondentId);

        if (profile is null)
            return (null, null);

        DateTime? start = null;

        if (dlrequest.ValidationDateTime is DateTime validation && profile.StartAfterValidation is double afterValidation)
            start = validation.AddSeconds(afterValidation);
        else if (profile.StartAfterRequest is double afterRequest)
            start = dlrequest.RequestDateTime.AddSeconds(afterRequest);

        return start is null ? (null, null) : (start, start.Value.AddSeconds(profile.Duration));
    }

    /// <summary>
    /// Загоняет вычисленные тайминги в интервал между фактическими метками строки te_dlrequests.
    /// Конец сбора данных известен точно, и он равен максимуму из концов задач, поэтому у последней
    /// задачи task_end_date_time выставляется ровно в qbch_tasks_end_date_time.
    /// </summary>
    private static void AlignTimings(List<TeQbchTask> tasks, TeDlrequest dlrequest)
    {
        var timed = tasks.Where(x => x.TaskStartDateTime is not null && x.TaskEndDateTime is not null).ToList();

        if (timed.Count == 0)
            return;

        var lower = dlrequest.ValidationDateTime ?? dlrequest.RequestDateTime;

        foreach (var task in timed)
        {
            var duration = task.TaskEndDateTime!.Value - task.TaskStartDateTime!.Value;

            if (task.TaskStartDateTime < lower)
            {
                task.TaskStartDateTime = lower;
                task.TaskEndDateTime = lower + duration;
            }

            if (dlrequest.QbchTasksEndDateTime is not DateTime limit)
                continue;

            if (task.TaskStartDateTime > limit)
                task.TaskStartDateTime = limit;

            if (task.TaskEndDateTime > limit)
                task.TaskEndDateTime = limit;

            if (task.TaskEndDateTime < task.TaskStartDateTime)
                task.TaskEndDateTime = task.TaskStartDateTime;
        }

        if (dlrequest.QbchTasksEndDateTime is DateTime tasksEnd)
        {
            timed.OrderByDescending(x => x.TaskEndDateTime)
                .ThenByDescending(x => x.QbchCorrespondentId)
                .First()
                .TaskEndDateTime = tasksEnd;
        }
    }

    private static int? Check(int? value, HashSet<int> dictionary, string dictionaryName, string? orderNum, string ogrn, List<string> warnings)
    {
        if (value is null || dictionary.Contains(value.Value))
            return value;

        warnings.Add($"пакет {orderNum}, КБКИ {ogrn}: значение {value} отсутствует в {dictionaryName} — оставлено NULL");

        return null;
    }

    private static RestoreResult Result(
        string responseGuid,
        RestoreStatus status,
        long? dlrequestKeyId,
        bool headerUpdated,
        QbchContext context,
        List<string> warnings,
        string? message = null)
        => new(responseGuid, status, dlrequestKeyId, headerUpdated, CountAdded(context), warnings, message);

    private static Dictionary<string, int> CountAdded(QbchContext context)
        => context.ChangeTracker.Entries()
            .Where(x => x.State == EntityState.Added)
            .GroupBy(x => TableByEntity.TryGetValue(x.Entity.GetType().Name, out var table) ? table : x.Entity.GetType().Name)
            .ToDictionary(x => x.Key, x => x.Count());

    private static async Task SafeRollbackAsync(IDbContextTransaction transaction, CancellationToken ct)
    {
        try
        {
            await transaction.RollbackAsync(ct);
        }
        catch
        {
            // транзакция уже закрыта или соединение потеряно — состояние БД не изменилось
        }
    }

    private static string Describe(Exception ex)
    {
        var message = ex is RestoreValidationException ? ex.Message : $"{ex.GetType().Name}: {ex.Message}";

        return ex.InnerException is null ? message : $"{message} -> {ex.InnerException.Message}";
    }
}
