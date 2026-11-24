using System.Numerics;
using BezierSurfaces.Core.Lighting;

namespace BezierSurfaces.App;

/// startowa tekstura cegieł i mapa normalnych tych samych spoin
static class DefaultImages
{
    public static RgbImage BrickColor { get; } = BuildBrickColor();

    public static RgbImage BrickNormal { get; } = BuildBrickNormal();

    /// maluje cegły i jaśniejsze spoiny na obrazie 128 na 128
    private static RgbImage BuildBrickColor()
    {
        const int size = 128;
        var rgb = new float[size * size * 3];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool mortar = IsMortar(x, y);
                float r = mortar ? 0.86f : 0.45f;
                float g = mortar ? 0.84f : 0.48f;
                float b = mortar ? 0.90f : 0.86f;
                Write(rgb, size, x, y, r, g, b);
            }
        }

        return new RgbImage(size, size, rgb);
    }

    /// liczy normalną ze skoku wysokości na spoinie i zapisuje ją jako kolor
    // składowa z wysokości jest dodatnia, więc niebieski zostaje powyżej 0,5
    private static RgbImage BuildBrickNormal()
    {
        const int size = 128;
        var rgb = new float[size * size * 3];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float left = Height(x - 1, y);
                float right = Height(x + 1, y);
                float upper = Height(x, y - 1);
                float lower = Height(x, y + 1);
                // wiersze obrazu rosną w dół, parametr v rośnie w górę
                var normal = Vector3.Normalize(new Vector3((left - right) * 4f, (lower - upper) * 4f, 1f));
                Write(rgb, size, x, y, normal.X * 0.5f + 0.5f, normal.Y * 0.5f + 0.5f, normal.Z * 0.5f + 0.5f);
            }
        }

        return new RgbImage(size, size, rgb);
    }

    /// wysokość cegły to 1, spoiny 0
    private static float Height(int x, int y)
    {
        x = Math.Clamp(x, 0, 127);
        y = Math.Clamp(y, 0, 127);
        return IsMortar(x, y) ? 0f : 1f;
    }

    /// spoina ma dwa piksele na brzegu cegły, co drugi rząd jest przesunięty
    private static bool IsMortar(int x, int y)
    {
        const int brickWidth = 32;
        const int brickHeight = 16;
        int row = Math.DivRem(y, brickHeight, out int yInBrick);
        int shift = (row % 2) * (brickWidth / 2);
        int xInBrick = (x + shift) % brickWidth;
        return xInBrick < 2 || yInBrick < 2;
    }

    /// wpisuje trzy kanały do spłaszczonej tablicy
    private static void Write(float[] rgb, int size, int x, int y, float r, float g, float b)
    {
        int index = (y * size + x) * 3;
        rgb[index] = r;
        rgb[index + 1] = g;
        rgb[index + 2] = b;
    }
}
