using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;

namespace VisualLocalizer.Library {
    public static class OutputWindow {

        private static IVsOutputWindow outputWindowService;

        static OutputWindow() {
            outputWindowService=Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outputWindowService != null) {
                Build = getStandardPane(VSConstants.GUID_BuildOutputWindowPane);
                General = getStandardPane(VSConstants.GUID_OutWindowGeneralPane);
                Debug = getStandardPane(VSConstants.GUID_OutWindowDebugPane);
            } else throw new Exception("Cannot consume SVsOutputWindow service.");
        }

        private static IVsOutputWindowPane getStandardPane(Guid paneGuid) {
            IVsOutputWindowPane pane = null;
            int hr=outputWindowService.GetPane(ref paneGuid, out pane);
            if (hr != VSConstants.S_OK || pane==null)
                throw new Exception(String.Format("Error retrieving standard output window pane {0}.",paneGuid.ToString("B")));

            return pane;
        }

        public static IVsOutputWindowPane Build {
            get;
            private set;
        }

        public static IVsOutputWindowPane General {
            get;
            private set;
        }

        public static IVsOutputWindowPane Debug {
            get;
            private set;
        }

        public static void CreatePane(Guid paneGuid,string name,bool clearWithSolution,bool initiallyVisible) {
            int hr=outputWindowService.CreatePane(ref paneGuid, name, initiallyVisible ? 1 : 0, clearWithSolution ? 1 : 0);

            if (hr != VSConstants.S_OK)
                throw new Exception(String.Format("Error creating output window pane {0}.", paneGuid.ToString("B")));
        }


        public static void DeletePane(string paneGuid) {
            DeletePane(new Guid(paneGuid));
        }

        public static void DeletePane(Guid paneGuid) {
            int hr = outputWindowService.DeletePane(ref paneGuid);
            
            if (hr != VSConstants.S_OK)
                throw new Exception(String.Format("Error deleting output window pane {0}.", paneGuid.ToString("B")));
        }


        public static IVsOutputWindowPane GetPane(string paneGuid) {
            return GetPane(new Guid(paneGuid));
        }        

        public static IVsOutputWindowPane GetPane(Guid paneGuid) {
            IVsOutputWindowPane pane=null;
            int hr=outputWindowService.GetPane(ref paneGuid, out pane);
            
            if (hr != VSConstants.S_OK || pane==null)
                throw new Exception(String.Format("Error retrieving output window pane {0}.", paneGuid.ToString("B")));
                            
            return pane;
        }

        public static IVsOutputWindowPane GetActivatedPane(string paneGuid) {
            IVsOutputWindowPane pane = GetPane(new Guid(paneGuid));
            pane.Activate();
            return pane;
        }

        public static IVsOutputWindowPane GetActivatedPane(Guid paneGuid) {
            IVsOutputWindowPane pane = GetPane(paneGuid);
            pane.Activate();
            return pane;
        }

      
    }
}
