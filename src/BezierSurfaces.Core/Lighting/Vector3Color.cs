namespace BezierSurfaces.Core.Lighting;

/// kolor jako trzy liczby od 0 do 1
public readonly struct Vector3Color
{
    /// zapamiętuje kanały czerwony, zielony i niebieski
    public Vector3Color(float r, float g, float b)
    {
        R = r;
        G = g;
        B = b;
    }

    public float R { get; }

    public float G { get; }

    public float B { get; }

    /// mnoży każdy kanał przez liczbę
    public static Vector3Color operator *(float factor, Vector3Color color)
    {
        return new Vector3Color(factor * color.R, factor * color.G, factor * color.B);
    }

    /// mnoży kanały dwóch kolorów, tak jak il razy io
    public static Vector3Color operator *(Vector3Color left, Vector3Color right)
    {
        return new Vector3Color(left.R * right.R, left.G * right.G, left.B * right.B);
    }

    /// dodaje dwa kolory składnik po składniku
    public static Vector3Color operator +(Vector3Color left, Vector3Color right)
    {
        return new Vector3Color(left.R + right.R, left.G + right.G, left.B + right.B);
    }
}
