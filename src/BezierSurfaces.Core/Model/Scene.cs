using System.Numerics;
using BezierSurfaces.Core.Bezier;
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

    public float FitRadius { get; private set; } = 1.2f;

    // krzywa zostaje w układzie obserwatora, obrót bryły jej nie rusza
    public LightCurve LightPath { get; } = new();

    public float LightParameter { get; set; }

    public Vector3 CurrentLightPosition => BezierCurve.Point(LightPath.Points, LightParameter);

    public Vector3Color LightColor { get; set; } = new(1f, 1f, 1f);

    public float DiffuseWeight { get; set; } = 0.75f;

    public float SpecularWeight { get; set; } = 0.35f;

    public float Shininess { get; set; } = 24f;

    public bool UseNormalMap { get; set; }

    public RgbImage? NormalMap { get; set; }

    // ile odcinków ma przedział od 0 do 1 w każdym kierunku
    public int Divisions { get; set; } = 8;

    /// liczy trójkąty od nowa i nakłada na nie aktualny obrót
    public void RebuildMesh()
    {
        Mesh = SurfaceMeshBuilder.Build(Surfaces, Divisions);
        FitRadius = 1.2f;
        foreach (BezierSurface surface in Surfaces)
        {
            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    FitRadius = Math.Max(FitRadius, surface.ControlPoints[row, column].Length());
                }
            }
        }

        ApplyOrientation();
    }

    /// podmienia macierz obrotu i przelicza punkty na ekran
    public void ReplaceOrientation(Matrix3 orientation)
    {
        Orientation = orientation.Orthonormalized();
        ApplyOrientation();
    }

    /// mnoży punkty i styczne przez aktualną macierz, normalną skraca do długości 1
    // punkty kontrolne zostają w pliku, obrót jest tylko przy rysowaniu
    // styczne kręcą się jak kierunki, długość 1 dostają dopiero przy mapie normalnych
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
