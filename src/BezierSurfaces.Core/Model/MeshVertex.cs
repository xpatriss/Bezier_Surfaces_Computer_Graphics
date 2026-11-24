using System.Numerics;

namespace BezierSurfaces.Core.Model;

/// jedno miejsce na płachcie, policzone dla pary u i v
public sealed class MeshVertex
{
    public MeshVertex(Vector3 position, float u, float v, int surfaceIndex)
    {
        Position = position;
        U = u;
        V = v;
        SurfaceIndex = surfaceIndex;
    }

    public Vector3 Position { get; }

    public float U { get; }

    public float V { get; }

    public int SurfaceIndex { get; }
}
