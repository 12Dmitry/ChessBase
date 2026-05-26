using Chess;

namespace ChessBase;

public struct EvalResultToWhiteScore(double evalWhite)
{
    public string Notation { get; set; } = ""; // Например, "e4" todo add
    public double EvalWhite { get; set; } = evalWhite; // оценка лучшего возможного хода Stockfish
}

public struct EvalMove
{
    public string Notation { get; set; } 
    public double EvalWhite { get; set; }
    public Accuracy Accuracy { get; set; }
}

public struct Accuracy
{
    public double MoveAccuracyToWhite { get; set; }
    public MoveCategory Type { get; set; }
}

public enum MoveCategory
{
    Best,
    Excellent,
    Good,
    Inaccuracy,
    Mistake,
    Blunder
}

public enum GameStage
{
    Opening,
    Middlegame,
    Endgame
}

public static class PieceTypeExtensions
{
    public static int Weight(this PieceType type)
    {
        if (type == PieceType.Queen)  return 9;
        if (type == PieceType.Rook)   return 5;
        return type == PieceType.Bishop || type == PieceType.Knight? 3 : 0;
    }
}
