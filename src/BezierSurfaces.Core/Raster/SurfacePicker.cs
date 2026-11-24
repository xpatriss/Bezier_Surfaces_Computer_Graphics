using System.Numerics;
using BezierSurfaces.Core.Geometry;
using BezierSurfaces.Core.Model;

namespace BezierSurfaces.Core.Raster;

/// wskazuje powierzchnię pod kursorem, tę z większym z
public static class SurfacePicker
{
    /// zwraca numer powierzchni albo nic, gdy kursor jest poza trójkątami
    public static int? FrontSurface(SurfaceMesh mesh, ScreenMap map, float screenX, float screenY)
    {
        int? found = null;
        float bestZ = float.NegativeInfinity;
        foreach (MeshTriangle triangle in mesh.Triangles)
        {
            Vector2 a = map.ToScreen(triangle.A.PositionView);
            Vector2 b = map.ToScreen(triangle.B.PositionView);
            Vector2 c = map.ToScreen(triangle.C.PositionView);
            float area = Area(b, c, a.X, a.Y);
            if (MathF.Abs(area) < 1e-4f)
            {
                continue;
            }

            float alpha = Area(b, c, screenX, screenY) / area;
            float beta = Area(c, a, screenX, screenY) / area;
            float gamma = 1f - alpha - beta;
            if (alpha < -0.001f || beta < -0.001f || gamma < -0.001f)
            {
                continue;
            }

            float z =
                alpha * triangle.A.PositionView.Z +
                beta * triangle.B.PositionView.Z +
                gamma * triangle.C.PositionView.Z;
            if (z > bestZ)
            {
                bestZ = z;
                found = triangle.SurfaceIndex;
            }
        }

        return found;
    }

    /// pole równoległoboku od odcinka do punktu, znak mówi po której stronie leży punkt
    private static float Area(Vector2 start, Vector2 end, float x, float y)
    {
        return (end.Y - start.Y) * (x - start.X) + (start.X - end.X) * (y - start.Y);
    }
}
