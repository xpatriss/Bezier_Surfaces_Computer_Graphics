using System.Numerics;

namespace BezierSurfaces.Core.Lighting;

/// liczy kolor piksela ze wzoru z polecenia, bez składnika ambient
// i = kd il io max(n·l, 0) + ks il io max(v·r, 0)^m
public static class LightingEquation
{
    // obserwator patrzy w dodatnie z, więc wektor do oka to (0, 0, 1)
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
        float diffuse = nDotL > 0f ? nDotL : 0f;
        float specular = 0f;
        if (nDotL > 0f)
        {
            // r = 2 (n·l) n − l, tylko gdy ściana jest od strony światła
            Vector3 reflection = VectorMath.NormalizeOrZero(2f * nDotL * n - l);
            float vDotR = Vector3.Dot(ViewDirection, reflection);
            if (vDotR < 0f)
            {
                vDotR = 0f;
            }

            specular = MathF.Pow(vDotR, shininess);
        }

        Vector3Color diffusePart = diffuseWeight * diffuse * lightColor * objectColor;
        Vector3Color specularPart = specularWeight * specular * lightColor * objectColor;
        return Clamp01(diffusePart + specularPart);
    }

    /// zamienia kolor 0–1 na piksel aarrggbb
    public static int ToBgra(Vector3Color color)
    {
        int r = ToByte(color.R);
        int g = ToByte(color.G);
        int b = ToByte(color.B);
        return (255 << 24) | (r << 16) | (g << 8) | b;
    }

    /// obcina każdy kanał do przedziału 0–1
    private static Vector3Color Clamp01(Vector3Color color)
    {
        return new Vector3Color(
            Math.Clamp(color.R, 0f, 1f),
            Math.Clamp(color.G, 0f, 1f),
            Math.Clamp(color.B, 0f, 1f));
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
