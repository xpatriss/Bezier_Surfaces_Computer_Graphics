using System.Numerics;
using BezierSurfaces.Core.Geometry;
using BezierSurfaces.Core.Lighting;
using BezierSurfaces.Core.Mesh;

namespace BezierSurfaces.Core.Model;

/// trzyma powierzchnie, ich trójkąty, obrót i to, co ma być narysowane
public sealed class Scene
{
    public List<BezierSurface> Surfaces { get; } = new();

    public SurfaceMesh Mesh { get; private set; } = new();

    public DrawOptions Draw { get; } = new();

    public Matrix3 Orientation { get; private set; } = Matrix3.Identity;

    // światło stoi w układzie obserwatora, obrót bryły go nie rusza
    public Vector3 LightPosition { get; set; } = new(0.6f, 0.8f, 1.8f);

    public Vector3Color LightColor { get; set; } = new(1f, 1f, 1f);

    public float DiffuseWeight { get; set; } = 0.75f;

    public float SpecularWeight { get; set; } = 0.35f;

    public float Shininess { get; set; } = 24f;

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

    /// mnoży punkty i styczne przez aktualną macierz, normalną skraca do długości 1
    // punkty kontrolne zostają w pliku, obrót jest tylko przy rysowaniu
    // styczne kręcą się jak kierunki, bez dzielenia przez długość, bo mapa normalnych przyjdzie później
    public void ApplyOrientation()
    {
        foreach (MeshVertex vertex in Mesh.Vertices)
        {
            vertex.PositionView = Orientation.Transform(vertex.Position);
            vertex.TangentUView = Orientation.Transform(vertex.TangentU);
            vertex.TangentVView = Orientation.Transform(vertex.TangentV);
            Vector3 rotatedNormal = Orientation.Transform(vertex.Normal);
            vertex.NormalView = VectorMath.NormalizeOrZero(rotatedNormal);
        }
    }
}
