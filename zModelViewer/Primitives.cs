using System;
using System.Collections.Generic;

namespace zModelViewer
{
    struct Vertex
    {
        public float x, y, z, w,
                     u, v,
                     r, g, b,
                     lightintensity;
        public Vertex(float x, float y, float z, float w, float u, float v, float r, float g, float b, float lightintensity)
        { this.x = x; this.y = y; this.z = z; this.w = w; this.u = u; this.v = v; this.r = r; this.g = g; this.b = b; this.lightintensity = lightintensity; }
    }

    struct Triangle
    {
        public int v1i, v2i, v3i; // vertex indices
        public int t1i, t2i, t3i; // texture coordinate indices
        public int n1i, n2i, n3i; // normal indices
    }

    struct Quad
    {
        public int v1i, v2i, v3i, v4i; // vertex indices
        public int t1i, t2i, t3i, t4i; // texture coordinate indices

        public Quad(int v1i, int v2i, int v3i, int v4i,
                    int t1i, int t2i, int t3i, int t4i)
        {
            this.v1i = v1i; this.v2i = v2i; this.v3i = v3i; this.v4i = v4i;
            this.t1i = t1i; this.t2i = t2i; this.t3i = t3i; this.t4i = t4i; 
        }
    }

    class Model
    {
        public Vec3D[] vertices;
        public Vec2D[] tcoords;
        public Vec3D[] normals;
        public Triangle[] polys;
        public float R;
    }

    struct zColor
    {
        public int C;
        public zColor(byte R, byte G, byte B) { C = B + (G << 8) + (R << 16); }
        public void Set(byte R, byte G, byte B) { C = B + (G << 8) + (R << 16); }
    }
}
