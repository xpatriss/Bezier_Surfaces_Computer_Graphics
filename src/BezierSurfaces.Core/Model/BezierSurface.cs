using BezierSurfaces.Core.Lighting;

namespace BezierSurfaces.Core.Model;

/// jedna powierzchnia opisana siatką punktów kontrolnych
public sealed class BezierSurface
{
    /// zapamiętuje siatkę i kolor obiektu io
    public BezierSurface(ControlGrid controlPoints, Vector3Color color)
    {
        ControlPoints = controlPoints;
        Color = color;
    }

    public ControlGrid ControlPoints { get; }

    public Vector3Color Color { get; }
}
