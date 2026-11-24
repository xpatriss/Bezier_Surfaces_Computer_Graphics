using System.Numerics;
using BezierSurfaces.Core.Geometry;
using BezierSurfaces.Core.Lighting;
using BezierSurfaces.Core.Model;

namespace BezierSurfaces.Core.Raster;

/// maluje trójkąty wiersz po wierszu, krawędzie trzyma w kubełkach
public sealed class EdgeBucketRasterizer
{
    private List<ActiveEdge>[] _buckets;
    private readonly List<int> _usedRows = new();
    private readonly List<ActiveEdge> _active = new();

    /// przygotowuje kubełek na każdy wiersz obrazu
    public EdgeBucketRasterizer(int capacity)
    {
        _buckets = CreateBuckets(Math.Max(1, capacity));
    }

    /// powiększa kubełki, gdy okno jest wyższe niż przy poprzednim rysowaniu
    public void EnsureHeight(int height)
    {
        if (height <= _buckets.Length)
        {
            return;
        }

        _buckets = CreateBuckets(height);
        _usedRows.Clear();
    }

    /// wypełnia trójkąty kolorem licznym osobno w każdym pikselu
    public void DrawMesh(
        Scene scene,
        ScreenMap map,
        FrameBuffer frame,
        Vector3 lightPosition,
        ReadOnlySpan<Vector3Color> wireColors = default)
    {
        EnsureHeight(frame.Height);
        var shader = new FragmentShader { Scene = scene, LightPosition = lightPosition };
        foreach (MeshTriangle triangle in scene.Mesh.Triangles)
        {
            Vector3Color? wireColor = wireColors.IsEmpty ? null : wireColors[triangle.SurfaceIndex % wireColors.Length];
            DrawTriangle(triangle, map, frame, shader, wireColor);
        }
    }

