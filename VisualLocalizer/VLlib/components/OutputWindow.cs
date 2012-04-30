﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;

namespace VisualLocalizer.Library {
    public class OutputWindow {

        protected static IVsOutputWindow outputWindowService;
        protected static Dictionary<Guid, OutputWindowPane> cache;

        public OutputWindow() { }

        static OutputWindow() {            
            cache = new Dictionary<Guid, OutputWindowPane>();
            outputWindowService = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outputWindowService == null) 
                throw new Exception("Cannot consume SVsOutputWindow service.");            
        }

        protected static OutputWindowPane getStandardPane(Guid paneGuid) {
            IVsOutputWindowPane pane = null;
            int hr=outputWindowService.GetPane(ref paneGuid, out pane);
            if (hr != VSConstants.S_OK || pane==null)                
                throw new Exception(String.Format("Error retrieving standard output window pane {0}.",paneGuid.ToString("B")));

            OutputWindowPane owpane = new OutputWindowPane(pane);

            return owpane;
        }

        public static OutputWindowPane Build {
            get {
                if (!cache.ContainsKey(VSConstants.GUID_BuildOutputWindowPane))
                    cache.Add(VSConstants.GUID_BuildOutputWindowPane, getStandardPane(VSConstants.GUID_BuildOutputWindowPane));

                return cache[VSConstants.GUID_BuildOutputWindowPane];
            }
        }

        public static OutputWindowPane General {
            get {
                if (!cache.ContainsKey(VSConstants.GUID_OutWindowGeneralPane))
                    cache.Add(VSConstants.GUID_OutWindowGeneralPane, getStandardPane(VSConstants.GUID_OutWindowGeneralPane));

                return cache[VSConstants.GUID_OutWindowGeneralPane];
            }
        }

        public static OutputWindowPane Debug {
            get {
                if (!cache.ContainsKey(VSConstants.GUID_OutWindowDebugPane))
                    cache.Add(VSConstants.GUID_OutWindowDebugPane, getStandardPane(VSConstants.GUID_OutWindowDebugPane));

                return cache[VSConstants.GUID_OutWindowDebugPane];
            }
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
            if (cache.ContainsKey(paneGuid))
                cache.Remove(paneGuid);

            int hr = outputWindowService.DeletePane(ref paneGuid);
            
            if (hr != VSConstants.S_OK)
                throw new Exception(String.Format("Error deleting output window pane {0}.", paneGuid.ToString("B")));
        }


        public static OutputWindowPane GetPane(string paneGuid) {
            return GetPane(new Guid(paneGuid));
        }        

        public static OutputWindowPane GetPane(Guid paneGuid) {
            if (cache.ContainsKey(paneGuid)) {
                return cache[paneGuid];
            } else {
                IVsOutputWindowPane pane = null;
                int hr = outputWindowService.GetPane(ref paneGuid, out pane);

                if (hr != VSConstants.S_OK || pane == null)
                    throw new Exception(String.Format("Error retrieving output window pane {0}.", paneGuid.ToString("B")));

                OutputWindowPane owpane = new OutputWindowPane(pane);
                cache.Add(paneGuid, owpane);

                return owpane;
            }
        }       

      
    }
}
