namespace BezierSurfaces.Core.Lighting;

/// obraz w liczbach 0–1, wiersz 0 pliku jest u góry
public sealed class RgbImage
{
    /// zapamiętuje szerokość, wysokość i spłaszczone kanały rgb
    public RgbImage(int width, int height, float[] rgb)
    {
        Width = width;
        Height = height;
        Rgb = rgb;
    }

    public int Width { get; }

    public int Height { get; }

    public float[] Rgb { get; }

    /// czyta kolor w parametrach u i v, z mieszaniem czterech sąsiednich pikseli
    // v = 0 jest na dole płata, a w obrazie na dole jest ostatni wiersz
    public Vector3Color Sample(float u, float v)
    {
        u = Math.Clamp(u, 0f, 1f);
        v = Math.Clamp(v, 0f, 1f);
        float x = u * (Width - 1);
        float y = (1f - v) * (Height - 1);
        int x0 = (int)MathF.Floor(x);
        int y0 = (int)MathF.Floor(y);
        int x1 = Math.Min(x0 + 1, Width - 1);
        int y1 = Math.Min(y0 + 1, Height - 1);
        float tx = x - x0;
        float ty = y - y0;

        Vector3Color c00 = Read(x0, y0);
        Vector3Color c10 = Read(x1, y0);
        Vector3Color c01 = Read(x0, y1);
        Vector3Color c11 = Read(x1, y1);
        Vector3Color top = Lerp(c00, c10, tx);
        Vector3Color bottom = Lerp(c01, c11, tx);
        return Lerp(top, bottom, ty);
    }

    /// zamienia kolor mapy normalnych z zakresu 0–1 na wektor od −1 do 1
    public static Vector3Color DecodeNormal(Vector3Color encoded)
    {
        return new Vector3Color(
            encoded.R * 2f - 1f,
            encoded.G * 2f - 1f,
            encoded.B * 2f - 1f);
    }

    /// bierze trzy kanały piksela o kolumnie x i wierszu y
    private Vector3Color Read(int x, int y)
    {
        int index = (y * Width + x) * 3;
        return new Vector3Color(Rgb[index], Rgb[index + 1], Rgb[index + 2]);
    }

    /// liczy kolor leżący w części t między dwoma kolorami
    private static Vector3Color Lerp(Vector3Color a, Vector3Color b, float t)
    {
        return new Vector3Color(
            a.R + (b.R - a.R) * t,
            a.G + (b.G - a.G) * t,
            a.B + (b.B - a.B) * t);
    }
}
