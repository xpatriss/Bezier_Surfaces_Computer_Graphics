using System.Numerics;

namespace BezierSurfaces.Core.Lighting;

/// dzielenie wektora przez długość, z zerem gdy wektor jest za krótki
public static class VectorMath
{
    public const float Epsilon = 1e-6f;

    /// zwraca wektor o długości 1 albo zero, gdy długość jest znikoma
    public static Vector3 NormalizeOrZero(Vector3 vector)
    {
        float length = vector.Length();
        if (length < Epsilon)
        {
            return Vector3.Zero;
        }

        return vector / length;
    }
}
