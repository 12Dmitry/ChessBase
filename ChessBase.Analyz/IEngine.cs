namespace ChessBase;

public interface IEngine
{
    Task<string> GetEvaluationAsync(string fen, int depth = 17);
}