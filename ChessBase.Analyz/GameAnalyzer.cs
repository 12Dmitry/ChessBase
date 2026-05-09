using Chess;

namespace ChessBase;

public class GameAnalyzer(IEngine engine)
{
    public async Task<List<EvalResultToWhiteScore>> AnalyzeAsync(ChessBoard chessBoard, bool userIsWhite)
    {
        var moves = new List<EvalResultToWhiteScore>();

        for (var i = 0; i < chessBoard.ExecutedMoves.Count; i++)
        {
            var isWhiteTurn = i % 2 == 0;
            if (IsNotMyMove()) continue;

            chessBoard.MoveIndex = i;

            var analysis = await engine.GetEvaluationAsync(chessBoard.ToFen());

            // Здесь мы получим что-то вроде: "info depth 10... score cp 15..."
            Console.WriteLine($"Ход {i}: {analysis}");

            moves.Add(UciParser.Parse(analysis, isWhiteTurn));

            bool IsNotMyMove() => !((!userIsWhite && !isWhiteTurn) || (userIsWhite && isWhiteTurn));
        }

        return moves;
    }
    
    public string GetGameStage(ChessBoard board)
    {
        if (board.ExecutedMoves.Count < 20) return "Opening";

        // Считаем баллы фигур на доске
        int npm = board.Pieces.Values
            .Where(p => p.Type!= PieceType.Pawn && p.Type!= PieceType.King)
            .Sum(p => p.Type switch {
                PieceType.Queen => 9,
                PieceType.Rook => 5,
                PieceType.Bishop => 3,
                PieceType.Knight => 3,
                _ => 0
            });

        return npm <= 24? "Endgame" : "Middlegame";
    }
    
    public string DetectStage(ChessBoard board) // todo доделатт
    {
        if (board.ExecutedMoves.Count < 20) return "Opening";

        // Считаем все фигуры на доске через свойства Gera.Chess
        board.
        int materialCount = board.Pieces.Values
            .Where(p => p.Type!= PieceType.Pawn && p.Type!= PieceType.King)
            .Sum(p => GetPieceValue(p.Type));

        return materialCount <= 24? "Endgame" : "Middlegame";
    }

    private List<EvalMove> Convert(List<EvalResultToWhiteScore> moves)
    {
        moves.
    }

    public async Task<object> AnalyzePgnAsync(object pgn)
    {
        throw new NotImplementedException();
    }
}