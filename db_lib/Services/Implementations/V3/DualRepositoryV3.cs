using cache_lib.Interfaces;
using db_lib.Services.Interfaces.V3;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace db_lib.Services.Implementations.V3;

public class DualRepositoryV3 : IRepositoryV3
{
    private const string PrimaryMarker = "primary";
    private const string SecondaryMarker = "secondary";
    private const string PrimarySchema = "основную";
    private const string SecondarySchema = "вторую";

    private static readonly HashSet<string> EmptyWritten = new();

    private readonly IRepositoryV3 _primary;
    private readonly IRepositoryV3 _secondary;
    private readonly ICacheService _cache;
    private readonly ILogger<DualRepositoryV3> _logger;

    public DualRepositoryV3(IRepositoryV3 primary,
        IRepositoryV3 secondary,
        ICacheService cache,
        ILogger<DualRepositoryV3> logger)
    {
        _primary = primary;
        _secondary = secondary;
        _cache = cache;
        _logger = logger;
    }
    
    public async Task<bool> CreateDlRequestV3(string hashKey, HashEntry[]? hashset, bool checkAlreadySaved = false)
    {
        var written = await GetWrittenDatabases(hashKey, checkAlreadySaved);

        var primaryOk = written.Contains(PrimaryMarker)
            || await WriteAsync(_primary.CreateDlRequestV3(hashKey, hashset), PrimarySchema, nameof(CreateDlRequestV3), hashKey);

        var secondaryOk = written.Contains(SecondaryMarker)
            || await WriteAsync(_secondary.CreateDlRequestV3(hashKey, hashset), SecondarySchema, nameof(CreateDlRequestV3), hashKey);

        return await FinalizeAsync(hashKey, checkAlreadySaved, primaryOk, secondaryOk, written);
    }

    public async Task<bool> CreateDlAnswerV3(string hashKey, HashEntry[]? hashset, bool checkAlreadySaved = false)
    {
        var written = await GetWrittenDatabases(hashKey, checkAlreadySaved);

        var primaryOk = written.Contains(PrimaryMarker)
            || await WriteAsync(_primary.CreateDlAnswerV3(hashKey, hashset), PrimarySchema, nameof(CreateDlAnswerV3), hashKey);

        var secondaryOk = written.Contains(SecondaryMarker)
            || await WriteAsync(_secondary.CreateDlAnswerV3(hashKey, hashset), SecondarySchema, nameof(CreateDlAnswerV3), hashKey);

        return await FinalizeAsync(hashKey, checkAlreadySaved, primaryOk, secondaryOk, written);
    }

    public async Task<bool> CreateDlPutV3(string hashKey, HashEntry[]? hashset, bool checkAlreadySaved = false)
    {
        var written = await GetWrittenDatabases(hashKey, checkAlreadySaved);

        var primaryOk = written.Contains(PrimaryMarker)
            || await WriteAsync(_primary.CreateDlPutV3(hashKey, hashset), PrimarySchema, nameof(CreateDlPutV3), hashKey);

        var secondaryOk = written.Contains(SecondaryMarker)
            || await WriteAsync(_secondary.CreateDlPutV3(hashKey, hashset), SecondarySchema, nameof(CreateDlPutV3), hashKey);

        return await FinalizeAsync(hashKey, checkAlreadySaved, primaryOk, secondaryOk, written);
    }

    public async Task<bool> CreateDlPutAnswerV3(string hashKey, HashEntry[]? hashset, bool checkAlreadySaved = false)
    {
        var written = await GetWrittenDatabases(hashKey, checkAlreadySaved);

        var primaryOk = written.Contains(PrimaryMarker)
            || await WriteAsync(_primary.CreateDlPutAnswerV3(hashKey, hashset), PrimarySchema, nameof(CreateDlPutAnswerV3), hashKey);

        var secondaryOk = written.Contains(SecondaryMarker)
            || await WriteAsync(_secondary.CreateDlPutAnswerV3(hashKey, hashset), SecondarySchema, nameof(CreateDlPutAnswerV3), hashKey);

        return await FinalizeAsync(hashKey, checkAlreadySaved, primaryOk, secondaryOk, written);
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
