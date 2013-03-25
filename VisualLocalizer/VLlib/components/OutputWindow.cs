using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;

namespace VisualLocalizer.Library {

    /// <summary>
    /// Wrapper for standard VS IVsOutputWindow service.
    /// </summary>
    public class OutputWindow {

        protected static IVsOutputWindow outputWindowService;
        protected static Dictionary<Guid, OutputWindowPane> cache;
        protected static Guid blackHoleGuid = new Guid("35416BF3-EA5A-4749-B4F9-09CF305C16D5");

        protected OutputWindow() { }

        static OutputWindow() {            
            cache = new Dictionary<Guid, OutputWindowPane>();
            outputWindowService = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            
            if (outputWindowService == null) throw new Exception("Cannot consume SVsOutputWindow service.");

            cache.Add(blackHoleGuid, new OutputWindowPane(null));            

            // obtain standard VS output window panes (if they exist)

            OutputWindowPane buildPane = GetStandardPane(VSConstants.GUID_BuildOutputWindowPane);
            if (buildPane != null) cache.Add(VSConstants.GUID_BuildOutputWindowPane, buildPane);            

            OutputWindowPane generalPane = GetStandardPane(VSConstants.GUID_OutWindowGeneralPane);
            if (generalPane != null) cache.Add(VSConstants.GUID_OutWindowGeneralPane, generalPane);            

            OutputWindowPane debugPane = GetStandardPane(VSConstants.GUID_OutWindowDebugPane);
            if (debugPane != null) cache.Add(VSConstants.GUID_OutWindowDebugPane, debugPane);            
        }

        /// <summary>
        /// Gets output window pane with specified GUID wrapped in OutputWindowPane wrapper
        /// </summary>        
        protected static OutputWindowPane GetStandardPane(Guid paneGuid) {
            if (paneGuid == null) throw new ArgumentNullException("paneGuid");

            IVsOutputWindowPane pane = null;
            outputWindowService.GetPane(ref paneGuid, out pane);            

            if (pane == null) {
                return null;
            } else {
                OutputWindowPane owpane = new OutputWindowPane(pane);
                return owpane;
            }
        }

        /// <summary>
        /// Returns Build output window pane (or black hole, if Build doesn't exist) - never returns null
        /// </summary>
        public static OutputWindowPane Build {
            get {
                return GetPaneOrBlackHole(VSConstants.GUID_BuildOutputWindowPane);
            }
        }

        /// <summary>
        /// Returns General output window pane (or black hole, if General doesn't exist) - never returns null
        /// </summary>
        public static OutputWindowPane General {
            get {
                return GetPaneOrBlackHole(VSConstants.GUID_OutWindowGeneralPane);
            }
        }

        /// <summary>
        /// Returns Debug output window pane (or black hole, if Debug doesn't exist) - never returns null
        /// </summary>
        public static OutputWindowPane Debug {
            get {
                return GetPaneOrBlackHole(VSConstants.GUID_OutWindowDebugPane);                
            }
        }

        /// <summary>
        /// Gets output window pane from cache or black hole pane, if specified pane doesn't exist - never returns null
        /// </summary>        
        protected static OutputWindowPane GetPaneOrBlackHole(Guid paneGuid) {
            if (paneGuid == null) throw new ArgumentNullException("paneGuid");

            if (cache.ContainsKey(paneGuid))
                return cache[paneGuid];
            else
                return cache[blackHoleGuid];
        }

        /// <summary>
        /// Creates new window pane with specified GUID, name and other info
        /// </summary>        
        public static void CreatePane(Guid paneGuid, string name, bool clearWithSolution, bool initiallyVisible) {
            if (paneGuid == null) throw new ArgumentNullException("paneGuid");
            
            int hr = outputWindowService.CreatePane(ref paneGuid, name, initiallyVisible ? 1 : 0, clearWithSolution ? 1 : 0);
            Marshal.ThrowExceptionForHR(hr);
        }

        /// <summary>
        /// Deletes window pane with specified GUID
        /// </summary>        
        public static void DeletePane(string paneGuid) {
            if (paneGuid == null) throw new ArgumentNullException("paneGuid");

            DeletePane(new Guid(paneGuid));
        }

        /// <summary>
        /// Deletes window pane with specified GUID
        /// </summary>        
        public static void DeletePane(Guid paneGuid) {
            if (paneGuid == null) throw new ArgumentNullException("paneGuid");

            if (cache.ContainsKey(paneGuid)) cache.Remove(paneGuid);

            int hr = outputWindowService.DeletePane(ref paneGuid);
            Marshal.ThrowExceptionForHR(hr);
        }

        /// <summary>
        /// Returns wrapped window pane with specified GUID (and adds it to cache)
        /// </summary>        
        public static OutputWindowPane GetPane(string paneGuid) {
            if (paneGuid == null) throw new ArgumentNullException("paneGuid");

            return GetPane(new Guid(paneGuid));
        }

        /// <summary>
        /// Returns wrapped window pane with specified GUID (and adds it to cache)
        /// </summary>        
        public static OutputWindowPane GetPane(Guid paneGuid) {
            if (paneGuid == null) throw new ArgumentNullException("paneGuid");

            if (cache.ContainsKey(paneGuid)) {
                return cache[paneGuid];
            } else {
                IVsOutputWindowPane pane = null;
                int hr = outputWindowService.GetPane(ref paneGuid, out pane);
                Marshal.ThrowExceptionForHR(hr);

                OutputWindowPane owpane = new OutputWindowPane(pane);
                cache.Add(paneGuid, owpane);

                return owpane;
            }
        }       

      
    }
}
