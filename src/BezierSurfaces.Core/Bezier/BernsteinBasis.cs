namespace BezierSurfaces.Core.Bezier;

/// liczy wagi wielomianów bernsteina dla płata stopnia 3
public static class BernsteinBasis
{
    /// liczy cztery wagi stopnia 3 dla parametru t
    // (1-t)^3, 3t(1-t)^2, 3t^2(1-t), t^3
    public static void Degree3(float t, Span<float> weights)
    {
        float opposite = 1f - t;
        float oppositeSquared = opposite * opposite;
        float parameterSquared = t * t;
        weights[0] = oppositeSquared * opposite;
        weights[1] = 3f * t * oppositeSquared;
        weights[2] = 3f * parameterSquared * opposite;
        weights[3] = parameterSquared * t;
    }

    /// liczy trzy wagi stopnia 2, używane przy pochodnej
    // (1-t)^2, 2t(1-t), t^2
    public static void Degree2(float t, Span<float> weights)
    {
        float opposite = 1f - t;
        weights[0] = opposite * opposite;
        weights[1] = 2f * t * opposite;
        weights[2] = t * t;
    }
}
