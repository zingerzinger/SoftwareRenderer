using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace zModelViewer
{
    static class Program
    {
        static void Main()
        {
            System.IO.File.WriteAllText("zModelViewer.exe.config", "<?xml version=\"1.0\" encoding=\"utf-8\"?><configuration><startup useLegacyV2RuntimeActivationPolicy=\"true\"><supportedRuntime version=\"v4.0\" sku=\".NETFramework,Version=v4.5\"/></startup></configuration>");
            mainForm frm = new mainForm();
            frm.Show();
            frm.MainLoop();
        }

        class mainForm : Form
        {
            static bool running = true;
            static Stopwatch timer = new Stopwatch();

            System.Timers.Timer statsTimer = new System.Timers.Timer();

            static Graphics Target;

            public mainForm()
            {
                AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
                AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
                BackColor = System.Drawing.Color.Black;
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                Icon = Properties.Resources.icon;
                StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
                Text = "zModelViewer";
                //cur
                //TransparencyKey = Color.Black;

                KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
                KeyUp += mainForm_KeyUp;
                MouseDown += new System.Windows.Forms.MouseEventHandler(this.Form1_MouseDown);
                GotFocus += mainForm_GotFocus;
                LostFocus += mainForm_LostFocus;

                SetStyle(ControlStyles.DoubleBuffer, false);
                SetStyle(ControlStyles.UserPaint, true);
                SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                SetStyle(ControlStyles.Opaque, true);


                if (System.IO.File.Exists("config.txt")) { config = System.IO.File.ReadAllLines("config.txt"); }
                if (config.Length != 4)
                { config = new string[] { "model.obj", "texture.bmp", "768", "768" }; errormessage = "Wrong screen resolution in config file.- Setting default 768x768.\n"; }
                if (!int.TryParse(config[2], out clW) || !int.TryParse(config[3], out clH))
                { clW = clH = 768; errormessage = "Wrong screen resolution in config file.- Setting default 768x768.\n"; }
                if (clW < 256 || clW > 1920 || clH < 256 || clH > 1080)
                { clW = clH = 768; errormessage = "Wrong screen resolution in config file.- Setting default 768x768.\n"; }

                Width = clW; Height = clH;

                Target = CreateGraphics();

                Target.SmoothingMode      = SmoothingMode.None;
                Target.InterpolationMode  = InterpolationMode.NearestNeighbor;
                Target.CompositingQuality = CompositingQuality.HighSpeed;
                Target.PixelOffsetMode    = PixelOffsetMode.Half;
                Target.CompositingMode    = CompositingMode.SourceCopy;

                SetBMPinfoHeader(clW, clH);
                hRef = new HandleRef(Target, Target.GetHdc());
            }

            bool focus = true;
            void mainForm_GotFocus(object sender, EventArgs e) 
            {
                focus = true; 
                Cursor.Position = new Point(Left + clW / 2, Top + clH / 2); 
                mx = Left + clW / 2;
                my = Top + clH / 2;
            }
            void mainForm_LostFocus(object sender, EventArgs e) { focus = false; }

            int clW, clH; // 768
            string[] config = new string[0];
            string errormessage = string.Empty;

            int frameCount     = 0;
            int frameCountPrev = 0;

            public void MainLoop()
            {
                Bitmap backbuffer = new Bitmap(clW, clH, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                Bitmap texture;
                Model model, skymodel = null;
                Body[] collisionbodies = null;
                string loadererror = string.Empty;

                if (!System.IO.File.Exists("mdx.dll") || !System.IO.File.Exists("mdxsound.dll") || !System.IO.File.Exists("mdxavpb.dll")) { MessageBox.Show("Sound dll's not found!\nCrashing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk); return; }
                Sound.Init(this);

                if (!Loader.LoadModel(config[0], out model, out loadererror))     { errormessage += loadererror; }
                if (!Loader.LoadTexture(config[1], out texture, out loadererror)) { errormessage += loadererror; }
                if (errormessage.Length != 0) { MessageBox.Show(errormessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk); }

                if (System.IO.File.Exists("levelsky.obj"))  { Loader.LoadModel("levelsky.obj", out skymodel, out loadererror); }
                if (System.IO.File.Exists("collision.obj")) { collisionbodies = Loader.LoadCollisionModel("collision.obj"); }

                IntPtr framebufferPtr = Rendition.Init(ref backbuffer, model, skymodel, texture);
                texture.Dispose();

                Physics.Init(collisionbodies);

                float dt = 0f;
                mx = Cursor.Position.X;
                my = Cursor.Position.Y;
                Cursor.Hide();

                Console.WriteLine("Initialization OK");

                statsTimer.Elapsed += new System.Timers.ElapsedEventHandler(mainForm_StatsTimerElapsed);
                statsTimer.Interval = 1000;
                statsTimer.Start();

                while (running)
                {
                    Application.DoEvents();
                    timer.Restart();

                    if (focus)
                    {
                        rollSpeed = 0f;
                        //if (kq) { rollSpeed -= 1f; }
                        //if (ke) { rollSpeed += 1f; }
                        Rendition.RotateCamera(Cursor.Position.X - mx, Cursor.Position.Y - my, rollSpeed);
                        Cursor.Position = new Point(Left + clW / 2, Top + clH / 2);
                        mx = Cursor.Position.X;
                        my = Cursor.Position.Y;
                    }

                    Physics.forwardSpeed = Physics.strafeSpeed = Physics.jumpSpeed = 0f;
                    if (shift && Physics.noclip)   { Speed = 5f; } else { Speed = 0.3f; }
                    if (kw)      { Physics.forwardSpeed += Speed; }
                    if (ks)      { Physics.forwardSpeed -= Speed; }
                    if (ka)      { Physics.strafeSpeed  -= Speed; }
                    if (kd)      { Physics.strafeSpeed  += Speed; }
                    if (kjump)   { Physics.jumpSpeed     = 4f; }
                    Physics.crouch = kcrouch;

                    Rendition.Render(dt, Physics.Step(dt));
                    
                    SetDIBitsToDevice(hRef, 0, 0, clW, clH, 0, 0, 0, clH, framebufferPtr, ref bi, 0); // nice!
                    if (timer.ElapsedMilliseconds < 30) { Thread.Sleep((int)(30 - timer.ElapsedMilliseconds)); }
                    dt = (float)timer.Elapsed.TotalSeconds;

                    frameCount++;
                }
            }

            int mx, my;
            bool kw, ka, ks, kd, kq, ke, shift, kjump, kcrouch;
            float Speed, rollSpeed;

            void mainForm_StatsTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
            {
                Console.WriteLine("FPS {0}", frameCount - frameCountPrev);
                frameCountPrev = frameCount;
            }

            void mainForm_KeyUp(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.W)          { kw = false; }
                if (e.KeyCode == Keys.A)          { ka = false; }
                if (e.KeyCode == Keys.S)          { ks = false; }
                if (e.KeyCode == Keys.D)          { kd = false; }
                if (e.KeyCode == Keys.Space)      { kjump = false; }
                if (e.KeyCode == Keys.ControlKey) { kcrouch = false; }
                if (e.KeyCode == Keys.ShiftKey)   { shift = false; }
                if (e.KeyCode == Keys.Q)          { kq = false; }
                if (e.KeyCode == Keys.E)          { ke = false; }
            }

            private void Form1_KeyDown(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.W)          { kw = true; }
                if (e.KeyCode == Keys.A)          { ka = true; }
                if (e.KeyCode == Keys.S)          { ks = true; }
                if (e.KeyCode == Keys.D)          { kd = true; }
                if (e.KeyCode == Keys.Space)      { kjump = true; }
                if (e.KeyCode == Keys.ControlKey) { kcrouch = true; }
                if (e.KeyCode == Keys.ShiftKey)   { shift = true; }
                if (e.KeyCode == Keys.Q)          { kq = true; }
                if (e.KeyCode == Keys.E)          { ke = true; }

                if ((int)e.KeyCode >= (int)Keys.D1 &&
                    (int)e.KeyCode <= (int)Keys.D4) { Rendition.ChangeRenderMode((int)e.KeyCode - (int)Keys.D1); return; } // 0-3

                switch (e.KeyCode)
                {
                    case Keys.Enter:
                        Rendition.DumpZBuffer();
                        break;

                    case Keys.Space: // change to jump
                        Rendition.modelrotating = !Rendition.modelrotating;
                        break;

                    case Keys.N:
                        Physics.noclip = !Physics.noclip;
                        break;

                    case Keys.R:
                        Physics.Respawn();
                        Rendition.ResetCameraOrientation();
                        break;

//                    case Keys.Back:
//                        Rendition.ChangeRenderMode(5); // test flag toggle
//                        break;

                        // TODO : hmm, more adequate way of switching
                    case Keys.D1: Rendition.ChangeRenderMode(0); break;
                    case Keys.D2: Rendition.ChangeRenderMode(1); break;
                    case Keys.D3: Rendition.ChangeRenderMode(2); break;
                    case Keys.D4: Rendition.ChangeRenderMode(3); break;
                    case Keys.D5: Rendition.ChangeRenderMode(4); break;
                    case Keys.D6: Rendition.ChangeRenderMode(5); break;

                    case Keys.Tab:
                        Rendition.ScreenShot();
                        break;

                    case Keys.Escape:
                        running = false;
                        break;
                }
            }

            const int WM_NCLBUTTONDOWN = 0xA1;
            const int HT_CAPTION = 0x2;

            [DllImportAttribute("user32.dll")]
            public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
            [DllImportAttribute("user32.dll")]
            public static extern bool ReleaseCapture();

            private void Form1_MouseDown(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                    Cursor.Position = new Point(Left + clW / 2, Top + clH / 2);
                    mx = Left + clW / 2;
                    my = Top + clH / 2;
                }
            }

            // fast bitmap copy to form:

            static BITMAPINFOHEADER biHeader = new BITMAPINFOHEADER();
            static BITMAPINFO bi = new BITMAPINFO();
            static HandleRef hRef;

            void SetBMPinfoHeader(int width, int height)
            {
                biHeader.bihBitCount = 32; // bits per pixel
				biHeader.bihPlanes = 1; // no idea
				biHeader.bihSize = 40; // bitmapinfoheader struct size in bytes
				biHeader.bihWidth = width;
				biHeader.bihHeight = -height; // why negated? ... ok
                biHeader.bihSizeImage = (width * height) * 4; // buffer size in bytes (4 bytes per pixel)

                bi.biHeader = biHeader;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct BITMAPINFOHEADER
            {
                public int bihSize;
                public int bihWidth;
                public int bihHeight;
                public short bihPlanes;
                public short bihBitCount;
                public int bihCompression;
                public int bihSizeImage;
                public double bihXPelsPerMeter;
                public double bihClrUsed;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct BITMAPINFO
            {
                public BITMAPINFOHEADER biHeader;
                public int biColors;
            }

            [DllImport("gdi32")]
            extern static int SetDIBitsToDevice(HandleRef hDC, int xDest, int yDest, int dwWidth, int dwHeight, int XSrc, int YSrc, int uStartScan, int cScanLines, IntPtr lpvBits, ref BITMAPINFO lpbmi, uint fuColorUse);
        }
    }
}
