using System.Numerics;

namespace BezierSurfaces.Core.Geometry;

/// macierz 3×3, która obraca wektor kolumnowy
// punkt po obrocie to r * p, środek obrotu to (0, 0, 0)
public readonly struct Matrix3
{
    public float M11 { get; }
    public float M12 { get; }
    public float M13 { get; }
    public float M21 { get; }
    public float M22 { get; }
    public float M23 { get; }
    public float M31 { get; }
    public float M32 { get; }
    public float M33 { get; }

    /// zapamiętuje dziewięć pól macierzy, wiersz po wierszu
    public Matrix3(
        float m11, float m12, float m13,
        float m21, float m22, float m23,
        float m31, float m32, float m33)
    {
        M11 = m11;
        M12 = m12;
        M13 = m13;
        M21 = m21;
        M22 = m22;
        M23 = m23;
        M31 = m31;
        M32 = m32;
        M33 = m33;
    }

    public static Matrix3 Identity { get; } = new(1, 0, 0, 0, 1, 0, 0, 0, 1);

    /// obrót wokół osi x
    // dodatni kąt przenosi +y w stronę +z
    public static Matrix3 RotationX(float radians)
    {
        float c = MathF.Cos(radians);
        float s = MathF.Sin(radians);
        return new Matrix3(
            1, 0, 0,
            0, c, -s,
            0, s, c);
    }

    /// obrót wokół osi y
    // dodatni kąt przenosi +z w stronę +x
    public static Matrix3 RotationY(float radians)
    {
        float c = MathF.Cos(radians);
        float s = MathF.Sin(radians);
        return new Matrix3(
            c, 0, s,
            0, 1, 0,
            -s, 0, c);
    }

    /// obrót wokół osi z
    // dodatni kąt przenosi +x w stronę +y
    public static Matrix3 RotationZ(float radians)
    {
        float c = MathF.Cos(radians);
        float s = MathF.Sin(radians);
        return new Matrix3(
            c, -s, 0,
            s, c, 0,
            0, 0, 1);
    }

    /// składa dwa obroty: najpierw b, potem a
    public static Matrix3 Multiply(Matrix3 a, Matrix3 b)
    {
        return new Matrix3(
            a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31,
            a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32,
            a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33,
            a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31,
            a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32,
            a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33,
            a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31,
            a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32,
            a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33);
    }

    /// mnoży macierz przez wektor kolumnowy
    public Vector3 Transform(Vector3 vector)
    {
        return new Vector3(
            M11 * vector.X + M12 * vector.Y + M13 * vector.Z,
            M21 * vector.X + M22 * vector.Y + M23 * vector.Z,
            M31 * vector.X + M32 * vector.Y + M33 * vector.Z);
    }

    public Matrix3 Orthonormalized()
    {
        // usuwa drobny dryf po wielu złożeniach obrotów
        Vector3 x = Vector3.Normalize(new Vector3(M11, M21, M31));
        Vector3 y = new(M12, M22, M32);
        y = Vector3.Normalize(y - Vector3.Dot(x, y) * x);
        Vector3 z = Vector3.Cross(x, y);
        return new Matrix3(x.X, y.X, z.X, x.Y, y.Y, z.Y, x.Z, y.Z, z.Z);
    }
}
