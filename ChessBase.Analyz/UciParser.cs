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

        return new EvalResultToWhiteScore(userIsWhite ? result : result * -1);
    }
}