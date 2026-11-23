namespace BezierSurfaces.Core.Model;

public sealed class Scene
{
    public List<BezierSurface> Surfaces { get; } = new();
}
