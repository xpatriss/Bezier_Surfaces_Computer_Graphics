using BezierSurfaces.Core.Geometry;
using BezierSurfaces.Core.Mesh;

namespace BezierSurfaces.Core.Model;

/// trzyma powierzchnie, ich trójkąty, obrót i to, co ma być narysowane
public sealed class Scene
{
    public List<BezierSurface> Surfaces { get; } = new();

    public SurfaceMesh Mesh { get; private set; } = new();

    public DrawOptions Draw { get; } = new();

    public Matrix3 Orientation { get; private set; } = Matrix3.Identity;

    // ile odcinków ma przedział od 0 do 1 w każdym kierunku
    public int Divisions { get; set; } = 8;

    /// liczy trójkąty od nowa i nakłada na nie aktualny obrót
    public void RebuildMesh()
    {
        Mesh = SurfaceMeshBuilder.Build(Surfaces, Divisions);
        ApplyOrientation();
    }

    /// podmienia macierz obrotu i przelicza punkty na ekran
    public void ReplaceOrientation(Matrix3 orientation)
    {
        Orientation = orientation;
        ApplyOrientation();
    }

    /// mnoży każdy punkt płata przez aktualną macierz
    // punkty kontrolne zostają w pliku, obrót jest tylko przy rysowaniu
    public void ApplyOrientation()
    {
        foreach (MeshVertex vertex in Mesh.Vertices)
        {
            vertex.PositionView = Orientation.Transform(vertex.Position);
        }
    }
}
