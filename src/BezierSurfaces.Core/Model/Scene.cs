namespace BezierSurfaces.Core.Model;

/// trzyma powierzchnie wczytane przy starcie programu
public sealed class Scene
{
    public List<BezierSurface> Surfaces { get; } = new();
}
