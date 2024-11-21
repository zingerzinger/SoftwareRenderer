using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zModelViewer
{
    // raytraces a character, represented by bounding sphere against the scene geometry
    static class Physics
    {
        public static void Init(Body[] collisionbodies)
        {
            if (collisionbodies == null) { Physics.bodies = new Body[0]; }
            else                         { Physics.bodies = collisionbodies; }
            Respawn();
        }

        static Body [] bodies;

        public static bool  noclip = false;
        public static float ballR = 0.3f;
        public static float forwardSpeed, strafeSpeed, jumpSpeed;

        static Vec3D position  = Vec3D.Zero;
        static Vec3D velocity  = Vec3D.Zero;
        static Vec3D cvelocity = Vec3D.Zero; // inframe velocity
        static Vec3D direction = new Vec3D(0f, 0f, -1f), strafedirection = new Vec3D(1f, 0f, 0f);
        public static Vec3D respawn = new Vec3D(-5f, 0.8f, -0.8f);
        static Vec3D gravity = new Vec3D(0f, -0.6f, 0f); // Vec3D.Zero;

        public static void Respawn() { Sound.Respawn(); velocity = Vec3D.Zero; position = respawn; }

        const float BOUND = 100f;
        static bool CheckBounds()
        {
            return position.x < -BOUND || position.y < -BOUND || position.z < -BOUND ||
                   position.x >  BOUND || position.y >  BOUND || position.z >  BOUND;
        }

        static float raylen   = 0f;
        static Vec3D raydir   = Vec3D.Zero;
        static Vec3D raystart = Vec3D.Zero;
        static Vec3D rayend   = Vec3D.Zero;

        static bool  intersects    = false;
        static float cdistance     = 0f; // for distance comparison
        static float distance      = float.MaxValue; // distance to contact point
        static Vec3D cContact      = Vec3D.Zero; // for current contact point
        static Vec3D contact       = Vec3D.Zero; // contact point
        static Vec3D cN            = Vec3D.Zero; // for current normal
        static Vec3D contactNormal = Vec3D.Zero; // contact normal
        static Vec3D reflection    = Vec3D.Zero; // velocity reflection in case of contact

        public static bool prevGround = false;
        public static bool onGround = false;
        public static bool crouch = false;

        static int solverIterations = 0;

        static float vertvelocity = 0f;
        static Vec3D cdirection = Vec3D.Zero;
        public static Vec3D Step(float dt) // returns final sphere position
        {
            if (noclip) 
            {
                cvelocity = (direction * Rendition.CAMERAMATRIX) * (forwardSpeed) + (strafedirection * Rendition.CAMERAMATRIX) * (strafeSpeed);
                position += cvelocity * dt;
                if (CheckBounds()) { Respawn(); Rendition.ResetCameraOrientation(); }
                velocity = Vec3D.Zero;
                return position; 
            }

            cdirection.x = (float)Math.Sin(Rendition.camYaw); // forward vector hack, because forward direction is influenced by vertical view direction
            cdirection.z = (float)Math.Cos(Rendition.camYaw);
            cdirection.y = 0f;
            
            if (crouch) { forwardSpeed = strafeSpeed = velocity.x = velocity.z = 0f; }

            if (prevGround) 
            { 
                cvelocity = (cdirection * -forwardSpeed) + (strafedirection * Rendition.CAMERAMATRIX) * (strafeSpeed);

                if (forwardSpeed != 0f || strafeSpeed != 0f)
                {
                    Sound.Walk();
                }
                
                if (jumpSpeed != 0f)
                {
                    Sound.Jump();
                    cvelocity.y = jumpSpeed;
                }
                onGround = false;
            }
            else
            { cvelocity.y = 0f; }
            
            velocity += (cvelocity + gravity) * dt;
            vertvelocity = velocity.y;

            if (CheckBounds()) { Respawn(); Rendition.ResetCameraOrientation(); return position; }
            
            raystart = position;
            rayend   = position + velocity;
            solverIterations = 0;

            do
            {
                intersects = false;
                cdistance  = 0f;
                distance   = float.MaxValue;

                raydir = rayend - raystart;
                raylen = raydir.Length();
                raydir = raydir * (1f / raylen);

                foreach (Body body in bodies)
                {
                    if (!SegmentVsBoundingSphere(body.origin, body.R + ballR)) { continue; }
                    
                    foreach(Face f in body.faces)
                    {
                        if (SegmentVsFace(f) && cdistance < distance)
                        {
                            distance      = cdistance;
                            contact       = cContact;
                            contactNormal = cN;
                            intersects    = true;
                        }
                    }

                    foreach (Edge e in body.edges)
                    {
                        if (SegmentVsEdge(e) && cdistance < distance)
                        {
                            distance      = cdistance;
                            contact       = cContact;
                            contactNormal = cN;
                            intersects    = true;
                        }
                    }

                    foreach (Vec3D v in body.vertices)
                    {
                        if (SegmentVsVertex(v) && cdistance < distance)
                        {
                            distance      = cdistance;
                            contact       = cContact;
                            contactNormal = cN;
                            intersects    = true;
                        }
                    }
                }

                if (intersects)
                {
                    reflection = Reflect(raystart - rayend, contactNormal) * 0.5f;

                    // slide along face:
                    rayend = contact + reflection * ((raylen - distance) / raylen); // maybe just stop?
                    rayend -= (Vec3D.Dot(contactNormal, rayend - contact) * contactNormal) * 0.999f;

                    if (contactNormal.y < -0.4f)
                    { 
                        velocity *= 0.9f;
                        velocity.y = 0f;
                        onGround = true;
                    }
                    else
                    {
                        velocity = Reflect(-velocity, contactNormal) * 0.9f;
                    }
                }
                else
                {
                    position = rayend;
                    break;
                }
                solverIterations++;
            } while (intersects && solverIterations < 10);

            if (!prevGround && onGround && vertvelocity < -0.2f) { Sound.HitGround(); }
            prevGround = onGround;

            return position;
        }

        static bool SegmentVsBoundingSphere(Vec3D center, float R) // R is the sum of bounding sphere radius and ballR
        {
            float B, C; // raydir is normalized, A = 1f
            Vec3D rxrc = raystart - center;
            
            B = 2f * (raydir.x * rxrc.x + raydir.y * rxrc.y + raydir.z * rxrc.z);
            C = rxrc.x*rxrc.x + rxrc.y*rxrc.y + rxrc.z*rxrc.z - R*R;

            float D = B * B - 4f * C;

            if (D < 0f) { return false; }

            D = (float)Math.Sqrt(D);

            float t1 = (B - D) * -0.5f,
                  t2 = (B + D) * -0.5f;

            return (t1 >= 0f && t1 <= raylen) || (t2 >= 0f && t2 <= raylen) ||
                   (t1 <= 0f && t2 >= raylen) || (t1 >= raylen && t2 <= 0f);
        }

        static bool SegmentVsVertex(Vec3D v) // vs sphere
        {
            float B, C; // raydir is normalized, A = 1f
            Vec3D rxrc = raystart - v;

            B = 2f * (raydir.x * rxrc.x + raydir.y * rxrc.y + raydir.z * rxrc.z);
            C = rxrc.x * rxrc.x + rxrc.y * rxrc.y + rxrc.z * rxrc.z - ballR * ballR;

            float D = B * B - 4f * C;

            if (D < 0f) { return false; }

            D = (float)Math.Sqrt(D);

            float t1 = (B - D) * -0.5f,
                  t2 = (B + D) * -0.5f;

            if (t1 >= 0f && t1 <= raylen)
            {
                if (t2 >= 0f && t2 <= raylen)
                {
                    cdistance = t1 < t2 ? t1 : t2;
                    cContact = raystart + raydir * cdistance;
                    cN = (cContact - v) * (1f / ballR);
                    return true;
                }
                else
                {
                    cdistance = t1;
                    cContact = raystart + raydir * cdistance;
                    cN = (cContact - v) * (1f / ballR);
                    return true;
                }
            }
            else if (t2 >= 0f && t2 <= raylen)
            {
                cdistance = t2;
                cContact = raystart + raydir * cdistance;
                cN = (cContact - v) * (1f / ballR);
                return true;
            }

            return false;
        }

        static bool SegmentVsEdge(Edge e) // vs cylinder
        {
            float A, B, C; // multiuse
            Vec3D rxrc = raystart - e.p; // multiuse

            A = (raydir - Vec3D.Dot(raydir, e.v) * e.v).LengthSq(); 
            B = 2f * Vec3D.Dot(raydir - Vec3D.Dot(raydir, e.v) * e.v, rxrc - Vec3D.Dot(rxrc, e.v) * e.v);
            C = (rxrc - Vec3D.Dot(rxrc, e.v)*e.v).LengthSq() - ballR * ballR;

            float D = B * B - 4f * A * C;

            if (D < 0f) { return false; }

            D = (float)Math.Sqrt(D);

            float t1 = (-B + D) / (2f * A),
                  t2 = (-B - D) / (2f * A);

            rxrc = raystart + raydir * t1 - e.p;
            A = Vec3D.Dot(rxrc, e.v); // t1
            rxrc = raystart + raydir * t2 - e.p;
            B = Vec3D.Dot(rxrc, e.v); // t2

            // now we`re choosing between A and B - t1 and t2

            if (A >= 0f && A <= e.len) // t1 belongs to cylinder
            {
                if (B >= 0f && B <= e.len) // test t1 and t2 belonging to a ray
                {
                    if (t1 >= 0f && t1 <= raylen)
                    {
                        if (t2 >= 0f && t2 <= raylen)
                        {
                            if (t1 < t2) { cdistance = t1; } // chose the nearest parameter to ray origin and A becomes the distance from edge origin to the point on cylinder axis
                            else         { cdistance = t2; A = B; }

                            cContact = raystart + raydir * cdistance;
                            cN = (cContact - (e.p + e.v * A)) * (1f / ballR);
                            return true;
                        }
                        else
                        {
                            cdistance = t1;
                            cContact  = raystart + raydir * cdistance;
                            cN = (cContact - (e.p + e.v * A)) * (1f / ballR);
                            return true;
                        }
                    }
                    else if (t2 >= 0f && t2 <= raylen)
                    {
                        cdistance = t2;
                        cContact  = raystart + raydir * cdistance;
                        cN = (cContact - (e.p + e.v * B)) * (1f / ballR);
                        return true;
                    }

                    return false;
                }
                else // test t1 belonging to a ray
                {
                    if (t1 >= 0f && t1 <= raylen) // t1 belongs to a ray
                    {
                        cdistance = t1;
                        cContact  = raystart + raydir * cdistance;
                        cN = (cContact - (e.p + e.v * A)) * (1f / ballR);
                        return true;
                    }
                    else
                    {
                        return false;
                    }   
                }
            }
            else if (B >= 0f && B <= e.len) // t2 belongs to cylinder, test it`s belonging to a ray
            {
                if (t2 >= 0f && t2 <= raylen) // t2 belongs to a ray
                {
                    cdistance = t2;
                    cContact  = raystart + raydir * cdistance;
                    cN = (cContact - (e.p + e.v * B)) * (1f / ballR);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            
            return false;
        }

        static bool SegmentVsFace(Face f) // vs extruded face
        {
            float denom = Vec3D.Dot(raydir, f.n); // also used as parameter t

            if (denom < 0.00001f) { return false; } // it`s a backface or ray and face are coplanar

            denom = -(Vec3D.Dot(raystart, f.n) + f.d) / denom;

            if (denom < 0f || denom > raylen) { return false; }

            cContact = raystart + denom * raydir;

            Vec3D a, b, N, cc = cContact - raystart; // checking if the contact point belongs to face

            for (int i = 0; i < f.vertices.Length; i++)
            {
                a = f.vertices[i];
                b = f.vertices[(i+1) % f.vertices.Length];

                N = Vec3D.Cross(b - raystart, a - raystart);
                if (Vec3D.Dot(cc, N) < 0f) { return false; }
            }

            cN = f.n;
            cdistance = denom;
            return true;
        }

        static Vec3D Reflect(Vec3D v, Vec3D n) { return (2f * Vec3D.Dot(v, n) * n - v); }
    }

    class Body
    {
        public Vec3D origin;
        public float R;
        public Face [] faces;
        public Vec3D[] vertices;
        public Edge [] edges;
        public Body(Vec3D[] vertices, Edge[] edges, Face[] faces, Vec3D origin, float R)
        {
            this.origin   = origin;
            this.R        = R;
            this.faces    = faces;
            this.vertices = vertices;
            this.edges    = edges;
        }
    }

    struct Edge
    {
        public Vec3D p, v;
        public float len;
        public Edge(Vec3D p, Vec3D v, float length)
        {
            this.p = p;
            this.v = v;
            this.len = length;
        }
    }

    struct Face
    {
        public Vec3D n;
        public float d;
        public Vec3D[] vertices;

        public Face(Vec3D n, float d, Vec3D[] vertices)
        {
            this.n = n;
            this.d = d;
            this.vertices = vertices;
        }
    }
}
