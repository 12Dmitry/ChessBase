using Chess;
using ChessBase.Data;

namespace ChessBase;

public class GameAnalyzer(IEngine engine)
{
    public async Task<AnalysisReport> AnalyzePgnAsync(string pgn, string username)
    {
        var board = ChessBoard.LoadFromPgn(pgn);
        
        var ecoCode = board.Headers.GetValueOrDefault("ECO", "0");
        var openingName = board.Headers.GetValueOrDefault("Opening", "Unknown");
    
        var userIsWhite = username.Equals(board.Headers.GetValueOrDefault("White", ""), StringComparison.OrdinalIgnoreCase);
        var gameResult = MapResult(board.Headers.GetValueOrDefault("Result", "*"), userIsWhite);
        
        var analyzedMoves = await EngineAnalyzeToWhiteScoreAsync(board, userIsWhite);
        var evalMoves = ConvertToEvalMoves(analyzedMoves, userIsWhite);

        return new AnalysisReport
        {
            EcoCode = ecoCode,
            OpeningName = openingName,
            UserIsWhite = userIsWhite,
            ResultForUser = gameResult,
            TotalAccuracy = ChessMath.CalculateGameAccuracy(evalMoves),
            Moves = evalMoves.Select(move => new MoveAnalysis
            {
                EvalWhite = move.EvalWhite,
                Accuracy = move.Accuracy.MoveAccuracyToWhite,
                Category = move.Accuracy.Type.ToString(),
                GameStage = DetectStage(board, move.PlyIndex).ToString(),
                Notation = move.Notation,
                MoveNumber = move.PlyIndex / 2 + 1
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

            var notation = board.ExecutedMoves[i].San;

            // 1. Оценка ЛУЧШЕГО хода (позиция ДО хода пользователя)
            board.MoveIndex = i - 1; 
            var analysisBefore = await engine.GetEvaluationAsync(board.ToFen());
            var bestMoveEval = UciParser.ParseToWhiteEval(analysisBefore, isWhiteTurn);

            // 2. Оценка СДЕЛАННОГО хода (позиция ПОСЛЕ хода пользователя)
            board.MoveIndex = i;
            var analysisAfter = await engine.GetEvaluationAsync(board.ToFen());
            // Важно: после хода очередь переходит к противнику (!isWhiteTurn), 
            // передаем это для правильной конвертации в оценку за белых.
            var actualMoveEval = UciParser.ParseToWhiteEval(analysisAfter, !isWhiteTurn);

            moves.Add(new EvalResultToWhiteScore
            {
                Notation = notation,
                BestMoveEval = bestMoveEval,
                ActualMoveEval = actualMoveEval,
                PlyIndex = i
            });
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

    private List<EvalMove> ConvertToEvalMoves(List<EvalResultToWhiteScore> moves, bool userIsWhite)
    {
        var evalMoves = new List<EvalMove>();
        foreach (var move in moves)
        {
            var accuracy = ChessMath.CalculateMoveAccuracy(move.ActualMoveEval, move.BestMoveEval, userIsWhite);
            evalMoves.Add(new EvalMove 
            { 
                Accuracy = accuracy, 
                EvalWhite = move.ActualMoveEval, 
                Notation = move.Notation,
                PlyIndex = move.PlyIndex
            });
        }
        return evalMoves;
    }

    private static GameStage DetectStage(ChessBoard board, int moveIndex)
    {
        board.MoveIndex = moveIndex;
        if (moveIndex < 22) 
            return GameStage.Opening;

        const int endgameMaterialThreshold = 24;
        const int initialNonPawnKingMaterial = 31 * 2; 

        var whiteLost = board.CapturedWhite.Sum(p => p.Type.Weight());
        var blackLost = board.CapturedBlack.Sum(p => p.Type.Weight());

        var totalNpm = initialNonPawnKingMaterial - whiteLost - blackLost;

        return totalNpm <= endgameMaterialThreshold ? GameStage.Endgame : GameStage.Middlegame;
    }    
}