using ChessBase.Api;
using ChessBase.Api.DTO;
using ChessBase.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChessBase.Kafka.Services;

public class GameDataPublisher(
    IChessComClient apiClient,
    IMessagePublisher messagePublisher,
    IOptions<KafkaOptions> kafkaOptions,
    ChessDbContext dbContext, // могу оставить так т.к. GameDataPublisher вызовается внутри scoped
    ILogger<GameDataPublisher> logger,
    IOptions<ChessComOptions> options)
{
    public async Task TakeAndPlaceCurrentGamesToMonthAsync(string? username = null)
    {
        var targetUsername = username ?? options.Value.Username;

        if (string.IsNullOrWhiteSpace(targetUsername))
        {
            throw new InvalidOperationException(
                "Username was not provided. Pass it as a method argument or configure it in appsettings.json.");
        }

        logger.LogInformation("Starting synchronization for: {Username}...", targetUsername);

        var apiGames = await apiClient.GetPlayerGamesAsync(targetUsername, DateTime.Now.Year, DateTime.Now.Month);

        foreach (var apiGame in apiGames)
        {
            try
            {
                await ProcessGameAsync(apiGame, targetUsername);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to process game {GameId}", apiGame.Uuid);

                dbContext.ChangeTracker.Clear();
            }
        }
    }

    private async Task ProcessGameAsync(GameResponse apiGame, string targetUsername)
    {
        if (await dbContext.Games.AnyAsync(g => g.ExternalId == apiGame.Uuid))
            return;

        var gameEvent = new GameFetchedEvent
        {
            Url =  apiGame.Url,
            Uuid = apiGame.Uuid,
            Pgn = apiGame.Pgn,
            TargetUsername = targetUsername,
            EndTimeSeconds = apiGame.EndTimeSeconds
        };

        await messagePublisher.PublishAsync(kafkaOptions.Value.TopicGamesToAnalyze, gameEvent);

        logger.LogInformation("Game {Id} published to Kafka for analysis {@gameEvent}", apiGame.Uuid, gameEvent);
    }
}