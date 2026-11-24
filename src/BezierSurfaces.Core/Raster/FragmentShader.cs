using System.Numerics;
using BezierSurfaces.Core.Lighting;
using BezierSurfaces.Core.Model;

namespace BezierSurfaces.Core.Raster;

/// liczy kolor jednego piksela z wag przy trzech rogach
public sealed class FragmentShader
{
    public required Scene Scene { get; init; }

    public required Vector3 LightPosition { get; init; }

    /// miesza rogi, ewentualnie podmienia normalną i woła wzór światła
    public Vector3Color Shade(MeshTriangle triangle, float alpha, float beta, float gamma)
    {
        Vector3 position =
            alpha * triangle.A.PositionView +
            beta * triangle.B.PositionView +
            gamma * triangle.C.PositionView;
        Vector3 normal =
            alpha * triangle.A.NormalView +
            beta * triangle.B.NormalView +
            gamma * triangle.C.NormalView;
        Vector3 tangentU =
            alpha * triangle.A.TangentUView +
            beta * triangle.B.TangentUView +
            gamma * triangle.C.TangentUView;
        Vector3 tangentV =
            alpha * triangle.A.TangentVView +
            beta * triangle.B.TangentVView +
            gamma * triangle.C.TangentVView;
        float u = alpha * triangle.A.U + beta * triangle.B.U + gamma * triangle.C.U;
        float v = alpha * triangle.A.V + beta * triangle.B.V + gamma * triangle.C.V;

        if (Scene.UseNormalMap && Scene.NormalMap is not null)
        {
            Vector3Color encoded = Scene.NormalMap.Sample(u, v);
            Vector3Color decoded = RgbImage.DecodeNormal(encoded);
            normal = LightingEquation.ApplyNormalMap(tangentU, tangentV, normal, decoded);
        }

        BezierSurface surface = Scene.Surfaces[triangle.SurfaceIndex];
        Vector3Color objectColor = ObjectColor(surface.Appearance, u, v);
        return LightingEquation.Compute(
            normal,
            position,
            LightPosition,
            Scene.LightColor,
            objectColor,
            Scene.DiffuseWeight,
            Scene.SpecularWeight,
            Scene.Shininess);
    }

    /// bierze piksel tekstury albo jednolity kolor powierzchni
    private static Vector3Color ObjectColor(SurfaceAppearance appearance, float u, float v)
    {
        if (appearance.UseTexture && appearance.Texture is not null)
        {
            return appearance.Texture.Sample(u, v);
        }

        return appearance.SolidColor;
    }
}
