using System.Numerics;

namespace BezierSurfaces.Core.Model;

// 4×4 punkty, wiersz i idzie z parametrem u, kolumna j z parametrem v
public sealed class ControlGrid
{
    private readonly Vector3[] _points = new Vector3[16];

    public Vector3 this[int row, int column]
    {
        get => _points[row * 4 + column];
        set => _points[row * 4 + column] = value;
    }
}
