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
    IRepository repositoryV2,
    IRepositoryV3 repositoryV3,
    IConfiguration config,
    string? errorTopic) : ISaverService
    {
        private readonly ILogger<SaverService> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;
        private readonly string? _errorTopic = errorTopic;
        private readonly IProducer<Null, string> _producer = producer;
        private readonly IRepositoryV3 _repositoryV3 = repositoryV3;
        private readonly IRepository _repositoryV2 = repositoryV2;
        private readonly IEnumerable<string> BKIPSRNList = config.GetSection("QBCH").GetChildren().Select(x => x.GetValue<string>("Ogrn") ?? string.Empty);
        private readonly int _DlAnswerExpirationMin = config.GetValue<int>("RedisCache:QBCHRequestExpirationMin");
        private readonly int _DlPutExpirationMin = config.GetValue<int>("RedisCache:DlPutExpirationMin");
        private readonly int _DlPutAnswerExpirationMin = config.GetValue<int>("RedisCache:DlPutAnswerExpirationMin");
        private readonly string? _eventTopic = config.GetValue<string>("Kafka:EventTopic");

        /// <summary>
        /// Имя поля Redis-хэша с версией API
        /// </summary>
        private const string ApiVersionField = "api_version";

        /// <summary>
        /// Имя поля Redis-хэша с версией контракта
        /// </summary>
        private const string ContractVersionField = "contract_version";

        /// <summary>
        /// Имя поля Redis-хэша с XML исходного запроса
        /// </summary>
        private const string RequestXmlField = "request_xml";

        /// <summary>
        /// Имя поля Redis-хэша с XML ответа
        /// </summary>
        private const string ResponseXmlField = "response_xml";

        /// <summary>
        /// Тип ключа Redis для dlrequest
        /// </summary>
        private const string RequestTypeDlRequest = "dlrequest";

        /// <summary>
        /// Тип ключа Redis для dlanswer
        /// </summary>
        private const string RequestTypeDlAnswer = "dlanswer";

        /// <summary>
        /// Тип ключа Redis для dlput
        /// </summary>
        private const string RequestTypeDlPut = "dlput";

        /// <summary>
        /// Тип ключа Redis для dlputanswer
        /// </summary>
        private const string RequestTypeDlPutAnswer = "dlputanswer";

        /// <summary>
        /// Имя поля Redis-хэша с кодом ошибки
        /// </summary>
        private const string ErrorCodeField = "error_code";

        /// <summary>
        /// Имя поля Redis-хэша с датой завершения задачи QBCH
        /// </summary>
        private const string QbchTasksEndDateTimeField = "qbch_tasks_end_date_time";

        /// <summary>
        /// Имя поля Redis-хэша с признаком отмены обработки
        /// </summary>
        private const string CancellationFlagField = "cancellation_flag";

        /// <summary>
        /// Код ошибки QBCH, при котором результат может быть отложенным
        /// </summary>
        private const int QbchErrorCodeWaitingResult = 12;

        /// <summary>
        /// Таймаут повторной обработки сообщения из error topic, в секундах
        /// </summary>
        private const int ErrorTopicHandlerTimeoutSeconds = 120;

        /// <summary>
        /// Пауза между повторными попытками обработки, в миллисекундах
        /// </summary>
        private const int ErrorTopicHandlerRetryDelayMilliseconds = 5000;


        private static bool IsV3(HashEntry[]? hashset)
        {
            if (hashset is null)
                return false;

            var apiVersion = hashset.FirstOrDefault(x => x.Name == ApiVersionField).Value.ToString();
            var contractVersion = hashset.FirstOrDefault(x => x.Name == ContractVersionField).Value.ToString();

            if (IsVersion3X(apiVersion) || IsVersion3X(contractVersion))
                return true;

            var xml = hashset.FirstOrDefault(x => x.Name == RequestXmlField).Value.ToString();
            xml = string.IsNullOrWhiteSpace(xml)
                ? hashset.FirstOrDefault(x => x.Name == ResponseXmlField).Value.ToString()
                : xml;

            return !string.IsNullOrWhiteSpace(xml) && xml.Contains("Версия=\"3.", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsVersion3X(string? version)
    => !string.IsNullOrWhiteSpace(version) && version.StartsWith("3.", StringComparison.Ordinal);

        private async Task ProduceRequestXmlIfExists(HashEntry[] hashset, string key)
        {
            var requestXml = hashset.FirstOrDefault(x => x.Name == RequestXmlField);
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
                case RequestTypeDlRequest:

                    var ErrorCode = (int)hashset.FirstOrDefault(x => x.Name == ErrorCodeField).Value;

                    if (!(ErrorCode == QbchErrorCodeWaitingResult && !hashset.Any(x => x.Name == QbchTasksEndDateTimeField)))
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
                        else
                        {
                            if (await _repositoryV2.CreateDlRequest(key, hashset))
                            {
                                await ProduceRequestXmlIfExists(hashset, key);

                                await _cacheService.ClearDLRequestHash(key, hashset, BKIPSRNList);
                                return;
                            }
                            break;
                        }
                    }
                    break;

                case RequestTypeDlAnswer:

                    if (hashset is null)
                        break;

                    if (IsV3(hashset))
                    {
                        if (await _repositoryV3.CreateDlAnswerV3(key, hashset))
                        {
                            await _cacheService.SetKeyExpirationInMinutes(key, _DlAnswerExpirationMin);
                            return;
                        }
                        break;
                    }

                    if (await _repositoryV2.CreateDlAnswer(key, hashset))
                    {
                        await _cacheService.SetKeyExpirationInMinutes(key, _DlAnswerExpirationMin);
                        return;
                    }

                    break;

                case RequestTypeDlPut:
                    if (IsV3(hashset) && await _repositoryV3.CreateDlPutV3(key, hashset))
                    {
                        await _cacheService.SetKeyExpirationInMinutes(key, _DlPutExpirationMin);
                        return;
                    }
                    break;

                case RequestTypeDlPutAnswer:
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
                case RequestTypeDlRequest:

                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ErrorTopicHandlerTimeoutSeconds));
                    bool IsCancelled = false;
                    HashEntry[]? hashset = null;

                    do
                    {
                        hashset = await _cacheService.TryGetHashAll(key);

                        if (hashset is not null)
                        {
                            var ErrorCode = (int)hashset.FirstOrDefault(x => x.Name == ErrorCodeField).Value;

                            if (!(ErrorCode == QbchErrorCodeWaitingResult && !hashset.Any(x => x.Name == QbchTasksEndDateTimeField)))
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

                                if (await _repositoryV2.CreateDlRequest(key, hashset))
                                {
                                    await ProduceRequestXmlIfExists(hashset, key);

                                    await _cacheService.ClearDLRequestHash(key, hashset, BKIPSRNList);
                                    return;
                                }
                            }

                            IsCancelled = hashset.Any(x => x.Name == CancellationFlagField);
                        }

                        await Task.Delay(ErrorTopicHandlerRetryDelayMilliseconds);
                    }
                    while (!IsCancelled || !cts.IsCancellationRequested);

                    break;

                case RequestTypeDlAnswer:
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

                        if (await _repositoryV2.CreateDlAnswer(key, hashset))
                        {
                            await _cacheService.SetKeyExpirationInMinutes(key, _DlAnswerExpirationMin);
                            return;
                        }
                    }

                    break;

                case RequestTypeDlPut:
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

                case RequestTypeDlPutAnswer:
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
                    _logger.LogError("Сообщение не доставлено до EventTopic {key}", key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Ошибка записи EventTopic.");
            }
        }
    }
}