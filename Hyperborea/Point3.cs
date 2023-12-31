namespace Hyperborea;
internal class Point3()
{
    internal Point3(float x, float y, float z) : this()
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }
    internal float X, Y, Z;

    internal Vector3 ToVector3() => new(X, Y, Z);
}

internal static class Point3Extensions
{
    internal static Point3 ToPoint3(this Vector3 v) => new(v.X, v.Y, v.Z);
}
