using ChessBase.Api;
using ChessBase.Api.DTO;
using ChessBase.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChessBase.Application.Services;

public class GameSyncService(
    IChessComClient apiClient,
    GameAnalyzer analyzer,
    ChessDbContext dbContext,
    ILogger<GameSyncService> logger,
    IOptions<ChessComOptions> options)
{
    public async Task SyncGamesAsync(string? username = null)
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
            
        logger.LogDebug("Analyzing game {gameId}...", apiGame.Uuid);

        var report = await analyzer.AnalyzePgnAsync(apiGame.Pgn, targetUsername);

        var game = new Game 
        { 
            ExternalId = apiGame.Uuid,
            PgnText = apiGame.Pgn,
            TotalAccuracy = report.TotalAccuracy,
            OpeningName = report.OpeningName,
            EcoCode = report.EcoCode,
            Result = report.ResultForUser,
            PlayedAt = DateTimeOffset.FromUnixTimeSeconds(apiGame.EndTime).UtcDateTime,
            Moves = report.Moves 
        };
        
        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync();
    }
}