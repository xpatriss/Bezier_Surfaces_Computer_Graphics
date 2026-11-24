using System.Runtime.InteropServices;

namespace BezierSurfaces.Core.Raster;

/// piksele i głębokość jednego rysunku
public sealed class FrameBuffer
{
    public int Width { get; private set; }

    public int Height { get; private set; }

    public int[] Pixels { get; private set; } = Array.Empty<int>();

    public float[] Depth { get; private set; } = Array.Empty<float>();

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
    }

    /// maluje tło i ustawia głębokość tak, żeby każdy punkt sceny był bliżej
    public void Clear(int background)
    {
        Array.Fill(Pixels, background);
        // bliższy punkt ma większe z, więc puste miejsce jest najdalej
        Array.Fill(Depth, float.NegativeInfinity);
    }

    /// zapisuje piksel tylko wtedy, gdy nowe z jest większe od zapamiętanego
    public void WriteIfCloser(int x, int y, float z, int color)
    {
        int index = y * Width + x;
        if (z <= Depth[index])
        {
            return;
        }

        Depth[index] = z;
        Pixels[index] = color;
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
