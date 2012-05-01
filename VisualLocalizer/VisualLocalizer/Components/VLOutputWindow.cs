using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using VisualLocalizer.Library;

namespace VisualLocalizer.Editor {
    internal sealed class VLOutputWindow : OutputWindow {

        static VLOutputWindow() {            
            cache.Add(typeof(Guids.VisualLocalizerWindowPane).GUID, getStandardPane(typeof(Guids.VisualLocalizerWindowPane).GUID));
        }

        public static OutputWindowPane VisualLocalizerPane {
            get {
                return GetPaneOrBlackHole(typeof(Guids.VisualLocalizerWindowPane).GUID);                
            }
        }
    }
}
