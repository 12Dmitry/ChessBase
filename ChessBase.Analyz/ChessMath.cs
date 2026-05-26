namespace ChessBase;

public static class ChessMath
{
    // Константа 'k' из модели Lichess/Chess.com для логистической регрессии
    private const double K = 0.00368208;

    /// <summary>
    /// Рассчитывает общую точность партии на основе списка ходов.
    /// </summary>
    public static double CalculateGameAccuracy(IEnumerable<EvalMove> moves) => // todo mb to int
        Math.Round(moves?.DefaultIfEmpty().Average(m => m.Accuracy.MoveAccuracyToWhite) ?? 0, 2); //todo if emty if it possible add log at least

    public static Accuracy GetAccuracy(double result, double prevResult)
    {
        var winProbBefore = ToWinProbability(prevResult);
        var winProbAfter = ToWinProbability(result);
        return new Accuracy
        {
            MoveAccuracyToWhite = CalculateAccuracy(winProbBefore, winProbAfter),
            Type = GetMoveCategory(winProbBefore, winProbAfter)
        };
    }

    /// <summary>
    /// Классификация хода по потере вероятности победы
    /// </summary>
    private static MoveCategory GetMoveCategory(double winProbBefore, double winProbAfter)
    {
        var loss = (winProbBefore - winProbAfter) / 100.0; // In fractions from 0 to 1

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

    /// <summary>
    /// Перевод оценки движка (центипашки) в вероятность победы (0..100)
    /// </summary>
    private static double ToWinProbability(double cp)
    {
        // Ограничиваем значение для стабильности функции [4]
        var clampedCp = Math.Clamp(cp, -1000, 1000);
        return 50 + 50 * (2 / (1 + Math.Exp(-K * clampedCp)) - 1);
    }

    /// <summary>
    /// Расчет точности хода на основе падения вероятности победы
    /// </summary>
    /// <param name="winProbBefore">Вероятность при лучшем ходе движка</param>
    /// <param name="winProbAfter">Вероятность после вашего реального хода</param>
    private static double CalculateAccuracy(double winProbBefore, double winProbAfter)
    {
        var diff = Math.Max(0, winProbBefore - winProbAfter);
        // Формула нормализации точности
        var accuracy = 103.1668 * Math.Exp(-0.04354 * diff) - 3.1669;
        return Math.Clamp(accuracy, 0, 100);
    }
}