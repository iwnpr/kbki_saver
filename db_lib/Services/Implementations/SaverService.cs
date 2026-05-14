using cache_lib.Interfaces;
using Confluent.Kafka;
using db_lib.Services.Interfaces;
using db_lib.Services.Interfaces.V3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace db_lib.Services.Implementations
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="db"></param>
    /// <param name="cacheService"></param>
    /// <param name="QBCH"></param>
    /// <param name="logger"></param>
    public partial class SaverService(
    ICacheService cacheService,
    ILogger<SaverService> logger,
    IProducer<Null, string> producer,
    IRepository repository,
    IRepositoryV3 repositoryV3,
    IConfiguration config,
    string? errorTopic) : ISaverService
    {
        private readonly ILogger<SaverService> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;
        private readonly string? _errorTopic = errorTopic;
        private readonly IProducer<Null, string> _producer = producer;
        private readonly IRepositoryV3 _repositoryV3 = repositoryV3;
        private readonly IRepository _repository = repository;
        private readonly IEnumerable<string> BKIPSRNList = config.GetSection("QBCH").GetChildren().Select(x => x.GetValue<string>("Ogrn") ?? string.Empty);
        private readonly int _DlAnswerExpirationMin = config.GetValue<int>("RedisCache:QBCHRequestExpirationMin");
        private readonly int _DlPutExpirationMin = config.GetValue<int>("RedisCache:DlPutExpirationMin");
        private readonly int _DlPutAnswerExpirationMin = config.GetValue<int>("RedisCache:DlPutAnswerExpirationMin");
        private readonly string? _eventTopic = config.GetValue<string>("Kafka:EventTopic");


        private static bool IsV3(HashEntry[]? hashset)
        {
            if (hashset is null)
                return false;

            var apiVersion = hashset.FirstOrDefault(x => x.Name == "api_version").Value.ToString();
            var contractVersion = hashset.FirstOrDefault(x => x.Name == "contract_version").Value.ToString();

            if (IsVersion3X(apiVersion) || IsVersion3X(contractVersion))
                return true;

            var xml = hashset.FirstOrDefault(x => x.Name == "request_xml").Value.ToString();
            xml = string.IsNullOrWhiteSpace(xml)
                ? hashset.FirstOrDefault(x => x.Name == "response_xml").Value.ToString()
                : xml;

            return !string.IsNullOrWhiteSpace(xml) && xml.Contains("Версия=\"3.", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsVersion3X(string? version)
    => !string.IsNullOrWhiteSpace(version) && version.StartsWith("3.", StringComparison.Ordinal);

        private async Task ProduceRequestXmlIfExists(HashEntry[] hashset, string key)
        {
            var requestXml = hashset.FirstOrDefault(x => x.Name == "request_xml");
            if (!requestXml.Name.HasValue || requestXml.Value.IsNullOrEmpty)
                return;

            await ProduceToEventTopic(requestXml.Value.ToString(), key);
        }

        public async Task SaveCriticalError(string key)
        {
            var json = JsonSerializer.Deserialize<ApplicationError>(key);

            if (json is null)
            {
                _logger.LogError("Ошибка десериализапции json {key}", key);
                return;
            }

            //await ExecuteSaving(key, json.ServiceName, json.guid, json);
        }

        /// <summary>
        /// Обработка 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task TopicHandler(string key)
        {
            var redisKey = key.Split(':');
            HashEntry[]? hashset = await _cacheService.TryGetHashAll(key);

            switch (redisKey[1])
            {
                case "dlrequest":

                    if (IsV3(hashset))
                    {
                        if (await _repositoryV3.CreateDlRequestV3(key, hashset))
                        {
                            await ProduceRequestXmlIfExists(hashset, key);

                            await _cacheService.ClearDLRequestHash(key, hashset, BKIPSRNList);
                            return;
                        }
                        break;
                    }
                    var ErrorCode = (int)hashset.FirstOrDefault(x => x.Name == "error_code").Value;

                    if (!(ErrorCode == 12 && !hashset.Any(x => x.Name == "qbch_tasks_end_date_time")))
                    {
                        if (await _repository.CreateDlRequest(key, hashset))
                        {
                            await ProduceRequestXmlIfExists(hashset, key);

                            await _cacheService.ClearDLRequestHash(key, hashset, BKIPSRNList);
                            return;
                        }
                    }
                    break;

                case "dlanswer":
                    if (hashset is not null)
                    {
                        if (IsV3(hashset))
                        {
                            if (await _repositoryV3.CreateDlAnswerV3(key, hashset))
                            {
                                await _cacheService.SetKeyExpirationInMinutes(key, _DlAnswerExpirationMin);
                                return;
                            }
                            break;
                        }

                        if (await _repository.CreateDlAnswer(key, hashset))
                        {
                            await _cacheService.SetKeyExpirationInMinutes(key, _DlAnswerExpirationMin);
                            return;
                        }
                    }
                    break;

                case "dlput":
                    if (IsV3(hashset) && await _repositoryV3.CreateDlPutV3(key, hashset))
                    {
                        await _cacheService.SetKeyExpirationInMinutes(key, _DlPutExpirationMin);
                        return;
                    }
                    break;

                case "dlputanswer":
                    if (IsV3(hashset) && await _repositoryV3.CreateDlPutAnswerV3(key, hashset))
                    {
                        await _cacheService.SetKeyExpirationInMinutes(key, _DlPutAnswerExpirationMin);
                        return;
                    }
                    break;

                default:
                    break;
            }

            await _producer.ProduceAsync(_errorTopic, new() { Value = key });
        }

        public async Task ErrorTopicHandler(string key)
        {
            var redisKey = key.Split(':');

            switch (redisKey[1])
            {
                case "dlrequest":

                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
                    bool IsCancelled = false;
                    HashEntry[]? hashset = null;

                    do
                    {
                        hashset = await _cacheService.TryGetHashAll(key);

                        if (hashset is not null)
                        {
                            var ErrorCode = (int)hashset.FirstOrDefault(x => x.Name == "error_code").Value;

                            if (!(ErrorCode == 12 && !hashset.Any(x => x.Name == "qbch_tasks_end_date_time")))
                            {
                                if (IsV3(hashset))
                                {
                                    if (await _repositoryV3.CreateDlRequestV3(key, hashset))
                                    {

                                        await ProduceRequestXmlIfExists(hashset, key);

                                        await _cacheService.ClearDLRequestHash(key, hashset, BKIPSRNList);
                                        return;
                                    }
                                    break;
                                }

                                if (await _repository.CreateDlRequest(key, hashset))
                                {
                                    await ProduceRequestXmlIfExists(hashset, key);

                                    await _cacheService.ClearDLRequestHash(key, hashset, BKIPSRNList);
                                    return;
                                }
                            }

                            IsCancelled = hashset.Any(x => x.Name == "cancellation_flag");
                        }

                        await Task.Delay(5000);
                    }
                    while (!IsCancelled || !cts.IsCancellationRequested);

                    break;
                case "dlanswer":
                    hashset = await _cacheService.TryGetHashAll(key);

                    if (hashset is not null)
                    {
                        if (IsV3(hashset))
                        {
                            if (await _repositoryV3.CreateDlAnswerV3(key, hashset))
                            {
                                await _cacheService.SetKeyExpirationInMinutes(key, _DlAnswerExpirationMin);
                                return;
                            }
                        }

                        if (await _repository.CreateDlAnswer(key, hashset))
                        {
                            await _cacheService.SetKeyExpirationInMinutes(key, _DlAnswerExpirationMin);
                            return;
                        }
                    }

                    break;

                case "dlput":
                    hashset = await _cacheService.TryGetHashAll(key);
                    if (hashset is null)
                    {
                        _logger.LogError("Redis hash не найден для ключа {key}", key);
                        break;
                    }
                    if (IsV3(hashset) && await _repositoryV3.CreateDlPutV3(key, hashset))
                    {
                        await _cacheService.SetKeyExpirationInMinutes(key, _DlPutExpirationMin);
                        return;
                    }
                    break;

                case "dlputanswer":
                    hashset = await _cacheService.TryGetHashAll(key);
                    if (hashset is null)
                    {
                        _logger.LogError("Redis hash не найден для ключа {key}", key);
                        break;
                    }
                    if (IsV3(hashset) && await _repositoryV3.CreateDlPutAnswerV3(key, hashset))
                    {
                        await _cacheService.SetKeyExpirationInMinutes(key, _DlPutAnswerExpirationMin);
                        return;
                    }
                    break;

                default:
                    break;
            }

            _logger.LogCritical("Lost QBCH key: {key}", key);
        }

        /// <summary>
        /// Отправка в EventTopic
        /// </summary>
        /// <param name="request_xml">Значение</param>
        /// <param name="key">Ключ redis'а</param>
        /// <returns>Task result</returns>
        private async Task ProduceToEventTopic(string? request_xml, string key)
        {
            // Проверка заполненности event topic
            if (string.IsNullOrWhiteSpace(_eventTopic))
            {
                _logger.LogDebug("EventTopic пустой");
                return;
            }

            // Проверка наличия значения
            if (string.IsNullOrWhiteSpace(request_xml))
            {
                _logger.LogError("request_xml пустой");
                return;
            }

            try
            {
                var result = await _producer.ProduceAsync(_eventTopic, new() { Value = request_xml });

                if (result.Status == PersistenceStatus.NotPersisted)
                {
                    _logger.LogError("Сообщение не доставлено до брокера {key}", key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Ошибка produce в кафку. EventTopic.");
            }
        }
    }
}