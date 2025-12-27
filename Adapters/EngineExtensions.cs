using System.Numerics;
using OpenTK.Mathematics;
using Vector4 = System.Numerics.Vector4;

namespace Adapters;

public static class EngineExtensions
{
    public static Matrix4 ToOpenTK(Matrix4x4 m)
    {
        return new Matrix4(
            m.M11, m.M21, m.M31, m.M41,
            m.M12, m.M22, m.M32, m.M42,
            m.M13, m.M23, m.M33, m.M43,
            m.M14, m.M24, m.M34, m.M44
        );
    }
    
    public static System.Numerics.Quaternion ToNumeric(this OpenTK.Mathematics.Quaternion q)
    {
        return  new System.Numerics.Quaternion(q.X, q.Y, q.Z, q.W);
    }

    public static OpenTK.Mathematics.Quaternion ToOpenTK(this System.Numerics.Quaternion q)
    {
        return new OpenTK.Mathematics.Quaternion(q.X, q.Y, q.Z, q.W);
    }
    
    public static OpenTK.Mathematics.Vector3 ToRGB(this OpenTK.Mathematics.Vector4 v)
    {
        return new OpenTK.Mathematics.Vector3(v.X, v.Y, v.Z);
    }


    public static System.Numerics.Vector3 ToVector3(this Vector4 vector4)
    {
        return new System.Numerics.Vector3(vector4.X, vector4.Y, vector4.Z);
    }
    

    public static Matrix4x4 ToNumerics(this Matrix4 m)
    {
        return new Matrix4x4(
            m.M11, m.M12, m.M13, m.M14,
            m.M21, m.M22, m.M23, m.M24,
            m.M31, m.M32, m.M33, m.M34,
            m.M41, m.M42, m.M43, m.M44
        );
    }
    

    public static Jitter2.LinearMath.JVector ToJitter(this OpenTK.Mathematics.Vector3 v)
    {
        return new Jitter2.LinearMath.JVector(v.X, v.Y, v.Z);
    }

    public static OpenTK.Mathematics.Vector3 ToOpenTK(this Jitter2.LinearMath.JVector v)
    {
        return new OpenTK.Mathematics.Vector3(v.X, v.Y, v.Z);
    }

    public static Jitter2.LinearMath.JQuaternion ToJitter(this OpenTK.Mathematics.Quaternion q)
    {
        return new Jitter2.LinearMath.JQuaternion(q.X, q.Y, q.Z, q.W);
    }

    public static OpenTK.Mathematics.Quaternion ToOpenTK(this Jitter2.LinearMath.JQuaternion q)
    {
        return new OpenTK.Mathematics.Quaternion(q.X, q.Y, q.Z, q.W);
    }

    public static System.Numerics.Vector3 ToNumerics(this OpenTK.Mathematics.Vector3 v)
    {
        return new System.Numerics.Vector3(v.X, v.Y, v.Z);
    }

    public static OpenTK.Mathematics.Vector3 ToOpenTK(this System.Numerics.Vector3 v)
    {
        return new OpenTK.Mathematics.Vector3(v.X, v.Y, v.Z);
    }

    public static OpenTK.Mathematics.Vector4 ToOpenTK(this Vector4 v)
    {
        return new OpenTK.Mathematics.Vector4(v.X, v.Y, v.Z, v.W);
    }
    
    public static OpenTK.Mathematics.Vector2 ToOpenTK(this System.Numerics.Vector2 v)
    {
        return new OpenTK.Mathematics.Vector2(v.X, v.Y);
    }
}