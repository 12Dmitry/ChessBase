using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace ChessBase;

public class StockfishEngine : IEngine, IDisposable
{
    private readonly Process _process;
    private readonly StreamWriter _input;
    private readonly StreamReader _output;

    public StockfishEngine(IOptions<StockfishOptions> options)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.Path))
        {
            throw new InvalidOperationException("Stockfish path is not configured in appsettings.json");
        }

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = settings.Path,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        
        _process.Start();
        _process.PriorityClass = ProcessPriorityClass.BelowNormal;
        
        _input = _process.StandardInput;
        _output = _process.StandardOutput;

        _input.WriteLine("uci"); 
        
        _input.WriteLine($"setoption name Threads value {settings.Threads}");
        _input.WriteLine($"setoption name Hash value {settings.Hash}"); 
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

    public void Dispose()
    {
        try
        {
            if (!_process.HasExited)
            {
                _input.WriteLine("quit");
                _process.WaitForExit(1000);
                _process.Kill();
            }
        }
        catch
        {
            // ignored
        }
        finally
        {
            _input.Dispose();
            _output.Dispose();
            _process.Dispose();
        }
    }
}