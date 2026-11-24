namespace BezierSurfaces.Core.Model;

/// jedna powierzchnia opisana siatką punktów kontrolnych
public sealed class BezierSurface
{
    /// zapamiętuje siatkę oraz kolor albo teksturę
    public BezierSurface(ControlGrid controlPoints, SurfaceAppearance appearance)
    {
        ControlPoints = controlPoints;
        Appearance = appearance;
    }

    public ControlGrid ControlPoints { get; }

    public SurfaceAppearance Appearance { get; }
}
