namespace ChessBase;

using System.Diagnostics;

public class StockfishEngine : IEngine
{
    private StreamWriter _input;
    private StreamReader _output;

    public StockfishEngine(string path)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = path,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.PriorityClass = ProcessPriorityClass.BelowNormal;
        _input = process.StandardInput;
        _output = process.StandardOutput;

        _input.WriteLine("uci"); // Инициализируем UCI режим [3]
        _input.WriteLine("setoption name Threads value 6");
        _input.WriteLine("setoption name Hash value 2048"); 
    }

    public async Task<string> GetEvaluationAsync(string fen, int depth = 17)
    {
        await _input.WriteLineAsync($"position fen {fen}");
        await _input.WriteLineAsync($"go depth {depth}");

        string line;
        var lastInfo = "";
        while ((line = await _output.ReadLineAsync())!= null)
        {
            if (line.StartsWith("info depth")) lastInfo = line; // Сохраняем последнюю строку с оценкой
            if (line.StartsWith("bestmove")) break; // Когда движок выдал лучший ход, анализ текущей позиции окончен
        }
        return lastInfo + line;
    }
}