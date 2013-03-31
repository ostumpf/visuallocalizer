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
using Microsoft.VisualStudio.OLE.Interop;

namespace VisualLocalizer.Library {

    /// <summary>
    /// Provides methods for handling files in VS.
    /// </summary>
    public class DocumentViewsManager {

        private static ServiceProvider serviceProvider;

        static DocumentViewsManager() {
            DTE2 dte2 = (DTE2)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE));
            if (dte2 == null) throw new InvalidOperationException("Cannot consume SDTE.");

            Microsoft.VisualStudio.OLE.Interop.IServiceProvider sp = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte2;
            serviceProvider = new ServiceProvider(sp);

            if (serviceProvider == null) throw new InvalidOperationException("Cannot create ServiceProvider.");
        }

        /// <summary>
        /// Gets IVsTextView for a file
        /// </summary>
        /// <param name="file">File path</param>
        /// <param name="forceOpen">Whether the file should be opened, if it's closed</param>
        /// <param name="activate">Whether the window frame should be activated (focused)</param>        
        public static IVsTextView GetTextViewForFile(string file, bool forceOpen, bool activate) {
            if (string.IsNullOrEmpty(file)) throw new ArgumentNullException("file");

            IVsWindowFrame frame = GetWindowFrameForFile(file, forceOpen);
            
            if (frame != null) {
                if (forceOpen || activate) frame.Show();
                if (activate) {
                    VsShellUtilities.GetWindowObject(frame).Activate();
                }
                return VsShellUtilities.GetTextView(frame);
            } else throw new Exception("Cannot get window frame for " + file); 
        }

        /// <summary>
        /// Gets IVsWindowFrame for a file
        /// </summary>
        /// <param name="file">File path</param>
        /// <param name="forceOpen">Whether the file should be opened, if it's closed</param>        
        public static IVsWindowFrame GetWindowFrameForFile(string file, bool forceOpen) {
            if (string.IsNullOrEmpty(file)) throw new ArgumentNullException("file");

            IVsUIHierarchy uiHierarchy;
            uint itemID;
            IVsWindowFrame windowFrame;

            if (VsShellUtilities.IsDocumentOpen(serviceProvider, file, Guid.Empty, out uiHierarchy, out itemID, out windowFrame)) {
                return windowFrame;
            } else if (forceOpen) {
                VsShellUtilities.OpenDocument(serviceProvider, file);
                if (VsShellUtilities.IsDocumentOpen(serviceProvider, file, Guid.Empty, out uiHierarchy, out itemID, out windowFrame)) {
                    return windowFrame;
                } else throw new InvalidOperationException("Cannot force open file " + file);
            } else return null;           
        }

        /// <summary>
        /// Gets IVsTextLines for a file
        /// </summary>
        /// <param name="file">File path</param>
        /// <param name="forceOpen">Whether the file should be opened, if it's closed</param>  
        public static IVsTextLines GetTextLinesForFile(string file, bool forceOpen) {
            if (string.IsNullOrEmpty(file)) throw new ArgumentNullException("file");

            IVsWindowFrame frame = GetWindowFrameForFile(file, forceOpen);
            if (frame != null) {
                IVsTextLines lines = null;

                object docData;
                int hr = frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out docData);
                Marshal.ThrowExceptionForHR(hr);

                lines = docData as IVsTextLines;
                if (lines == null) {
                    var bufferProvider = docData as IVsTextBufferProvider;

                    if (bufferProvider != null) {
                        hr = bufferProvider.GetTextBuffer(out lines);
                        Marshal.ThrowExceptionForHR(hr);
                    }
                }
                if (lines == null) {
                    IVsTextView view = VsShellUtilities.GetTextView(frame);
                    if (view != null) {
                        hr = view.GetBuffer(out lines);
                        Marshal.ThrowExceptionForHR(hr);
                    }
                }
                if (lines == null) throw new Exception("Cannot get IVsTextLines for " + file);

                return lines;
            } else throw new Exception("Cannot get IVsWindowFrame for " + file);
        }

        

    }
}
