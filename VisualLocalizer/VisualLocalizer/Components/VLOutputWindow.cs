using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {
    internal sealed class VLOutputWindow : OutputWindow {        

        public static OutputWindowPane VisualLocalizerPane {
            get {
                if (!cache.ContainsKey(typeof(Guids.VisualLocalizerWindowPane).GUID))
                    cache.Add(typeof(Guids.VisualLocalizerWindowPane).GUID, getStandardPane(typeof(Guids.VisualLocalizerWindowPane).GUID));

                return cache[typeof(Guids.VisualLocalizerWindowPane).GUID];
            }
        }
    }
}
