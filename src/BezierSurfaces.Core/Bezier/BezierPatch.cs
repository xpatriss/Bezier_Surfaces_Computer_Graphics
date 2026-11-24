using System.Numerics;
using BezierSurfaces.Core.Model;

namespace BezierSurfaces.Core.Bezier;

/// liczy punkt, styczne i normalną płata stopnia 3
public static class BezierPatch
{
    /// liczy punkt powierzchni jako ważoną sumę 16 punktów kontrolnych
    public static Vector3 Point(ControlGrid grid, float u, float v)
    {
        Span<float> alongU = stackalloc float[4];
        Span<float> alongV = stackalloc float[4];
        BernsteinBasis.Degree3(u, alongU);
        BernsteinBasis.Degree3(v, alongV);

        Vector3 sum = Vector3.Zero;
        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                float weight = alongU[row] * alongV[column];
                sum += grid[row, column] * weight;
            }
        }

        return sum;
    }

    /// liczy wektor styczny przy zmianie u i ustalonym v
    // stopień po u spada do 2, a różnica sąsiadów w wierszu jest krawędzią siatki
    // mnożnik 3 to stopień płata ze wzoru na pochodną
    public static Vector3 TangentU(ControlGrid grid, float u, float v)
    {
        Span<float> alongU = stackalloc float[3];
        Span<float> alongV = stackalloc float[4];
        BernsteinBasis.Degree2(u, alongU);
        BernsteinBasis.Degree3(v, alongV);

        Vector3 sum = Vector3.Zero;
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                Vector3 edge = grid[row + 1, column] - grid[row, column];
                float weight = alongU[row] * alongV[column];
                sum += edge * weight;
            }
        }

        return 3f * sum;
    }

    /// liczy wektor styczny przy zmianie v i ustalonym u
    public static Vector3 TangentV(ControlGrid grid, float u, float v)
    {
        Span<float> alongU = stackalloc float[4];
        Span<float> alongV = stackalloc float[3];
        BernsteinBasis.Degree3(u, alongU);
        BernsteinBasis.Degree2(v, alongV);

        Vector3 sum = Vector3.Zero;
        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                Vector3 edge = grid[row, column + 1] - grid[row, column];
                float weight = alongU[row] * alongV[column];
                sum += edge * weight;
            }
        }

        return 3f * sum;
    }

    /// liczy normalną jako iloczyn wektorowy pu i pv
    // wynik nie ma jeszcze długości 1
    public static Vector3 Normal(ControlGrid grid, float u, float v)
    {
        return Vector3.Cross(TangentU(grid, u, v), TangentV(grid, u, v));
    }
}
