namespace ChessBase.Kafka;

public class GameFetchedEvent
{
    public string Url { get; set; }
    public string Uuid { get; set; }
    public string Pgn { get; set; }
    public string TargetUsername { get; set; }
    public long EndTimeSeconds { get; set; }
}