using System.Runtime.InteropServices;

namespace BezierSurfaces.Core.Raster;

/// piksele i głębokość jednego rysunku
public sealed class FrameBuffer
{
    public int Width { get; private set; }

    public int Height { get; private set; }

    public int[] Pixels { get; private set; } = Array.Empty<int>();

    public float[] Depth { get; private set; } = Array.Empty<float>();
    private int[] _surfaceIndices = Array.Empty<int>();

    /// tworzy tablice od nowa, gdy zmienił się rozmiar okna
    public void Resize(int width, int height)
    {
        if (width == Width && height == Height)
        {
            return;
        }

        Width = width;
        Height = height;
        Pixels = new int[width * height];
        Depth = new float[width * height];
        _surfaceIndices = new int[width * height];
        Array.Fill(Depth, float.NegativeInfinity);
    }

    /// maluje tło i ustawia głębokość tak, żeby każdy punkt sceny był bliżej
    public void Clear(int background)
    {
        Array.Fill(Pixels, background);
        // bliższy punkt ma większe z, więc puste miejsce jest najdalej
        Array.Fill(Depth, float.NegativeInfinity);
    }

    /// odczytuje powierzchnię, która wygrała test głębokości
    public int? SurfaceAt(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            return null;
        }

        int index = y * Width + x;
        return float.IsNegativeInfinity(Depth[index]) ? null : _surfaceIndices[index];
    }

    internal void WriteVisible(int index, float z, int color, int surfaceIndex)
    {
        Depth[index] = z;
        Pixels[index] = color;
        _surfaceIndices[index] = surfaceIndex;
    }

    /// kopiuje piksele do bitmapy okna
    public void CopyTo(IntPtr scan0, int stride)
    {
        if (stride == Width * 4)
        {
            Marshal.Copy(Pixels, 0, scan0, Pixels.Length);
            return;
        }

        for (int y = 0; y < Height; y++)
        {
            Marshal.Copy(Pixels, y * Width, scan0 + y * stride, Width);
        }
    }
}
