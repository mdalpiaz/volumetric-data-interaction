using UnityEngine;

public struct Vector3Simple
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public static Vector3 ToVector3(Vector3Simple vector)
    {
        return new Vector3
        {
            x = vector.X,
            y = vector.Y,
            z = vector.Z
        };
    }
    
    public static Vector3Simple FromVector3(Vector3 vector)
    {
        return new Vector3Simple
        {
            X = vector.x,
            Y = vector.y,
            Z = vector.z
        };
    }
}
