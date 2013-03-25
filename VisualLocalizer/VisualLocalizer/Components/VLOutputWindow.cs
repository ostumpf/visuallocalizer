using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {

    /// <summary>
    /// Adds "Visual Localizer" output window pane.
    /// </summary>
    internal sealed class VLOutputWindow : OutputWindow {

        static VLOutputWindow() {
            cache.Add(typeof(Guids.VisualLocalizerWindowPane).GUID, GetStandardPane(typeof(Guids.VisualLocalizerWindowPane).GUID));            
        }

        public static OutputWindowPane VisualLocalizerPane {
            get {
                return GetPaneOrBlackHole(typeof(Guids.VisualLocalizerWindowPane).GUID);                
            }
        }
    }
}
