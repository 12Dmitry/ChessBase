namespace ChessBase;

public struct EvalResultToWhiteScore
{
    public double MyEvalCp { get; set; } // оценка лучшего возможного хода Stockfish
    public string BestMove { get; set; }
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

public struct EvalMove
{
    public double WinProbabilityToWhiteScore { get; set; }
    public MoveCategory Type { get; set; }
}