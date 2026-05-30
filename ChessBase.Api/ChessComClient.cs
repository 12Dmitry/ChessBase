using ChessBase.Api.DTO;

namespace ChessBase.Api;

public class ChessComClient : IChessComClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChessComClient> _logger;

    public ChessComClient(HttpClient httpClient, ILogger<ChessComClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _logger.LogWarning("User-Agent is missing. Setting default User-Agent for Chess.com API");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ChessBaseApp/1.0 (contact: your-email@example.com)");
        }
    }

    public async Task<List<GameResponse>> GetPlayerGamesAsync(string username, int year, int month)
    {
        _logger.LogInformation("Fetching games for user {Username} for {Year}-{Month}", username, year, month);
        
        var archives = await GetArchivesAsync(username);
    
        // API Chess.com использует формат: .../games/YYYY/MM 
        var monthString = month.ToString("D2");
        var archiveUrlSuffix = $"{year}/{monthString}";

        var targetArchive = archives.FirstOrDefault(a => a.EndsWith(archiveUrlSuffix));

        if (targetArchive == null)
        {
            _logger.LogWarning("No archives found for user {Username} at {Year}-{Month}", username, year, month);
            return [];
        }

        return await GetRapidGamesFromArchiveAsync(targetArchive);
    }

    public async Task<string[]> GetArchivesAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));

        _logger.LogDebug("Fetching archives list for user {Username}", username);
        var url = $"https://api.chess.com/pub/player/{username}/games/archives";
        
        var response = await _httpClient.GetAsync(url);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Archives not found for user {Username}", username);
            return [];
        }

        response.EnsureSuccessStatusCode();

        var archivesResponse = await response.Content.ReadFromJsonAsync<ArchivesResponse>();
        var archives = archivesResponse?.Archives ?? [];
        _logger.LogDebug("Found {Count} archives for user {Username}", archives.Length, username);
        
        return archives;
    }

    public async Task<List<GameResponse>> GetRapidGamesFromArchiveAsync(string archiveUrl)
    {
        if (string.IsNullOrWhiteSpace(archiveUrl))
            throw new ArgumentException("Archive URL cannot be empty", nameof(archiveUrl));

        _logger.LogDebug("Fetching games from archive: {ArchiveUrl}", archiveUrl);
        var response = await _httpClient.GetAsync(archiveUrl);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Archive not found: {ArchiveUrl}", archiveUrl);
            return [];
        }

        response.EnsureSuccessStatusCode();

        var monthlyArchiveResponse = await response.Content.ReadFromJsonAsync<MonthlyArchiveResponse>();

        var rapidGames = monthlyArchiveResponse?.Games?
            .Where(game => game.TimeClass == "rapid")
            .ToList() ?? [];
            
        _logger.LogInformation("Loaded {TotalCount} games from archive, filtered to {RapidCount} rapid games", 
            monthlyArchiveResponse?.Games?.Length ?? 0, rapidGames.Count);

        return rapidGames;
    }
}