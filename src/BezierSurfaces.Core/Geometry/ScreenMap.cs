using System.Numerics;

namespace BezierSurfaces.Core.Geometry;

/// opisuje, jak punkt sceny pada na piksele okna
// środek obszaru to (0, 0), oś y matematyczna rośnie w górę
public readonly struct ScreenMap
{
    public int Width { get; }
    public int Height { get; }
    public float Scale { get; }
    public float CenterX { get; }
    public float CenterY { get; }

    /// zapamiętuje skalę i położenie środka
    public ScreenMap(int width, int height, float scale, float centerX, float centerY)
    {
        Width = width;
        Height = height;
        Scale = scale;
        CenterX = centerX;
        CenterY = centerY;
    }

    /// liczy skalę i środek dla aktualnej szerokości i wysokości okna
    public static ScreenMap Fit(int width, int height, float fitRadius)
    {
        float span = MathF.Min(width, height);
        // 0.90 zostawia margines przy brzegu okna
        float scale = 0.90f * span / (2f * fitRadius);
        return new ScreenMap(width, height, scale, width * 0.5f, height * 0.5f);
    }

    /// zamienia x i y punktu na numer piksela
    public Vector2 ToScreen(Vector3 point)
    {
        return new Vector2(
            CenterX + point.X * Scale,
            CenterY - point.Y * Scale);
    }

    /// zamienia piksel z powrotem na punkt sceny o zadanym z
    // y na ekranie rośnie w dół, w scenie w górę, stąd minus
    public Vector3 ToView(float screenX, float screenY, float z)
    {
        return new Vector3(
            (screenX - CenterX) / Scale,
            (CenterY - screenY) / Scale,
            z);
    }
}
