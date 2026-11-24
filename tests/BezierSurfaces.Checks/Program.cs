using System.Numerics;
using BezierSurfaces.Core.Bezier;
using BezierSurfaces.Core.Files;
using BezierSurfaces.Core.Geometry;
using BezierSurfaces.Core.Lighting;
using BezierSurfaces.Core.Model;
using BezierSurfaces.Core.Raster;

Check("dane startowe, próbki i dopasowanie sceny", CheckGeometry);
Check("krawędzie wspólne ukośne i poziome", CheckSharedEdges);
Check("głębokość interpolowana i wybór powierzchni", CheckDepth);
Check("siatka bez prześwitywania zasłoniętych krawędzi", CheckVisibleWire);
Check("światło z obu stron i końcowe nasycenie RGB", CheckLighting);
Check("neutralna mapa i orientacja składowych", CheckNormalMap);
Check("domknięcie i wysokość toru lampy po edycji", CheckLightCurve);

static void Check(string label, Action action)
{
    action();
    Console.WriteLine($"OK  {label}");
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static bool Near(float first, float second)
{
    return MathF.Abs(first - second) < 0.0001f;
}

static void CheckGeometry()
{
    var scene = new Scene { Divisions = 8 };
    string file = Path.Combine(AppContext.BaseDirectory, "data", "surfaces.txt");
    List<ControlGrid> grids = SurfaceFileReader.Read(file);
    Require(grids.Count >= 2, "scena zawiera co najmniej dwa płaty");
    foreach (ControlGrid grid in grids)
    {
        Require(BezierPatch.Point(grid, 1f, 0f) == grid[3, 0], "narożnik płata pokrywa się z punktem kontrolnym");
        scene.Surfaces.Add(new BezierSurface(grid, new SurfaceAppearance()));
    }

    scene.RebuildMesh();
    Require(scene.Mesh.Triangles.Count == grids.Count * 2 * 8 * 8, "każde oczko daje dwa trójkąty");
    ScreenMap map = ScreenMap.Fit(420, 280, scene.FitRadius);
    var orientation = Matrix3.Identity;
    Matrix3 step = TurntableRotation.Increment(7, -4, TurnAxis.ScreenXAndY);
    for (int i = 0; i < 10_000; i++)
    {
        orientation = Matrix3.Multiply(step, orientation).Orthonormalized();
    }

    scene.ReplaceOrientation(orientation);
    foreach (MeshVertex vertex in scene.Mesh.Vertices)
    {
        Vector2 screen = map.ToScreen(vertex.PositionView);
        Require(screen.X >= 0 && screen.X < map.Width && screen.Y >= 0 && screen.Y < map.Height, "płat mieści się w panelu");
        Require(Near(vertex.Position.Length(), vertex.PositionView.Length()), "złożony obrót zachowuje długość");
        Require(Near(vertex.NormalView.Length(), 1f), "normalna po obrocie ma długość 1");
    }
}

static void CheckSharedEdges()
{
    var a = Vertex(0.5f, 0.5f);
    var b = Vertex(4.5f, 0.5f);
    var c = Vertex(4.5f, 4.5f);
    var d = Vertex(0.5f, 4.5f);
    FrameBuffer right = Render(new MeshTriangle(a, b, c, 0));
    FrameBuffer left = Render(new MeshTriangle(a, c, d, 0));
    for (int y = 0; y < 5; y++)
    {
        for (int x = 0; x < 5; x++)
        {
            bool coveredByRight = right.SurfaceAt(x, y) is not null;
            bool coveredByLeft = left.SurfaceAt(x, y) is not null;
            Require(!(coveredByLeft && coveredByRight), "przekątna ma tylko jednego właściciela");
            Require((coveredByLeft || coveredByRight) == (x < 4 && y < 4), "kwadrat bez szczelin i nadmiarowych pikseli");
        }
    }

    var sharedLeft = Vertex(0.5f, 2.5f);
    var sharedRight = Vertex(4.5f, 2.5f);
    FrameBuffer top = Render(new MeshTriangle(sharedLeft, sharedRight, Vertex(2.5f, 0.5f), 0));
    FrameBuffer bottom = Render(new MeshTriangle(sharedLeft, Vertex(2.5f, 4.5f), sharedRight, 0));
    for (int x = 0; x < 4; x++)
    {
        Require(top.SurfaceAt(x, 2) is null && bottom.SurfaceAt(x, 2) == 0, "wspólny poziomy wiersz należy do dolnego trójkąta");
    }
}

static void CheckDepth()
{
    var rising = new MeshTriangle(Vertex(0, 0, 0), Vertex(6, 0, 3), Vertex(0, 6, 0), 0);
    var level = new MeshTriangle(Vertex(0, 0, 1), Vertex(6, 0, 1), Vertex(0, 6, 1), 1);
    foreach (FrameBuffer frame in new[] { Render(rising, level), Render(level, rising) })
    {
        Require(frame.SurfaceAt(0, 0) == 1, "bliżej lewej wystaje poziomy trójkąt");
        Require(frame.SurfaceAt(3, 0) == 0, "bliżej prawej wystaje wznoszący trójkąt");
        Require(Near(frame.Depth[3], 1.75f), "głębokość jest interpolowana w środku piksela");
        Require(frame.SurfaceAt(7, 7) is null, "poza trójkątem jest tło");
    }
}

static void CheckLighting()
{
    var white = new Vector3Color(1f, 1f, 1f);
    var material = new Vector3Color(0.5f, 0.25f, 1f);
    Vector3Color front = LightingEquation.Compute(Vector3.UnitZ, Vector3.Zero, Vector3.UnitZ, white, material, 1, 1, 12);
    Vector3Color flipped = LightingEquation.Compute(-Vector3.UnitZ, Vector3.Zero, Vector3.UnitZ, white, material, 1, 1, 12);
    Vector3Color lightBehind = LightingEquation.Compute(Vector3.UnitZ, Vector3.Zero, -Vector3.UnitZ, white, material, 1, 1, 12);
    Require(front.Equals(flipped), "sam zwrot normalnej nie może zmieniać oświetlenia cienkiego płata");
    Require(lightBehind.Equals(material), "od tylnej lampy widać rozproszenie, bez odwróconego refleksu");
    Require(Near(front.B, 2f) && (LightingEquation.ToBgra(front) & 255) == 255, "nasycenie zachodzi dopiero przy końcowej konwersji");
}

static void CheckVisibleWire()
{
    var front = new MeshTriangle(Vertex(0.5f, 0.5f, 1), Vertex(6.5f, 0.5f, 1), Vertex(0.5f, 6.5f, 1), 0);
    var hidden = new MeshTriangle(Vertex(0.5f, 0.5f), Vertex(4.5f, 0.5f), Vertex(0.5f, 4.5f), 1);
    Vector3Color[] colors = [new(0, 1, 0), new(1, 0, 0)];
    FrameBuffer frontOnly = RenderWithWires([front], colors);
    FrameBuffer layered = RenderWithWires([hidden, front], colors);

    Require(frontOnly.Pixels.SequenceEqual(layered.Pixels), "czubki tylnej siatki nie zmieniają widocznej bitmapy");
    Require(frontOnly.Pixels[0] != Render(front).Pixels[0], "widoczna krawędź ma własną nakładkę");
}

static void CheckNormalMap()
{
    Vector3 neutral = RgbImage.DecodeNormal(new Vector3Color(127f / 255f, 127f / 255f, 1));
    Require(neutral == Vector3.UnitZ, "kolor (127,127,255) jest neutralny");
    Vector3 mapped = LightingEquation.ApplyNormalMap(Vector3.UnitY, Vector3.UnitZ, Vector3.UnitX, neutral);
    Require(mapped == Vector3.UnitX, "neutralna mapa zachowuje normalną po obrocie");
    Vector3 green = RgbImage.DecodeNormal(new Vector3Color(0.5f, 1f, 1f));
    mapped = LightingEquation.ApplyNormalMap(Vector3.UnitY, Vector3.UnitZ, Vector3.UnitX, green);
    Require(mapped.Z > 0 && mapped.Y == 0 && Near(mapped.Length(), 1f), "zielony przechyla normalną w stronę drugiej stycznej");
}

static void CheckLightCurve()
{
    var path = new LightCurve();
    path.MoveXy(0, 0.32f, -0.45f);
    path.SetZ(-0.7f);
    Require(path.Points[0] == path.Points[^1], "pierwszy punkt jest ostatnim");
    Require(BezierCurve.Point(path.Points, 0) == BezierCurve.Point(path.Points, 1), "koniec obiegu jest jego początkiem");
    Require(Near(BezierCurve.Point(path.Points, 0.7f).Z, path.Z), "lampa pozostaje na płaszczyźnie z = const");
}

static MeshVertex Vertex(float x, float y, float z = 0)
{
    return new MeshVertex(new Vector3(x, -y, z), 0, 0, 0) { NormalView = Vector3.UnitZ };
}

static FrameBuffer Render(params MeshTriangle[] triangles)
{
    return RenderWithWires(triangles, []);
}

static FrameBuffer RenderWithWires(MeshTriangle[] triangles, ReadOnlySpan<Vector3Color> colors)
{
    var scene = new Scene();
    scene.Surfaces.Add(new BezierSurface(new ControlGrid(), new SurfaceAppearance()));
    scene.Surfaces.Add(new BezierSurface(new ControlGrid(), new SurfaceAppearance()));
    scene.Mesh.Triangles.AddRange(triangles);
    var frame = new FrameBuffer();
    frame.Resize(8, 8);
    frame.Clear(0);
    new EdgeBucketRasterizer(frame.Height).DrawMesh(scene, new ScreenMap(8, 8, 1f, 0, 0), frame, new Vector3(0, 0, 5), colors);
    return frame;
}
