namespace ChessBase.Data;

public class Game
{
    public Guid Id { get; set; }
    public string ExternalId { get; set; } // UUID из Chess.com для дедупликации
    public bool UserIsWhite  { get; set; }
    public string Result { get; set; } // Win/Loss/Draw
    public string PgnText { get; set; }
    public double TotalAccuracy { get; set; }
    public string OpeningName { get; set; }
    public string EcoCode { get; set; } // Код дебюта (например, B20)
    public DateTime PlayedAt { get; set; }
    
    public List<MoveAnalysis> Moves { get; set; } = new();
}