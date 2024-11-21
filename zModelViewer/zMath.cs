using System;
using System.Collections.Generic;

namespace zModelViewer
{
    class Mat44
    {
        public float m00, m01, m02, m03,
                     m10, m11, m12, m13,
                     m20, m21, m22, m23,
                     m30, m31, m32, m33;

        public Mat44(float m00, float m01, float m02, float m03,
                     float m10, float m11, float m12, float m13,
                     float m20, float m21, float m22, float m23,
                     float m30, float m31, float m32, float m33)
        {
            this.m00 = m00; this.m01 = m01; this.m02 = m02; this.m03 = m03;
            this.m10 = m10; this.m11 = m11; this.m12 = m12; this.m13 = m13;
            this.m20 = m20; this.m21 = m21; this.m22 = m22; this.m23 = m23;
            this.m30 = m30; this.m31 = m31; this.m32 = m32; this.m33 = m33;
        }

        public static Mat44 BuildIdentityMatrix()
        {
            return new Mat44(1f, 0f, 0f, 0f,
                             0f, 1f, 0f, 0f,
                             0f, 0f, 1f, 0f,
                             0f, 0f, 0f, 1f);
        }

        public static Mat44 BuildNDCMatrix(float aspect, float hfov, float vfov, float znear, float zfar) // NDC (Clip) Matrix
        {
            float right  = (float)Math.Tan(hfov * 0.5f),
                  left   = -right,
                  top    = (float)Math.Tan(vfov * 0.5f),
                  bottom = -top;

            return new Mat44( 1f / (right - left) / aspect,/**/                  0f,/**/                               0f,/**/ 0f,
                                                        0f,/**/ 1f / (top - bottom),/**/                               0f,/**/ 0f,
                                                        0f,/**/                  0f,/**/ -(zfar + znear) / (zfar - znear),/**/ (-2f * znear * zfar) / (zfar - znear),
                                                        0f,/**/                  0f,/**/                              -1f,/**/ 0f
            );
        }

        public static Mat44 BuildModelMatrix(float pitch, float yaw, float roll, Vec3D origin)
        {
            float sp = (float)Math.Sin(pitch),
                  cp = (float)Math.Cos(pitch),
                  sy = (float)Math.Sin(yaw),
                  cy = (float)Math.Cos(yaw),
                  sr = (float)Math.Sin(roll),
                  cr = (float)Math.Cos(roll);

            Vec3D x = new Vec3D(      cy,  0f,     -sy );
            Vec3D y = new Vec3D( sy * sp,  cp, cy * sp );
            Vec3D z = new Vec3D( sy * cp, -sp, cp * cy );

            // Create a 4x4 view matrix from the new axes and translation, projected on the new axes
            return (new Mat44(
                             x.x * cr - y.x * sr, x.x * sr + y.x * cr, z.x, origin.x,
                             x.y * cr - y.y * sr, x.y * sr + y.y * cr, z.y, origin.y,
                             x.z * cr - y.z * sr, x.z * sr + y.z * cr, z.z, origin.z,
                                              0f,              0f,      0f,       1f ));
        }

        public static Mat44 BuildCameraMatrix(float yaw, float pitch, float roll, Vec3D origin)
        {
            float sp = (float)Math.Sin(pitch),
                  cp = (float)Math.Cos(pitch),
                  sy = (float)Math.Sin(yaw),
                  cy = (float)Math.Cos(yaw),
                  sr = (float)Math.Sin(roll),
                  cr = (float)Math.Cos(roll);

            Vec3D x = new Vec3D(    cy,  0f,   -sy ); // right
            Vec3D y = new Vec3D( sy*sp,  cp, cy*sp ); // up
            Vec3D z = new Vec3D( sy*cp, -sp, cp*cy ); // forward

            // Create a 4x4 view matrix from the new axes and translation, projected on the new axes
            return (new Mat44(
                             x.x*cr - y.x*sr,       x.x*sr + y.x*cr,                   z.x, 0f,
                             x.y*cr - y.y*sr,       x.y*sr + y.y*cr,                   z.y, 0f,
                             x.z*cr - y.z*sr,       x.z*sr + y.z*cr,                   z.z, 0f,
                       -Vec3D.Dot(x, origin), -Vec3D.Dot(y, origin), -Vec3D.Dot(z, origin), 1f ));
        }

        public static Vec3D operator *(Vec3D v, Mat44 m)
        {
            return new Vec3D(v.x * m.m00 + v.y * m.m01 + v.z * m.m02 + v.w * m.m03,
                             v.x * m.m10 + v.y * m.m11 + v.z * m.m12 + v.w * m.m13,
                             v.x * m.m20 + v.y * m.m21 + v.z * m.m22 + v.w * m.m23,
                             v.x * m.m30 + v.y * m.m31 + v.z * m.m32 + v.w * m.m33);
        }

