using ChessBase.Api.DTO;

namespace ChessBase.Api;

public class ChessComClient : IChessComClient
{
    private readonly HttpClient _httpClient;

    public ChessComClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        // Настройка User-Agent обязательна для Chess.com API
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ChessBaseApp/1.0 (contact: your-email@example.com)");
        }
    }

    public async Task<List<GameResponse>> GetPlayerGamesAsync(string username, int year, int month)
    {
        // 1. Получаем список всех доступных архивов (ссылок на месяцы) [cite: 848]
        var archives = await GetArchivesAsync(username);
    
        // 2. Формируем строку поиска для нужного года и месяца. 
        // API Chess.com использует формат: .../games/YYYY/MM 
        var monthString = month.ToString("D2"); // Чтобы 1 превратилось в "01"
        var archiveUrlSuffix = $"{year}/{monthString}";

        // 3. Находим нужный архив в списке 
        var targetArchive = archives.FirstOrDefault(a => a.EndsWith(archiveUrlSuffix));

        if (targetArchive == null)
        {
            // Если за этот месяц игр не было, возвращаем пустой список
            return [];
        }

        // 4. Загружаем и возвращаем партии из этого архива 
        // Метод GetRapidGamesFromArchiveAsync уже содержит фильтрацию по "rapid" [из вашего примера]
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