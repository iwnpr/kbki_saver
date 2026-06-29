using StackExchange.Redis;

namespace db_lib.Services.Interfaces
{
    public interface IRepository
    {
        Task<bool> CreateDlRequest(string HaskKey, HashEntry[]? hashset, bool checkAlreadySaved = false);

        Task<bool> CreateDlAnswer(string HaskKey, HashEntry[]? hashset, bool checkAlreadySaved = false);

        Task CreateDlPut(string HaskKey);

        Task CreateDlPutAnswer(string HaskKey);        
    }
}
