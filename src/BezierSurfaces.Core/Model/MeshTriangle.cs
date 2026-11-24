namespace BezierSurfaces.Core.Model;

/// trzy miejsca na płachcie, które tworzą jeden płaski trójkąt
public sealed class MeshTriangle
{
    public MeshTriangle(MeshVertex a, MeshVertex b, MeshVertex c, int surfaceIndex)
    {
        A = a;
        B = b;
        C = c;
        SurfaceIndex = surfaceIndex;
    }

    public MeshVertex A { get; }

    public MeshVertex B { get; }

    public MeshVertex C { get; }

    public int SurfaceIndex { get; }
}
