using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zModelViewer
{
    static partial class Rendition
    {
        static zColor linecolor = new zColor(255, 255, 255);

        static Vertex v1, v2, v3,
                      vtop, vleft, vright, vbottom, vmid,
                      vtemp;

        static float fracA, fracB; // interpolation coeffs

        static void PrepareTriangle() // prepares current triangle for rasterization - vertex indices sort, midpoint calculation, assumes triangle to be fully on screen
        {
            if (v1.y > v2.y) { vtemp = v1; v1 = v2; v2 = vtemp; }
            if (v1.y > v3.y) { vtemp = v1; v1 = v3; v3 = vtemp; }
            if (v2.y > v3.y) { vtemp = v2; v2 = v3; v3 = vtemp; }

            fracA = (v3.y - v1.y); if (fracA == 0f) { return; } // hmm
            fracB = (v2.y - v1.y) / fracA;
            fracA = 1f - fracB;

            v1.u *= v1.w; v1.v *= v1.w;
            v2.u *= v2.w; v2.v *= v2.w;
            v3.u *= v3.w; v3.v *= v3.w;

            v1.lightintensity *= v1.w;
            v2.lightintensity *= v2.w;
            v3.lightintensity *= v3.w; 

            vmid = new Vertex(v1.x * fracA + v3.x * fracB, v2.y, v1.z * fracA + v3.z * fracB, v1.w * fracA + v3.w * fracB,
                              v1.u * fracA + v3.u * fracB,
                              v1.v * fracA + v3.v * fracB,
                              v1.r * fracA + v3.r * fracB,
                              v1.g * fracA + v3.g * fracB,
                              v1.b * fracA + v3.b * fracB,
                              v1.lightintensity * fracA + v3.lightintensity * fracB);
            
            vtop = v1; vbottom = v3;

            if (vmid.x < v2.x) { vleft = vmid; vright = v2; }
            else               { vleft = v2; vright = vmid; }
        }

        static void PrepareSkyTriangle() // prepares current triangle for rasterization - vertex indices sort, midpoint calculation, assumes triangle to be fully on screen
        {
            if (v1.y > v2.y) { vtemp = v1; v1 = v2; v2 = vtemp; }
            if (v1.y > v3.y) { vtemp = v1; v1 = v3; v3 = vtemp; }
            if (v2.y > v3.y) { vtemp = v2; v2 = v3; v3 = vtemp; }

            fracA = (v3.y - v1.y); if (fracA == 0f) { return; } // hmm
            fracB = (v2.y - v1.y) / fracA;
            fracA = 1f - fracB;

            vmid = new Vertex(v1.x * fracA + v3.x * fracB, v2.y, 0f, v1.w * fracA + v3.w * fracB,
                              v1.u * fracA + v3.u * fracB,
                              v1.v * fracA + v3.v * fracB,
                              0f,
                              0f,
                              0f,
                              0f);

            vtop = v1; vbottom = v3;

            if (vmid.x < v2.x) { vleft = vmid; vright = v2; }
            else               { vleft = v2; vright = vmid; }
        }

        static float xl, xr; // left/right x traversing coordinate
        static float dxl, dxr; // left x delta, right x delta
        static float height; // top/bottom flat triangle height
        static float width; // top/bottom flat triangle scanline width
        static float zl, zr, zf; // resulting interpolated z value for zbuffer
        static int   y0; // vtop.y - start point
        static float z; // it`s depth, not the z coordinate!
        static float w, wl, wr; // 1/z for perspective correct interpolation (w = perspective z value)
        static float ul, vl, ur, vr;
        static int   u, v; // resulting interpolated texture coordinates
        static float lil, lir; // left/right light intensity
        static float li; // // resulting light intensity

        static float lightintensity;
        static Vec3D facenormal;

        static void RasterizeTextured() // maps texture to the current triangle
        {
            height = (int)Math.Ceiling(vleft.y - vtop.y);
            dxl = (vleft.x - vtop.x) / height;
            dxr = (vright.x - vtop.x) / height;

            y0 = (int)vtop.y;
            xl = xr = vtop.x;

            height = height == 0f ? 1f : height;

            for (int y = y0; y < (int)Math.Ceiling(vleft.y); y++)
            {
                fracB = (y - y0) / height;
                fracA = 1f - fracB;

                wl = fracA * vtop.w + fracB * vleft.w;
                wr = fracA * vtop.w + fracB * vright.w;

                ul = fracA * vtop.u + fracB * vleft.u;
                vl = fracA * vtop.v + fracB * vleft.v;
                ur = fracA * vtop.u + fracB * vright.u;
                vr = fracA * vtop.v + fracB * vright.v;

                lil = fracA * vtop.lightintensity + fracB * vleft.lightintensity;
                lir = fracA * vtop.lightintensity + fracB * vright.lightintensity;

                zl = fracA * vtop.z + fracB * vleft.z;
                zr = fracA * vtop.z + fracB * vright.z;

                width = xr - xl;
                pixels += (int)width;
                width = width < 1f ? 1f : (float)Math.Ceiling(width);

                for (int x = (int)xl; x < (int)Math.Ceiling(xr); x++)
                {
                    fracB = (x - (int)xl) / width;
                    fracA = 1f - fracB;

                    z = fracA * zl + fracB * zr;
                    zbufptr = y * clW + x;
                    if (z > zbuffer[zbufptr]) { continue; }
                    zbuffer[zbufptr] = z;

                    w = 1f / (fracA * wl + fracB * wr); // getting the actual perspective z value

                    u = (int)((fracA * ul + fracB * ur) * w * texWf);
                    v = (int)((fracA * vl + fracB * vr) * w * texHf);
                    li = (fracA * lil + fracB * lir) * w;

                    framebufptr = y * framestride + x;
                    texturebufptr = v * texturestride + u;

                    //framebuffer[framebufptr] = texturebuffer[texturebufptr];
                    
                    C = texturebuffer[texturebufptr];
                    framebuffer[framebufptr] = ((int)(((C & 0x00FF0000) >> 16) * li) << 16) +
                                               ((int)(((C & 0x0000FF00) >>  8) * li) <<  8) +
                                                (int)(( C & 0x000000FF)        * li);
                }
                
                xl += dxl;
                xr += dxr;
            }

            height = (int)Math.Ceiling(vbottom.y - vleft.y);
            dxl = (vbottom.x - vleft.x) / height;
            dxr = (vbottom.x - vright.x) / height;

            y0 = (int)vleft.y;
            xl = vleft.x;
            xr = vright.x;

            height = height == 0f ? 1f : height;

            for (int y = y0; y < (int)Math.Ceiling(vbottom.y); y++)
            {
                fracB = (y - y0) / height;
                fracA = 1f - fracB;

                wl = fracA * vleft.w + fracB * vbottom.w;
                wr = fracA * vright.w + fracB * vbottom.w;

                ul = fracA * vleft.u + fracB * vbottom.u;
                vl = fracA * vleft.v + fracB * vbottom.v;
                ur = fracA * vright.u + fracB * vbottom.u;
                vr = fracA * vright.v + fracB * vbottom.v;

                lil = fracA * vleft.lightintensity + fracB * vbottom.lightintensity;
                lir = fracA * vright.lightintensity + fracB * vbottom.lightintensity;

                zl = fracA * vleft.z + fracB * vbottom.z;
                zr = fracA * vright.z + fracB * vbottom.z;

                width = xr - xl;
                pixels += (int)width;
                width = width < 1f ? 1f : (float)Math.Ceiling(width);

                for (int x = (int)xl; x < (int)Math.Ceiling(xr); x++)
                {
                    fracB = (x - (int)xl) / width;
                    fracA = 1f - fracB;

                    z = fracA * zl + fracB * zr;
                    zbufptr = y * clW + x;
                    if (z > zbuffer[zbufptr]) { continue; }
                    zbuffer[zbufptr] = z;

                    w = 1f / (fracA * wl + fracB * wr); // getting the actual perspective z value

                    u = (int)((fracA * ul + fracB * ur) * w * texWf);
                    v = (int)((fracA * vl + fracB * vr) * w * texHf);
                    li = (fracA * lil + fracB * lir) * w;

                    framebufptr   = y * framestride + x;
                    texturebufptr = v * texturestride + u;

                    //framebuffer[framebufptr] = texturebuffer[texturebufptr];
                    C = texturebuffer[texturebufptr];
                    framebuffer[framebufptr] = ((int)(((C & 0x00FF0000) >> 16) * li) << 16) +
                                               ((int)(((C & 0x0000FF00) >>  8) * li) <<  8) +
                                                (int)(( C & 0x000000FF)        * li);
                }
                
                xl += dxl;
                xr += dxr;
            }
        }

        static int C;

        static void RasterizeSkyBox() // maps skybox texture to the current triangle
        {
            height = (int)Math.Ceiling(vleft.y - vtop.y);
            dxl = (vleft.x - vtop.x) / height;
            dxr = (vright.x - vtop.x) / height;

            y0 = (int)vtop.y;
            xl = xr = vtop.x;

            height = height == 0f ? 1f : height;

            for (int y = y0; y < (int)Math.Ceiling(vleft.y); y++)
            {
                fracB = (y - y0) / height;
                fracA = 1f - fracB;

                wl = fracA * vtop.w + fracB * vleft.w;
                wr = fracA * vtop.w + fracB * vright.w;

                ul = fracA * vtop.u + fracB * vleft.u;
                vl = fracA * vtop.v + fracB * vleft.v;
                ur = fracA * vtop.u + fracB * vright.u;
                vr = fracA * vtop.v + fracB * vright.v;

                width = xr - xl;
                pixels += (int)width;
                width = width < 1f ? 1f : (float)Math.Ceiling(width);

                for (int x = (int)xl; x < (int)Math.Ceiling(xr); x++)
                {
                    fracB = (x - (int)xl) / width;
                    fracA = 1f - fracB;

                    w = 1f / (fracA * wl + fracB * wr); // getting the actual perspective z value

                    u = (int)((fracA * ul + fracB * ur) * w * skytexWf);
                    v = (int)((fracA * vl + fracB * vr) * w * skytexHf);

                    framebufptr = y * framestride + x;
                    texturebufptr = v * skytexturestride + u;
                    framebuffer[framebufptr] = skyboxtexture[texturebufptr];
                }

                xl += dxl;
                xr += dxr;
            }

            height = (int)Math.Ceiling(vbottom.y - vleft.y);
            dxl = (vbottom.x - vleft.x) / height;
            dxr = (vbottom.x - vright.x) / height;

            y0 = (int)vleft.y;
            xl = vleft.x;
            xr = vright.x;

            height = height == 0f ? 1f : height;

            for (int y = y0; y < (int)Math.Ceiling(vbottom.y); y++)
            {
                fracB = (y - y0) / height;
                fracA = 1f - fracB;

                wl = fracA * vleft.w + fracB * vbottom.w;
                wr = fracA * vright.w + fracB * vbottom.w;

                ul = fracA * vleft.u + fracB * vbottom.u;
                vl = fracA * vleft.v + fracB * vbottom.v;
                ur = fracA * vright.u + fracB * vbottom.u;
                vr = fracA * vright.v + fracB * vbottom.v;

                width = xr - xl;
                pixels += (int)width;
                width = width < 1f ? 1f : (float)Math.Ceiling(width);

                for (int x = (int)xl; x < (int)Math.Ceiling(xr); x++)
                {
                    fracB = (x - (int)xl) / width;
                    fracA = 1f - fracB;

                    w = 1f / (fracA * wl + fracB * wr); // getting the actual perspective z value

                    u = (int)((fracA * ul + fracB * ur) * w * skytexWf);
                    v = (int)((fracA * vl + fracB * vr) * w * skytexHf);

                    framebufptr = y * framestride + x;
                    texturebufptr = v * skytexturestride + u;
                    framebuffer[framebufptr] = skyboxtexture[texturebufptr];
                }

                xl += dxl;
                xr += dxr;
            }
        }

        static void RasterizeResearch() // research
        {
            // affine texture mapping to a triangle according to its position on screen
            vtop.u    = vtop.x    / clWf; vtop.v    = vtop.y    / clHf;
            vleft.u   = vleft.x   / clWf; vleft.v   = vleft.y   / clHf;
            vright.u  = vright.x  / clWf; vright.v  = vright.y  / clHf;
            vbottom.u = vbottom.x / clWf; vbottom.v = vbottom.y / clHf;
            //

            height = (int)Math.Ceiling(vleft.y - vtop.y);
            dxl = (vleft.x - vtop.x) / height;
            dxr = (vright.x - vtop.x) / height;

            y0 = (int)vtop.y;
            xl = xr = vtop.x;

            height = height == 0f ? 1f : height;

            for (int y = y0; y < (int)Math.Ceiling(vleft.y); y++)
            {
                fracB = (y - y0) / height;
                fracA = 1f - fracB;

                ul = fracA * vtop.u + fracB * vleft.u;
                vl = fracA * vtop.v + fracB * vleft.v;
                ur = fracA * vtop.u + fracB * vright.u;
                vr = fracA * vtop.v + fracB * vright.v;

                zl = fracA * vtop.z + fracB * vleft.z;
                zr = fracA * vtop.z + fracB * vright.z;

                width = xr - xl;
                pixels += (int)width;
                width = width < 1f ? 1f : (float)Math.Ceiling(width);

                for (int x = (int)xl; x < (int)Math.Ceiling(xr); x++)
                {
                    fracB = (x - (int)xl) / width;
                    fracA = 1f - fracB;

                    z = fracA * zl + fracB * zr;
                    zbufptr = y * clW + x;
                    if (z > zbuffer[zbufptr]) { continue; }
                    zbuffer[zbufptr] = z;

                    u = (int)((fracA * ul + fracB * ur) * texWf) % texW;
                    v = (int)((fracA * vl + fracB * vr) * texHf) % texH;

                    if (u < 0) { u = texW + u; }
                    if (v < 0) { v = texH + v; }

                    framebufptr = y * framestride + x;
                    texturebufptr = v * texturestride + u;
                    framebuffer[framebufptr] = texturebuffer[texturebufptr];
                }

                xl += dxl;
                xr += dxr;
            }

            height = (int)Math.Ceiling(vbottom.y - vleft.y);
            dxl = (vbottom.x - vleft.x) / height;
            dxr = (vbottom.x - vright.x) / height;

            y0 = (int)vleft.y;
            xl = vleft.x;
            xr = vright.x;

            height = height == 0f ? 1f : height;

            for (int y = y0; y < (int)Math.Ceiling(vbottom.y); y++)
            {
                fracB = (y - y0) / height;
                fracA = 1f - fracB;

                ul = fracA * vleft.u + fracB * vbottom.u;
                vl = fracA * vleft.v + fracB * vbottom.v;
                ur = fracA * vright.u + fracB * vbottom.u;
                vr = fracA * vright.v + fracB * vbottom.v;

                zl = fracA * vleft.z + fracB * vbottom.z;
                zr = fracA * vright.z + fracB * vbottom.z;

                width = xr - xl;
                pixels += (int)width;
                width = width < 1f ? 1f : (float)Math.Ceiling(width);

                for (int x = (int)xl; x < (int)Math.Ceiling(xr); x++)
                {
                    fracB = (x - (int)xl) / width;
                    fracA = 1f - fracB;

                    z = fracA * zl + fracB * zr;
                    zbufptr = y * clW + x;
                    if (z > zbuffer[zbufptr]) { continue; }
                    zbuffer[zbufptr] = z;

                    u = (int)((fracA * ul + fracB * ur) * texWf) % texW;
                    v = (int)((fracA * vl + fracB * vr) * texHf) % texH;

                    if (u < 0) { u = texW + u; }
                    if (v < 0) { v = texH + v; }

                    framebufptr = y * framestride + x;
                    texturebufptr = v * texturestride + u;
                    framebuffer[framebufptr] = texturebuffer[texturebufptr];
                }

                xl += dxl;
                xr += dxr;
            }
        }

        static float rl, gl, bl, rr, gr, br;
        static zColor dotColor = new zColor(255, 255, 255); // resulting interpolated color

        static void RasterizeColored() // rasterizes current triangle with color interpolation
        {
            height = (int)Math.Ceiling(vleft.y - vtop.y);
            dxl =  (vleft.x - vtop.x) / height;
            dxr = (vright.x - vtop.x) / height;

            y0 = (int)vtop.y;
            xl = xr = vtop.x;

            height = height == 0f ? 1f : height;

            for (int y = y0; y < (int)Math.Ceiling(vleft.y); y++)
            {
                fracB = (y - y0) / height;
                fracA = 1f - fracB;

                rl = fracA * vtop.r + fracB * vleft.r;
                gl = fracA * vtop.g + fracB * vleft.g;
                bl = fracA * vtop.b + fracB * vleft.b;
                rr = fracA * vtop.r + fracB * vright.r;
                gr = fracA * vtop.g + fracB * vright.g;
                br = fracA * vtop.b + fracB * vright.b;

                zl = fracA * vtop.z + fracB * vleft.z;
                zr = fracA * vtop.z + fracB * vright.z;

                width = xr - xl;
                pixels += (int)width;
                width = width < 1f ? 1f : (float)Math.Ceiling(width);

                for (int x = (int)xl; x < (int)xr; x++)
                {
                    fracB = (x - xl) / width;
                    fracA = 1f - fracB;

                    z = fracA * zl + fracB * zr;
                    zbufptr = y * clW + x;
                    if (z > zbuffer[zbufptr]) { continue; }
                    zbuffer[zbufptr] = z;

                    dotColor.Set( (byte)((fracA * rl + fracB * rr) * 255f),
                                  (byte)((fracA * gl + fracB * gr) * 255f),
                                  (byte)((fracA * bl + fracB * br) * 255f));

                    framebufptr = y * framestride + x;
                    framebuffer[framebufptr] = dotColor.C;
                }

                xl += dxl;
                xr += dxr;
            }

            height = (int)Math.Ceiling(vbottom.y - vleft.y);
            dxl = (vbottom.x - vleft.x) / height;
            dxr = (vbottom.x - vright.x) / height;

            y0 = (int)vleft.y;
            xl = vleft.x;
            xr = vright.x;

            height = height == 0f ? 1f : height;

            for (int y = y0; y < (int)Math.Ceiling(vbottom.y); y++)
            {
                fracB = (y - y0) / height;
                fracA = 1f - fracB;

                rl = fracA * vleft.r + fracB * vbottom.r;
                gl = fracA * vleft.g + fracB * vbottom.g;
                bl = fracA * vleft.b + fracB * vbottom.b;
                rr = fracA * vright.r + fracB * vbottom.r;
                gr = fracA * vright.g + fracB * vbottom.g;
                br = fracA * vright.b + fracB * vbottom.b;

                zl = fracA * vleft.z + fracB * vbottom.z;
                zr = fracA * vright.z + fracB * vbottom.z;

                width = xr - xl;
                pixels += (int)width;
                width = width < 1f ? 1f : (float)Math.Ceiling(width);

                for (int x = (int)xl; x < (int)xr; x++)
                {
                    fracB = (x - xl) / width;
                    fracA = 1f - fracB;

                    z = fracA * zl + fracB * zr;
                    zbufptr = y * clW + x;
                    if (z > zbuffer[zbufptr]) { continue; }
                    zbuffer[zbufptr] = z;

                    dotColor.Set( (byte)((fracA * rl + fracB * rr) * 255f),
                                  (byte)((fracA * gl + fracB * gr) * 255f),
                                  (byte)((fracA * bl + fracB * br) * 255f));

                    framebufptr = y * framestride + x;
                    framebuffer[framebufptr] = dotColor.C;
                }

                xl += dxl;
                xr += dxr;
            }
        }

        static void RasterizeWireframe() // draws current triangle edges
        {
            DrawLine((int)v1.x, (int)v1.y, (int)v2.x, (int)v2.y);
            DrawLine((int)v2.x, (int)v2.y, (int)v3.x, (int)v3.y);
            DrawLine((int)v3.x, (int)v3.y, (int)v1.x, (int)v1.y);
        }

        static void DrawLine(int x0, int y0, int x1, int y1)
        {
            int deltaX, deltaY;
            int temp;

            if (y0 > y1)
            {
                temp = y0;
                y0 = y1;
                y1 = temp;
                temp = x0;
                x0 = x1;
                x1 = temp;
            }

            deltaX = x1 - x0;
            deltaY = y1 - y0;

            if (deltaX > 0) // deltaY always positive (down direction)
            {
                if (deltaX > deltaY) // x - major axis
                {
                    pixels += deltaX;
                    DrawLineOctant0(x0, y0, deltaX, deltaY, 1);
                }
                else  // y - major axis
                {
                    pixels += deltaY;
                    DrawLineOctant1(x0, y0, deltaX, deltaY, 1);
                }
            }
            else
            {
                deltaX = -deltaX;

                if (deltaX > deltaY) // x - major axis
                {
                    pixels += deltaX;
                    DrawLineOctant0(x0, y0, deltaX, deltaY, -1);
                }
                else // y - major axis
                {
                    pixels += deltaY;
                    DrawLineOctant1(x0, y0, deltaX, deltaY, -1);
                }
            }
        }

        static void DrawLineOctant0(int x0, int y0, int deltaX, int deltaY, int directionX)
        {

            int DeltaYx2 = deltaY << 1; // scaling for half-error with integers
            int DeltaYx2MinusDeltaXx2 = DeltaYx2 - (deltaX << 1);
            int ErrorTerm = DeltaYx2 - deltaX;

            framebufptr = y0 * framestride + x0;
            framebuffer[framebufptr] = linecolor.C;

            while (deltaX-- > 0)
            {
                if (ErrorTerm >= 0)
                {
                    y0++;
                    ErrorTerm += DeltaYx2MinusDeltaXx2;
                }
                else
                {
                    ErrorTerm += DeltaYx2;
                }

                x0 += directionX;

                framebufptr = y0 * framestride + x0;
                framebuffer[framebufptr] = linecolor.C;
            }
        }

        static void DrawLineOctant1(int x0, int y0, int deltaX, int deltaY, int directionX)
        {
            int DeltaXx2 = deltaX << 1;
            int DeltaXx2MinusDeltaYx2 = DeltaXx2 - (deltaY << 1);
            int ErrorTerm = DeltaXx2 - deltaY;

            framebufptr = y0 * framestride + x0;
            framebuffer[framebufptr] = linecolor.C;

            while (deltaY-- > 0)
            {
                if (ErrorTerm >= 0)
                {
                    x0 += directionX;
                    ErrorTerm += DeltaXx2MinusDeltaYx2;
                }
                else
                {
                    ErrorTerm += DeltaXx2;
                }

                y0++;

                framebufptr = y0 * framestride + x0;
                framebuffer[framebufptr] = linecolor.C;
            }
        }
    }
}
