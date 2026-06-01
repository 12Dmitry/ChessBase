using System.Text.RegularExpressions;

namespace ChessBase;

public static class UciParser
{
    public static double ParseToWhiteEval(string rawOutput, bool isWhiteTurn)
    {
        double result = 0;

        var cpMatch = Regex.Match(rawOutput, @"score cp (-?\d+)");
        if (cpMatch.Success)
        {
            result = double.Parse(cpMatch.Groups[1].Value);
        }

        var mateMatch = Regex.Match(rawOutput, @"score mate (-?\d+)");
        if (mateMatch.Success)
        {
            var distance = int.Parse(mateMatch.Groups[1].Value);
            result = distance > 0 ? 10000 - distance : -10000 - distance;
        }

        return isWhiteTurn ? result : -result;
    }
}