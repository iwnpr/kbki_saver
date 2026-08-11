using Confluent.Kafka;
using db_lib.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace qbch_db_saver_sdc
{
    internal class QBCHSaverBackgroundService(IConfiguration configuration, ILogger<QBCHSaverBackgroundService> logger, IServiceProvider serviceProvider) : BackgroundService
    {
        private readonly IConfiguration _config = configuration;
        private readonly ILogger<QBCHSaverBackgroundService> _logger = logger;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var bootstrapServers = _config.GetValue<string>("Kafka:BootstrapServers");
            var errorTopic = _config.GetValue<string>("Kafka:ErrorTopic");
            var topic = _config.GetSection("Kafka:Topic").Value;
            var isErrorApp = _config.GetValue<bool>("App:IsErrorApp");
            var eventTopic = _config.GetValue<string>("Kafka:EventTopic");
            var groupId = _config.GetValue<string>("Kafka:GroupId");
            Console.Title = isErrorApp ? $"IsError: {isErrorApp} | Topic: {errorTopic} | Event topic: {eventTopic}" : $"IsError: {isErrorApp} | Topic: {topic} | Event topic: {eventTopic}";

            // Создание продюсера для перезаписи id в Кафку
            IProducer<Null, string> producer = new ProducerBuilder<Null, string>(new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                LingerMs = 0,
                Acks = Acks.All
            }).Build();

            // Конфигурация для подписчика
            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                EnableAutoCommit = false,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                LogQueue = true
            };

            _logger.LogWarning("Event Topic установлен в значение {eventTopic}", eventTopic ?? "Null");
            _logger.LogWarning("Application started IsErrorApp:{_isErrorApp}", isErrorApp);

            if (!int.TryParse(Environment.GetEnvironmentVariable("PARALLEL"), out var ParallelConsumersCount))
                ParallelConsumersCount = configuration.GetValue<int?>("App:ParallelConsumers") ?? 1;

            _logger.LogWarning("Кол-во подключений к кафке {numberOfPartitions}", ParallelConsumersCount);

            await Parallel.ForAsync(0, ParallelConsumersCount, async (i, ct) =>
            {
                CreateConsumerAsync(_serviceProvider, new ConsumerBuilder<Ignore, string>(config).Build());
            });

            Task CreateConsumerAsync(IServiceProvider serviceProvider, IConsumer<Ignore, string> consumer)
            {
                consumer.Subscribe(isErrorApp ? errorTopic : topic);
                _logger.LogWarning("Subscribe to topic partition {_topic}", isErrorApp ? errorTopic : topic);
                ConsumeResult<Ignore, string> cr;
                bool Is500;
                string key;

                while (true)
                {
                    IServiceProvider scope = serviceProvider.CreateScope().ServiceProvider;
                    ISaverService _repository = scope.GetRequiredService<ISaverService>();
                    cr = consumer.Consume();
                    key = cr.Message.Value;
                    Is500 = key.Split(':')[0] != "QBCH";

                    try
                    {
                        if (Is500)
                            _repository.SaveCriticalError(key).Wait();

                        else if (isErrorApp)
                            _repository.ErrorTopicHandler(key).Wait();

                        else
                            _repository.TopicHandler(key).Wait();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Возникла ошибка записи в БД - {key}", key);

                        if (!isErrorApp)
                            producer.ProduceAsync(errorTopic, new() { Value = cr.Message.Value }).Wait();
                    }

                    consumer.Commit(cr);
                    _logger.LogWarning("Partition: {prt} Offset: {offset} Message:{Message}", string.Join(',', consumer.Assignment), cr.Offset.Value.ToString(), cr.Message.Value);
                }
            }
        }
    }
}