    /// wkłada trzy krawędzie do kubełków i maluje kolejne wiersze
    private void DrawTriangle(
        MeshTriangle triangle,
        ScreenMap map,
        FrameBuffer frame,
        FragmentShader shader,
        Vector3Color? wireColor)
    {
        Vector2 a = map.ToScreen(triangle.A.PositionView);
        Vector2 b = map.ToScreen(triangle.B.PositionView);
        Vector2 c = map.ToScreen(triangle.C.PositionView);
        double area = SignedArea(b, c, a.X, a.Y);
        // trzy punkty na jednej prostej nie mają wnętrza
        if (Math.Abs(area) < 1e-4)
        {
            return;
        }

        ClearBuckets();
        AddEdge(a, b, frame.Height);
        AddEdge(b, c, frame.Height);
        AddEdge(c, a, frame.Height);
        if (_usedRows.Count == 0)
        {
            return;
        }

        int yMin = _usedRows.Min();
        int yMax = yMin;
        foreach (int row in _usedRows)
        {
            foreach (ActiveEdge edge in _buckets[row])
            {
                if (edge.YEnd > yMax)
                {
                    yMax = edge.YEnd;
                }
            }
        }

        double inverseArea = 1.0 / area;
        float absoluteArea = (float)Math.Abs(area);
        var wireHeights = wireColor.HasValue
            ? new Vector3(absoluteArea / Vector2.Distance(b, c), absoluteArea / Vector2.Distance(c, a), absoluteArea / Vector2.Distance(a, b))
            : Vector3.Zero;
        _active.Clear();
        for (int y = yMin; y < yMax; y++)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].YEnd <= y)
                {
                    _active.RemoveAt(i);
                }
            }

            _active.AddRange(_buckets[y]);
            _active.Sort(static (left, right) => left.X.CompareTo(right.X));
            FillPairs(triangle, a, b, c, inverseArea, y, frame, shader, wireColor, wireHeights);
            for (int i = 0; i < _active.Count; i++)
            {
                ActiveEdge edge = _active[i];
                _active[i] = edge with { X = edge.X + edge.DxPerDy };
            }
        }
    }

    /// maluje odcinki między parami przecięć na jednym wierszu
    private void FillPairs(
        MeshTriangle triangle,
        Vector2 a,
        Vector2 b,
        Vector2 c,
        double inverseArea,
        int y,
        FrameBuffer frame,
        FragmentShader shader,
        Vector3Color? wireColor,
        Vector3 wireHeights)
    {
        for (int pair = 0; pair + 1 < _active.Count; pair += 2)
        {
            double left = _active[pair].X;
            double right = _active[pair + 1].X;
            // środek piksela może leżeć na lewej granicy, prawa należy do sąsiada
            int x0 = (int)Math.Clamp(Math.Ceiling(left - 0.5), 0, frame.Width);
            int xEnd = (int)Math.Clamp(Math.Ceiling(right - 0.5), 0, frame.Width);

            double sampleY = y + 0.5;
            int index = y * frame.Width + x0;
            for (int x = x0; x < xEnd; x++, index++)
            {
                double sampleX = x + 0.5;
                float alpha = (float)(SignedArea(b, c, sampleX, sampleY) * inverseArea);
                float beta = (float)(SignedArea(c, a, sampleX, sampleY) * inverseArea);
                float gamma = 1f - alpha - beta;
                float z =
                    alpha * triangle.A.PositionView.Z +
                    beta * triangle.B.PositionView.Z +
                    gamma * triangle.C.PositionView.Z;
                if (z > frame.Depth[index])
                {
                    Vector3Color color = shader.Shade(triangle, alpha, beta, gamma);
                    if (wireColor is Vector3Color wire)
                    {
                        // waga barycentryczna razy wysokość to odległość od przeciwległej krawędzi
                        float distance = MathF.Min(alpha * wireHeights.X, MathF.Min(beta * wireHeights.Y, gamma * wireHeights.Z));
                        float opacity = 0.82f * Math.Clamp(1f - distance, 0f, 1f);
                        color = (1f - opacity) * color + opacity * wire;
                    }

                    frame.WriteVisible(index, z, LightingEquation.ToBgra(color), triangle.SurfaceIndex);
                }
            }
        }
    }

    /// wstawia krawędź do kubełka wiersza, na którym się zaczyna
    private void AddEdge(Vector2 start, Vector2 end, int height)
    {
        Vector2 top = start.Y <= end.Y ? start : end;
        Vector2 bottom = start.Y <= end.Y ? end : start;
        double dy = (double)bottom.Y - top.Y;
        if (dy == 0)
        {
            return;
        }

        // górny koniec wliczamy, dolny pomijamy
        int yStart = (int)Math.Clamp(Math.Ceiling(top.Y - 0.5), 0, height);
        int yEnd = (int)Math.Clamp(Math.Ceiling(bottom.Y - 0.5), 0, height);
        if (yEnd <= yStart)
        {
            return;
        }

        double dxPerDy = ((double)bottom.X - top.X) / dy;
        double x = top.X + (yStart + 0.5 - top.Y) * dxPerDy;
        _buckets[yStart].Add(new ActiveEdge(yEnd, x, dxPerDy));
        _usedRows.Add(yStart);
    }

    /// czyści tylko te kubełki, do których coś wpadło
    private void ClearBuckets()
    {
        foreach (int row in _usedRows)
        {
            _buckets[row].Clear();
        }

        _usedRows.Clear();
    }

    /// pole równoległoboku między bokiem a punktem, ze znakiem
    private static double SignedArea(Vector2 start, Vector2 end, double x, double y)
    {
        return ((double)end.Y - start.Y) * (x - start.X) + ((double)start.X - end.X) * (y - start.Y);
    }

    private static List<ActiveEdge>[] CreateBuckets(int height)
    {
        var buckets = new List<ActiveEdge>[height];
        for (int i = 0; i < height; i++)
        {
            buckets[i] = new List<ActiveEdge>(2);
        }

        return buckets;
    }

    /// krawędź, która przecina aktualny wiersz
    private readonly record struct ActiveEdge(int YEnd, double X, double DxPerDy);
}
