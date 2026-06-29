using cache_lib.Implementations;
using cache_lib.Interfaces;
using Confluent.Kafka;
using db_lib.Entities;
using db_lib.Services.Implementations;
using db_lib.Services.Implementations.V3;
using db_lib.Services.Interfaces;
using db_lib.Services.Interfaces.V3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using qbch_db_saver_sdc;
using QBCH_lib.CommonTypes.Api;
using Serilog;
using StackExchange.Redis;
using System.ServiceProcess;
using Xml_service_lib;

ThreadPool.SetMinThreads(200, 200);

if (!Environment.UserInteractive && OperatingSystem.IsWindows())
{
    ServiceBase[] ServicesToRun;
    ServicesToRun = [new QBCHSaverBGService()];
    ServiceBase.Run(ServicesToRun);
}
else
{
    if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
    {
        // Отключаем в консоли возможность редактирования
        DisableConsoleQuickEdit.Go();
    }

    //Подключаем файл конфигурации
    IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

    var bootstrapServers = configuration.GetValue<string>("Kafka:BootstrapServers");
    var topic = configuration.GetSection("Kafka:Topic").Value;
    var errorTopic = configuration.GetValue<string>("Kafka:ErrorTopic");
    var groupId = configuration.GetValue<string>("Kafka:GroupId");


    if (!bool.TryParse(Environment.GetEnvironmentVariable("ERROR_APP"), out bool isErrorApp))
        isErrorApp = configuration.GetValue<bool?>("App:IsErrorApp") ?? true;

    Console.Title = isErrorApp ? $"IsError: {isErrorApp} | Topic: {errorTopic}" : $"IsError: {isErrorApp} | Topic: {topic}";

    // Конфигурация для подписчика
    var config = new ConsumerConfig
    {
        BootstrapServers = bootstrapServers,
        GroupId = groupId,
        EnableAutoCommit = false,
        SocketKeepaliveEnable = true,
        AutoOffsetReset = AutoOffsetReset.Earliest,
    };

    // Создание продюсера для перезаписи id в Кафку
    IProducer<Null, string> producer = new ProducerBuilder<Null, string>(new ProducerConfig
    {
        BootstrapServers = bootstrapServers,
        LingerMs = 0,
        RequestTimeoutMs = 10000,
        Acks = Acks.All
    }).Build();

    ServiceCollection services = new();
    services.AddSingleton(configuration);
    
    services.AddDbContext<QbchContext>(o =>
    {
        o.UseNpgsql(connectionString: configuration.GetConnectionString("DataBase"));
    }, ServiceLifetime.Transient);
    
    services.AddDbContext<QbchSecondaryContext>(o =>
    {
        o.UseNpgsql(connectionString: configuration.GetConnectionString("DataBaseSecondary"));
    }, ServiceLifetime.Transient);
    services.AddTransient<ISaverService>(o => new SaverService(
        o.GetRequiredService<ICacheService>(),
        o.GetRequiredService<ILogger<SaverService>>(),
        producer,
        o.GetRequiredService<IRepository>(),
        o.GetRequiredService<IRepositoryV3>(),
        o.GetRequiredService<IConfiguration>(),
        errorTopic));
    services.AddScoped<ICacheService, CacheService>();
    services.AddSingleton<IXmlService, XmlService>();


    services.AddTransient<RepositoryV3>();
    services.AddTransient<IRepositoryV3>(sp => new DualRepositoryV3(
        sp.GetRequiredService<RepositoryV3>(),
        new RepositoryV3(
            sp.GetRequiredService<QbchSecondaryContext>(),
            sp.GetRequiredService<ILogger<RepositoryV3>>(),
            sp.GetRequiredService<IXmlService>(),
            sp.GetRequiredService<ICacheService>(),
            sp.GetRequiredService<IBKIRequisitsHandler>()),
        sp.GetRequiredService<ICacheService>(),
        sp.GetRequiredService<ILogger<DualRepositoryV3>>()));

    services.AddTransient<Repository>();
    services.AddTransient<IRepository>(sp => new DualRepository(
        sp.GetRequiredService<Repository>(),
        new Repository(
            sp.GetRequiredService<QbchSecondaryContext>(),
            sp.GetRequiredService<ICacheService>(),
            sp.GetRequiredService<ILogger<Repository>>(),
            sp.GetRequiredService<IXmlService>(),
            sp.GetRequiredService<IBKIRequisitsHandler>()),
        sp.GetRequiredService<ICacheService>(),
        sp.GetRequiredService<ILogger<DualRepository>>()));

    services.AddTransient<IBKIRequisitsHandler, BKIRequsits>();
    services.AddLogging(builder => builder.AddSerilog(new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger()));
    services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!));

    IServiceProvider ServiceProvider = services.BuildServiceProvider();
    ILogger<Program> _logger = ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (string.IsNullOrWhiteSpace(topic))
    {
        _logger.LogError("Основной topic не заполнен.");
        return;
    }

    if (string.IsNullOrWhiteSpace(errorTopic))
    {
        _logger.LogError("Error topic не заполнен.");
        return;
    }

    _logger.LogDebug("Application started IsErrorApp:{_isErrorApp}", isErrorApp);

    if (!int.TryParse(Environment.GetEnvironmentVariable("PARALLEL"), out var ParallelConsumersCount))
        ParallelConsumersCount = configuration.GetValue<int?>("App:ParallelConsumers") ?? 1;

    _logger.LogDebug("Кол-во подключений к кафке {numberOfPartitions}", ParallelConsumersCount);

    await Parallel.ForAsync(0, ParallelConsumersCount, async (i, ct) =>
    {
        CreateConsumerAsync(ServiceProvider, new ConsumerBuilder<Ignore, string>(config).Build());
    });

    Task CreateConsumerAsync(IServiceProvider serviceProvider, IConsumer<Ignore, string> consumer)
    {
        consumer.Subscribe(isErrorApp ? errorTopic : topic);
        _logger.LogDebug("Subscribe to topic partition {_topic}", isErrorApp ? errorTopic : topic);
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

                if (isErrorApp)
                    _repository.ErrorTopicHandler(key).Wait();
                else
                    _repository.TopicHandler(key).Wait();

                _logger.LogInformation("Обработан ключ {key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Возникла ошибка записи в БД - {key}", key);

                if (!isErrorApp)
                    producer.ProduceAsync(errorTopic, new() { Value = cr.Message.Value }).Wait();
            }

            consumer.Commit(cr);
            _logger.LogDebug("Partition: {prt} Offset: {offset} Message:{Message}", string.Join(',', consumer.Assignment), cr.Offset.Value.ToString(), cr.Message.Value);
        }
    }
}