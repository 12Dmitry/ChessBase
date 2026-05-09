namespace ChessBase.Data;

public class MoveAnalysis
{
    public int Id { get; set; }
    public Guid GameId { get; set; }
    public int MoveNumber { get; set; }
    public string Notation { get; set; } // Например, "e4"
    public double EvalWhite { get; set; } // Оценка, приведенная к белым
    public double Accuracy { get; set; } // 0-100%
    public string Category { get; set; } // Blunder, Great и т.д.
    public string GameStage { get; set; } // Opening, Middlegame, Endgame
}