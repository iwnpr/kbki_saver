using cache_lib.Interfaces;
using StackExchange.Redis;
using System.Diagnostics.CodeAnalysis;

namespace qbch_db_saver.test.moq;

internal class MoqCahseService : ICacheService
{
    private readonly HashEntry[] _hashEntries = [];
    public MoqCahseService(HashEntry[] setupData)
    {
        _hashEntries = setupData;
    }

    public void AddHash(string methodName, string pKey, string pField, byte[] pData, int? dbIndex = null)
    {
        throw new NotImplementedException();
    }

    public void AddHash(string methodName, string pKey, string pField, string pData, int? dbIndex = null)
    {
        throw new NotImplementedException();
    }

    public void AddUniqueRequestId(string methodName, string requestId, string ogrn, DateTime? requestDate = null, int? dbIndex = null)
    {
        throw new NotImplementedException();
    }

    public Task ClearDLRequestHash(string key, HashEntry[] hashset, IEnumerable<string> QBCH)
    {
        throw new NotImplementedException();
    }

    public Task DeleteKeyExpiration(string key)
    {
        throw new NotImplementedException();
    }

    public IDatabase GetDatabase()
    {
        throw new NotImplementedException();
    }

    public Task<int> GetIntHashValueAsync(string hashKey, string fieldKey)
    {
        throw new NotImplementedException();
    }

    public Task<string?> GetStringHashValueAsync(string hashKey, string fieldKey)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HashFieldExists(string pKey, string fieldName)
    {
        throw new NotImplementedException();
    }

    public bool IsUniqueRequestId(string requestId, string ogrn, string methodName, int? dbIndex = null)
    {
        throw new NotImplementedException();
    }

    public bool KeyExists(string[] keys, int? dbIndex = null)
    {
        throw new NotImplementedException();
    }

    public Task SetKeyExpirationInMinutes(string key, int expirationInHours)
    {
        throw new NotImplementedException();
    }

    public bool TryGetHash(string key, string pField, [NotNullWhen(true)] out byte[]? bytes, int? dbIndex = null)
    {
        throw new NotImplementedException();
    }

    public Task<HashEntry[]> TryGetHashAll(string pKey, int? dbIndex = null)
    {
        throw new NotImplementedException();
    }

    public bool TryGetIntValue(string methodName, string key, string field, [NotNullWhen(true)] out int result, int? dbIndex = null)
    {
        throw new NotImplementedException();
    }
}
