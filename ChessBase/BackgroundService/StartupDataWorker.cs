using Microsoft.Extensions.DependencyInjection;
using ChessBase.Kafka.Services;

namespace ChessBase.BackgroundService;

public class StartupDataWorker(IServiceProvider serviceProvider) : Microsoft.Extensions.Hosting.BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        
        var syncService = scope.ServiceProvider.GetRequiredService<GameDataPublisher>();
        await syncService.TakeAndPlaceCurrentGamesToMonthAsync();
    }
}