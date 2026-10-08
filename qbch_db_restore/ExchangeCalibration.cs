using db_lib.Entities;
using Microsoft.EntityFrameworkCore;

namespace qbch_db_restore;

/// <summary>
/// Как выглядит обмен с одним КБКИ: коды, задержки, число опросов и образцы XML,
/// снятые с живых записей te_qbch_dlrequests и te_qbch_dlanswers этой же БД.
/// Конверты (блок Абонент пересланного запроса, форма Результата, форма «ответ не готов»)
/// не придумываются, а берутся из реального обмена и перештамповываются идентификаторами
/// восстанавливаемой записи.
/// </summary>
internal sealed record ExchangeProfile(
    int DlrequestErrorCode,
    int? DlrequestHttpCode,
    double DlrequestLatency,
    int PendingAnswers,
    int PendingErrorCode,
    int? PendingHttpCode,
    string? PendingErrorMessage,
    int FinalErrorCode,
    int? FinalHttpCode,
    double AnswerLatency,
    string? RequestTemplate,
    string? ResultTemplate,
    string? PendingTemplate,
    int Samples,
    int AnswerSamples);

internal sealed class ExchangeCalibration
{
    private readonly Dictionary<int, ExchangeProfile> _byCorrespondent;

    private ExchangeCalibration(Dictionary<int, ExchangeProfile> byCorrespondent) => _byCorrespondent = byCorrespondent;

    public IReadOnlyDictionary<int, ExchangeProfile> ByCorrespondent => _byCorrespondent;

    /// <summary>
    /// Профиль есть только у тех КБКИ, у которых обмен реально ведётся: у своего бюро данные
    /// собираются внутри, без HTTP, и строк обмена у него нет — для него ничего не создаётся.
    /// </summary>
    public ExchangeProfile? For(int correspondentId)
        => _byCorrespondent.TryGetValue(correspondentId, out var profile) ? profile : null;

    public static async Task<ExchangeCalibration> LoadAsync(QbchContext context, int sampleSize, CancellationToken ct)
    {
        var requests = await context.TeQbchDlrequests
            .AsNoTracking()
            .Where(x => x.QbchTaskId != null)
            .OrderByDescending(x => x.KeyId)
            .Take(sampleSize)
            .Select(x => new RequestSample(
                x.QbchTask!.QbchCorrespondentId,
                x.QbchTaskId!.Value,
                x.ErrorCode,
                x.HttpResponseCode,
                x.RequestDateTime,
                x.ResponseDateTime))
            .ToListAsync(ct);

        var answers = await context.TeQbchDlanswers
            .AsNoTracking()
            .Where(x => x.QbchTaskId != null)
            .OrderByDescending(x => x.KeyId)
            .Take(sampleSize)
            .Select(x => new AnswerSample(
                x.QbchTask!.QbchCorrespondentId,
                x.QbchTaskId!.Value,
                x.ErrorCode,
                x.HttpResponseCode,
                x.ErrorMessage,
                x.RequestDateTime,
                x.ResponseDateTime))
            .ToListAsync(ct);

        var profiles = new Dictionary<int, ExchangeProfile>();

        foreach (var correspondentId in requests.Select(x => x.CorrespondentId).Distinct())
        {
            var ownRequests = requests.Where(x => x.CorrespondentId == correspondentId).ToList();
            var ownAnswers = answers.Where(x => x.CorrespondentId == correspondentId).ToList();

            var pending = ownAnswers.Where(x => x.ErrorCode != 0).ToList();
            var final = ownAnswers.Where(x => x.ErrorCode == 0).ToList();

            var templates = await LoadTemplatesAsync(context, correspondentId, ct);

            profiles[correspondentId] = new ExchangeProfile(
                DlrequestErrorCode: Mode(ownRequests.Select(x => x.ErrorCode)) ?? 0,
                DlrequestHttpCode: ModeOrNull(ownRequests.Select(x => x.HttpResponseCode)),
                DlrequestLatency: MedianSeconds(ownRequests.Select(x => Difference(x.RequestDateTime, x.ResponseDateTime))) ?? 0,
                PendingAnswers: MedianCount(pending),
                PendingErrorCode: Mode(pending.Select(x => x.ErrorCode)) ?? 12,
                PendingHttpCode: ModeOrNull(pending.Select(x => x.HttpResponseCode)),
                PendingErrorMessage: ModeText(pending.Select(x => x.ErrorMessage)),
                FinalErrorCode: Mode(final.Select(x => x.ErrorCode)) ?? 0,
                FinalHttpCode: ModeOrNull(final.Select(x => x.HttpResponseCode)),
                AnswerLatency: MedianSeconds(ownAnswers.Select(x => Difference(x.RequestDateTime, x.ResponseDateTime))) ?? 0,
                RequestTemplate: templates.Request,
                ResultTemplate: templates.Result,
                PendingTemplate: templates.Pending,
                Samples: ownRequests.Count,
                AnswerSamples: ownAnswers.Count);
        }

        return new ExchangeCalibration(profiles);
    }

