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

    public Vector3 PositionView { get; set; }

    public Vector3 TangentU { get; set; }

    public Vector3 TangentV { get; set; }

    public Vector3 Normal { get; set; }

    public Vector3 TangentUView { get; set; }

    public Vector3 TangentVView { get; set; }

    // normalna po obrocie, już o długości 1
    public Vector3 NormalView { get; set; }

    public float U { get; }

    public float V { get; }

    public int SurfaceIndex { get; }
}
