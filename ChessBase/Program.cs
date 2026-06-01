using System.Diagnostics;
using Chess;
using ChessBase;
using Microsoft.Extensions.Hosting;
using ChessBase.Application.Services;
using ChessBase.Api;
using ChessBase.Api.Handlers;
using ChessBase.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

// todo add logs revert it from console.WL
//todo api с соблюдением всех ограничений и кеширования
//todo check dto and upload full context
// todo Write Test
//todo add Message Broker?

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMemoryCache();

// 0. todo Инициализируем статический логгер Serilog (нужен для логов до старта DI контейнера)
// все try catch и finally выводим в файл + статический логгер для старта
// Это заменит стандартный ILoggerFactory на Serilog
// все try catch и finally выводим в файл + статический логгер для старта
builder.Services.AddSerilog((services, configuration) => configuration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services));

// 1. Data Layer
builder.Services.AddDbContext<ChessDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,                     
            maxRetryDelay: TimeSpan.FromSeconds(5),
            null
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
builder.Services.AddTransient<GameSyncService>();

using IHost host = builder.Build();

using (var startupScope = host.Services.CreateScope())
{
    var context = startupScope.ServiceProvider.GetRequiredService<ChessDbContext>();
    
    var maxRetries = 5;
    var delaySeconds = 3;
    
    for (var i = 1; i <= maxRetries; i++)
    {
        try
        {
            Console.WriteLine("Попытка применить миграции к базе данных...");
            await context.Database.MigrateAsync();
            Console.WriteLine("База данных успешно обновлена и готова к работе!");
            break;
        }
        catch (Exception ex) when (i < maxRetries)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"База данных еще не готова (Попытка {i} из {maxRetries}). Ожидание {delaySeconds} сек... Ошибка: {ex.Message}");
            Console.ResetColor();
            
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }
        catch (Exception)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Критическая ошибка: Не удалось подключиться к базе данных после нескольких попыток.");
            Console.ResetColor();
            throw;
        }
    }
}

using (IServiceScope scope = host.Services.CreateScope())
{
    var syncService = scope.ServiceProvider.GetRequiredService<GameSyncService>();
    
    await syncService.SyncGamesAsync();
} 

await host.RunAsync();