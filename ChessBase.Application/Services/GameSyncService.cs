using ChessBase.Api;
using ChessBase.Api.DTO;
using ChessBase.Data;
using Microsoft.EntityFrameworkCore;

namespace ChessBase.Application.Services;

public class GameSyncService(IChessComClient apiClient, GameAnalyzer analyzer, ChessDbContext dbContext)
{
    public async Task SyncGamesAsync(string username)
    {
        var apiGames = await apiClient.GetPlayerGamesAsync(username, DateTime.Now.Year, DateTime.Now.Month);

        foreach (var apiGame in apiGames)
        {
            if (await dbContext.Games.AnyAsync(g => g.ExternalId == apiGame.Uuid))
                continue;

            // Анализатор теперь делает всю грязную работу по парсингу PGN
            var report = await analyzer.AnalyzePgnAsync(apiGame.Pgn, username);

            // Маппинг в сущность БД
            var game = new Game 
            { 
                ExternalId = apiGame.Uuid,
                PgnText = apiGame.Pgn,
                TotalAccuracy = report.TotalAccuracy,
                OpeningName = report.OpeningName,
                EcoCode = report.EcoCode,
                Result = report.Result,
                // Chess.com EndTime - это Unix Timestamp
                PlayedAt = DateTimeOffset.FromUnixTimeSeconds(apiGame.EndTime).UtcDateTime,
                Moves = report.Moves 
            };
        
            dbContext.Games.Add(game);
        }

        await dbContext.SaveChangesAsync();
    }}