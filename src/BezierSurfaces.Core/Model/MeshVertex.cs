using System.Numerics;

namespace BezierSurfaces.Core.Model;

/// jedno miejsce na płachcie, policzone dla pary u i v
public sealed class MeshVertex
{
    public MeshVertex(Vector3 position, float u, float v, int surfaceIndex)
    {
        Position = position;
        PositionView = position;
        U = u;
        V = v;
        SurfaceIndex = surfaceIndex;
    }

    public Vector3 Position { get; }

    // ten sam punkt po aktualnym obrocie, rysunek bierze właśnie jego
    public Vector3 PositionView { get; set; }

    public float U { get; }

    public float V { get; }

    public int SurfaceIndex { get; }
}
