using System.Text.RegularExpressions;

namespace ChessBase;

public static class UciParser
{
    public static EvalResultToWhiteScore Parse(string rawOutput, bool userIsWhite)
    {
        double result = 0;

        // 1. Ищем оценку в центипашках (cp)
        // Пример: "score cp 15" 
        var cpMatch = Regex.Match(rawOutput, @"score cp (-?\d+)");
        if (cpMatch.Success)
        {
            result = double.Parse(cpMatch.Groups[1].Value);
        }

        // 2. Ищем оценку мата (mate)
        // Пример: "score mate 3" [4, 5]
        var mateMatch = Regex.Match(rawOutput, @"score mate (-?\d+)");
        if (mateMatch.Success)
        {
            var distance = int.Parse(mateMatch.Groups[1].Value);
            // Превращаем мат в очень большое число центипашек для мат. формул
            // Если distance > 0 — мат ставит тот, чей ход. Если < 0 — ему ставят мат.
            result = distance > 0? 10000 - distance : -10000 - distance;
        }

        // 3. Ищем лучший ход (bestmove)
        // // Пример: "bestmove e2e4" [6]
        // var moveMatch = Regex.Match(rawOutput, @"bestmove (\w+)");
        // if (moveMatch.Success)
        // {
        //     result.BestMove = moveMatch.Groups[1].Value;
        // }

        return new EvalResultToWhiteScore(userIsWhite ? result : result * -1);
    }
}

public static class ChessMath
{
    // Константа 'k' из модели Lichess/Chess.com для логистической регрессии [1, 2]
    private const double K = 0.00368208;

    /// <summary>
    /// Рассчитывает общую точность партии на основе списка ходов.
    /// </summary>
    public static double CalculateGameAccuracy(IEnumerable<EvalMove> moves) => // todo mb to int
        Math.Round(moves?.DefaultIfEmpty().Average(m => m.Accuracy.MoveAccuracyToWhite) ?? 0, 2);

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

    /// <summary>
    /// Классификация хода по потере вероятности победы
    /// </summary>
    public static MoveCategory GetMoveCategory(double winProbBefore, double winProbAfter)
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
}
