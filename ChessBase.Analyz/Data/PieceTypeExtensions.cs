using Chess;

namespace ChessBase.Data;

public static class PieceTypeExtensions
{
    public static int Weight(this PieceType type)
    {
        if (type == PieceType.Queen)  return 9;
        if (type == PieceType.Rook)   return 5;
        return type == PieceType.Bishop || type == PieceType.Knight ? 3 : 0;
    }
}