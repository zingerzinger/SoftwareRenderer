using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DirectX.DirectSound;
using Microsoft.DirectX.AudioVideoPlayback;
using System.Media;

namespace SoftwareRenderer
{
    static class Sound
    {
        static Device sp;

        static SecondaryBuffer respawn;
        static SecondaryBuffer jump;
        static SecondaryBuffer hitground;
        static SecondaryBuffer walk;

        static int ambientIndex;
        static Audio[] ambients = new Audio[0];
        static Audio ambient;

        static Random rand = new Random();

        public static void Init(System.Windows.Forms.Control owner)
        {
            sp = new Device();
            sp.SetCooperativeLevel(owner, CooperativeLevel.Priority);

            WaveFormat wf = new WaveFormat();
            BufferDescription bufDesc = new BufferDescription(wf);
            respawn   = new SecondaryBuffer(Properties.Resources.respawn,   bufDesc, sp);
            jump      = new SecondaryBuffer(Properties.Resources.jump,      bufDesc, sp);
            hitground = new SecondaryBuffer(Properties.Resources.hitground, bufDesc, sp);
            walk      = new SecondaryBuffer(Properties.Resources.walk,      bufDesc, sp);

            string path = "amb";
            int index = 0;
            List<Audio> temp = new List<Audio>();

            while (File.Exists(path + index.ToString() + ".mp3") && index < 10) { temp.Add(new Audio(path + index.ToString() + ".mp3")); index++; }
            if (temp.Count == 0)
            {
                File.WriteAllBytes("default.mp3", Properties.Resources.ambient);
                temp.Add(new Audio("default.mp3"));
                if (File.Exists("default.mp3")) { File.Delete("default.mp3"); }
            }

            ambients = temp.ToArray();
            foreach (Audio au in ambients) { au.Ending += ambient_Ending; }
            ambient_Ending(null, null);
        }

        static void ambient_Ending(object sender, EventArgs e) 
        {
            int tries = 0, index;
            do
            {
                index = rand.Next(ambients.Length);
                tries++;
            } while (index == ambientIndex && tries < 5);
            ambientIndex = index;

            ambient = ambients[ambientIndex];
            ambient.Volume = -1000;
            ambient.CurrentPosition = 0d;
            ambient.Play(); 
        }
        public static void Respawn()   { respawn.Play  (0, BufferPlayFlags.Default); }
        public static void Jump()      { jump.Play     (0, BufferPlayFlags.Default); }
        public static void HitGround() { hitground.Play(0, BufferPlayFlags.Default); }
        public static void Walk()      { walk.Play     (0, BufferPlayFlags.Default); }
    }
}
