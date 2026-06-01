using ChessBase.Data;

namespace ChessBase;

public static class ChessMath
{
    private const double K = 0.00368208;

    public static double CalculateGameAccuracy(IEnumerable<EvalMove> moves)
    {
        var evalMoves = moves as EvalMove[] ?? moves.ToArray();
        if (!evalMoves.Any()) return 0;
        
        return Math.Round(evalMoves.Average(m => m.Accuracy.MoveAccuracyToWhite), 2);
    }

    public static Accuracy CalculateMoveAccuracy(double actualMoveEval, double bestMoveEval, bool userIsWhite)
    {
        // Приводим оценку к перспективе пользователя (положительное число = пользователю хорошо)
        var bestForUser = userIsWhite ? bestMoveEval : -bestMoveEval;
        var actualForUser = userIsWhite ? actualMoveEval : -actualMoveEval;

        var winProbBefore = ToWinProbability(bestForUser);
        var winProbAfter = ToWinProbability(actualForUser);

        return new Accuracy
        {
            MoveAccuracyToWhite = CalculateAccuracy(winProbBefore, winProbAfter),
            Type = GetMoveCategory(winProbBefore, winProbAfter)
        };
    }

    private static MoveCategory GetMoveCategory(double winProbBefore, double winProbAfter)
    {
        var loss = (winProbBefore - winProbAfter) / 100.0; 

        return loss switch
        {
            <= 0.00 => MoveCategory.Best,
            <= 0.02 => MoveCategory.Excellent,
            <= 0.05 => MoveCategory.Good,
            <= 0.10 => MoveCategory.Inaccuracy,
            <= 0.20 => MoveCategory.Mistake,
            _ => MoveCategory.Blunder
        };
    }

    private static double ToWinProbability(double cp)
    {
        var clampedCp = Math.Clamp(cp, -1000, 1000);
        return 50 + 50 * (2 / (1 + Math.Exp(-K * clampedCp)) - 1);
    }

    private static double CalculateAccuracy(double winProbBefore, double winProbAfter)
    {
        // Берем разницу. Если ход лучше, чем ожидал движок на текущей глубине (бывает редко), loss будет < 0, берем 0.
        var diff = Math.Max(0, winProbBefore - winProbAfter);
        
        var accuracy = 103.1668 * Math.Exp(-0.04354 * diff) - 3.1669;
        return Math.Clamp(accuracy, 0, 100);
    }
}