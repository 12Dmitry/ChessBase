using ChessBase.Api;
using ChessBase.Api.DTO;
using ChessBase.Data;
using Microsoft.EntityFrameworkCore;

namespace ChessBase.Application.Services;

public class GameSyncService(IChessComClient apiClient, GameAnalyzer analyzer, ChessDbContext dbContext)
{
    public async Task SyncGamesAsync(string username)
    {
        // 1. Download
        var apiGames = await apiClient.GetPlayerGamesAsync(username, DateTime.Now.Year, DateTime.Now.Month);

        foreach (var apiGame in apiGames)
        {
            // 2. Check Duplicates
            if (await dbContext.Games.AnyAsync(g => g.ExternalId == apiGame.Uuid))
                continue;

            // 3. Analyze
            var analysisResult = await analyzer.AnalyzePgnAsync(apiGame.Pgn);

            // 4. Map & Save
            var game = new Game 
            { 
                ExternalId = apiGame.Uuid,
                PgnText = apiGame.Pgn,
                TotalAccuracy = analysisResult.Accuracy,
                PlayedAt = new DateTime(apiGame.EndTime) // todo UTC?
                // ... other fields
            };
            
            dbContext.Games.Add(game);
        }

        await dbContext.SaveChangesAsync();
    }
}