using StackExchange.Redis;

namespace db_lib.Services.Interfaces.V3;

public interface IRepositoryV3
{
    Task<bool> CreateDlRequestV3(string hashKey, HashEntry[]? hashset);
    Task<bool> CreateDlAnswerV3(string hashKey, HashEntry[]? hashset);
    Task<bool> CreateDlPutV3(string hashKey, HashEntry[]? hashset);
    Task<bool> CreateDlPutAnswerV3(string hashKey, HashEntry[]? hashset);
}