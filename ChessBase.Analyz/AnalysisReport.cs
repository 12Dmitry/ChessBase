using ChessBase.Data;

namespace ChessBase;

public class AnalysisReport
{
    public string EcoCode { get; init; }
    public string OpeningName { get; init; }
    public string Result { get; init; } // Win/Loss/Draw для конкретного игрока
    public double TotalAccuracy { get; init; }
    public List<MoveAnalysis> Moves { get; init; } = new();
}