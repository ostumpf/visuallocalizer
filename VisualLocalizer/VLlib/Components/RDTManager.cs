using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TextManager.Interop;
using System.IO;
using System.Threading;
using System.Diagnostics;
using EnvDTE;
using Microsoft.VisualStudio;

namespace VisualLocalizer.Library.Components {

    /// <summary>
    /// Running Documents Table manager - provides methods for getting status of documents opened in VS.
    /// </summary>
    public static class RDTManager {

        /// <summary>
        /// Instance of the IVsRunningDocumentTable service
        /// </summary>
        private static IVsRunningDocumentTable IVsRunningDocumentTable;

        /// <summary>
        /// Instance of the DTE object
        /// </summary>
        private static EnvDTE80.DTE2 DTE;
        
        /// <summary>
        /// Initialize the services
        /// </summary>
        static RDTManager() {
            IVsRunningDocumentTable = (IVsRunningDocumentTable)Package.GetGlobalService(typeof(SVsRunningDocumentTable));
            DTE = (EnvDTE80.DTE2)Package.GetGlobalService(typeof(EnvDTE.DTE));

            if (IVsRunningDocumentTable == null || DTE == null) throw new InvalidOperationException("Cannot obtain RDTManager services.");
        }

        /// <summary>
        /// Performs given operation with a file so that VS doesn't find out the document has been changed.
        /// </summary>        
        public static void SilentlyModifyFile(string path, Action<string> modify) {
            if (string.IsNullOrEmpty(path)) return;

            SetIgnoreFileChanges(path, true);

            modify(path);
            
            SilentlyReloadFile(path); 
            SetIgnoreFileChanges(path, false);            
        }

        /// <summary>
        /// Turns on/off file ignore option. When file changes are ignored, VS doesn't display a dialog asking user about reloading the changes.
        /// </summary>        
        public static void SetIgnoreFileChanges(string path, bool ignore) {
            if (string.IsNullOrEmpty(path)) return;

            IVsHierarchy ppHier;
            uint pitemid;
            IntPtr pPunkDocData;
            uint pdwCookie;
            int hr = IVsRunningDocumentTable.FindAndLockDocument((uint)_VSRDTFLAGS.RDT_NoLock, path,
                out ppHier, out pitemid, out pPunkDocData, out pdwCookie);
            Marshal.ThrowExceptionForHR(hr);

            if (pPunkDocData != IntPtr.Zero) {
                IVsFileChangeEx fileChange = (IVsFileChangeEx)Package.GetGlobalService(typeof(SVsFileChangeEx));
                if (fileChange == null) throw new InvalidOperationException("Cannot consume IVsFileChangeEx.");

                IVsDocDataFileChangeControl changeControl = (IVsDocDataFileChangeControl)Marshal.GetObjectForIUnknown(pPunkDocData);

                if (ignore) {
                    hr = fileChange.IgnoreFile(0, path, 1);
                    Marshal.ThrowExceptionForHR(hr);

                    hr = changeControl.IgnoreFileChanges(1);                    
                } else {
                    hr = fileChange.IgnoreFile(0, path, 0);
                    Marshal.ThrowExceptionForHR(hr);

                    hr = changeControl.IgnoreFileChanges(0);                    
                }
            }             
        }          

        /// <summary>
        /// Reloads file buffer without displaying a GUI dialog.
        /// </summary>        
        public static void SilentlyReloadFile(string path) {
            if (string.IsNullOrEmpty(path)) return;

            IVsHierarchy ppHier;
            uint pitemid;
            IntPtr pPunkDocData;
            uint pdwCookie;
            int hr = IVsRunningDocumentTable.FindAndLockDocument((uint)_VSRDTFLAGS.RDT_NoLock, path,
                out ppHier, out pitemid, out pPunkDocData, out pdwCookie);
            Marshal.ThrowExceptionForHR(hr);

            if (pPunkDocData != IntPtr.Zero) {
                IVsPersistDocData d = (IVsPersistDocData)Marshal.GetObjectForIUnknown(pPunkDocData);
                hr = d.ReloadDocData(0);
                Marshal.ThrowExceptionForHR(hr);            
            }
        }

        /// <summary>
        /// Returns false if VS registers the file as not saved (asterisk by the name of the file)
        /// </summary>        
        public static bool IsFileSaved(string path) {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");

            bool open = DTE.get_IsOpenFile(null, path);
            return !open || DTE.Documents.Item(path).Saved;
        }

        /// <summary>
        /// Returns true if file is opened in VS
        /// </summary>
        public static bool IsFileOpen(string path) {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
            
            return DTE.get_IsOpenFile(null, path);
        }

        /// <summary>
        /// Returns true if file has Readonly attribute set
        /// </summary>        
        public static bool IsFileReadonly(string path) {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");

            FileAttributes attrs = new FileInfo(path).Attributes;
            return (attrs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
        }

        /// <summary>
        /// Sets file saved state
        /// </summary>        
        public static void SetFileSaved(string path,bool saved) {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");

            bool open = DTE.get_IsOpenFile(null, path);            
            if (open) DTE.Documents.Item(path).Saved = saved;
        }

        /// <summary>
        /// Returns true if the file's editor window is visible
        /// </summary>        
        public static bool IsFileVisible(string path) {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");

            IVsWindowFrame frame = DocumentViewsManager.GetWindowFrameForFile(path, false);
            if (frame == null) return false;

            Window win = VsShellUtilities.GetWindowObject(frame);
            if (win == null) return false;

            return win.Visible;
        }
    }    
}
