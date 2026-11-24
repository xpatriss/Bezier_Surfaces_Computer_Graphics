namespace BezierSurfaces.Core.Geometry;

public enum TurnAxis
{
    ScreenXAndY,
    ScreenZ
}

/// zamienia ruch myszy na mały obrót wokół osi ekranu
public static class TurntableRotation
{
    // 100 pikseli to około 1 radian, czyli 57 stopni
    public const float RadiansPerPixel = 0.01f;

    /// buduje przyrost: lewy przycisk obraca wokół x i y, prawy wokół z
    public static Matrix3 Increment(float deltaXPixels, float deltaYPixels, TurnAxis axis)
    {
        if (axis == TurnAxis.ScreenZ)
        {
            return Matrix3.RotationZ(deltaXPixels * RadiansPerPixel);
        }

        float alpha = deltaYPixels * RadiansPerPixel;
        float beta = deltaXPixels * RadiansPerPixel;
        // najpierw obrót od ruchu w bok, potem od ruchu w górę i w dół
        return Matrix3.Multiply(Matrix3.RotationX(alpha), Matrix3.RotationY(beta));
    }
}
