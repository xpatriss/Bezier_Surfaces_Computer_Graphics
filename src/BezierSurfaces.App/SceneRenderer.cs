using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Numerics;
using BezierSurfaces.Core.Bezier;
using BezierSurfaces.Core.Geometry;
using BezierSurfaces.Core.Lighting;
using BezierSurfaces.Core.Model;
using BezierSurfaces.Core.Raster;

namespace BezierSurfaces.App;

internal sealed class SceneRenderer(Scene scene) : IDisposable
{
    private readonly Scene _scene = scene;
    private readonly FrameBuffer _frame = new();
    private readonly EdgeBucketRasterizer _rasterizer = new(1);
    private Bitmap? _bitmap;
    private static readonly Vector3Color[] WireColors =
    [
        new(0.24f, 0.58f, 0.43f),
        new(0.29f, 0.44f, 0.86f)
    ];

    public int? VisibleSurface(Point location)
    {
        return _frame.SurfaceAt(location.X, location.Y);
    }

    public void Draw(Graphics graphics, ScreenMap map)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        DrawAxes(graphics, map);
        if (_scene.Draw.FilledTriangles)
        {
            DrawFilled(graphics, map, map.Width, map.Height);
        }

        else if (_scene.Draw.TriangleMesh)
        {
            DrawMesh(graphics, map);
        }

        if (_scene.Draw.ControlPolygons)
        {
            for (int index = 0; index < _scene.Surfaces.Count; index++)
            {
                DrawControlPolygon(
                    graphics,
                    map,
                    _scene.Surfaces[index].ControlPoints,
                    _scene.Orientation,
                    ColorFor(index));
            }
        }

        DrawLight(graphics, map);
    }

    /// wypełnia trójkąty do bitmapy i kładzie ją pod liniami
    // kolor liczy wzór światła w każdym pikselu, o wygranej decyduje większe z
    private void DrawFilled(Graphics graphics, ScreenMap map, int width, int height)
    {
        _frame.Resize(width, height);
        _frame.Clear(0);
        ReadOnlySpan<Vector3Color> wireColors = _scene.Draw.TriangleMesh ? WireColors : [];
        _rasterizer.DrawMesh(_scene, map, _frame, _scene.CurrentLightPosition, wireColors);
        if (_bitmap is null || _bitmap.Width != width || _bitmap.Height != height)
        {
            _bitmap?.Dispose();
            _bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        var bits = _bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);
        try
        {
            _frame.CopyTo(bits.Scan0, bits.Stride);
        }
        finally
        {
            _bitmap.UnlockBits(bits);
        }

        graphics.DrawImageUnscaled(_bitmap, 0, 0);
    }

    /// rysuje boki trójkątów leżących na płachcie
    private void DrawMesh(Graphics graphics, ScreenMap map)
    {
        using var first = new Pen(Color.FromArgb(145, CurveColorFor(0)), 1f);
        using var second = new Pen(Color.FromArgb(145, CurveColorFor(1)), 1f);
        foreach (MeshTriangle triangle in _scene.Mesh.Triangles)
        {
            Pen pen = triangle.SurfaceIndex % 2 == 0 ? first : second;
            DrawSegment(graphics, pen, map, triangle.A.PositionView, triangle.B.PositionView);
            DrawSegment(graphics, pen, map, triangle.B.PositionView, triangle.C.PositionView);
            DrawSegment(graphics, pen, map, triangle.C.PositionView, triangle.A.PositionView);
        }
    }

    /// rysuje szary krzyż osi przez środek okna
    private static void DrawAxes(Graphics graphics, ScreenMap map)
    {
        using var pen = new Pen(Color.FromArgb(180, 190, 190, 190), 1f);
        graphics.DrawLine(pen, 0, map.CenterY, map.Width, map.CenterY);
        graphics.DrawLine(pen, map.CenterX, 0, map.CenterX, map.Height);
    }

    /// rysuje drogę światła, jej punkty kontrolne i samą lampę
    // krzywa jest w układzie okna, więc nie mnożymy jej przez obrót bryły
    private void DrawLight(Graphics graphics, ScreenMap map)
    {
        using var curvePen = new Pen(Color.FromArgb(205, 192, 99, 15), 1.4f);
        using var handleBrush = new SolidBrush(Color.FromArgb(215, 191, 83, 18));
        using var lampBrush = new SolidBrush(Color.Gold);
        using var lampOutline = new Pen(Color.SaddleBrown, 1f);
        Vector2 previous = map.ToScreen(BezierCurve.Point(_scene.LightPath.Points, 0f));
        const int samples = 80;
        for (int i = 1; i <= samples; i++)
        {
            float t = i / (float)samples;
            Vector2 next = map.ToScreen(BezierCurve.Point(_scene.LightPath.Points, t));
            graphics.DrawLine(curvePen, previous.X, previous.Y, next.X, next.Y);
            previous = next;
        }

        for (int i = 0; i < LightCurve.ControlPointCount - 1; i++)
        {
            Vector2 screen = map.ToScreen(_scene.LightPath.Points[i]);
            graphics.FillRectangle(handleBrush, screen.X - 4f, screen.Y - 4f, 8f, 8f);
        }

        Vector2 lamp = map.ToScreen(_scene.CurrentLightPosition);
        var marker = new RectangleF(lamp.X - 5f, lamp.Y - 5f, 10f, 10f);
        graphics.FillEllipse(lampBrush, marker);
        graphics.DrawEllipse(lampOutline, marker);
    }

    /// łączy sąsiednie punkty kontrolne i zaznacza same punkty
    private static void DrawControlPolygon(
        Graphics graphics,
        ScreenMap map,
        ControlGrid grid,
        Matrix3 orientation,
        Color color)
    {
        using var pen = new Pen(color, 1.6f);
        using var brush = new SolidBrush(color);
        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                DrawSegment(
                    graphics,
                    pen,
                    map,
                    orientation.Transform(grid[row, column]),
                    orientation.Transform(grid[row, column + 1]));
            }
        }

        for (int column = 0; column < 4; column++)
        {
            for (int row = 0; row < 3; row++)
            {
                DrawSegment(
                    graphics,
                    pen,
                    map,
                    orientation.Transform(grid[row, column]),
                    orientation.Transform(grid[row + 1, column]));
            }
        }

        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                Vector2 screen = map.ToScreen(orientation.Transform(grid[row, column]));
                graphics.FillRectangle(brush, screen.X - 3f, screen.Y - 3f, 6f, 6f);
            }
        }
    }

    /// rysuje jeden odcinek między dwoma już obróconymi punktami
    // rzut prostokątny: na ekran idą x i y, z po obrocie już siedzi w tych dwóch liczbach
    private static void DrawSegment(Graphics graphics, Pen pen, ScreenMap map, Vector3 start, Vector3 end)
    {
        Vector2 a = map.ToScreen(start);
        Vector2 b = map.ToScreen(end);
        graphics.DrawLine(pen, a.X, a.Y, b.X, b.Y);
    }

    /// wybiera jaśniejszy kolor boków trójkątów
    private static Color CurveColorFor(int surfaceIndex)
    {
        return surfaceIndex % 2 == 0 ? Color.SeaGreen : Color.RoyalBlue;
    }

    /// wybiera kolor wieloboku: zielony dla pierwszej powierzchni, niebieski dla drugiej
    private static Color ColorFor(int surfaceIndex)
    {
        return surfaceIndex % 2 == 0 ? Color.DarkGreen : Color.DarkBlue;
    }

    public void Dispose()
    {
        _bitmap?.Dispose();
    }
}
