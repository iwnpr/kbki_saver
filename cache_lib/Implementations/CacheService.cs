using cache_lib.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
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
        private readonly int _DlRequestExpirationMin;
        private readonly int _DlAnswerExpirationMin;

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
            _DlRequestExpirationMin = _config.GetValue<int>("RedisCache:DlRequestExpirationMin");
            _DlAnswerExpirationMin = _config.GetValue<int>("RedisCache:QBCHRequestExpirationMin");
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

        public IDatabase GetDatabase()
        {
            return _redisDb;
        }

        public async Task AddHashAsync(string key, string field, string data)
        {
            try
            {
                await _redisDb.HashSetAsync(key, [new(field, data)]);
            }
            catch (Exception e)
            {
                _log.LogWarning(e, "Ошибка добавления ключа в Redis {key}, поле {field}", key, field);
                throw;
            }
        }

        public async Task ClearDLRequestHash(string key, HashEntry[] hashset, IEnumerable<string> QBCHPSRNList)
        {
            try
            {                
                //foreach (var hash in hashset)
                //{
                //    if (hash.Name != "qbch_tasks_aggregate_xml")
                //        await _redisDb.HashDeleteAsync(key, hash.Name);
                //}
                
                await SetKeyExpirationInMinutes(key, _DlRequestExpirationMin);

                foreach (var psrn in QBCHPSRNList)
                {

                    await SetKeyExpirationInMinutes($"{key}:{psrn}", _DlAnswerExpirationMin);

                    if(hashset.Any(x => x.Name == "package_error"))
                        await SetKeyExpirationInMinutes($"{key}:{psrn}:package_error", _DlAnswerExpirationMin);

                    await SetKeyExpirationInMinutes($"{key}:{psrn}:dlrequest", _DlAnswerExpirationMin);
                    await SetKeyExpirationInMinutes($"{key}:{psrn}:dlanswer", _DlAnswerExpirationMin);
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Не удалось почистить данные в redis");
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
        public async Task SetKeyExpirationInMinutes(string key, int expirationTime)
        {
            try
            {
                await _redisDb.KeyExpireAsync(key, TimeSpan.FromMinutes(expirationTime));
            }
            catch(Exception ex)
            {
                _log.LogWarning(ex, "Не удалось почистить данные в redis");
            }
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
        public async Task<int> GetIntHashValueAsync(string hashKey, string fieldKey)
        {
            return (int?)await _redisDb.HashGetAsync(hashKey, fieldKey) ?? 0;
        }

        /// <summary>
        /// Метод преобразующий полученные из redis данные в указанный тип
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hashKey"></param>
        /// <param name="fieldKey"></param>
        /// <returns></returns>
        public async Task<string?> GetStringHashValueAsync(string hashKey, string fieldKey)
        {
            return await _redisDb.HashGetAsync(hashKey, fieldKey);
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
