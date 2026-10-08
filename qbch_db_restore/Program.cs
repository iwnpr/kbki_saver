using db_lib.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QBCH_lib.CommonTypes.Api;
using System.Text;

namespace qbch_db_restore;

/// <summary>
/// Разовая утилита восстановления потомков te_dlrequests.
/// Параметров командной строки нет: список response_guid, строка подключения и режим
/// берутся из файлов рядом с программой.
/// </summary>
internal static class Program
{
    private const string SettingsFileName = "appsettings.json";
    private const string DefaultGuidsFileName = "guids.txt";
    private const string DefaultBureauName = "BKICI";
    private const int DefaultCalibrationSampleSize = 20000;

    private static readonly string[] ReportTables =
    [
        "td_users",
        "te_requests",
        "te_subjects",
        "te_subjects_documents",
        "te_subjects_full_name",
        "te_qbch_tasks",
        "te_responses",
        "te_qbch_dlrequests",
        "te_qbch_dlanswers"
    ];

    private static async Task<int> Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var baseDirectory = AppContext.BaseDirectory;
        var settingsPath = Path.Combine(baseDirectory, SettingsFileName);

        if (!File.Exists(settingsPath))
        {
            Console.Error.WriteLine($"Файл настроек не найден: {settingsPath}");
            return 2;
        }

        IConfiguration configuration;

        try
        {
            configuration = new ConfigurationBuilder().AddJsonFile(settingsPath, optional: false).Build();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Не удалось прочитать {settingsPath}: {ex.Message}");
            return 2;
        }

        var connectionString = configuration.GetConnectionString("DataBase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine($"ConnectionStrings:DataBase не заполнен в {settingsPath}");
            return 2;
        }

        var guidsPath = Resolve(baseDirectory, configuration.GetValue<string>("Restore:GuidsFile"), DefaultGuidsFileName);

        if (!File.Exists(guidsPath))
        {
            Console.Error.WriteLine($"Файл со списком response_guid не найден: {guidsPath}");
            Console.Error.WriteLine("Положите его рядом с программой или укажите путь в Restore:GuidsFile.");
            return 2;
        }

        List<string> guids;

        try
        {
            guids = ReadGuids(guidsPath);
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Не удалось прочитать {guidsPath}: {ex.Message}");
            return 2;
        }

        if (guids.Count == 0)
        {
            Console.Error.WriteLine($"В {guidsPath} нет ни одного response_guid");
            return 2;
        }

        var dryRun = configuration.GetValue<bool?>("Restore:DryRun") ?? false;
        var bureauName = configuration.GetValue<string>("Restore:OurBureauName") ?? DefaultBureauName;
        var ourBureauOgrn = new BKIRequsits(configuration).GetBureaList().FirstOrDefault(x => x.Name == bureauName)?.ogrn;

        var reportDirectory = Resolve(baseDirectory, configuration.GetValue<string>("Restore:ReportDirectory"), ".");
        var reportPath = Path.Combine(reportDirectory, $"restore-report-{DateTime.Now:yyyyMMdd-HHmmss}.csv");

        Console.WriteLine($"Режим: {(dryRun ? "предпросмотр (Restore:DryRun = true, запись отключена)" : "запись в БД")}");
        Console.WriteLine($"БД: {DescribeConnection(connectionString)}");
        Console.WriteLine($"Список: {guidsPath} — response_guid к обработке: {guids.Count}");

        if (ourBureauOgrn is null)
            Console.WriteLine($"ВНИМАНИЕ: бюро {bureauName} нет в секции QBCH — amp_response_type будет вычислен без признака своих данных");

        Console.WriteLine();

        QbchContext CreateContext() => new(new DbContextOptionsBuilder<QbchContext>().UseNpgsql(connectionString).Options);

        Dictionaries dictionaries;
        TimingCalibration calibration;
        ExchangeCalibration exchange;

        try
        {
            var sampleSize = configuration.GetValue<int?>("Restore:CalibrationSampleSize") ?? DefaultCalibrationSampleSize;

            await using var context = CreateContext();
            dictionaries = await Dictionaries.LoadAsync(context, cts.Token);
            calibration = await TimingCalibration.LoadAsync(context, sampleSize, cts.Token);
            exchange = await ExchangeCalibration.LoadAsync(context, sampleSize, cts.Token);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Не удалось прочитать справочники: {ex.Message}");
            return 2;
        }

        await PrintCalibrationAsync(CreateContext, calibration, exchange, cts.Token);

