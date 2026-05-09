using ChessBase.Api.DTO;

namespace ChessBase.Api;

public interface IChessComClient
{
    /// <summary>
    /// Получает список URL архивов игр для указанного пользователя.
    /// </summary>
    Task<string[]> GetArchivesAsync(string username);

    /// <summary>
    /// Получает список игр из указанного архива.
    /// </summary>
    Task<List<GameResponse>> GetRapidGamesFromArchiveAsync(string archiveUrl);

    Task<List<GameResponse>> GetPlayerGamesAsync(string username, int year, int month);
}