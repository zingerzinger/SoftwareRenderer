using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace zModelViewer
{
    static partial class Rendition
    {
        static int[]  framebuffer;
        static int    clW, clH; // framebuffer width, height
        static float  clWf, clHf;
        static float  clWhalff, clHhalff;

        static float  soffX, soffY; // offsets
        static int    framestride;
        static int    framebufptr;

        static int[]  texturebuffer;
        static int    texW, texH; // texture buffer width, height
        static float  texWf, texHf;
        static int    texturestride;
        static int    texturebufptr;

        static int[]  skyboxtexture;
        static int    skytexW, skytexH; // texture buffer width, height
        static float  skytexWf, skytexHf;
        static int    skytexturestride;
        static int    skytexturebufptr;

        static float[] zbuffer;
        static int     zbufstride;
        static int     zbufptr;

        static Graphics framebufg; // frame buffer (Bitmap) Graphics object

        static SolidBrush fontbrushRed   = new SolidBrush(Color.Red),
                          fontbrushGreen = new SolidBrush(Color.Green),
                          fontbrushBlue  = new SolidBrush(Color.Blue),
                          fontbrushWhite = new SolidBrush(Color.White);
        static Font font = new Font("Courier New", 10f, FontStyle.Regular);

        static string RenderModeInfo = "1:tex+\n2:col-\n3:wrf-\n4:bfc+";

        delegate void dlgtRasterizationMethod();
        static dlgtRasterizationMethod RasterizationMode;

        static Vec3D CamDir = new Vec3D(0f, 0f, -1f), CamStrafeDir = new Vec3D(1f, 0f, 0f);
        public static float camPitch, camYaw, camRoll; // camera orientation
        static float zNear, zFar;
        static float hfov, vfov;
        public static Vec3D CAMERAORIGIN;
        static Mat44 MODELMATRIX,
                     NDCMATRIX,
                     MODELCAMERAMATRIX,
                     MODELCAMERANDCMATRIX;
        public static Mat44 CAMERAMATRIX;

        static Vec3D lightDir;

        static Vec3D   [] vertices;
        static Vec3D   [] normals;
        static Vec2D   [] tcoords;
        static Triangle[] Triangles;

        static Vec3D   [] skyverts; // special sky triangle vertices
        static Triangle[] skyTriangles; // special skybox polygons
        const float one  = 1f;
        const float half = 0.5f;
        const float th   = 1f / 3f;
        const float sx = 2f / 3f;

        static Vec3D[] skyboxverts = new Vec3D[] { new Vec3D(-one,  one, -one), new Vec3D(-one,  one,  one), 
                                                   new Vec3D( one,  one,  one), new Vec3D( one,  one, -one),
                                                   new Vec3D(-one, -one, -one), new Vec3D(-one, -one,  one),
                                                   new Vec3D( one, -one,  one), new Vec3D( one, -one, -one) }; // skybox vertices for quads

        static Vec3D[] skyv = new Vec3D[8]; // skybox vertices transformed by camera matrix

        const float eps = 0.001f; // skybox texture coordinates need to be slightly offset for no overlapping
        static Vec2D sc1tl = new Vec2D(0f, 0f), // left
                     sc1tr = new Vec2D(th, 0f),
                     sc1bl = new Vec2D(0f, half),
                     sc1br = new Vec2D(th, half),

                     sc2tl = new Vec2D(th + eps, 0f), // right
                     sc2tr = new Vec2D(sx, 0f),
                     sc2bl = new Vec2D(th + eps, half),
                     sc2br = new Vec2D(sx, half),

                     sc3tl = new Vec2D(sx + eps, 0f), // front
                     sc3tr = new Vec2D(1f, 0f),
                     sc3bl = new Vec2D(sx + eps, half),
                     sc3br = new Vec2D(1f, half),

                     sc4tl = new Vec2D(0f, half + eps), // back
                     sc4tr = new Vec2D(th, half + eps),
                     sc4bl = new Vec2D(0f, 1f),
                     sc4br = new Vec2D(th, 1f),

                     sc5tl = new Vec2D(th + eps, half + eps), // top
                     sc5tr = new Vec2D(sx, half + eps),
                     sc5bl = new Vec2D(th + eps, 1f),
                     sc5br = new Vec2D(sx, 1f),

                     sc6tl = new Vec2D(sx + eps, half + eps), // bottom
                     sc6tr = new Vec2D(1f, half + eps),
                     sc6bl = new Vec2D(sx + eps, 1f),
                     sc6br = new Vec2D(1f, 1f);

        static Vec2D[] skytcoords = new Vec2D[] { sc2tr, sc2tl, sc2bl, sc2br,
                                                  sc1tr, sc1tl, sc1bl, sc1br,
                                                  sc3tr, sc3tl, sc3bl, sc3br,
                                                  sc4tr, sc4tl, sc4bl, sc4br,
                                                  sc5tr, sc5tl, sc5bl, sc5br,
                                                  sc6tr, sc6tl, sc6bl, sc6br}; // skybox texture coordinates for quads

        static Quad [] skyquads    = new Quad[] { new Quad(0, 1, 5, 4, /**/  0,  1,  2,  3),
                                                  new Quad(2, 3, 7, 6, /**/  4,  5,  6,  7),
                                                  new Quad(1, 2, 6, 5, /**/  8,  9, 10, 11),
                                                  new Quad(3, 0, 4, 7, /**/ 12, 13, 14, 15),
                                                  new Quad(0, 3, 2, 1, /**/ 16, 17, 18, 19),
                                                  new Quad(5, 6, 7, 4, /**/ 20, 21, 22, 23) }; // skybox planes (left, right, front, back, top, bottom)

        static float catx, caty, catz; // for deleting camera matrix translation

        static float meshR; // mesh bounding sphere radius

        const  float PIx2f = (float)(Math.PI * 2d);
        static float modelpitch = 0f, modelyaw = 0f, modelroll = 0f;
        static float mrotspeedp = 0.3f, mrotspeedy = 0.3f, mrotspeedr = 0.3f;
        static Vec3D modelorigin;

        static public bool modelrotating = false;
        static int pixels = 0;
        static int trianglesRendered = 0;
        static int meshesInFrustum = 0;

        static bool rm_texture    = true,
                    rm_color      = false,
                    rm_wireframe  = false,
                    rm_backface   = true,
                    testoperation = false,
                    testflag      = false;

        public static IntPtr Init(ref Bitmap backbuffer, Model model, Model skymodel, Bitmap texture)
        {
            clWf = clW = backbuffer.Width;
            clHf = clH = backbuffer.Height;
            clWhalff = clWf / 2f;
            clHhalff = clHf / 2f;
            soffX = clWf / 2f - 0.5f; soffY = clHf / 2f - 0.5f;

            framestride = clW;

            framebuffer = new int[clW * clH];
            GCHandle gchandle = GCHandle.Alloc(framebuffer, GCHandleType.Pinned);
            backbuffer = new Bitmap(clW, clH, framestride * 4, PixelFormat.Format32bppRgb, gchandle.AddrOfPinnedObject());
            framebufferBmp = backbuffer;

            texWf = texW = texture.Width;
            texHf = texH = texture.Height;
            texWf--; texHf--;
            texturestride = texW;

            texturebuffer = new int[texW * texH];

            BitmapData bmd = texture.LockBits(new Rectangle(0, 0, texW, texH), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
            //int[] temp = new int[texturebuffer.Length]; // fuck Marshal.Copy
            byte[] dbgb = new byte[texturebuffer.Length * 4];
            Marshal.Copy(bmd.Scan0, dbgb, 0, dbgb.Length);
            for (int i = 3; i < dbgb.Length; i+= 4) { dbgb[i] = 0; }
            Buffer.BlockCopy(dbgb, 0, texturebuffer, 0, dbgb.Length);
            texture.UnlockBits(bmd);

            Bitmap sky = Properties.Resources.skybox;
            skytexWf = skytexW = sky.Width;
            skytexHf = skytexH = sky.Height;
            skytexWf--; skytexHf--;
            skytexturestride = skytexW;
            skyboxtexture = new int[skytexW * skytexH];
            bmd = sky.LockBits(new Rectangle(0, 0, skytexW, skytexH), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
            //temp = new int[skyboxtexture.Length]; // fuck Marshal.Copy
            Marshal.Copy(bmd.Scan0, skyboxtexture, 0, skyboxtexture.Length);
            //Buffer.BlockCopy(temp, 0, skyboxtexture, 0, skyboxtexture.Length * 4);
            sky.UnlockBits(bmd);

            zbuffer = new float[clW * clH];
            zbufstride = clW;

            vertices  = model.vertices;
            normals   = model.normals;
            tcoords   = model.tcoords;
            Triangles = model.polys;
            meshR     = model.R;

            if (skymodel == null)
            {
                skyverts     = new Vec3D[0];
                skyTriangles = new Triangle[0];
            }
            else
            {
                skyverts     = skymodel.vertices;
                skyTriangles = skymodel.polys;
            }


            framebufg = Graphics.FromImage(backbuffer);
            framebufg.SmoothingMode      = SmoothingMode.None;
            framebufg.InterpolationMode  = InterpolationMode.NearestNeighbor;
            framebufg.CompositingQuality = CompositingQuality.HighSpeed;
            framebufg.PixelOffsetMode    = PixelOffsetMode.Half;
            framebufg.PageUnit = GraphicsUnit.Pixel;

            RasterizationMode = RasterizeTextured;

            MODELMATRIX  = Mat44.BuildModelMatrix(modelpitch, modelyaw, modelroll, modelorigin);
            CAMERAMATRIX = Mat44.BuildCameraMatrix(camYaw, camPitch, camRoll, CAMERAORIGIN);

            zNear = 0.01f; zFar = 100f;
            hfov = PIx2f / 8f; vfov = PIx2f / 8f;

            SetFrustumPlanes(clWf / clHf, hfov, vfov, zNear, zFar);
            NDCMATRIX = Mat44.BuildNDCMatrix(clWf / clHf, hfov, vfov, zNear, zFar);
            return gchandle.AddrOfPinnedObject();
        }

        static Bitmap framebufferBmp;
        public static void ScreenShot() 
        {
            if (!System.IO.Directory.Exists("screenshots")) { System.IO.Directory.CreateDirectory("screenshots"); }
            modelrotating = false; framebufferBmp.Save(string.Format("screenshots\\scr_{0}.png", DateTime.Now.Ticks), ImageFormat.Png); 
        }

        public static void DumpZBuffer()
        {
            Bitmap zbufim = new Bitmap(clW, clH);
            byte b = 0;

            for (int x = 0; x < clW; x++)
            {
                for (int y = 0; y < clH; y++)
                {
                    zbufptr = y * clW + x; 
                    b = (byte)((1f / zbuffer[zbufptr]) / (zFar - zNear) * 255f);
                    zbufim.SetPixel(x, y, Color.FromArgb(b, b, b));
                }
            }

            zbufim.Save("zbufferdump.png", ImageFormat.Png);
        }

        public static void ChangeRenderMode(int mode)
        {
            switch (mode)
            {
                case 0: RasterizationMode = RasterizeTextured;  rm_texture   = true; rm_color   = false; rm_wireframe = false; break;
                case 1: RasterizationMode = RasterizeColored;   rm_color     = true; rm_texture = false; rm_wireframe = false; break;
                case 2: RasterizationMode = RasterizeWireframe; rm_wireframe = true; rm_texture = false; rm_color     = false; break;
                case 3: rm_backface       = !rm_backface; break;
                case 4: testoperation     = true;         break;
                case 5: testflag          = !testflag;    break;
            }

            RenderModeInfo = "1:tex" + (rm_texture ? '+' : '-') + "\n2:col" + (rm_color ? '+' : '-') + "\n3:wrf" + (rm_wireframe ? '+' : '-') + "\n4:bfc" + (rm_backface ? '+' : '-');
        }

        public static void RotateCamera(float dyaw, float dpitch, float droll) // dyaw/dpitch - number of pixels the mouse has moved in x/y screen axes, droll - Q/E
        {
            camYaw   -= dyaw   * 0.002f;
            camPitch -= dpitch * 0.002f;

            // pi/2 1.570796f, pi*2 6.283185f

            if      (camPitch < -1.570796f) { camPitch = -1.570796f; }
            else if (camPitch >  1.570796f) { camPitch =  1.570796f; }

            if      (camYaw < 0f       ) { camYaw += 6.283185f; }
            else if (camYaw > 6.283185f) { camYaw -= 6.283185f; }
        }

        public static void ResetCameraOrientation() { camYaw = camPitch = camRoll = 0f; }

        static Triangle curTriangle;
        static Vec3D va, vb, vc; // current triangle vertices, represented by 3D vectors
        static Vec3D na, nb, nc; // vertex normals
        static float lia, lib, lic; // vertex light intensities
        static bool onScreen;
        static Vec3D axisX, axisY, axisZ;
        static float invw;

        static Vec2D[] skyportal; // clipper for skybox
        static int sinvi;
        static int souvi;
        static Vertex[] sinv = new Vertex[12];
        static Vertex[] souv = new Vertex[12];
        static Vec2D clipa, clipb;
        static Vertex sa, sb;

        static Vec3D sva, svb, svc, svd; // intermidiate skybox vertices
        static int skyquadsintriangle;

        static Vertex ClipSegment(Vec2D cla, Vec2D clb, Vertex sa, Vertex sb) // clip segment (sa, sb) by line (cla, clb)
        {
            float temp; // multiuse
            
            float AL = clb.y - cla.y,
                  BL = cla.x - clb.x,
                  CL = cla.y * clb.x - cla.x * clb.y,

                  AS = sb.y - sa.y,
                  BS = sa.x - sb.x,
                  CS = sa.y * sb.x - sa.x * sb.y;

            temp = 1f / (AL * BS - BL * AS);

            float x, y; // intersection

            x = (BL * CS - CL * BS) * temp;
            y = (CL * AS - AL * CS) * temp;

            float fracA, fracB;

            float diffX = sb.x - sa.x,
                  diffY = sb.y - sa.y;

            if (Math.Abs(diffX) > Math.Abs(diffY)) { fracB = (x - sa.x) / diffX; }
            else                                   { fracB = (y - sa.y) / diffY; }
            fracA = 1f - fracB;

            return new Vertex(x, y, 0f,
                              fracA * sa.w + fracB * sb.w,
                              fracA * sa.u + fracB * sb.u,
                              fracA * sa.v + fracB * sb.v,
                              0f, 0f, 0f, 0f);
        }

        public static void Render(float dt, Vec3D position)
        {
            if (rm_wireframe) { for (int i = 0; i < framebuffer.Length; i++) { framebuffer[i] =              0; } }
                                for (int i = 0; i < zbuffer.Length;     i++) { zbuffer[i]     = float.MaxValue; }

            linecolor.Set(255, 255, 255);

            // rendering pipeline start:

            position.y += 0.25f;
            CAMERAMATRIX = Mat44.BuildCameraMatrix(camYaw, camPitch, camRoll, position);
            //MODELMATRIX  = Mat44.BuildModelMatrix(modelyaw, modelpitch, modelroll, modelorigin);

            MODELCAMERAMATRIX    = CAMERAMATRIX.Transponse();// *MODELMATRIX; // camera matrix may be already transponsed during camera matrix building
            MODELCAMERANDCMATRIX = NDCMATRIX * MODELCAMERAMATRIX;
            //lightDir = (CAMERAORIGIN - modelorigin).Unit();

            // first render skybox triangles:

            catx = CAMERAMATRIX.m30; CAMERAMATRIX.m30 = 0f; // deleting translation
            caty = CAMERAMATRIX.m31; CAMERAMATRIX.m31 = 0f;
            catz = CAMERAMATRIX.m32; CAMERAMATRIX.m32 = 0f;
            for (int i = 0; i < skyv.Length; i++) { skyv[i] = skyboxverts[i] * CAMERAMATRIX.Transponse(); }
            CAMERAMATRIX.m30 = catx; // returning translation
            CAMERAMATRIX.m31 = caty;
            CAMERAMATRIX.m32 = catz;

            for (int i = 0; i < skyTriangles.Length; i++)
            {
                curTriangle = skyTriangles[i];

                va = skyverts[curTriangle.v1i] * MODELCAMERANDCMATRIX; // all the coordinates end up in clip space
                vb = skyverts[curTriangle.v2i] * MODELCAMERANDCMATRIX;
                vc = skyverts[curTriangle.v3i] * MODELCAMERANDCMATRIX;


                inv[0] = new Vertex(va.x, va.y, va.z, va.w, 0f, 0f, 1f, 0f, 0f, 0f);
                inv[1] = new Vertex(vb.x, vb.y, vb.z, vb.w, 0f, 0f, 0f, 1f, 0f, 0f);
                inv[2] = new Vertex(vc.x, vc.y, vc.z, vc.w, 0f, 0f, 0f, 0f, 1f, 0f);

                invi = 2;
                ouvi = 0;
                Clip();

                for (int k = 0; k < ouvi; k++)
                {
                    v1 = ouv[k];
                    invw = 1f / (v1.w); v1.x *= invw; v1.y *= invw; v1.z *= invw; v1.w = invw; // perspecive division (-> -1f...1f), w -> 0f...1f
                    ouv[k] = v1;
                }

                if (ouvi < 3) { continue; }

                v1 = ouv[0];
                v2 = ouv[1];
                v3 = ouv[2];

                if (BackFace(v1, v2 ,v3)) { continue; }

                // project skybox quads to screen, cull back facing quads, clip quads by skyportal, get skyvertices to viewport, render the triangles
                skyportal = new Vec2D[ouvi];
                for (int j = 0; j < ouvi; j++) { skyportal[j] = new Vec2D(ouv[j].x, ouv[j].y); }

                Quad quad;
                for (int j = 0; j < skyquads.Length; j++)
                {
                    quad = skyquads[j];
                    sva = skyv[quad.v1i] * NDCMATRIX; // ok, we have our skyboxquad vertices in 4D clip space
                    svb = skyv[quad.v2i] * NDCMATRIX;
                    svc = skyv[quad.v3i] * NDCMATRIX;
                    svd = skyv[quad.v4i] * NDCMATRIX;

                    inv[0] = new Vertex(sva.x, sva.y, sva.z, sva.w, skytcoords[quad.t1i].x, skytcoords[quad.t1i].y, 1f, 0f, 0f, 0f);
                    inv[1] = new Vertex(svb.x, svb.y, svb.z, svb.w, skytcoords[quad.t2i].x, skytcoords[quad.t2i].y, 0f, 1f, 0f, 0f);
                    inv[2] = new Vertex(svc.x, svc.y, svc.z, svc.w, skytcoords[quad.t3i].x, skytcoords[quad.t3i].y, 0f, 0f, 1f, 0f);
                    inv[3] = new Vertex(svd.x, svd.y, svd.z, svd.w, skytcoords[quad.t4i].x, skytcoords[quad.t4i].y, 1f, 1f, 0f, 0f);

                    invi = 3;
                    ouvi = 0;
                    Clip();

                    List<Vertex> ins = new List<Vertex>();
                    List<Vertex> ous = new List<Vertex>();

                    for (int k = 0; k < ouvi; k++)
                    {
                        v1 = ouv[k];
                        invw = 1f / (v1.w); v1.x *= invw; v1.y *= invw; v1.z *= invw; v1.w = invw; v1.u *= invw; v1.v *= invw; // perspecive division (-> -1f...1f), w -> 0f...1f
                        ins.Add(v1);
                    }

                    if (ins.Count < 3 || BackFace(ins[0], ins[1], ins[2])) { continue; } // check backfacing skybox quad

                    for (int m = 0; m < skyportal.Length; m++)
                    {
                        clipa = skyportal[m];
                        clipb = skyportal[(m + 1) % skyportal.Length];
                        
                        for (int k = 0; k < ins.Count; k++)
                        {
                            a = ins[k];
                            b = ins[(k + 1) % ins.Count];

                            Vec2D clipvec = clipb - clipa;

                            aOK = Vec2D.Cross(clipvec, new Vec2D(a.x - clipa.x, a.y - clipa.y)) > 0f;
                            bOK = Vec2D.Cross(clipvec, new Vec2D(b.x - clipa.x, b.y - clipa.y)) > 0f;

                            if (aOK)        { ous.Add(a); }
                            if (aOK == bOK) { continue; }

                            ous.Add(ClipSegment(clipa, clipb, a, b));
                        }
                        // swap ins and ous
                        ins.Clear();
                        ins.AddRange(ous);
                        ous.Clear();
                    }

                    for (int k = 0; k < ins.Count; k++)
                    {
                        v1 = ins[k];
                        v1.x = v1.x * (clWhalff) + soffX; v1.y = -v1.y * (clHhalff) + soffY; // viewport
                        ins[k] = v1;
                    }

                    for (int m = 1; m < ins.Count - 1; m++)
                    {
                        v1 = ins[0];
                        v2 = ins[m];
                        v3 = ins[m + 1];

                        PrepareSkyTriangle();
                        RasterizeSkyBox(); // bug with u,v,x,y coordinates only while debugging, maybe because of float optimizations/x86 target platform ?!
                    }
                }

                trianglesRendered++;
            }

            for (int i = 0; i < Triangles.Length; i++)
            {
                curTriangle = Triangles[i];

                va = vertices[curTriangle.v1i];
                vb = vertices[curTriangle.v2i];
                vc = vertices[curTriangle.v3i];

                lightDir = (position - (va + vb + vc) * (1f / 3f)).Unit();

                va *= MODELCAMERANDCMATRIX; // all the coordinates end up in clip space
                vb *= MODELCAMERANDCMATRIX;
                vc *= MODELCAMERANDCMATRIX;

                // before clipping cheap test:

                onScreen = va.x >= -va.w && va.x <= va.w && // we also can check how much screen vertices are inside the triangle - optimization
                           va.y >= -va.w && va.y <= va.w &&
                           va.z >= -va.w && va.z <= va.w &&

                           vb.x >= -vb.w && vb.x <= vb.w &&
                           vb.y >= -vb.w && vb.y <= vb.w &&
                           vb.z >= -vb.w && vb.z <= vb.w &&

                           vc.x >= -vc.w && vc.x <= vc.w &&
                           vc.y >= -vc.w && vc.y <= vc.w &&
                           vc.z >= -vc.w && vc.z <= vc.w;

                if (onScreen)  // triangle is completely inside the screen
                {
                    invw = 1f / va.w; va.x *= invw; va.y *= invw; va.z *= invw; va.w = invw; // perspecive division (-> -1f...1f)
                    invw = 1f / vb.w; vb.x *= invw; vb.y *= invw; vb.z *= invw; vb.w = invw;
                    invw = 1f / vc.w; vc.x *= invw; vc.y *= invw; vc.z *= invw; vc.w = invw;

                    if (rm_backface && BackFace(va, vb, vc)) { continue; }

                    na = normals[curTriangle.n1i] * MODELMATRIX; // light intensity calculation
                    nb = normals[curTriangle.n2i] * MODELMATRIX;
                    nc = normals[curTriangle.n3i] * MODELMATRIX;
                    lia = Vec3D.Dot(na, lightDir); lia = lia < 0.1f ? 0.1f : lia;
                    lib = Vec3D.Dot(nb, lightDir); lib = lib < 0.1f ? 0.1f : lib;
                    lic = Vec3D.Dot(nc, lightDir); lic = lic < 0.1f ? 0.1f : lic;
                    //if (testflag) { lia = lib = lic = (lia + lib + lic) * (1f / 3f); }
                    if (testflag) { lia = lib = lic = 1f; }

                    va.x = va.x * (clWhalff) + soffX; va.y = -va.y * (clHhalff) + soffY; // viewport
                    vb.x = vb.x * (clWhalff) + soffX; vb.y = -vb.y * (clHhalff) + soffY;
                    vc.x = vc.x * (clWhalff) + soffX; vc.y = -vc.y * (clHhalff) + soffY;

                    v1 = new Vertex(va.x, va.y, va.z, va.w, tcoords[curTriangle.t1i].x, tcoords[curTriangle.t1i].y, 1f, 0f, 0f, lia);
                    v2 = new Vertex(vb.x, vb.y, vb.z, vb.w, tcoords[curTriangle.t2i].x, tcoords[curTriangle.t2i].y, 0f, 1f, 0f, lib);
                    v3 = new Vertex(vc.x, vc.y, vc.z, vc.w, tcoords[curTriangle.t3i].x, tcoords[curTriangle.t3i].y, 0f, 0f, 1f, lic);

                    PrepareTriangle();
                    RasterizationMode();
                    trianglesRendered++;
                }
                else  // triangle is partially on the screen (in some cases all the vertices are outside, but the edge may cross the screen), needs clipping
                {
                    na = normals[curTriangle.n1i] * MODELMATRIX; // light intensity calculation
                    nb = normals[curTriangle.n2i] * MODELMATRIX;
                    nc = normals[curTriangle.n3i] * MODELMATRIX;
                    lia = Vec3D.Dot(na, lightDir); lia = lia < 0.1f ? 0.1f : lia;
                    lib = Vec3D.Dot(nb, lightDir); lib = lib < 0.1f ? 0.1f : lib;
                    lic = Vec3D.Dot(nc, lightDir); lic = lic < 0.1f ? 0.1f : lic;
                    //if (testflag) { lia = lib = lic = (lia + lib + lic) * (1f / 3f); }
                    if (testflag) { lia = lib = lic = 1f; }

                    inv[0] = new Vertex(va.x, va.y, va.z, va.w, tcoords[curTriangle.t1i].x, tcoords[curTriangle.t1i].y, 1f, 0f, 0f, lia);
                    inv[1] = new Vertex(vb.x, vb.y, vb.z, vb.w, tcoords[curTriangle.t2i].x, tcoords[curTriangle.t2i].y, 0f, 1f, 0f, lib);
                    inv[2] = new Vertex(vc.x, vc.y, vc.z, vc.w, tcoords[curTriangle.t3i].x, tcoords[curTriangle.t3i].y, 0f, 0f, 1f, lic);

                    invi = 2; // index
                    ouvi = 0; // index
                    Clip();

                    for (int k = 0; k <= ouvi; k++)
                    {
                        v1 = ouv[k];
                        invw = 1f / (v1.w); v1.x *= invw; v1.y *= invw; v1.z *= invw; v1.w = invw; // perspecive division (-> -1f...1f), w -> 0f...1f
                        v1.x = v1.x * (clWhalff) + soffX; v1.y = -v1.y * (clHhalff) + soffY; // viewport
                        ouv[k] = v1;
                    }

                    for (int k = 1; k < ouvi - 1; k++)
                    {
                        v1 = ouv[0];
                        v2 = ouv[k];
                        v3 = ouv[k + 1];

                        if (rm_backface && !BackFace(v1, v2, v3)) { continue; } // calculating in screenspace, y is negated!
                                                     
                        PrepareTriangle();
                        RasterizationMode();
                        trianglesRendered++;
                    }
                }
            }
            meshesInFrustum++;
            // rendering pipleline end.

            framebufg.DrawString(string.Format(System.Globalization.CultureInfo.GetCultureInfo(14337), "{0:00.000} ms, {1:#} fps,~{2} pixels\n{3} meshes in frustum\n{4} triangles\n{5}\nx: {6}\ny: {7}\nz: {8}",
                                 dt * 1000f, 1f / dt, pixels, meshesInFrustum, trianglesRendered, RenderModeInfo,
                                 position.x, position.y, position.z), font, fontbrushWhite, 0f, 0f);

            axisX = new Vec3D(CAMERAMATRIX.m00 * 50f + 60f, CAMERAMATRIX.m01 * 50f + 200f, CAMERAMATRIX.m30); // Axis orientation helper
            axisY = new Vec3D(CAMERAMATRIX.m10 * 50f + 60f, CAMERAMATRIX.m11 * 50f + 200f, CAMERAMATRIX.m31);
            axisZ = new Vec3D(CAMERAMATRIX.m20 * 50f + 60f, CAMERAMATRIX.m21 * 50f + 200f, CAMERAMATRIX.m32);

            linecolor.Set(255, 0, 0); DrawLine(60, 200, (int)axisX.x, (int)axisX.y);
            linecolor.Set(0, 128, 0); DrawLine(60, 200, (int)axisY.x, (int)axisY.y);
            linecolor.Set(0, 0, 255); DrawLine(60, 200, (int)axisZ.x, (int)axisZ.y);
            framebufg.DrawString(axisX.z > 0f ? "+x" : "-x", font, fontbrushRed,   axisX.x, axisX.y);
            framebufg.DrawString(axisY.z > 0f ? "+y" : "-y", font, fontbrushGreen, axisY.x, axisY.y);
            framebufg.DrawString(axisZ.z > 0f ? "+z" : "-z", font, fontbrushBlue,  axisZ.x, axisZ.y);
            
            pixels = trianglesRendered = meshesInFrustum = 0;
        }

        static bool BackFace(Vec3D a, Vec3D b, Vec3D c)    { return ((c.x - a.x) * (b.y - a.y) - (c.y - a.y) * (b.x - a.x)) >= 0f; }
        static bool BackFace(Vertex a, Vertex b, Vertex c) { return ((c.x - a.x) * (b.y - a.y) - (c.y - a.y) * (b.x - a.x)) >= 0f; }

        static int      invi = 0;
        static int      ouvi = 0;
        static bool     aOK, bOK;
        static Vertex   a, b;
        static Vertex[] inv = new Vertex[12];
        static Vertex[] ouv = new Vertex[12];
        static Vertex[] temp;

        static void Clip()
        {
            int i;
            
            for (i = 0; i <= invi; i++) // zNear
            {
                a = inv[i];
                b = inv[(i + 1) % (invi + 1)];

                aOK = a.z >= -a.w;
                bOK = b.z >= -b.w;

                if (aOK) { ouv[ouvi] = a; ouvi++; }
                if (aOK == bOK) { continue; }

                fracB = (a.z + a.w) / ((a.w + a.z) - (b.w + b.z));
                fracA = 1f - fracB;

                ouv[ouvi] = new Vertex(fracA * a.x + fracB * b.x,
                                       fracA * a.y + fracB * b.y,
                                       fracA * a.z + fracB * b.z,
                                       fracA * a.w + fracB * b.w,

                                       fracA * a.u + fracB * b.u,
                                       fracA * a.v + fracB * b.v,
                                       fracA * a.r + fracB * b.r,
                                       fracA * a.g + fracB * b.g,
                                       fracA * a.b + fracB * b.b,
                                       fracA * a.lightintensity + fracB * b.lightintensity);
                ouvi++;
            }
            // swap initial vertices array with resulting array:
            invi = ouvi - 1;
            ouvi = 0;
            temp = inv;
            inv = ouv;
            ouv = temp;

            for (i = 0; i <= invi; i++) // zFar
            {
                a = inv[i];
                b = inv[(i + 1) % (invi + 1)];

                aOK = a.z <= a.w;
                bOK = b.z <= b.w;

                if (aOK) { ouv[ouvi] = a; ouvi++; }
                if (aOK == bOK) { continue; }

                fracB = (a.z - a.w) / ((b.w - b.z) - (a.w - a.z));
                fracA = 1f - fracB;

                if (float.IsInfinity(fracA) || float.IsInfinity(fracA)) {return;}

                ouv[ouvi] = new Vertex(fracA * a.x + fracB * b.x,
                                       fracA * a.y + fracB * b.y,
                                       fracA * a.z + fracB * b.z,
                                       fracA * a.w + fracB * b.w,

                                       fracA * a.u + fracB * b.u,
                                       fracA * a.v + fracB * b.v,
                                       fracA * a.r + fracB * b.r,
                                       fracA * a.g + fracB * b.g,
                                       fracA * a.b + fracB * b.b,
                                       fracA * a.lightintensity + fracB * b.lightintensity);
                ouvi++;
            }
            // swap initial vertices array with resulting array:
            invi = ouvi - 1;
            ouvi = 0;
            temp = inv;
            inv = ouv;
            ouv = temp;
            
            for (i = 0; i <= invi; i++) // top
            {
                a = inv[i];
                b = inv[(i + 1) % (invi + 1)];

                aOK = a.y <= a.w;
                bOK = b.y <= b.w;

                if (aOK)        { ouv[ouvi] = a; ouvi++; }
                if (aOK == bOK) { continue; }

                fracB = (a.y - a.w) / ((b.w - b.y) - (a.w - a.y));
                fracA = 1f - fracB;

                ouv[ouvi] = new Vertex(fracA * a.x + fracB * b.x,
                                       fracA * a.y + fracB * b.y,
                                       fracA * a.z + fracB * b.z,
                                       fracA * a.w + fracB * b.w,

                                       fracA * a.u + fracB * b.u,
                                       fracA * a.v + fracB * b.v,
                                       fracA * a.r + fracB * b.r,
                                       fracA * a.g + fracB * b.g,
                                       fracA * a.b + fracB * b.b,
                                       fracA * a.lightintensity + fracB * b.lightintensity);
                ouvi++;
            }
            // swap initial vertices array with resulting array:
            invi = ouvi - 1;
            ouvi = 0;
            temp = inv;
            inv = ouv;
            ouv = temp;
        
            for (i = 0; i <= invi; i++) // bottom
            {
                a = inv[i];
                b = inv[(i + 1) % (invi + 1)];

                aOK = a.y >= -a.w;
                bOK = b.y >= -b.w;

                if (aOK)        { ouv[ouvi] = a; ouvi++; }
                if (aOK == bOK) { continue; }

                fracB = (a.y + a.w) / ((a.w + a.y) - (b.w + b.y));
                fracA = 1f - fracB;

                ouv[ouvi] = new Vertex(fracA * a.x + fracB * b.x,
                                       fracA * a.y + fracB * b.y,
                                       fracA * a.z + fracB * b.z,
                                       fracA * a.w + fracB * b.w,

                                       fracA * a.u + fracB * b.u,
                                       fracA * a.v + fracB * b.v,
                                       fracA * a.r + fracB * b.r,
                                       fracA * a.g + fracB * b.g,
                                       fracA * a.b + fracB * b.b,
                                       fracA * a.lightintensity + fracB * b.lightintensity);
                ouvi++;
            }
            // swap initial vertices array with resulting array:
            invi = ouvi - 1;
            ouvi = 0;
            temp = inv;
            inv = ouv;
            ouv = temp;
        
            for (i = 0; i <= invi; i++) // left
            {
                a = inv[i];
                b = inv[(i + 1) % (invi + 1)];

                aOK = a.x >= -a.w;
                bOK = b.x >= -b.w;

                if (aOK)        { ouv[ouvi] = a; ouvi++; }
                if (aOK == bOK) { continue; }

                fracB = (a.x + a.w) / ((a.w + a.x) - (b.w + b.x));
                fracA = 1f - fracB;

                ouv[ouvi] = new Vertex(fracA * a.x + fracB * b.x,
                                       fracA * a.y + fracB * b.y,
                                       fracA * a.z + fracB * b.z,
                                       fracA * a.w + fracB * b.w,

                                       fracA * a.u + fracB * b.u,
                                       fracA * a.v + fracB * b.v,
                                       fracA * a.r + fracB * b.r,
                                       fracA * a.g + fracB * b.g,
                                       fracA * a.b + fracB * b.b,
                                       fracA * a.lightintensity + fracB * b.lightintensity);
                ouvi++;
            }
            // swap initial vertices array with resulting array:
            invi = ouvi - 1;
            ouvi = 0;
            temp = inv;
            inv = ouv;
            ouv = temp;
            
            for (i = 0; i <= invi; i++) // right
            {
                a = inv[i];
                b = inv[(i + 1) % (invi + 1)];

                aOK = a.x <= a.w;
                bOK = b.x <= b.w;

                if (aOK)        { ouv[ouvi] = a; ouvi++; }
                if (aOK == bOK) { continue; }

                fracB = (a.x - a.w) / ((b.w - b.x)-(a.w - a.x));
                fracA = 1f - fracB;

                ouv[ouvi] = new Vertex(fracA * a.x + fracB * b.x,
                                       fracA * a.y + fracB * b.y,
                                       fracA * a.z + fracB * b.z,
                                       fracA * a.w + fracB * b.w,

                                       fracA * a.u + fracB * b.u,
                                       fracA * a.v + fracB * b.v,
                                       fracA * a.r + fracB * b.r,
                                       fracA * a.g + fracB * b.g,
                                       fracA * a.b + fracB * b.b,
                                       fracA * a.lightintensity + fracB * b.lightintensity);
                ouvi++;
            }
        }

        static void SetFrustumPlanes(float aspect, float hfov, float vfov, float znear, float zfar) // calculates frustrum plane normals in camera space
        {
            float sh = (float)Math.Sin(hfov * aspect);
            float sv = (float)Math.Sin(vfov);
            float ch = (float)Math.Cos(hfov * aspect);
            float cv = (float)Math.Cos(vfov);

            leftn   = -new Vec3D( ch,  0f, sh);
            rightn  = -new Vec3D(-ch,  0f, sh);
            topn    = -new Vec3D( 0f,  cv, sv);
            bottomn = -new Vec3D( 0f, -cv, sv);

            /*
            right  = (float)Math.Tan(hfov * 0.5f); // ^ -z   *-----------*    ^ +y  ____
            left   = -right;                       // |       \    f    /     |    / t  |
            top    = (float)Math.Tan(vfov * 0.5f); // |        \l     r/      |   <n   f|
            bottom = -top;                         // |    +x   \  n  /       |    \_b__|
                                                   // O----->    *---*        O------> -z  */
        }

        static Vec3D leftn, rightn, topn, bottomn;

        static bool BoundingSphereInFrustum()
        {
            Vec3D MOCS = modelorigin * CAMERAMATRIX.Transponse(); // transform mesh origin to camera space

            return -(MOCS.z - meshR) >= zNear &&
                   -(MOCS.z + meshR) <= zFar  &&
                   -Vec3D.Dot(MOCS, leftn)   < meshR &&
                   -Vec3D.Dot(MOCS, rightn)  < meshR &&
                   -Vec3D.Dot(MOCS, topn)    < meshR &&
                   -Vec3D.Dot(MOCS, bottomn) < meshR;
            /*
            //near, far, left, right, bottom, top // ... culling with plane slopes
            return MOCS.z - meshR  <= -zNear &&
                   MOCS.z - meshR  >= -zFar  &&
                   MOCS.x + meshR  >= -(MOCS.z - meshR) * left   &&
                   MOCS.x - meshR  <= -(MOCS.z - meshR) * right;//  &&
                   //(MOCS.z - meshR) >= -(MOCS.y - meshR) * bottom;// &&
                   //MOCS.x - meshR <= (MOCS.z - meshR) * top;
             */
        }
    }
}
