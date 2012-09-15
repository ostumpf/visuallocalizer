using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;

namespace VisualLocalizer.Library {
    public static class DocumentViewsManager {

        private static DTE2 dte2;
        private static Microsoft.VisualStudio.OLE.Interop.IServiceProvider sp;
        private static ServiceProvider serviceProvider;

        static DocumentViewsManager() {
            dte2 = (DTE2)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE));
            sp = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte2;
            serviceProvider = new ServiceProvider(sp);
            
        }

        public static IVsTextView GetTextViewForFile(string file,bool forceOpen,bool activate) {
            IVsWindowFrame frame = GetWindowFrameForFile(file, forceOpen);
            if (forceOpen || activate) frame.Show();

            if (frame != null) {
                return VsShellUtilities.GetTextView(frame);
            }
            return null;
        }

        public static IVsWindowFrame GetWindowFrameForFile(string file, bool forceOpen) {
            IVsUIHierarchy uiHierarchy;
            uint itemID;
            IVsWindowFrame windowFrame;

            if (VsShellUtilities.IsDocumentOpen(serviceProvider, file, Guid.Empty, out uiHierarchy, out itemID, out windowFrame)) {
                return windowFrame;
            } else if (forceOpen) {
                VsShellUtilities.OpenDocument(serviceProvider, file);
                if (VsShellUtilities.IsDocumentOpen(serviceProvider, file, Guid.Empty, out uiHierarchy, out itemID, out windowFrame)) {
                    return windowFrame;
                }
            }
            return null;
        }

        public static IVsTextLines GetTextLinesForFile(string file, bool forceOpen) {           
            IVsWindowFrame frame = GetWindowFrameForFile(file, forceOpen);            
            if (frame != null) {
                IVsTextLines lines = null;
                
                object docData;
                int hr = frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out docData);
                Marshal.ThrowExceptionForHR(hr);
                
                var buffer = docData as VsTextBuffer;   
                
                if (buffer == null) {
                    var bufferProvider = docData as IVsTextBufferProvider;
                    
                    if (bufferProvider != null) {                        
                        hr = bufferProvider.GetTextBuffer(out lines); 
                        Marshal.ThrowExceptionForHR(hr);  
                    }
                } else {
                    lines = (IVsTextLines)buffer;               
                }

                return lines;
            }

            return null;
        }
    }
}
