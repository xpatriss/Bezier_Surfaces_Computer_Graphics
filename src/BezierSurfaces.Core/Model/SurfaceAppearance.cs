using BezierSurfaces.Core.Lighting;

namespace BezierSurfaces.Core.Model;

/// kolor jednolity albo tekstura jednej powierzchni
public sealed class SurfaceAppearance
{
    public bool UseTexture { get; set; }

    public Vector3Color SolidColor { get; set; } = new(0.8f, 0.3f, 0.3f);

    public RgbImage? Texture { get; set; }
}