        var service = new RestoreService(CreateContext, dictionaries, calibration, exchange, ourBureauOgrn, apply: !dryRun);
        var results = new List<RestoreResult>(guids.Count);

        for (var i = 0; i < guids.Count; i++)
        {
            if (cts.IsCancellationRequested)
            {
                Console.WriteLine("Прервано пользователем.");
                break;
            }

            var result = await service.RestoreAsync(guids[i], cts.Token);
            results.Add(result);
            PrintResult(i + 1, guids.Count, result);
        }

        try
        {
            // Без токена отмены: отчёт по уже обработанным guid нужен и после Ctrl+C.
            await WriteReportAsync(reportPath, results);
            Console.WriteLine();
            Console.WriteLine($"Отчёт: {reportPath}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Не удалось записать отчёт {reportPath}: {ex.Message}");
        }

        PrintSummary(results);

        return results.Any(x => x.Status is RestoreStatus.Failed
            or RestoreStatus.NotFound
            or RestoreStatus.Ambiguous
            or RestoreStatus.NoRequestXml)
            ? 1
            : 0;
    }

    /// <summary>
    /// Путь из настроек: абсолютный берётся как есть, относительный — от каталога программы,
    /// чтобы результат не зависел от того, откуда программу запустили.
    /// </summary>
    private static string Resolve(string baseDirectory, string? configured, string fallback)
    {
        var value = string.IsNullOrWhiteSpace(configured) ? fallback : configured.Trim();

        return Path.GetFullPath(Path.IsPathRooted(value) ? value : Path.Combine(baseDirectory, value));
    }

    /// <summary>
    /// Медианы, по которым считаются тайминги задач, печатаются перед прогоном: это единственная
    /// запись о том, какими числами заполнены task_start_date_time и task_end_date_time.
    /// </summary>
    private static async Task PrintCalibrationAsync(
        Func<QbchContext> contextFactory, TimingCalibration calibration, ExchangeCalibration exchange, CancellationToken ct)
    {
        Console.WriteLine("Калибровка таймингов по здоровым записям (медианы, сек):");

        if (calibration.Overall is null)
        {
            Console.WriteLine("  нет ни одной задачи с заполненными таймингами — task_start_date_time и task_end_date_time останутся NULL");
            Console.WriteLine();
            return;
        }

        await using var context = contextFactory();

        var correspondentIds = calibration.ByCorrespondent.Keys.Union(exchange.ByCorrespondent.Keys).ToList();

        var abonents = await context.TrAbonents
            .AsNoTracking()
            .Where(x => correspondentIds.Contains(x.KeyId))
            .ToDictionaryAsync(x => x.KeyId, x => x.Ogrn, ct);

        foreach (var (correspondentId, profile) in calibration.ByCorrespondent.OrderBy(x => x.Key))
        {
            var ogrn = abonents.TryGetValue(correspondentId, out var value) ? value : correspondentId.ToString();

            Console.WriteLine($"  КБКИ {ogrn}: старт после валидации {Format(profile.StartAfterValidation)}"
                + $", старт после запроса {Format(profile.StartAfterRequest)}"
                + $", длительность {profile.Duration:0.###}"
                + $", response_id {(profile.ResponseIdShare == 0 ? "не заполняется" : $"заполнен у {profile.ResponseIdShare:P0}")}"
                + $", задач в выборке {profile.Samples}");
        }

        Console.WriteLine($"  общая медиана: длительность {calibration.Overall.Duration:0.###}, задач в выборке {calibration.Overall.Samples}");
        Console.WriteLine();

        Console.WriteLine("Калибровка обмена с КБКИ (по здоровым строкам te_qbch_dlrequests / te_qbch_dlanswers):");

        if (exchange.ByCorrespondent.Count == 0)
        {
            Console.WriteLine("  строк обмена не найдено — te_qbch_dlrequests и te_qbch_dlanswers восстанавливаться не будут");
        }
        else
        {
            foreach (var (correspondentId, profile) in exchange.ByCorrespondent.OrderBy(x => x.Key))
            {
                var ogrn = abonents.TryGetValue(correspondentId, out var value) ? value : correspondentId.ToString();

                Console.WriteLine($"  КБКИ {ogrn}: dlrequest {profile.DlrequestErrorCode}/{profile.DlrequestHttpCode?.ToString() ?? "—"}"
                    + $" за {profile.DlrequestLatency:0.###} с, опросов «не готов» {profile.PendingAnswers}"
                    + $" ({profile.PendingErrorCode}/{profile.PendingHttpCode?.ToString() ?? "—"})"
                    + $", финал {profile.FinalErrorCode}/{profile.FinalHttpCode?.ToString() ?? "—"}"
                    + $", образцы XML {(profile.RequestTemplate is null ? "нет" : "есть")}"
                    + $", строк dlrequest {profile.Samples}"
                    + (profile.AnswerSamples == 0
                        ? ", строк dlanswer НЕТ — опросы не восстанавливаются, http-код финала пустой"
                        : $", строк dlanswer {profile.AnswerSamples}"));
            }

            var withoutExchange = calibration.ByCorrespondent.Keys.Except(exchange.ByCorrespondent.Keys).ToList();

            foreach (var correspondentId in withoutExchange)
            {
                var ogrn = abonents.TryGetValue(correspondentId, out var value) ? value : correspondentId.ToString();
                Console.WriteLine($"  КБКИ {ogrn}: обмена по HTTP нет — строки te_qbch_dlrequests/te_qbch_dlanswers не создаются");
            }
        }

        Console.WriteLine();

        static string Format(double? value) => value is null ? "нет данных" : value.Value.ToString("0.###");
    }

    private static List<string> ReadGuids(string path)
    {
        var raw = File.ReadAllLines(path)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0 && !x.StartsWith('#'))
            .ToList();

        var unique = new List<string>(raw.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var guid in raw)
        {
            if (seen.Add(guid))
                unique.Add(guid);
        }

        if (unique.Count != raw.Count)
            Console.WriteLine($"Дубликаты в списке пропущены: {raw.Count - unique.Count}");

        return unique;
    }

    private static void PrintResult(int number, int total, RestoreResult result)
    {
        var rows = ReportTables
            .Where(x => result.Added(x) > 0)
            .Select(x => $"{x}={result.Added(x)}")
            .ToList();

        if (result.HeaderUpdated)
            rows.Insert(0, "te_dlrequests=1(update)");

        Console.WriteLine($"[{number}/{total}] {result.ResponseGuid} {result.Status}"
            + (result.DlrequestKeyId is null ? string.Empty : $" key_id={result.DlrequestKeyId}")
            + (rows.Count == 0 ? string.Empty : " " + string.Join(" ", rows))
            + (result.Message is null ? string.Empty : $" — {result.Message}"));

        foreach (var warning in result.Warnings)
            Console.WriteLine($"        ! {warning}");
    }

    private static void PrintSummary(List<RestoreResult> results)
    {
        Console.WriteLine();
        Console.WriteLine("Итого по статусам:");

        foreach (var group in results.GroupBy(x => x.Status).OrderBy(x => x.Key.ToString(), StringComparer.Ordinal))
            Console.WriteLine($"  {group.Key}: {group.Count()}");

        Console.WriteLine("Строк по таблицам:");

        foreach (var table in ReportTables)
            Console.WriteLine($"  {table}: {results.Sum(x => x.Added(table))}");

        Console.WriteLine($"  te_dlrequests (обновлено): {results.Count(x => x.HeaderUpdated)}");
    }

    private static async Task WriteReportAsync(string path, List<RestoreResult> results)
    {
        var builder = new StringBuilder();

        builder.AppendLine(string.Join(';', new[] { "response_guid", "status", "dlrequest_key_id", "header_updated" }
            .Concat(ReportTables)
            .Concat(new[] { "warnings", "message" })));

        foreach (var result in results)
        {
            var cells = new List<string>
            {
                result.ResponseGuid,
                result.Status.ToString(),
                result.DlrequestKeyId?.ToString() ?? string.Empty,
                result.HeaderUpdated ? "1" : "0"
            };

            cells.AddRange(ReportTables.Select(x => result.Added(x).ToString()));
            cells.Add(string.Join(" | ", result.Warnings));
            cells.Add(result.Message ?? string.Empty);

            builder.AppendLine(string.Join(';', cells.Select(Escape)));
        }

        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string Escape(string value)
        => value.Contains(';') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

    /// <summary>
    /// Описание подключения для баннера без логина и пароля.
    /// </summary>
    private static string DescribeConnection(string connectionString)
        => string.Join("; ", connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.StartsWith("Server=", StringComparison.OrdinalIgnoreCase)
                || x.StartsWith("Host=", StringComparison.OrdinalIgnoreCase)
                || x.StartsWith("Port=", StringComparison.OrdinalIgnoreCase)
                || x.StartsWith("Database=", StringComparison.OrdinalIgnoreCase)));
}
