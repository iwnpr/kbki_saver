using cache_lib.Implementations;
using cache_lib.Interfaces;
using Confluent.Kafka;
using db_lib.Entities;

//using db_lib.DBEntity;
//using db_lib.Entity.CommonTypes.Api;
using db_lib.Services.Implementations;
using db_lib.Services.Interfaces;
using db_lib.Services.Interfaces.V3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QBCH_lib.CommonTypes.Api;
using Serilog;
using StackExchange.Redis;
using System.ServiceProcess;
using Xml_service_lib;

namespace qbch_db_saver_sdc
{
    public class QBCHSaverBGService : ServiceBase
    {
        protected override void OnStart(string[] args)
        {
            //Подключаем файл конфигурации
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var bootstrapServers = configuration.GetValue<string>("Kafka:BootstrapServers");
            var isErrorApp = configuration.GetValue<bool>("App:IsErrorApp");
            var topic = configuration.GetSection("Kafka:Topic").Value;
            var errorTopic = configuration.GetValue<string>("Kafka:ErrorTopic");
            var eventTopic = configuration.GetValue<string>("Kafka:EventTopic");
            var groupId = configuration.GetValue<string>("Kafka:GroupId");

            Console.Title = isErrorApp ? $"IsError: {isErrorApp} | Topic: {errorTopic} | Event topic: {eventTopic}" : $"IsError: {isErrorApp} | Topic: {topic} | Event topic: {eventTopic}";

            // Конфигурация для подписчика
            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                EnableAutoCommit = false,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                LogQueue = true
            };

            // Создание продюсера для перезаписи id в Кафку
            IProducer<Null, string> producer = new ProducerBuilder<Null, string>(new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                LingerMs = 0,
                Acks = Acks.All
            }).Build();

            ServiceCollection services = new();
            services.AddSingleton(configuration);
            services.AddDbContext<QbchContext>(o => {
                o.UseNpgsql(configuration.GetConnectionString("DataBase"));
                //o.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }, ServiceLifetime.Transient);
            services.AddTransient<ISaverService>(o => new SaverService(
                o.GetRequiredService<ICacheService>(),
                o.GetRequiredService<ILogger<SaverService>>(),
                producer,
                o.GetRequiredService<IRepository>(),
                o.GetRequiredService<IRepositoryV3>(),
                o.GetRequiredService<IConfiguration>(),
                errorTopic));
            services.AddSingleton<ICacheService, CacheService>();
            services.AddSingleton<IXmlService, XmlService>();
            services.AddTransient<IRepository, Repository>();
            services.AddSingleton<IBKIRequisitsHandler, BKIRequsits>();
            services.AddLogging(builder => builder.AddSerilog(new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger()));
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!));

            IServiceProvider ServiceProvider = services.BuildServiceProvider();
            ILogger<Program> _logger = ServiceProvider.GetRequiredService<ILogger<Program>>();

            if (string.IsNullOrWhiteSpace(topic))
            {
                _logger.LogError("Topic не заполнен.");
                return;
            }

            if (string.IsNullOrWhiteSpace(errorTopic))
            {
                _logger.LogError("Error_topic не заполнен.");
                return;
            }

            _logger.LogWarning("Event Topic установлен в значение {eventTopic}", eventTopic ?? "Null");

            _logger.LogWarning("Application started IsErrorApp:{_isErrorApp}", isErrorApp);
            var numberOfPartitions = configuration.GetValue<int>("Kafka:PartitionsNumber");
            var parallelDBServices = configuration.GetValue<int?>("App:ParallelDBServices") ?? numberOfPartitions;
            _logger.LogWarning("Кол-во сервисов для параллельной работы с БД {ParallelDBServices}", parallelDBServices);

            Parallel.ForAsync(0, numberOfPartitions, async (i, ct) =>
            {
                await CreateConsumerAsync(ServiceProvider, new ConsumerBuilder<Ignore, string>(config).Build());
            });

            async Task CreateConsumerAsync(IServiceProvider serviceProvider, IConsumer<Ignore, string> consumer)
            {
                consumer.Subscribe(isErrorApp ? errorTopic : topic);
                _logger.LogWarning("Subscribe to topic partition {_topic}", isErrorApp ? errorTopic : topic);
                ConsumeResult<Ignore, string> cr;
                bool Is500;
                string key;

                await Task.Run(async () =>
                {
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
                                await _repository.SaveCriticalError(key);

                            if (isErrorApp)
                                await _repository.ErrorTopicHandler(key);
                            else
                                await _repository.TopicHandler(key);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogCritical(ex, "Возникла ошибка записи в БД - {key}", key);

                            if (!isErrorApp)
                                await producer.ProduceAsync(errorTopic, new() { Value = cr.Message.Value });
                        }

                        consumer.Commit(cr);
                        _logger.LogWarning("Partition: {prt} Offset: {offset} Message:{Message}", string.Join(',', consumer.Assignment), cr.Offset.Value.ToString(), cr.Message.Value);
                    }
                });
            }
        }
    }
}