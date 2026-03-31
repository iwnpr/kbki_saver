using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace db_lib.Services.Interfaces
{
    public interface IRepository
    {
        Task<bool> CreateDlRequest(string HaskKey, HashEntry[]? hashset);

        Task<bool> CreateDlAnswer(string HaskKey, HashEntry[]? hashset);

        Task CreateDlPut(string HaskKey);

        Task CreateDlPutAnswer(string HaskKey);        
    }
}
