using BezierSurfaces.Core.Bezier;
using BezierSurfaces.Core.Model;

namespace BezierSurfaces.Core.Mesh;

/// tnie każdą płachtę na trójkąty o zadanej gęstości
public static class SurfaceMeshBuilder
{
    /// buduje miejsca na płachcie i łączy sąsiednie oczka w pary trójkątów
    public static SurfaceMesh Build(IReadOnlyList<BezierSurface> surfaces, int divisions)
    {
        var mesh = new SurfaceMesh();
        // jeden podział to dwa trójkąty na całą płachtę, zero nie ma sensu
        int steps = Math.Max(1, divisions);
        for (int surfaceIndex = 0; surfaceIndex < surfaces.Count; surfaceIndex++)
        {
            AddSurface(mesh, surfaces[surfaceIndex], surfaceIndex, steps);
        }

        return mesh;
    }

    /// układa tabelkę miejsc i tnie każde oczko przekątną
    private static void AddSurface(SurfaceMesh mesh, BezierSurface surface, int surfaceIndex, int steps)
    {
        int count = steps + 1;
        var grid = new MeshVertex[count, count];
        for (int row = 0; row < count; row++)
        {
            // row = steps daje dokładnie 1, bez błędu zaokrąglenia w ostatnim kroku
            float u = row / (float)steps;
            for (int column = 0; column < count; column++)
            {
                float v = column / (float)steps;
                var vertex = new MeshVertex(
                    BezierPatch.Point(surface.ControlPoints, u, v),
                    u,
                    v,
                    surfaceIndex);
                grid[row, column] = vertex;
                mesh.Vertices.Add(vertex);
            }
        }

        for (int row = 0; row < steps; row++)
        {
            for (int column = 0; column < steps; column++)
            {
                MeshVertex v00 = grid[row, column];
                MeshVertex v10 = grid[row + 1, column];
                MeshVertex v11 = grid[row + 1, column + 1];
                MeshVertex v01 = grid[row, column + 1];
                // przekątna od (u, v) do (u + krok, v + krok), bo cztery rogi nie leżą na jednej płaszczyźnie
                mesh.Triangles.Add(new MeshTriangle(v00, v10, v11, surfaceIndex));
                mesh.Triangles.Add(new MeshTriangle(v00, v11, v01, surfaceIndex));
            }
        }
    }
}