        public static Mat44 operator *(Mat44 a, Mat44 b)
        {
            return new Mat44(

            a.m00 * b.m00 + a.m01 * b.m10 + a.m02 * b.m20 + a.m03 * b.m30, /**/ a.m00 * b.m01 + a.m01 * b.m11 + a.m02 * b.m21 + a.m03 * b.m31, /**/ a.m00 * b.m02 + a.m01 * b.m12 + a.m02 * b.m22 + a.m03 * b.m32, /**/ a.m00 * b.m03 + a.m01 * b.m13 + a.m02 * b.m23 + a.m03 * b.m33,
            a.m10 * b.m00 + a.m11 * b.m10 + a.m12 * b.m20 + a.m13 * b.m30, /**/ a.m10 * b.m01 + a.m11 * b.m11 + a.m12 * b.m21 + a.m13 * b.m31, /**/ a.m10 * b.m02 + a.m11 * b.m12 + a.m12 * b.m22 + a.m13 * b.m32, /**/ a.m10 * b.m03 + a.m11 * b.m13 + a.m12 * b.m23 + a.m13 * b.m33,
            a.m20 * b.m00 + a.m21 * b.m10 + a.m22 * b.m20 + a.m23 * b.m30, /**/ a.m20 * b.m01 + a.m21 * b.m11 + a.m22 * b.m21 + a.m23 * b.m31, /**/ a.m20 * b.m02 + a.m21 * b.m12 + a.m22 * b.m22 + a.m23 * b.m32, /**/ a.m20 * b.m03 + a.m21 * b.m13 + a.m22 * b.m23 + a.m23 * b.m33,
            a.m30 * b.m00 + a.m31 * b.m10 + a.m32 * b.m20 + a.m33 * b.m30, /**/ a.m30 * b.m01 + a.m31 * b.m11 + a.m32 * b.m21 + a.m33 * b.m31, /**/ a.m30 * b.m02 + a.m31 * b.m12 + a.m32 * b.m22 + a.m33 * b.m32, /**/ a.m30 * b.m03 + a.m31 * b.m13 + a.m32 * b.m23 + a.m33 * b.m33

            );
        }

        public Mat44 Transponse()
        {
            return new Mat44(m00, m10, m20, m30,
                             m01, m11, m21, m31,
                             m02, m12, m22, m32,
                             m03, m13, m23, m33);
        }
    }

    struct Vec3D
    {
        public float x, y, z, w;

        public Vec3D(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public Vec3D(float x, float y, float z) { this.x = x; this.y = y; this.z = z; this.w = 1f; }
        public float LengthSq() { return x * x + y * y + z * z; }
        public float Length() { return (float)Math.Sqrt(x * x + y * y + z * z); }
        public static Vec3D operator +(Vec3D a, Vec3D b) { return new Vec3D(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vec3D operator -(Vec3D a, Vec3D b) { return new Vec3D(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vec3D operator -(Vec3D v) { return new Vec3D(-v.x, -v.y, -v.z); }
        public static Vec3D operator *(Vec3D v, float c) { return new Vec3D(v.x * c, v.y * c, v.z * c); }
        public static Vec3D operator *(float c, Vec3D v) { return new Vec3D(v.x * c, v.y * c, v.z * c); }
        public Vec3D Unit()
        {
            float len = 1f / (float)Math.Sqrt(x * x + y * y + z * z);
            return new Vec3D(x * len, y * len, z * len);
        }
        public Vec3D Unit(float len) { return new Vec3D(x / len, y / len, z / len, w); }
        public static float Dot(Vec3D a, Vec3D b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
        public static Vec3D Cross(Vec3D a, Vec3D b)
        {
            return new Vec3D(a.y * b.z - a.z * b.y,
                             a.z * b.x - a.x * b.z,
                             a.x * b.y - a.y * b.x);
        }
        public static float CrossZ(Vec3D a, Vec3D b) { return a.x * b.y - a.y * b.x; }
        public static Vec3D Zero = new Vec3D(0f, 0f, 0f);

        public static bool operator ==(Vec3D a, Vec3D b) { return a.x == b.x && a.y == b.y && a.z == b.z; }
        public static bool operator !=(Vec3D a, Vec3D b) { return a.x != b.x || a.y != b.y || a.z != b.z; }
    }

    struct Vec2D
    {
        public float x, y;

        public static Vec2D Zero;

        public Vec2D(float x, float y) { this.x = x; this.y = y; }

        public float LenSq() { return x * x + y * y; }

        public float Len() { return (float)Math.Sqrt(x * x + y * y); }

        static public float Cross(Vec2D a, Vec2D b) { return a.x * b.y - a.y * b.x; }

        public Vec2D Unit()
        {
            float invlen = 1f / (float)Math.Sqrt(x * x + y * y);
            return new Vec2D(x * invlen, y * invlen);
        }

        public Vec2D GetNormal()
        {
            float invlen = 1f / (float)Math.Sqrt(x * x + y * y);
            return new Vec2D(-y * invlen, x * invlen);
        }

        public static float Dot(Vec2D a, Vec2D b) { return a.x * b.x + a.y * b.y; }

        public static Vec2D operator +(Vec2D a, Vec2D b) { return new Vec2D(a.x + b.x, a.y + b.y); }

        public static Vec2D operator -(Vec2D a, Vec2D b) { return new Vec2D(a.x - b.x, a.y - b.y); }

        public static Vec2D operator -(Vec2D a) { return new Vec2D(-a.x, -a.y); }

        public static Vec2D operator *(Vec2D a, float c) { return new Vec2D(a.x * c, a.y * c); }
        public static Vec2D operator *(float c, Vec2D a) { return new Vec2D(a.x * c, a.y * c); }
    }
}
