using Chess;
using ChessBase.Data;

namespace ChessBase;

public class GameAnalyzer(IEngine engine)
{
    public async Task<AnalysisReport> AnalyzePgnAsync(string pgn, string username) // todo test
    {
        var board = ChessBoard.LoadFromPgn(pgn);
        
        var ecoCode = board.Headers.GetValueOrDefault("ECO", "0");
        var openingName = board.Headers.GetValueOrDefault("Opening", "Unknown");
    
        var userIsWhite =
            username.Equals(board.Headers.GetValueOrDefault("White", ""), StringComparison.OrdinalIgnoreCase); //todo if hasn't header -> wrong behaviour
        var gameResult = MapResult(board.Headers.GetValueOrDefault("Result", "*"), userIsWhite);
        
        var evalMoves = ConvertToEvalMoves(await EngineAnalyzeToWhiteScoreAsync(board, userIsWhite));

        return new AnalysisReport
        {
            EcoCode = ecoCode,
            OpeningName = openingName,
            ResultForUser = gameResult,
            TotalAccuracy = ChessMath.CalculateGameAccuracy(evalMoves),
            Moves = evalMoves.Select((move, i) => new MoveAnalysis()
            {
                EvalWhite = move.EvalWhite,
                Accuracy = move.Accuracy.MoveAccuracyToWhite,
                Category = move.Accuracy.Type.ToString(),
                GameStage = DetectStage(board, i).ToString(),
                Notation = move.Notation,
                MoveNumber = ++i
            }).ToList()
        };
    }

    private async Task<List<EvalResultToWhiteScore>> EngineAnalyzeToWhiteScoreAsync(ChessBoard board, bool userIsWhite)
    {
        var moves = new List<EvalResultToWhiteScore>();

        for (var i = 0; i < board.ExecutedMoves.Count; i++)
        {
            var isWhiteTurn = i % 2 == 0;
            if (IsNotMyMove()) continue;

            board.MoveIndex = i;

            var analysis = await engine.GetEvaluationAsync(board.ToFen());

            moves.Add(UciParser.Parse(analysis, isWhiteTurn));

            bool IsNotMyMove() => !((!userIsWhite && !isWhiteTurn) || (userIsWhite && isWhiteTurn));
        }

        return moves;
    }

    private string MapResult(string rawResult, bool isWhite)
    {
        if (rawResult == "1/2-1/2") return "Draw";
        if (rawResult == "1-0") return isWhite ? "Win" : "Loss";
        if (rawResult == "0-1") return isWhite ? "Loss" : "Win";
        return "Unknown"; // todo Enum?
    }

    private List<EvalMove> ConvertToEvalMoves(List<EvalResultToWhiteScore> moves)
    {
        var evalMoves = new List<EvalMove>();
        var prevEvalCp = 0.0;
        foreach (var move in moves)
        {
            var accuracy = ChessMath.GetAccuracy(move.EvalWhite, prevEvalCp);
            evalMoves.Add(new EvalMove { Accuracy = accuracy, EvalWhite = move.EvalWhite, Notation = move.Notation });
            prevEvalCp = move.EvalWhite;
        }

        return evalMoves;
    }

    private static GameStage DetectStage(ChessBoard board, int moveIndex)
    {
        board.MoveIndex = moveIndex;
        if (board.ExecutedMoves.Count < 22) 
            return GameStage.Opening;

        const int totalPiceMaterial = 31 * 2; 

        var whiteLost = board.CapturedWhite.Sum(p => p.Type.Weight());
        var blackLost = board.CapturedBlack.Sum(p => p.Type.Weight());

        var totalNpm = totalPiceMaterial - whiteLost - blackLost;

        return totalNpm <= 24 ? GameStage.Endgame : GameStage.Middlegame;
    }    
}