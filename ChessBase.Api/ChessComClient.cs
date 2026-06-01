using System.Net;
using ChessBase.Api.DTO;
using Microsoft.Extensions.Caching.Memory;

namespace ChessBase.Api;

public class ChessComClient : IChessComClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChessComClient> _logger;
    private readonly IMemoryCache _cache;

    // Ключи для кэша
    private const string ArchivesCacheKeyPrefix = "chess_archives_";
    private const string GamesCacheKeyPrefix = "chess_games_";

    public ChessComClient(HttpClient httpClient, ILogger<ChessComClient> logger, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
        
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _logger.LogWarning("User-Agent is missing. Setting default User-Agent for Chess.com API");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ChessBaseApp/1.0 (contact: dim4ik121313@gmail.com)");
        }
    }

    public async Task<List<GameResponse>> GetPlayerGamesAsync(string username, int year, int month)
    {
        _logger.LogInformation("Fetching games for user {Username} for {Year}-{Month}", username, year, month);
        
        var archives = await GetArchivesAsync(username);
    
        var monthString = month.ToString("D2");
        var archiveUrlSuffix = $"{year}/{monthString}";

        var targetArchive = archives.FirstOrDefault(a => a.EndsWith(archiveUrlSuffix));

        if (targetArchive == null)
        {
            _logger.LogWarning("No archives found for user {Username} at {Year}-{Month}", username, year, month);
            return [];
        }

        var cacheKey = $"{GamesCacheKeyPrefix}{username}_{year}_{monthString}";

        if (_cache.TryGetValue(cacheKey, out List<GameResponse>? cachedGames) && cachedGames != null)
        {
            _logger.LogInformation("Returning cached games for {Username} ({Year}-{Month}). Total: {Count}", 
                username, year, month, cachedGames.Count);
            return cachedGames;
        }

        var rapidGames = await GetRapidGamesFromArchiveAsync(targetArchive);

        var now = DateTime.UtcNow;
        var isCurrentMonth = (now.Year == year && now.Month == month);

        var cacheOptions = new MemoryCacheEntryOptions();
        if (isCurrentMonth)
        {
            cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            _logger.LogDebug("Current month detected. Setting short cache lifetime (15m) for games.");
        }
        else
        {
            cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
            _logger.LogDebug("Historical month detected. Setting long cache lifetime (1d) for games.");
        }

        _cache.Set(cacheKey, rapidGames, cacheOptions);

        return rapidGames;
    }

    public async Task<string[]> GetArchivesAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));

        var cacheKey = $"{ArchivesCacheKeyPrefix}{username.ToLower().Trim()}";

        if (_cache.TryGetValue(cacheKey, out string[]? cachedArchives) && cachedArchives != null)
        {
            _logger.LogDebug("Returning cached archives list for user {Username}. Count: {Count}", username, cachedArchives.Length);
            return cachedArchives;
        }

        _logger.LogDebug("Fetching archives list from Chess.com API for user {Username}", username);
        var url = $"https://api.chess.com/pub/player/{username}/games/archives";
        
        var response = await _httpClient.GetAsync(url);
        
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Archives not found for user {Username}", username);
            return [];
        }

        response.EnsureSuccessStatusCode();

        var archivesResponse = await response.Content.ReadFromJsonAsync<ArchivesResponse>();
        var archives = archivesResponse?.Archives ?? [];
        
        _logger.LogDebug("Found {Count} archives for user {Username} from API", archives.Length, username);
        
        _cache.Set(cacheKey, archives, TimeSpan.FromHours(1));
        
        return archives;
    }

    public async Task<List<GameResponse>> GetRapidGamesFromArchiveAsync(string archiveUrl)
    {
        if (string.IsNullOrWhiteSpace(archiveUrl))
            throw new ArgumentException("Archive URL cannot be empty", nameof(archiveUrl));

        _logger.LogDebug("Fetching games from archive via API: {ArchiveUrl}", archiveUrl);
        var response = await _httpClient.GetAsync(archiveUrl);
        
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Archive not found: {ArchiveUrl}", archiveUrl);
            return [];
        }

        response.EnsureSuccessStatusCode();

        var monthlyArchiveResponse = await response.Content.ReadFromJsonAsync<MonthlyArchiveResponse>();

        var rapidGames = monthlyArchiveResponse?.Games?
            .Where(game => game.TimeClass == "rapid")
            .ToList() ?? [];
            
        _logger.LogInformation("Loaded {TotalCount} games from archive API, filtered to {RapidCount} rapid games", 
            monthlyArchiveResponse?.Games?.Length ?? 0, rapidGames.Count);

        return rapidGames;
    }
}