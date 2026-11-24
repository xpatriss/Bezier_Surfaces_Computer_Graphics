using System.Numerics;

namespace BezierSurfaces.Core.Lighting;

/// liczy kolor piksela ze wzoru z polecenia, bez składnika ambient
public static class LightingEquation
{
    // obserwator jest po stronie dodatniego z
    public static readonly Vector3 ViewDirection = new(0f, 0f, 1f);

    /// zwraca kolor rozproszony i błysk dla jednego punktu
    public static Vector3Color Compute(
        Vector3 normal,
        Vector3 position,
        Vector3 lightPosition,
        Vector3Color lightColor,
        Vector3Color objectColor,
        float diffuseWeight,
        float specularWeight,
        float shininess)
    {
        Vector3 n = VectorMath.NormalizeOrZero(normal);
        Vector3 toLight = lightPosition - position;
        Vector3 l = VectorMath.NormalizeOrZero(toLight);
        if (n == Vector3.Zero || l == Vector3.Zero)
        {
            return new Vector3Color(0f, 0f, 0f);
        }

        float nDotL = Vector3.Dot(n, l);
        // światło rozproszone zależy od kąta do oświetlonej strony płata
        float diffuse = MathF.Abs(nDotL);
        float specular = 0f;
        if (specularWeight > 0f && nDotL * n.Z > 0f)
        {
            // zwierciadlany refleks widzimy po stronie lampy, w kierunku odbicia
            Vector3 reflection = VectorMath.NormalizeOrZero(2f * nDotL * n - l);
            float vDotR = Vector3.Dot(ViewDirection, reflection);
            if (vDotR > 0f)
            {
                specular = MathF.Pow(vDotR, shininess);
            }
        }

        Vector3Color diffusePart = diffuseWeight * diffuse * lightColor * objectColor;
        Vector3Color specularPart = specularWeight * specular * lightColor * objectColor;
        return diffusePart + specularPart;
    }

    /// składa normalną z mapy w układ stycznych płata
    // kolumny macierzy to pu, pv i normalna powierzchni, już po obrocie
    public static Vector3 ApplyNormalMap(Vector3 tangentU, Vector3 tangentV, Vector3 surfaceNormal, Vector3 textureNormal)
    {
        Vector3 t = VectorMath.NormalizeOrZero(tangentU);
        Vector3 b = VectorMath.NormalizeOrZero(tangentV);
        Vector3 n = VectorMath.NormalizeOrZero(surfaceNormal);
        if (t == Vector3.Zero || b == Vector3.Zero || n == Vector3.Zero)
        {
            return n;
        }

        Vector3 mapped = t * textureNormal.X + b * textureNormal.Y + n * textureNormal.Z;
        Vector3 normalized = VectorMath.NormalizeOrZero(mapped);
        return normalized == Vector3.Zero ? n : normalized;
    }

    /// zamienia kolor 0–1 na piksel aarrggbb
    public static int ToBgra(Vector3Color color)
    {
        int r = ToByte(color.R);
        int g = ToByte(color.G);
        int b = ToByte(color.B);
        return (255 << 24) | (r << 16) | (g << 8) | b;
    }

    /// zamienia jeden kanał na bajt 0–255
    private static int ToByte(float channel)
    {
        float scaled = channel * 255f;
        if (scaled < 0f)
        {
            return 0;
        }

        if (scaled > 255f)
        {
            return 255;
        }

        return (int)MathF.Round(scaled);
    }
}
