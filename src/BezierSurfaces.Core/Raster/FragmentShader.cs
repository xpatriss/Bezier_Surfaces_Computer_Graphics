using System.Numerics;
using BezierSurfaces.Core.Lighting;
using BezierSurfaces.Core.Model;

namespace BezierSurfaces.Core.Raster;

/// liczy kolor jednego piksela z wag przy trzech rogach
public sealed class FragmentShader
{
    public required Scene Scene { get; init; }

    public required Vector3 LightPosition { get; init; }

    /// miesza pozycję i normalną, potem woła wzór światła
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
        BezierSurface surface = Scene.Surfaces[triangle.SurfaceIndex];
        return LightingEquation.Compute(
            normal,
            position,
            LightPosition,
            Scene.LightColor,
            surface.Color,
            Scene.DiffuseWeight,
            Scene.SpecularWeight,
            Scene.Shininess);
    }
}
