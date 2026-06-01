using ChessBase.Data;

namespace ChessBase;

public class AnalysisReport
{
    public string EcoCode { get; init; }
    public string OpeningName { get; init; }
    public bool UserIsWhite { get; init; }
    public string ResultForUser { get; init; }
    public double TotalAccuracy { get; init; }
    public List<MoveAnalysis> Moves { get; init; } = new();
}