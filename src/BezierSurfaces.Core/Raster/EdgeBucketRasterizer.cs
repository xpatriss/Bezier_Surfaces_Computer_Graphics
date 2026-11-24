using System.Numerics;
using BezierSurfaces.Core.Geometry;
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

    /// wypełnia wszystkie trójkąty sceny jednym kolorem na powierzchnię
    public void DrawMesh(Scene scene, ScreenMap map, FrameBuffer frame, int firstColor, int secondColor)
    {
        EnsureHeight(frame.Height);
        foreach (MeshTriangle triangle in scene.Mesh.Triangles)
        {
            int color = triangle.SurfaceIndex % 2 == 0 ? firstColor : secondColor;
            DrawTriangle(triangle, map, frame, color);
        }
    }

    /// wkłada trzy krawędzie do kubełków i maluje kolejne wiersze
    public void DrawTriangle(MeshTriangle triangle, ScreenMap map, FrameBuffer frame, int color)
    {
        Vector2 a = map.ToScreen(triangle.A.PositionView);
        Vector2 b = map.ToScreen(triangle.B.PositionView);
        Vector2 c = map.ToScreen(triangle.C.PositionView);
        float area = SignedArea(b, c, a);
        // trzy punkty na jednej prostej nie mają wnętrza
        if (MathF.Abs(area) < 1e-4f)
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

        _active.Clear();
        for (int y = yMin; y < yMax; y++)
        {
            if (y < frame.Height)
            {
                _active.AddRange(_buckets[y]);
            }

            // krawędź kończy się na swoim górnym wierszu i nie maluje go drugi raz
            _active.RemoveAll(edge => edge.YEnd <= y);
            _active.Sort(static (left, right) => left.X.CompareTo(right.X));
            FillPairs(triangle, a, b, c, area, y, frame, color);
            foreach (ActiveEdge edge in _active)
            {
                edge.X += edge.DxPerDy;
            }
        }
    }

    /// maluje odcinki między parami przecięć na jednym wierszu
    private void FillPairs(
        MeshTriangle triangle,
        Vector2 a,
        Vector2 b,
        Vector2 c,
        float area,
        int y,
        FrameBuffer frame,
        int color)
    {
        for (int pair = 0; pair + 1 < _active.Count; pair += 2)
        {
            float left = _active[pair].X;
            float right = _active[pair + 1].X;
            if (right < left)
            {
                (left, right) = (right, left);
            }

            // środek piksela ma leżeć między przecięciami
            int x0 = (int)MathF.Ceiling(left - 0.5f);
            int x1 = (int)MathF.Floor(right - 0.5f);
            if (x0 < 0)
            {
                x0 = 0;
            }

            if (x1 >= frame.Width)
            {
                x1 = frame.Width - 1;
            }

            float sampleY = y + 0.5f;
            for (int x = x0; x <= x1; x++)
            {
                float sampleX = x + 0.5f;
                float alpha = SignedArea(b, c, sampleX, sampleY) / area;
                float beta = SignedArea(c, a, sampleX, sampleY) / area;
                float gamma = 1f - alpha - beta;
                float z =
                    alpha * triangle.A.PositionView.Z +
                    beta * triangle.B.PositionView.Z +
                    gamma * triangle.C.PositionView.Z;
                frame.WriteIfCloser(x, y, z, color);
            }
        }
    }

    /// wstawia krawędź do kubełka wiersza, na którym się zaczyna
    private void AddEdge(Vector2 start, Vector2 end, int height)
    {
        Vector2 lower = start.Y <= end.Y ? start : end;
        Vector2 upper = start.Y <= end.Y ? end : start;
        float dy = upper.Y - lower.Y;
        // pozioma krawędź nie przecina żadnego wiersza
        if (dy < 1e-5f || height <= 0)
        {
            return;
        }

        int yStart = (int)MathF.Ceiling(lower.Y - 0.5f);
        int yEnd = (int)MathF.Floor(upper.Y - 0.5f) + 1;
        if (yEnd <= yStart)
        {
            return;
        }

        float dxPerDy = (upper.X - lower.X) / dy;
        float x = lower.X + ((yStart + 0.5f) - lower.Y) * dxPerDy;
        if (yStart < 0)
        {
            x += dxPerDy * -yStart;
            yStart = 0;
        }

        if (yStart >= height)
        {
            return;
        }

        if (yEnd > height)
        {
            yEnd = height;
        }

        if (yEnd <= yStart)
        {
            return;
        }

        var edge = new ActiveEdge
        {
            YEnd = yEnd,
            X = x,
            DxPerDy = dxPerDy
        };
        _buckets[yStart].Add(edge);
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
    private static float SignedArea(Vector2 start, Vector2 end, Vector2 point)
    {
        return SignedArea(start, end, point.X, point.Y);
    }

    private static float SignedArea(Vector2 start, Vector2 end, float x, float y)
    {
        return (end.Y - start.Y) * (x - start.X) + (start.X - end.X) * (y - start.Y);
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
    private sealed class ActiveEdge
    {
        public int YEnd;
        public float X;
        public float DxPerDy;
    }
}
