using System.Text.Json.Serialization;

namespace ChessBase.Api.DTO;

// Информация о конкретной партии
public record GameResponse(
    string Url, 
    string Pgn, 
    [property: JsonPropertyName("time_class")]
    string TimeClass, 
    [property: JsonPropertyName("end_time")]
    long EndTimeSeconds, 
    string Uuid,
    PlayerInfo White, 
    PlayerInfo Black
);