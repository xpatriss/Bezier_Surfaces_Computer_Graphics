using System.Numerics;

namespace BezierSurfaces.Core.Model;

/// trzyma siedem punktów kontrolnych drogi światła
// szósty jest kopią zerowego, więc krzywa zaczyna się i kończy w tym samym miejscu
public sealed class LightCurve
{
    public const int ControlPointCount = 7;

    /// układa sześć punktów na okręgu i domyka krzywą kopią pierwszego
    public LightCurve()
    {
        const float radius = 1.05f;
        for (int i = 0; i < 6; i++)
        {
            float angle = i * MathF.PI / 3f;
            Points[i] = new Vector3(radius * MathF.Cos(angle), radius * MathF.Sin(angle), 1.0f);
        }

        Points[6] = Points[0];
    }

    public Vector3[] Points { get; } = new Vector3[ControlPointCount];

    public float Z => Points[0].Z;

    /// przesuwa punkt w płaszczyźnie stałego z
    // indeks 6 to ten sam punkt co 0, więc ruszamy oryginał i odświeżamy kopię
    public void MoveXy(int index, float x, float y)
    {
        int slot = index == 6 ? 0 : index;
        float z = Points[0].Z;
        Points[slot] = new Vector3(x, y, z);
        Points[6] = Points[0];
    }

    /// ustawia tę samą wysokość z wszystkim punktom
    public void SetZ(float z)
    {
        for (int i = 0; i < 6; i++)
        {
            Points[i] = new Vector3(Points[i].X, Points[i].Y, z);
        }

        Points[6] = Points[0];
    }
}
