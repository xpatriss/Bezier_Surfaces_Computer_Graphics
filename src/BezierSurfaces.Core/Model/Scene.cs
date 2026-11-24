using BezierSurfaces.Core.Mesh;

namespace BezierSurfaces.Core.Model;

/// trzyma powierzchnie, ich trójkąty i to, co ma być narysowane
public sealed class Scene
{
    public List<BezierSurface> Surfaces { get; } = new();

    public SurfaceMesh Mesh { get; private set; } = new();

    public DrawOptions Draw { get; } = new();

    // ile odcinków ma przedział od 0 do 1 w każdym kierunku
    public int Divisions { get; set; } = 8;

    /// liczy trójkąty od nowa dla aktualnego podziału
    public void RebuildMesh()
    {
        Mesh = SurfaceMeshBuilder.Build(Surfaces, Divisions);
    }
}
