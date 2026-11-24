using System.Numerics;

namespace BezierSurfaces.Core.Bezier;

/// liczy punkt na krzywej béziera o dowolnej liczbie punktów kontrolnych
public static class BezierCurve
{
    /// sumuje punkty kontrolne razy wagi bernsteina dla parametru t
    public static Vector3 Point(IReadOnlyList<Vector3> controlPoints, float t)
    {
        int degree = controlPoints.Count - 1;
        Vector3 sum = Vector3.Zero;
        for (int i = 0; i <= degree; i++)
        {
            sum += controlPoints[i] * BernsteinBasis.Weight(i, degree, t);
        }

        return sum;
    }
}
