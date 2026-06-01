namespace ChessBase.Data;

public struct EvalResultToWhiteScore
{
    public string Notation { get; set; } 
    public double BestMoveEval { get; set; }
    public double ActualMoveEval { get; set; }
    public int PlyIndex { get; set; } // Индекс полухода 
}