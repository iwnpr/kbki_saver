using cache_lib.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace cache_lib.Implementations
{
    /// <summary>
    /// 
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<CacheService> _log;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IDatabase _redisDb;
        private static IDatabase _additionDB = null!;
        private readonly int _expirityHours;
        private readonly int _uniqueIdExpirityDays;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public CacheService(IConfiguration config, ILogger<CacheService> logger, IConnectionMultiplexer connectionMultiplexer)
        {
            _config = config;
            _log = logger;
            _connectionMultiplexer = connectionMultiplexer;
            _redisDb = _connectionMultiplexer.GetDatabase(int.Parse(_config.GetSection("RedisCache:DBIndex").Value!));
            _additionDB = _redisDb;
        }

        /// <summary>
        /// Получить БД для записи по id
        /// </summary>
        /// <param name="id">Идентифкатор БД</param>
        /// <returns>База данных по id</returns>
        private void GetDatabase(int id)
        {
            _additionDB = _connectionMultiplexer.GetDatabase(id);
        }

        /// <summary>
        /// Установить значение в redis Hash
        /// </summary>
        /// <param name="pKey">Ключ</param>
        /// <param name="pField">Поле</param>
        /// <param name="pData">Данные</param>
        /// <param name="dbIndex">id БД</param>
        public void AddHash(string methodName, string pKey, string pField, string pStrData, int? dbIndex = null)
        {
            AddHash(methodName, pKey, pField, Encoding.UTF8.GetBytes(pStrData), dbIndex);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="pKey"></param>
        /// <param name="pField"></param>
        /// <param name="pData"></param>
        /// <param name="dbIndex"></param>
        public void AddHash(string methodName, string pKey, string pField, byte[] pData, int? dbIndex = null)
        {
            try
            {
                var key = KeyFormatter([methodName, pKey]);

                if (dbIndex.HasValue)
                {
                    GetDatabase(dbIndex.Value);
                    _additionDB.HashSet(key, [new(pField, pField == "SignedRequest" ? pData : Encoding.UTF8.GetString(pData))]);
                }
                else
                {
                    _redisDb.HashSet(key, [new(pField, pField == "SignedRequest" ? pData : Encoding.UTF8.GetString(pData))]);
                }

                _redisDb.KeyExpireAsync(key, TimeSpan.FromHours(_expirityHours));
                _log.LogDebug("Redis add cache db key: {pGuid}", pKey);
            }
            catch (Exception e)
            {
                _log.LogCritical("Redis critical: {Message}", e.Message);
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task DeleteKeyExpiration(string key)
        {
            await _redisDb.KeyPersistAsync(key);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task SetKeyExpiration(string key, int expirationTime)
        {
            await _redisDb.KeyExpireAsync(key, TimeSpan.FromMinutes(expirationTime));
        }

        /// <summary>
        /// Формирование ключа для сохранения в redis
        /// </summary>
        /// <param name="keyParts">Список частей ключа</param>
        /// <returns></returns>
        private static string KeyFormatter(string[] keyParts)
        {
            return $"QBCH:{string.Join(':', keyParts)}";
        }

        /// <summary>
        /// Получить значение из redis hash
        /// </summary>
        /// <param name="pKey">Ключ</param>
        /// <param name="pField">Поле</param>
        /// <param name="dbIndex">id БД</param>
        /// <returns></returns>
        public bool TryGetHash(string key, string pField, [NotNullWhen(true)] out byte[]? bytes, int? dbIndex = null)
        {
            if (dbIndex.HasValue)
            {
                GetDatabase(dbIndex.Value);
                bytes = _additionDB.HashGet(key, pField);
            }
            else
            {
                bytes = _redisDb.HashGet(key, pField);
            }

            if (bytes is null || bytes.Length == 0)
            {
                bytes = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Метод преобразующий полученные из redis данные в указанный тип
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hashKey"></param>
        /// <param name="fieldKey"></param>
        /// <returns></returns>
        public async Task<int?> GetHashSetValueAsync(string hashKey, string fieldKey)
        {
            var value = await _redisDb.HashGetAsync(hashKey, fieldKey);

            if (int.TryParse(value, out var result))
                return result;

            return null;
        }


        public async Task<HashEntry[]> TryGetHashAll(string pKey, int? dbIndex = null)
        {
            if (dbIndex.HasValue)
            {
                GetDatabase(dbIndex.Value);
                return await _additionDB.HashGetAllAsync(pKey);
            }
            else
            {
                return await _redisDb.HashGetAllAsync(pKey);
            }
        }

        /// <summary>
        /// Провекра существования ключа в БД
        /// </summary>
        /// <param name="key"></param>
        /// <param name="dbIndex"></param>
        /// <returns></returns>
        public bool KeyExists(string[] keys, int? dbIndex = null)
        {
            if (dbIndex.HasValue)
            {
                GetDatabase(dbIndex.Value);
                return _additionDB.KeyExists(KeyFormatter(keys));
            }
            return _redisDb.KeyExists(KeyFormatter(keys));
        }

        /// <summary>
        /// Устновка значения уникального request id в рамках организации
        /// </summary>
        /// <param name="requestId">Id Запроса</param>
        /// <param name="inn">ИНН</param>
        /// <param name="ogrn">ОГРН</param>
        /// <param name="requestDate">Дата запроса</param>
        public void AddUniqueRequestId(string methodName, string requestId, string ogrn, DateTime? requestDate = null, int? dbIndex = null)
        {
            var key = KeyFormatter(new[] { methodName, ogrn, requestId });

            if (dbIndex.HasValue)
            {
                GetDatabase(dbIndex.Value);
                _additionDB.SetAdd(key, requestDate?.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy"));
                _additionDB.KeyExpireAsync(key, DateTime.Today.AddDays(int.Parse(_config.GetSection("RedisCache:RequestIdUniqueDays").Value ?? "1")));
                return;
            }

            _redisDb.SetAdd(key, requestDate?.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy"));
            _redisDb.KeyExpireAsync(key, DateTime.Today.AddDays(_uniqueIdExpirityDays));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryGetIntValue(string methodName, string key, string field, [NotNullWhen(true)] out int result, int? dbIndex = null)
        {
            if (dbIndex.HasValue)
            {
                GetDatabase(dbIndex.Value);
                return _additionDB.HashGet(KeyFormatter(new[] { methodName, key }), field).TryParse(out result);
            }
            else
                return _redisDb.HashGet(KeyFormatter(new[] { methodName, key }), field).TryParse(out result);
        }

        /// <summary>
        /// Валидация уникальности requestid в течение календароного дня
        /// </summary>
        /// <param name="pKey"></param>
        /// <param name="requestDate"></param>
        /// <returns></returns>
        public bool IsUniqueRequestId(string requestId, string ogrn, string methodName, int? dbIndex = null)
        {
            return !KeyExists(new[] { methodName, ogrn, requestId });
        }

        public async Task<bool> HashFieldExists(string pKey, string fieldName)
        {
            return await _redisDb.HashExistsAsync(pKey, fieldName);
        }
    }
}
