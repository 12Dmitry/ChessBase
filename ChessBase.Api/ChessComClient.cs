using ChessBase.Api.DTO;

namespace ChessBase.Api;

public class ChessComClient : IChessComClient
{
    private readonly HttpClient _httpClient;

    public ChessComClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Настройка User-Agent обязательна для Chess.com API");
            Console.ResetColor();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ChessBaseApp/1.0 (contact: your-email@example.com)");
        }
    }

    public async Task<List<GameResponse>> GetPlayerGamesAsync(string username, int year, int month)
    {
        var archives = await GetArchivesAsync(username);
    
        // API Chess.com использует формат: .../games/YYYY/MM 
        var monthString = month.ToString("D2");
        var archiveUrlSuffix = $"{year}/{monthString}";

        var targetArchive = archives.FirstOrDefault(a => a.EndsWith(archiveUrlSuffix));

        if (targetArchive == null)
        {
            return [];
        }

        return await GetRapidGamesFromArchiveAsync(targetArchive);
    }

    public async Task<string[]> GetArchivesAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));

        var url = $"https://api.chess.com/pub/player/{username}/games/archives";
        
        var response = await _httpClient.GetAsync(url);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return [];

        response.EnsureSuccessStatusCode();

        var archivesResponse = await response.Content.ReadFromJsonAsync<ArchivesResponse>();
        return archivesResponse?.Archives ?? [];
    }

    public async Task<List<GameResponse>> GetRapidGamesFromArchiveAsync(string archiveUrl)
    {
        if (string.IsNullOrWhiteSpace(archiveUrl))
            throw new ArgumentException("Archive URL cannot be empty", nameof(archiveUrl));

        var response = await _httpClient.GetAsync(archiveUrl);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return [];

        response.EnsureSuccessStatusCode();

        var monthlyArchiveResponse = await response.Content.ReadFromJsonAsync<MonthlyArchiveResponse>();

        // Оставляем только Rapid игры
        return monthlyArchiveResponse?.Games?
            .Where(game => game.TimeClass == "rapid")
            .ToList() ?? [];
    }
}