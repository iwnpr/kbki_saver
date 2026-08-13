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
    IRepositoryV3 repositoryV3,
    IConfiguration config,
    string? errorTopic) : ISaverService
    {
        private readonly ILogger<SaverService> _logger = logger;
        private readonly ICacheService _cacheService = cacheService;
        private readonly string? _errorTopic = errorTopic;
        private readonly IProducer<Null, string> _producer = producer;
        private readonly IRepositoryV3 _repositoryV3 = repositoryV3;
        private readonly IEnumerable<string> BKIPSRNList = config.GetSection("QBCH").GetChildren().Select(x => x.GetValue<string>("Ogrn") ?? string.Empty);
        private readonly int _DlAnswerExpirationMin = config.GetValue<int>("RedisCache:QBCHRequestExpirationMin");
        private readonly int _DlPutExpirationMin = config.GetValue<int>("RedisCache:DlPutExpirationMin");
        private readonly int _DlPutAnswerExpirationMin = config.GetValue<int>("RedisCache:DlPutAnswerExpirationMin");
        private readonly string? _eventTopic = config.GetValue<string>("Kafka:EventTopic");
        private readonly string? _dlqTopic = config.GetValue<string>("Kafka:DlqTopic");

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
        /// Проверяем условия возможноси записи в БД
        /// </summary>
        /// <param name="hashset">Хэш из Redis с данными запроса</param>
        /// <returns>true, если условия записи в БД выполняются, иначе - false</returns>
        private static bool IsReadyToSave(HashEntry[] hashset)
        {
            bool hasEndDateTime = hashset.Any(x => x.Name == QbchTasksEndDateTimeField);
            bool hasErrorCode = hashset.Any(x => x.Name == ErrorCodeField);

            if (!hasEndDateTime && !hasErrorCode)
                return false;

            if (hasErrorCode && !hasEndDateTime)
            {
                var errorCode = (int)hashset.First(x => x.Name == ErrorCodeField).Value;
                if (errorCode == QbchErrorCodeWaitingResult)
                    return false;
            }

            return true;
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

            if (hashset is null || hashset.Length == 0)
                _logger.LogWarning("Не удалось найти ключ в кеше {key}", key);
            else
                _logger.LogDebug("Данные из Redis: {hashset}, {key}", string.Join("; ", hashset.Select(x => $"{x.Name}={x.Value}")), key);

            switch (redisKey[1])
            {
                case RequestTypeDlRequest:

                    if (!IsReadyToSave(hashset))
                        break;

                    if (await _repositoryV3.CreateDlRequestV3(key, hashset))
                    {
                        await _cacheService.ClearDLRequestHash(key, hashset, BKIPSRNList);
                        return;
                    }
                    break;

                case RequestTypeDlAnswer:

                    if (hashset is null)
                        break;

                    if (await _repositoryV3.CreateDlAnswerV3(key, hashset))
                    {
                        await _cacheService.SetKeyExpirationInMinutes(key, _DlAnswerExpirationMin);
                        return;
                    }
                    break;

                case RequestTypeDlPut:

                    if (await _repositoryV3.CreateDlPutV3(key, hashset))
                    {
                        await _cacheService.SetKeyExpirationInMinutes(key, _DlPutExpirationMin);
                        return;
                    }
                    break;

                case RequestTypeDlPutAnswer:

                    if (await _repositoryV3.CreateDlPutAnswerV3(key, hashset))
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

                        if (hashset is null || hashset.Length == 0)
                        {
                            _logger.LogWarning("Не удалось найти ключ в кеше {key}", key);
                            await SendToDlqAsync(key);
                            return;
                        }

                        if (hashset is not null)
                        {
                            _logger.LogDebug("Данные из Redis: {hashset}, {key}", string.Join("; ", hashset.Select(x => $"{x.Name}={x.Value}")), key);

                            if (IsReadyToSave(hashset))
                            {

                                if (await _repositoryV3.CreateDlRequestV3(key, hashset, checkAlreadySaved: true))
                                {
                                    await _cacheService.ClearDLRequestHash(key, hashset, BKIPSRNList);
                                    return;
                                }
                                break;
                            }

                            IsCancelled = hashset.Any(x => x.Name == CancellationFlagField);
                        }

                        await Task.Delay(ErrorTopicHandlerRetryDelayMilliseconds);
                    }

                    // завершаем при появлении cancellation_flag, либо по таймауту
                    while (!IsCancelled && !cts.IsCancellationRequested);

                    break;

                case RequestTypeDlAnswer:
                    hashset = await _cacheService.TryGetHashAll(key);

                    if (hashset is null || hashset.Length == 0)
                    {
                        _logger.LogWarning("Не удалось найти ключ в кеше {key}", key);
                        await SendToDlqAsync(key);
                        return;
                    }

                    if (hashset is not null)
                    {

                        _logger.LogDebug("Данные из Redis: {hashset}, {key}", string.Join("; ", hashset.Select(x => $"{x.Name}={x.Value}")), key);


                        if (await _repositoryV3.CreateDlAnswerV3(key, hashset, checkAlreadySaved: true))
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
                    break;

                case RequestTypeDlPutAnswer:
                    hashset = await _cacheService.TryGetHashAll(key);
                    if (hashset is null)
                    {
                        _logger.LogError("Redis hash не найден для ключа {key}", key);
                        break;
                    }
                    break;

                default:
                    break;
            }

            await SendToDlqAsync(key);
        }

        /// <summary>
        /// Отправка необработанного ключа в DLQ-топик (тупиковая очередь для дальнейшего ручного разбора).
        /// Имя топика берётся из конфига Kafka:DlqTopic. Если топик не задан — фиксируется только потеря в логе.
        /// </summary>
        /// <param name="key">Ключ Redis, который не удалось обработать</param>
        private async Task SendToDlqAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(_dlqTopic))
            {
                _logger.LogCritical("DLQ-топик не настроен — Kafka:DlqTopic", key);
                return;
            }

            try
            {
                var result = await _producer.ProduceAsync(_dlqTopic, new() { Value = key });

                if (result.Status == PersistenceStatus.NotPersisted)
                    _logger.LogCritical("Lost QBCH key: {key} — не удалось доставить в DLQ {dlq}", key, _dlqTopic);
                else
                    _logger.LogCritical("Lost QBCH key: {key} — отправлен в DLQ {dlq}", key, _dlqTopic);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Ошибка при обработки DQL", key, _dlqTopic);
            }
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