    /// <summary>
    /// По одному свежему образцу каждого документа: их XML не выбирается пачкой, чтобы не тянуть
    /// из БД десятки мегабайт ради конвертов.
    /// </summary>
    private static async Task<(string? Request, string? Result, string? Pending)> LoadTemplatesAsync(
        QbchContext context, int correspondentId, CancellationToken ct)
    {
        var request = await context.TeQbchDlrequests
            .AsNoTracking()
            .Where(x => x.QbchTask!.QbchCorrespondentId == correspondentId && x.RequestXml != null && x.ResponseXml != null)
            .OrderByDescending(x => x.KeyId)
            .Select(x => new { x.RequestXml, x.ResponseXml })
            .FirstOrDefaultAsync(ct);

        var pending = await context.TeQbchDlanswers
            .AsNoTracking()
            .Where(x => x.QbchTask!.QbchCorrespondentId == correspondentId && x.ErrorCode != 0 && x.ResponseXml != null)
            .OrderByDescending(x => x.KeyId)
            .Select(x => x.ResponseXml)
            .FirstOrDefaultAsync(ct);

        return (request?.RequestXml, request?.ResponseXml, pending);
    }

    private static double? Difference(DateTime? from, DateTime? to)
        => from is null || to is null ? null : (to.Value - from.Value).TotalSeconds;

    private static double? MedianSeconds(IEnumerable<double?> values)
    {
        var sorted = values.OfType<double>().Where(x => x >= 0).OrderBy(x => x).ToList();

        if (sorted.Count == 0)
            return null;

        return sorted.Count % 2 == 1
            ? sorted[sorted.Count / 2]
            : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2;
    }

    /// <summary>
    /// Сколько строк такого типа приходится на одну задачу — медиана по задачам выборки.
    /// </summary>
    private static int MedianCount(List<AnswerSample> samples)
    {
        var counts = samples.GroupBy(x => x.TaskId).Select(x => (double)x.Count()).ToList();
        var median = MedianSeconds(counts.Select(x => (double?)x));

        return median is null ? 0 : (int)Math.Round(median.Value, MidpointRounding.AwayFromZero);
    }

    /// <summary>Самое частое значение; null, если значений нет.</summary>
    private static T? Mode<T>(IEnumerable<T> values) where T : struct
        => values.GroupBy(x => x).OrderByDescending(x => x.Count()).Select(x => (T?)x.Key).FirstOrDefault();

    private static T? ModeOrNull<T>(IEnumerable<T?> values) where T : struct
        => values.OfType<T>().GroupBy(x => x).OrderByDescending(x => x.Count()).Select(x => (T?)x.Key).FirstOrDefault();

    private static string? ModeText(IEnumerable<string?> values)
        => values.Where(x => !string.IsNullOrWhiteSpace(x)).GroupBy(x => x).OrderByDescending(x => x.Count()).Select(x => x.Key).FirstOrDefault();

    private sealed record RequestSample(
        int CorrespondentId, long TaskId, int ErrorCode, int? HttpResponseCode, DateTime? RequestDateTime, DateTime? ResponseDateTime);

    private sealed record AnswerSample(
        int CorrespondentId, long TaskId, int ErrorCode, int? HttpResponseCode, string? ErrorMessage, DateTime? RequestDateTime, DateTime? ResponseDateTime);
}
