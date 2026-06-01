using ChessBase;
using Microsoft.Extensions.Hosting;
using ChessBase.Api;
using ChessBase.Api.Handlers;
using ChessBase.BackgroundService;
using ChessBase.Data;
using ChessBase.Kafka;
using ChessBase.Kafka.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

// --- ИНИЦИАЛИЗАЦИЯ БУТСТРАП-ЛОГГЕРА ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/startup-log.txt", rollingInterval: RollingInterval.Day) // Пишем в файл
    .CreateBootstrapLogger();

try
{
    Log.Information("Запуск приложения ChessBase...");

    var builder = Host.CreateApplicationBuilder(args);

    // --- ИНТЕГРАЦИЯ SERILOG В DI ---
    builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/app-log.txt", rollingInterval: RollingInterval.Day));

    builder.Services.AddMemoryCache();

    // 1. Data Layer
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        Log.Fatal("Строка подключения 'DefaultConnection' не найдена! Текущая папка: {CurrentDirectory}", Directory.GetCurrentDirectory());
        throw new InvalidOperationException("Строка подключения 'DefaultConnection' не найдена в конфигурации appsettings.json!");
    }

    builder.Services.AddDbContext<ChessDbContext>(options =>
        options.UseNpgsql(
            connectionString,
            npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null
            )
        ));

    // 2. Infrastructure (Api Client)
    builder.Services.AddTransient<ChessComLoggingHandler>();
    builder.Services.AddHttpClient<IChessComClient, ChessComClient>(client =>
    {
        client.BaseAddress = new Uri("https://api.chess.com/pub/");
        client.DefaultRequestHeaders.Add("User-Agent", "ChessBaseApp/1.0 (contact: dim4ik121313@gmail.com)");
    }).AddHttpMessageHandler<ChessComLoggingHandler>();

    // 3. Analyz Layer
    builder.Services.AddTransient<GameAnalyzer>();
    builder.Services.AddTransient<IEngine, StockfishEngine>();

    // 4. Application Layer
    builder.Services.Configure<StockfishOptions>(builder.Configuration.GetSection("Stockfish"));
    builder.Services.Configure<ChessComOptions>(builder.Configuration.GetSection("ChessCom"));
    builder.Services.AddTransient<GameDataPublisher>();

    // 5. Message Broker (Kafka)
    builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection("Kafka"));
    builder.Services.PostConfigure<KafkaOptions>(options =>
    {
        if (!string.IsNullOrEmpty(options.BootstrapServers))
        {
            options.Producer.BootstrapServers ??= options.BootstrapServers;
            options.Consumer.BootstrapServers ??= options.BootstrapServers;
        }
    });

    builder.Services.AddSingleton<IMessagePublisher, KafkaMessagePublisher>();

    // 6. Фоновые задачи (Workers)
    builder.Services.AddHostedService<AnalysisConsumeWorker>();
    builder.Services.AddHostedService<StartupDataWorker>();

    using IHost host = builder.Build();

    // --- Логирование миграций БД с помощью статического логгера ---
    using (var startupScope = host.Services.CreateScope())
    {
        var context = startupScope.ServiceProvider.GetRequiredService<ChessDbContext>();

        var maxRetries = 5;
        var delaySeconds = 3;

        for (var i = 1; i <= maxRetries; i++)
        {
            try
            {
                Log.Information("Попытка применить миграции к базе данных (Попытка {Attempt} из {MaxRetries})...", i, maxRetries);
                await context.Database.MigrateAsync();
                Log.Information("База данных успешно обновлена и готова к работе!");
                break;
            }
            catch (Exception ex) when (i < maxRetries)
            {
                Log.Warning(ex, "База данных еще не готова. Ожидание {DelaySeconds} сек...", delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Критическая ошибка: Не удалось подключиться к БД после {MaxRetries} попыток.", maxRetries);
                throw; // Останавливаем запуск приложения
            }
        }
    }

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Приложение аварийно завершило работу.");
}
finally
{
    Log.CloseAndFlush();
}