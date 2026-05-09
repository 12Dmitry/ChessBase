namespace ChessBase.Api.DTO;

// Информация о конкретной партии
public record GameResponse(
    string Url, 
    string Pgn, 
    string TimeClass, 
    long EndTime, 
    string Uuid, // Тот самый уникальный ID для базы
    PlayerInfo White, 
    PlayerInfo Black
    //EcoCode`
);