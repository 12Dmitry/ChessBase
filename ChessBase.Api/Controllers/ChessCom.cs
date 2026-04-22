using ChessBase.Api.DTO;
using Microsoft.AspNetCore.Mvc;

namespace ChessBase.Api.Controllers;

[ApiController] 
[Route("api/[controller]")]
public class ChessCom
{
    private readonly HttpClient _client;

    public ChessCom(IHttpClientFactory factory)
    {
        _client = factory.CreateClient("ChessClient");
    }

    public async Task<string[]> GetArchivesAsync(string username = "thenafig")
    {
        var url = $"https://api.chess.com/pub/player/{username}/games/archives";
        var response = await _client.GetFromJsonAsync<ArchivesResponse>(url);
        return response?.Archives?? Array.Empty<string>();
    }

    public async Task<List<GameResponse>> GetGamesFromArchiveAsync(string archiveUrl)
    {
        var response = await _client.GetFromJsonAsync<MonthlyArchiveResponse>(archiveUrl);
        return response?.Games.Where(game => game.TimeClass == "rapid").ToList() ?? [];
    }
}