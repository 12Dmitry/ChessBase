namespace ChessBase.Data;

public struct EvalMove
{
    public string Notation { get; set; } 
    public double EvalWhite { get; set; }
    public Accuracy Accuracy { get; set; }
    public int PlyIndex { get; set; } 
}