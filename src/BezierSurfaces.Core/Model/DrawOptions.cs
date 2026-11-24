namespace BezierSurfaces.Core.Model;

/// które warstwy rysunku są włączone
public sealed class DrawOptions
{
    public bool ControlPolygons { get; set; } = true;

    public bool TriangleMesh { get; set; } = true;

    public bool FilledTriangles { get; set; } = true;
}
