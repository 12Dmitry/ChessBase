namespace ChessBase;

public class StockfishOptions
{
    public string Path { get; set; } = string.Empty;
    public int Threads { get; set; } = 2;
    public int Hash { get; set; } = 256;
}