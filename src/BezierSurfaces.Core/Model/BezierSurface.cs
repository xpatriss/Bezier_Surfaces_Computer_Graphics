namespace BezierSurfaces.Core.Model;

/// jedna powierzchnia opisana siatką punktów kontrolnych
public sealed class BezierSurface
{
    /// zapamiętuje gotową siatkę
    public BezierSurface(ControlGrid controlPoints)
    {
        ControlPoints = controlPoints;
    }

    public ControlGrid ControlPoints { get; }
}
