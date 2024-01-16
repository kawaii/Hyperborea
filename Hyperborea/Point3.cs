namespace Hyperborea;
public class Point3()
{
    public Point3(float x, float y, float z) : this()
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }
    public float X, Y, Z;

    public Vector3 ToVector3() => new(X, Y, Z);
}

public static class Point3Extensions
{
    public static Point3 ToPoint3(this Vector3 v) => new(v.X, v.Y, v.Z);
}
