using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace zModelViewer
{
    static class Loader // loads .bmp textures and the .obj model files
    {
        public static bool LoadTexture(string path, out Bitmap texture, out string errormessage)
        {
            bool ok = true; errormessage = string.Empty;

            try { texture = (Bitmap)Bitmap.FromFile(path); }
            catch
            {
                ok = false;
                errormessage = "File \"" + path + "\" not found.- Using default texture.\n";
                texture = Properties.Resources.defaulttexture;
            }

            if (texture.PixelFormat != System.Drawing.Imaging.PixelFormat.Format24bppRgb)
            {
                Bitmap temp = new Bitmap(texture.Width, texture.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                Graphics g = Graphics.FromImage(temp);
                g.DrawImageUnscaled(texture, 0, 0);
                texture = temp;
                g.Dispose();
            }

            if (ok) { Console.WriteLine("Loader: loaded texture {0} OK", path); }
            else    { Console.WriteLine("Loader: loaded texture {0} FAIL, error: {1}", path, errormessage); }

            return ok;
        }

        public static bool LoadModel(string path, out Model model, out string errormessage)
        {
            bool ok = true; errormessage = string.Empty;

            try 
            {
                s = System.IO.File.ReadAllText(path, Encoding.ASCII);
                s = s.Replace(" \r", "\n");
            }
            catch
            {
                ok = false;
                errormessage += "File \"" + path + "\" not found.- Using default model.\n";
                s = Properties.Resources.defaultmodel;
            }

            Parse();

            // normalize vertices to -1f...1f form:

            float maxlen = -1f, len = 0f;
            for (int i = 0; i < vertices.Count; i++)
            {
                len = vertices[i].LengthSq();
                if (len > maxlen) { maxlen = len; }
            }

            maxlen = len == 0f ? 1f : len;
            len = 1f / (float)Math.Sqrt(maxlen);

            Vec2D v2;
            for (int i = 0; i < tcoords.Count; i++)
            {
                v2 = tcoords[i];
                v2.x = v2.x > 1f ? 1f : v2.x;
                v2.y = v2.y > 1f ? 1f : v2.y;
                tcoords[i] = v2;
            }

            GenerateNormals();

            model = new Model();
            model.vertices = vertices.ToArray();
            model.tcoords = tcoords.Count == 0 ? (new Vec2D[] { new Vec2D() }) : tcoords.ToArray();
            model.normals = normals.Count == 0 ? (new Vec3D[] { new Vec3D() }) : normals.ToArray();
            model.polys = triangles.ToArray();
            model.R = (float)Math.Sqrt(maxlen);// * len; // equals 1f

            if (ok) {
                Console.WriteLine("Loader: loaded model {0} OK", path);
                Console.WriteLine("{0} vertices\n{1} tex coordinates\n{2} normals\n{3} polys",
                    model.vertices.Length,
                    model.tcoords .Length,
                    model.normals .Length,
                    model.polys   .Length
                    );
            } else {
                Console.WriteLine("Loader: loaded model {0} FAIL, error: {1}", path, errormessage);
            }

            return ok;
        }

        static List<Vec3D>    vertices;
        static List<Vec2D>    tcoords;
        static List<Vec3D>    normals;
        static List<Triangle> triangles;

        static string s = null;

        static void Parse()
        {
            vertices  = new List<Vec3D>();
            tcoords   = new List<Vec2D>();
            normals   = new List<Vec3D>();
            triangles = new List<Triangle>();

            string[] data = s.Split(new char[] { '\n' });
            string[] buf; char[] delimiter = new char[] { ' ' };

            foreach (string str in data)
            {
                buf = str.Split(delimiter);

                switch (buf[0])
                {
                    case "v":
                        GetVertex(buf);
                        break;
                    case "vt":
                        GetTextureCoordinates(buf);
                        break;
                    case "vn":
                        GetVertexNormal(buf);
                        break;
                    case "f":
                        GetFace(buf);
                        break;
                }
            }
        }

        static void GetVertex(string[] buf)
        {
            Vec3D v = new Vec3D(Convert.ToSingle(buf[1]), Convert.ToSingle(buf[2]), Convert.ToSingle(buf[3]));
            vertices.Add(v);
        }

        static void GetTextureCoordinates(string[] buf)
        {
            Vec2D t = new Vec2D((float)Math.Abs(Convert.ToSingle(buf[1])), (float)Math.Abs(Convert.ToSingle(buf[2])));
            tcoords.Add(t);
        }

        static void GetVertexNormal(string[] buf)
        {
            Vec3D n = new Vec3D(Convert.ToSingle(buf[1]), Convert.ToSingle(buf[2]), Convert.ToSingle(buf[3]));
            normals.Add(n);
        }

        static char[] facedelim = new char[] { '/' };
        static void GetFace(string[] buf)
        {
            Triangle tri = new Triangle();
            string[] fv1 = buf[1].Split(facedelim);
            string[] fv2 = buf[2].Split(facedelim);
            string[] fv3 = buf[3].Split(facedelim);

            if (fv1.Length == 1)
            {
                tri.v1i = Convert.ToInt32(fv1[0]) - 1;
                tri.v2i = Convert.ToInt32(fv2[0]) - 1;
                tri.v3i = Convert.ToInt32(fv3[0]) - 1;
            }
            else if (fv1.Length == 2)
            {
                tri.v1i = Convert.ToInt32(fv1[0]) - 1;
                tri.t1i = Convert.ToInt32(fv1[1]) - 1;

                tri.v2i = Convert.ToInt32(fv2[0]) - 1;
                tri.t2i = Convert.ToInt32(fv2[1]) - 1;

                tri.v3i = Convert.ToInt32(fv3[0]) - 1;
                tri.t3i = Convert.ToInt32(fv3[1]) - 1;
            }
            else if (fv1.Length == 3)
            {
                tri.v1i = Convert.ToInt32(fv1[0]) - 1;
                tri.t1i = fv1[1].Length == 0 ? 0 : (Convert.ToInt32(fv1[1]) - 1);
                tri.n1i = Convert.ToInt32(fv1[2]) - 1;

                tri.v2i = Convert.ToInt32(fv2[0]) - 1;
                tri.t2i = fv2[1].Length == 0 ? 0 : (Convert.ToInt32(fv2[1]) - 1);
                tri.n2i = Convert.ToInt32(fv2[2]) - 1;

                tri.v3i = Convert.ToInt32(fv3[0]) - 1;
                tri.t3i = fv3[1].Length == 0 ? 0 : (Convert.ToInt32(fv3[1]) - 1);
                tri.n3i = Convert.ToInt32(fv3[2]) - 1;
            }

            triangles.Add(tri);

            if (buf.Length == 5)
            {
                string[] fv4 = buf[4].Split(facedelim);
                Triangle tri2 = new Triangle();
                int v4i = 0, t4i = 0, n4i = 0;

                switch (fv4.Length)
                {
                    case 1:
                        v4i = Convert.ToInt32(fv4[0]) - 1;
                        break;
                    case 2:
                        v4i = Convert.ToInt32(fv4[0]) - 1;
                        t4i = Convert.ToInt32(fv4[1]) - 1;
                        break;
                    case 3:
                        v4i = Convert.ToInt32(fv4[0]) - 1;
                        t4i = fv4[1].Length == 0 ? 0 : (Convert.ToInt32(fv4[1]) - 1);
                        n4i = Convert.ToInt32(fv4[2]) - 1;
                        break;
                }

                tri2.v1i = tri.v1i;
                tri2.t1i = tri.t1i;
                tri2.n1i = tri.n1i;

                tri2.v2i = tri.v3i;
                tri2.t2i = tri.t3i;
                tri2.n2i = tri.n3i;

                tri2.v3i = v4i;
                tri2.t3i = t4i;
                tri2.n3i = n4i;

                triangles.Add(tri2);
            }
        }

        static void GenerateNormals()
        {
            float[] belonging = new float[vertices.Count];
            normals.Clear();
            for (int i = 0; i < vertices.Count; i++) { normals.Add(new Vec3D()); }

            Vec3D trin, ta, tb;
            Triangle tri;
            for (int i = 0; i < triangles.Count; i++)
            {
                tri = triangles[i];
                ta = vertices[tri.v2i] - vertices[tri.v1i];
                tb = vertices[tri.v3i] - vertices[tri.v1i];
                trin = Vec3D.Cross(ta, tb).Unit();

                tri.n1i = tri.v1i;
                tri.n2i = tri.v2i;
                tri.n3i = tri.v3i;

                triangles[i] = tri;

                normals[tri.n1i] += trin;
                normals[tri.n2i] += trin;
                normals[tri.n3i] += trin;

                belonging[tri.n1i] += 1f;
                belonging[tri.n2i] += 1f;
                belonging[tri.n3i] += 1f;
            }

            for (int i = 0; i < normals.Count; i++) 
            {
                normals[i] *= 1f / belonging[i]; normals[i] = normals[i].Unit();
                normals[i] = new Vec3D(normals[i].x, normals[i].y, normals[i].z, 0f); // w = 0, normal should not be translated!
            }
        }

        // *** COLLISION MODEL LOADING ***

        static List<List<Face>> bodies = new List<List<Face>>();
        static List<List<Vec3D>> bverts = new List<List<Vec3D>>(); // list of vertices for each body
        static List<Face> faces = new List<Face>(); // current faces list containing vertex indices
        static List<Vec3D> verts = new List<Vec3D>(); // all the vertices
        static int lastvertindex = 0;
        const float ballR = 0.3f;
        static Body[] rbodies;

        public static Body[] LoadCollisionModel(string path) // we need o,v,f - objects, vertices and faces
        {
            s = System.IO.File.ReadAllText(path, Encoding.ASCII);
            s = s.Replace(" \r", "\n");
            s = s.Replace('.', ',');

            string[] data = s.Split(new char[] { '\n' });
            string[] buf; char[] delimiter = new char[] { ' ' };

            foreach (string str in data)
            {
                buf = str.Split(delimiter);

                switch (buf[0])
                {
                    case "o":
                        bverts.Add(new List<Vec3D>());

                        //List<Vec3D> cbv = new List<Vec3D>();
                        //for (int i = lastvertindex; i < verts.Count; i++)
                        //{
                            //cbv.Add(verts[i]);
                        //}
                        //if (cbv.Count != 0) { bverts.Add(cbv); }
                        //lastvertindex = verts.Count;

                        if (faces.Count != 0) // last created body
                        {
                            bodies[bodies.Count - 1] = faces;
                            faces = new List<Face>();
                        }
                        bodies.Add(new List<Face>());
                        break;
                    case "v":
                        verts.Add(CGetVertex(buf));
                        bverts[bverts.Count - 1].Add(verts[verts.Count - 1]);
                        break;
                    case "f":
                        faces.Add(CGetFace(buf));
                        break;
                }
            }

            if (faces.Count != 0) // last created body
            {
                bodies[bodies.Count - 1] = faces;
            }

            /*
            if (bverts.Count != 0) // last created body vertex list
            {
                List<Vec3D> cbv = new List<Vec3D>();
                for (int i = lastvertindex; i < verts.Count; i++)
                {
                    cbv.Add(verts[i]);
                }
                bverts.Add(cbv);
                lastvertindex = verts.Count;
            } */

            rbodies = new Body[bodies.Count];

            Edge[] bodyedges;
            Vec3D bodyorigin;
            float bodyR;

            for (int i = 0; i < rbodies.Length; i++)
            {
                bodyedges  = GetBodyEdges(bodies[i]);
                bodyorigin = GetBodyCenter(bverts[i]);
                bodyR      = GetBodySphereRadius(bverts[i], bodyorigin);
                for (int k = 0; k < bodies[i].Count; k++)
                {
                    bodies[i][k] = ExtrudeFace(bodies[i][k]);
                }

                rbodies[i] = new Body(bverts[i].ToArray(), bodyedges, bodies[i].ToArray(), bodyorigin, bodyR);
            }

            Console.WriteLine("Loader: loaded physical model : {0} bodies", rbodies.Length);

            return rbodies;
        }

        static Vec3D CGetVertex(string[] buf) { return new Vec3D(Convert.ToSingle(buf[1]), Convert.ToSingle(buf[2]), Convert.ToSingle(buf[3])); }
        static Face CGetFace(string[] buf) // gets vertex indices of a face
        {
            List<Vec3D> vertices = new List<Vec3D>();
            int index;
            for (int i = 1; i < buf.Length; i++) 
            {
                index = Convert.ToInt32(buf[i].Substring(0, buf[i].IndexOf('/'))) - 1;
                vertices.Add(verts[index]);
            }

            Vec3D n = Vec3D.Cross(vertices[2] - vertices[0], vertices[1] - vertices[0]).Unit();
            return new Face(n, 0f, vertices.ToArray());
        }
        static Face ExtrudeFace(Face f)
        {
            Vec3D v;
            for (int i = 0; i < f.vertices.Length; i++) 
            {
                v = f.vertices[i] - f.n * ballR;
                f.vertices[i] = v; 
            }
            f.d = -Vec3D.Dot(f.n, f.vertices[0]);
            return f;
        }
        static Vec3D GetBodyCenter(List<Vec3D> verts)
        {
            Vec3D center = Vec3D.Zero;
            foreach (Vec3D v in verts)
            {
                center += v;
            }
            center *= 1f / verts.Count;
            return center;
        }
        static float GetBodySphereRadius(List<Vec3D> verts, Vec3D center)
        {
            float R = -1f, len;
            foreach (Vec3D v in verts)
            {
                len = (v - center).LengthSq();
                if (len > R) { R = len; }
            }
            return (float)Math.Sqrt(R);
        }
        static Edge[] GetBodyEdges(List<Face> faces)
        {
            List<edge> result = new List<edge>();

            Vec3D a, b;
            bool ok;
            foreach (Face f in faces)
            {
                for (int i = 0; i < f.vertices.Length; i++)
                {
                    a = f.vertices[i];
                    b = f.vertices[(i + 1) % f.vertices.Length];
                    ok = true;

                    foreach (edge e in result)
                    {
                        if ((a == e.a && b == e.b) ||
                            (b == e.a && a == e.b)) { ok = false; break; }
                    }

                    if (ok) { result.Add(new edge(a, b)); }
                }
            }

            Edge[] finalresult = new Edge[result.Count];
            float len;
            Vec3D v;

            for (int i = 0; i < result.Count; i++)
            {
                v = result[i].b - result[i].a;
                len = v.Length();
                v *= 1f / len;
                finalresult[i] = new Edge(result[i].a, v, len);
            }

            return finalresult;
        }

        struct edge
        {
            public Vec3D a, b;
            public edge(Vec3D a, Vec3D b) { this.a = a; this.b = b; }
        }
    }
}
