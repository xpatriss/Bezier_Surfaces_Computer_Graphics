namespace BezierSurfaces.Core.Model;

/// wszystkie miejsca i trójkąty aktualnego podziału
public sealed class SurfaceMesh
{
    public List<MeshVertex> Vertices { get; } = new();

    public List<MeshTriangle> Triangles { get; } = new();
}
