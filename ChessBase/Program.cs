using System.Diagnostics;
using Chess;
using ChessBase;
using Microsoft.Extensions.Hosting;
using ChessBase.Application.Services;
using ChessBase.Api;
using ChessBase.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

//todo прописать нормально api с соблюдением всех ограничений и кеширования, еще добавить cath нормально +
// todo TEST!
//todo check dto and upload full context
// todo Write Test
//todo messag queue

var builder = Host.CreateApplicationBuilder(args);

// 1. Data Layer
builder.Services.AddDbContext<ChessDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Infrastructure (Api Client)
builder.Services.AddHttpClient<IChessComClient, ChessComClient>(client => {
    client.BaseAddress = new Uri("https://api.chess.com/pub/");
    client.DefaultRequestHeaders.Add("User-Agent", "ChessBase-App");
});

// 3. Analyz Layer
builder.Services.AddScoped<GameAnalyzer>();
// builder.Services.AddScoped<IEngine, StockfishEngine>();

// 4. Application Layer
builder.Services.AddScoped<GameSyncService>();

using IHost host = builder.Build();

// Run the sync
var syncService = host.Services.GetRequiredService<GameSyncService>();
await syncService.SyncGamesAsync("your_username"); // todo from json

await host.RunAsync();