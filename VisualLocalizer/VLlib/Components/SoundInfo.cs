using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace VisualLocalizer.Library.Components {

    /// <summary>
    /// Static methods for working with audio wav files.
    /// </summary>
    public static class SoundInfo {
        [DllImport("winmm.dll")]
        private static extern uint mciSendString(
            string command,
            StringBuilder returnValue,
            int returnLength,
            IntPtr winHandle);

        /// <summary>
        /// Returns play time of given wav file in miliseconds.
        /// </summary>        
        public static int GetSoundLength(string fileName) {
            if (fileName == null) throw new ArgumentNullException("fileName");

            try {
                StringBuilder lengthBuf = new StringBuilder(32);
                mciSendString(string.Format("open \"{0}\" type waveaudio alias wave", fileName), null, 0, IntPtr.Zero);
                mciSendString("status wave length", lengthBuf, lengthBuf.Capacity, IntPtr.Zero);
                mciSendString("close wave", null, 0, IntPtr.Zero);

                int length = 0;
                int.TryParse(lengthBuf.ToString(), out length);

                return length;
            } catch (Exception) {
                return 0;
            }
        }
    }
}
