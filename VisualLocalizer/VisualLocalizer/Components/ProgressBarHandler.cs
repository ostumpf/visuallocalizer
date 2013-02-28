using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System.Timers;

namespace VisualLocalizer.Components {
    internal static class ProgressBarHandler {

        private static IVsStatusbar statusBar = null;
        private static uint statusBarCookie = 0, total;
        private static string statusBarText = "Translating...";
        private static object icon = (short)Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Find;
        private static bool determinateTimerHit;

        private static void checkInstance() {            
            if (statusBar == null) statusBar = Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar;
        }

        public static void StartIndeterminate() {
            checkInstance();            
            statusBar.Animation(1, ref icon);
        }

        public static void StopIndeterminate() {
            checkInstance();
            statusBar.Animation(0, ref icon);
        }

        public static void StartDeterminate(int totalAmount) {
            checkInstance();

            statusBarCookie = 0;
            total = (uint)totalAmount;
            statusBar.Progress(ref statusBarCookie, 1, statusBarText, 0, total);
        }

        public static void StopDeterminate() {
            checkInstance();

            statusBar.Progress(ref statusBarCookie, 1, "Translation finished", total, total);

            determinateTimerHit = false;
            Timer t = new Timer();
            t.Enabled = true;
            t.Interval = 500;
            t.Elapsed += new ElapsedEventHandler((o,e) => {
                if (determinateTimerHit) {
                    statusBar.Progress(ref statusBarCookie, 0, "Ready", total, total);
                    t.Stop();
                    t.Dispose();
                }
                determinateTimerHit = true;
            });
            t.Start();            
        }

        public static void SetDeterminateProgress(int completed) {
            checkInstance();

            statusBar.Progress(ref statusBarCookie, 1, statusBarText, (uint)completed, total);                    
        }
    }
}
