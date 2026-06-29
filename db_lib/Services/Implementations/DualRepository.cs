using cache_lib.Interfaces;
using db_lib.Services.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace db_lib.Services.Implementations;

/// <summary>
/// Декоратор <see cref="IRepository"/> (V2), дублирующий запись в две схемы базы данных.
/// Пишет последовательно: сначала в основную схему, затем во вторую. Каждая запись
/// изолирована: сбой (в т.ч. исключение) одной схемы не мешает попытке записи в другую
/// и логируется с указанием схемы. Методы, возвращающие <see cref="bool"/>, возвращают
/// <see langword="true"/> только если запись прошла в обе схемы (семантика "обе схемы обязательны").
///
/// Дедуп при повторной обработке (вариант B): при частичном сбое в исходный Redis hash пишется маркер
/// с именами успешно записанных схем; при повторе (<c>checkAlreadySaved = true</c>)
/// уже записанные схемы пропускаются. На полностью успешном первом проходе Redis не трогается.
/// </summary>
public class DualRepository : IRepository
{
    private const string PrimaryMarker = "primary";
    private const string SecondaryMarker = "secondary";
    private const string PrimarySchema = "основную";
    private const string SecondarySchema = "вторую";

    private static readonly HashSet<string> EmptyWritten = new();

    private readonly IRepository _primary;
    private readonly IRepository _secondary;
    private readonly ICacheService _cache;
    private readonly ILogger<DualRepository> _logger;

    public DualRepository(IRepository primary,
        IRepository secondary,
        ICacheService cache,
        ILogger<DualRepository> logger)
    {
        _primary = primary;
        _secondary = secondary;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> CreateDlRequest(string hashKey, HashEntry[]? hashset, bool checkAlreadySaved = false)
    {
        var written = await GetWrittenDatabases(hashKey, checkAlreadySaved);

        var primaryOk = written.Contains(PrimaryMarker)
            || await WriteAsync(_primary.CreateDlRequest(hashKey, hashset), PrimarySchema, nameof(CreateDlRequest), hashKey);

        var secondaryOk = written.Contains(SecondaryMarker)
            || await WriteAsync(_secondary.CreateDlRequest(hashKey, hashset), SecondarySchema, nameof(CreateDlRequest), hashKey);

        return await FinalizeAsync(hashKey, checkAlreadySaved, primaryOk, secondaryOk, written);
    }

    public async Task<bool> CreateDlAnswer(string hashKey, HashEntry[]? hashset, bool checkAlreadySaved = false)
    {
        var written = await GetWrittenDatabases(hashKey, checkAlreadySaved);

        var primaryOk = written.Contains(PrimaryMarker)
            || await WriteAsync(_primary.CreateDlAnswer(hashKey, hashset), PrimarySchema, nameof(CreateDlAnswer), hashKey);

        var secondaryOk = written.Contains(SecondaryMarker)
            || await WriteAsync(_secondary.CreateDlAnswer(hashKey, hashset), SecondarySchema, nameof(CreateDlAnswer), hashKey);

        return await FinalizeAsync(hashKey, checkAlreadySaved, primaryOk, secondaryOk, written);
    }

    public async Task CreateDlPut(string hashKey)
    {
        await WriteAsync(_primary.CreateDlPut(hashKey), PrimarySchema, nameof(CreateDlPut), hashKey);
        await WriteAsync(_secondary.CreateDlPut(hashKey), SecondarySchema, nameof(CreateDlPut), hashKey);
    }

    public async Task CreateDlPutAnswer(string hashKey)
    {
        await WriteAsync(_primary.CreateDlPutAnswer(hashKey), PrimarySchema, nameof(CreateDlPutAnswer), hashKey);
        await WriteAsync(_secondary.CreateDlPutAnswer(hashKey), SecondarySchema, nameof(CreateDlPutAnswer), hashKey);
    }

    /// <summary>
    /// Дожидается записи в конкретную схему, изолируя её от другой. Любое исключение или
    /// результат <see langword="false"/> логируется с указанием схемы и операции.
    /// </summary>
    private async Task<bool> WriteAsync(Task<bool> writeTask, string schema, string operation, string hashKey)
    {
        try
        {
            if (await writeTask)
                return true;

            _logger.LogCritical("Не удалось записать в {schema} схему. Операция {operation}, ключ {key}", schema, operation, hashKey);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Ошибка записи в {schema} схему. Операция {operation}, ключ {key}", schema, operation, hashKey);
        }

        return false;
    }

    /// <summary>
    /// Перегрузка для методов без возвращаемого результата (dlput/dlputanswer V2):
    /// изолирует исключение и логирует схему/операцию, не прерывая запись в другую схему.
    /// </summary>
    private async Task WriteAsync(Task writeTask, string schema, string operation, string hashKey)
    {
        try
        {
            await writeTask;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Ошибка записи в {schema} схему. Операция {operation}, ключ {key}", schema, operation, hashKey);
        }
    }

    /// <summary>
    /// Фиксирует результат прохода. На горячем пути (первый проход, обе схемы успешны) Redis не трогается.
    /// Иначе реально записанные в этом проходе схемы помечаются прямо в исходном hash по <paramref name="hashKey"/>,
    /// чтобы повтор не задублировал уже записанное.
    /// </summary>
    private async Task<bool> FinalizeAsync(string hashKey, bool checkAlreadySaved, bool primaryOk, bool secondaryOk, HashSet<string> alreadyWritten)
    {
        var fullSuccess = primaryOk && secondaryOk;

        if (!checkAlreadySaved && fullSuccess)
            return true;

        if (primaryOk && !alreadyWritten.Contains(PrimaryMarker))
            await _cache.AddHashAsync(hashKey, PrimaryMarker, "");

        if (secondaryOk && !alreadyWritten.Contains(SecondaryMarker))
            await _cache.AddHashAsync(hashKey, SecondaryMarker, "");

        return fullSuccess;
    }

    private async Task<HashSet<string>> GetWrittenDatabases(string hashKey, bool checkAlreadySaved)
    {
        if(!checkAlreadySaved)
            return EmptyWritten;
        
        HashSet<string> written = [];
        if(await _cache.HashFieldExists(hashKey, PrimaryMarker))
            written.Add(PrimaryMarker);
        if(await _cache.HashFieldExists(hashKey, SecondaryMarker))
            written.Add(SecondaryMarker);
        return written;
    }
}
