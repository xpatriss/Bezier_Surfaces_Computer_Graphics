using System.Numerics;

namespace BezierSurfaces.Core.Geometry;

/// opisuje, jak punkt sceny pada na piksele okna
// środek obszaru to (0, 0), oś y matematyczna rośnie w górę
public readonly struct ScreenMap
{
    // najdalszy punkt, który ma się zmieścić, leży 2 jednostki od (0, 0, 0)
    public const float FitRadius = 2.0f;

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
    public static ScreenMap Fit(int width, int height)
    {
        float span = MathF.Min(width, height);
        // 0.90 zostawia margines przy brzegu okna
        float scale = 0.90f * span / (2f * FitRadius);
        return new ScreenMap(width, height, scale, (width - 1) * 0.5f, (height - 1) * 0.5f);
    }

    /// zamienia x i y punktu na numer piksela
    public Vector2 ToScreen(Vector3 point)
    {
        return new Vector2(
            CenterX + point.X * Scale,
            CenterY - point.Y * Scale);
    }
}
