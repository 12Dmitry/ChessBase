using ChessBase.Api.DTO;
using Microsoft.AspNetCore.Mvc;

namespace ChessBase.Api.Controllers;

//todo Кэширование: API поддерживает заголовки ETag. Если вы планируете запускать скрипт часто, можно сохранять ETag последнего запроса и отправлять его в заголовке If-None-Match. Если новых партий нет, сервер вернет код 304 Not Modified, и вы сэкономите трафик.

[ApiController]
[Route("api/[controller]")]
public class ChessController(IHttpClientFactory factory)
    : ControllerBase // Наследуемся от ControllerBase для доступа к IActionResult
{
    private readonly HttpClient _client = factory.CreateClient("ChessClient");

    /// <summary>
    /// Получает список URL архивов игр для указанного пользователя.
    /// </summary>
    /// <param name="username">Имя пользователя на chess.com</param>
    /// <returns>Список URL архивов или ошибка.</returns>
    [HttpGet("chesscom/archives/{username}")]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)] // Указываем тип ответа для документации
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)] // Cache
    public async Task<ActionResult<string[]>> GetChessComArchivesAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest("Имя пользователя не может быть пустым.");
        }

        var url = $"https://api.chess.com/pub/player/{username}/games/archives";
        HttpResponseMessage response;
        try
        {
            response = await _client.GetAsync(url);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ошибка HTTP-запроса при получении архивов: {ex.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Ошибка сети при доступе к chess.com: {ex.Message}");
        }

        if (!response.IsSuccessStatusCode)
        {
            // Если пользователь не найден (404) или другая ошибка
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound($"Архивы для пользователя '{username}' не найдены.");
            }

            return StatusCode((int)response.StatusCode, $"Ошибка при получении архивов: {response.ReasonPhrase}");
        }

        try
        {
            var archivesResponse = await response.Content.ReadFromJsonAsync<ArchivesResponse>();
            // Проверяем, что Archives не null, если API вернул пустой объект {}
            return archivesResponse?.Archives ?? Array.Empty<string>();
        }
        catch (System.Text.Json.JsonException ex)
        {
            Console.WriteLine($"Ошибка десериализации JSON для архивов: {ex.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ошибка обработки данных от chess.com.");
        }
    }

    /// <summary>
    /// Получает список игр (только Rapid) из указанного URL архива.
    /// </summary>
    /// <param name="archiveUrl">URL архива, полученный из GetChessComArchivesAsync</param>
    /// <returns>Список игр или ошибка.</returns>
    [HttpGet("chesscom/games")] // Маршрут может быть общим, если archiveUrl передается как параметр запроса
    [ProducesResponseType(typeof(List<GameResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<GameResponse>>>
        GetGamesFromArchiveAsync(
            [FromQuery] string archiveUrl) // Используем FromQuery для передачи URL как параметра запроса
    {
        if (string.IsNullOrWhiteSpace(archiveUrl))
        {
            return BadRequest("URL архива не может быть пустым.");
        }

        HttpResponseMessage response;
        try
        {
            // Важно: Убедитесь, что _client настроен для отправки GetFromJsonAsync или используйте GetAsync и ReadFromJsonAsync
            // Для простоты, используем GetAsync и ReadFromJsonAsync
            response = await _client.GetAsync(archiveUrl);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ошибка HTTP-запроса при получении игр из архива: {ex.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Ошибка сети при доступе к chess.com: {ex.Message}");
        }

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound($"Архив по URL '{archiveUrl}' не найден.");
            }

            return StatusCode((int)response.StatusCode, $"Ошибка при получении игр из архива: {response.ReasonPhrase}");
        }

        try
        {
            var monthlyArchiveResponse = await response.Content.ReadFromJsonAsync<MonthlyArchiveResponse>();

            var rapidGames = monthlyArchiveResponse?.Games?
                .Where(game => game.TimeClass == "rapid")
                .ToList() ?? new List<GameResponse>();

            return rapidGames;
        }
        catch (System.Text.Json.JsonException ex)
        {
            Console.WriteLine($"Ошибка десериализации JSON для игр из архива: {ex.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ошибка обработки данных от chess.com.");
        }
    }
}