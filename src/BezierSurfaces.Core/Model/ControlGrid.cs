using System.Numerics;

namespace BezierSurfaces.Core.Model;

/// trzyma 16 punktów kontrolnych jednej powierzchni
// wiersz i idzie z parametrem u, kolumna j z parametrem v
public sealed class ControlGrid
{
    private readonly Vector3[] _points = new Vector3[16];

    /// zwraca albo ustawia punkt o podanym wierszu i kolumnie
    public Vector3 this[int row, int column]
    {
        // wiersz 0 zajmuje indeksy 0..3, wiersz 1 indeksy 4..7, dlatego skok wynosi 4
        get => _points[row * 4 + column];
        set => _points[row * 4 + column] = value;
    }
}
