namespace BezierSurfaces.Core.Model;

public sealed class BezierSurface
{
    public BezierSurface(ControlGrid controlPoints)
    {
        ControlPoints = controlPoints;
    }

    public ControlGrid ControlPoints { get; }
}
