using System.Numerics;

namespace BezierSurfaces.Core.Model;

/// trzyma siedem punktów kontrolnych drogi światła
// szósty jest kopią zerowego, więc krzywa zaczyna się i kończy w tym samym miejscu
public sealed class LightCurve
{
    public const int ControlPointCount = 7;
    private const int ClosingPoint = ControlPointCount - 1;
    private readonly Vector3[] _points = new Vector3[ControlPointCount];

    /// układa sześć punktów na okręgu i domyka krzywą kopią pierwszego
    public LightCurve()
    {
        const float radius = 1.05f;
        for (int i = 0; i < ClosingPoint; i++)
        {
            float angle = i * MathF.PI / 3f;
            _points[i] = new Vector3(radius * MathF.Cos(angle), radius * MathF.Sin(angle), 1.0f);
        }

        _points[ClosingPoint] = _points[0];
        Points = Array.AsReadOnly(_points);
    }

    public IReadOnlyList<Vector3> Points { get; }

    public float Z => Points[0].Z;

    /// przesuwa punkt w płaszczyźnie stałego z
    // indeks 6 to ten sam punkt co 0, więc ruszamy oryginał i odświeżamy kopię
    public void MoveXy(int index, float x, float y)
    {
        int slot = index == ClosingPoint ? 0 : index;
        _points[slot] = new Vector3(x, y, Z);
        _points[ClosingPoint] = _points[0];
    }

    /// ustawia tę samą wysokość z wszystkim punktom
    public void SetZ(float z)
    {
        for (int i = 0; i < ClosingPoint; i++)
        {
            _points[i] = new Vector3(_points[i].X, _points[i].Y, z);
        }

        _points[ClosingPoint] = _points[0];
    }
}
