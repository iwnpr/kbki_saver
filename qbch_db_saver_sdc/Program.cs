using cache_lib.Implementations;
using cache_lib.Interfaces;
using Confluent.Kafka;
using db_lib.DBEntity;
using db_lib.Entity.CommonTypes.Api;
using db_lib.Entity.CommonTypes.Xml;
using db_lib.Entity.qcb_xml.qcb_answer;
using db_lib.Entity.qcb_xml.qcb_result;
using db_lib.Services.Implementations;
using db_lib.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using StackExchange.Redis;
using System.Diagnostics;
using System.Xml.Serialization;


using IHost host = Host.CreateDefaultBuilder(args).Build();
//Подключаем файл конфигурации
IConfiguration _configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

//Внедрение сервисов
static IServiceProvider ConfigureServices(IConfiguration configuration)
{
    ServiceCollection services = new();
    services.AddSingleton(configuration);
    services.AddDbContext<qbchContext>(o => o.UseNpgsql(configuration.GetConnectionString("DataBase")), contextLifetime: ServiceLifetime.Scoped, optionsLifetime: ServiceLifetime.Scoped);
    services.AddTransient<IRepository, Repository>();
    services.AddSingleton<ICacheService, CacheService>();
    services.AddSingleton<IBKIRequisitsHandler, BKIRequsits>();
    services.AddLogging(builder => builder.AddSerilog(new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger()));
    services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!));

    return services.BuildServiceProvider();
}

IServiceProvider ServiceProvider = ConfigureServices(_configuration);
ILogger<Program> _logger = ServiceProvider.GetRequiredService<ILogger<Program>>();
IServiceScopeFactory serviceScopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();
IRepository _repository;

var _bootstrapServers = _configuration.GetSection("Kafka:BootstrapServers").Value;
var _isErrorApp = _configuration.GetValue<bool>("Kafka:IsErrorApp");
var _topic = _configuration.GetSection("Kafka:Topic").Value;
var _errorTopic = _configuration.GetSection("Kafka:ErrorTopic").Value;
var _groupId = _configuration.GetSection("Kafka:GroupId").Value;

_logger.LogWarning("Application started IsErrorApp:{_isErrorApp}", _isErrorApp);

// Конфигурация для подписчика
var config = new ConsumerConfig
{
    BootstrapServers = _bootstrapServers,
    GroupId = _groupId,
    EnableAutoCommit = true,
};

// Создание продюсера для перезаписи id в Кафку
IProducer<Null, string> _producer = new ProducerBuilder<Null, string>(new ProducerConfig
{
    BootstrapServers = _bootstrapServers,
    LingerMs = 0,
    Acks = Acks.All
}).Build();

// Прослушивание кафки
using (var c = new ConsumerBuilder<Ignore, string>(config).Build())
{
    Stopwatch _sw = new();
    _sw.Start();

    c.Subscribe(_topic);
    _logger.LogWarning("Subscribe to topic {_topic}", _topic);

    while (true)
    {
        using var scope = serviceScopeFactory.CreateScope();
        _sw.Restart();
        var _st = DateTime.Now;
        _repository = scope.ServiceProvider.GetRequiredService<IRepository>();

        var cr = c.Consume();

        try
        {
            if (_isErrorApp)
                await _repository.SaveToDBErrorApp(cr.Message.Value);
            else
                await _repository.SaveToDB(cr.Message.Value, _producer, _errorTopic);
        }
        catch (ConsumeException e)
        {
            _logger.LogCritical(e, "Возникла ошибка записи в БД");
            await _producer.ProduceAsync(_errorTopic, new() { Value = cr.Message.Value });
        }

        c.Commit(cr);
        _logger.LogInformation("Saving message '{Message}' start time '{StartTime}' end time '{EndTime}' all time: '{SavingTime}' ms.", cr.Message.Value, _st, DateTime.Now, _sw.ElapsedMilliseconds);
        _logger.LogWarning("Offset: {offset} Message:{Message}", cr.Offset.Value.ToString(), cr.Message.Value);
    }

}