using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

namespace VisualLocalizer.Library {
    public static class RDTManager {

        public static IVsRunningDocumentTable IVsRunningDocumentTable;
        private static Dictionary<string, IntPtr> lockedFiles;
        private static Dictionary<string, uint> lockedCookies;

        static RDTManager() {
            lockedFiles = new Dictionary<string, IntPtr>();
            lockedCookies = new Dictionary<string, uint>();
        }

        public static void SetIgnoreFileChanges(string path, bool ignore,int timeout) {
            if (IVsRunningDocumentTable == null)
                throw new InvalidOperationException("RDTManager class has not been initialized - set IVsRunningDocumentTable instance.");

            if (timeout > 0) {
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback((object o) => {
                    System.Threading.Thread.Sleep(timeout);
                    SetIgnoreFileChanges(path, ignore, 0);
                }), null);
            } else {
                if (ignore) {
                    IVsHierarchy ppHier;
                    uint pitemid;
                    IntPtr pPunkDocData;
                    uint pdwCookie;
                    IVsRunningDocumentTable.FindAndLockDocument((uint)_VSRDTFLAGS.RDT_NoLock, path,
                        out ppHier, out pitemid, out pPunkDocData, out pdwCookie);
                   
                    if (pPunkDocData != IntPtr.Zero) {
                        IVsDocDataFileChangeControl c = (IVsDocDataFileChangeControl)Marshal.GetObjectForIUnknown(pPunkDocData);
                        int hResult = c.IgnoreFileChanges(1);
                        Marshal.ThrowExceptionForHR(hResult);

                        if (!lockedFiles.ContainsKey(path)) {
                            lockedFiles.Add(path, pPunkDocData);
                            lockedCookies.Add(path, pdwCookie);
                        }
                    } else {
                        IVsRunningDocumentTable.UnlockDocument((uint)_VSRDTFLAGS.RDT_NoLock, pdwCookie);                        
                    }
                } else {
                    if (lockedFiles.ContainsKey(path)) {
                        IntPtr pPunkDocData = lockedFiles[path];
                        IVsDocDataFileChangeControl c = (IVsDocDataFileChangeControl)Marshal.GetObjectForIUnknown(pPunkDocData);
                        int hResult = c.IgnoreFileChanges(0);
                        Marshal.ThrowExceptionForHR(hResult);

                        IVsRunningDocumentTable.NotifyDocumentChanged(lockedCookies[path], (uint)__VSRDTATTRIB.RDTA_DocDataIsDirty);                        
                        IVsRunningDocumentTable.UnlockDocument((uint)_VSRDTFLAGS.RDT_NoLock, lockedCookies[path]);                        

                        lockedFiles.Remove(path);
                        lockedCookies.Remove(path);
                    }
                }
            }
        }

        public static void SilentlySaveFile(string path) {
            if (IVsRunningDocumentTable == null)
                throw new InvalidOperationException("RDTManager class has not been initialized - set IVsRunningDocumentTable instance.");

            IVsHierarchy ppHier;
            uint pitemid;
            IntPtr pPunkDocData;
            uint pdwCookie;
            IVsRunningDocumentTable.FindAndLockDocument((uint)_VSRDTFLAGS.RDT_NoLock, path,
                out ppHier, out pitemid, out pPunkDocData, out pdwCookie);
            
            if (pPunkDocData != IntPtr.Zero) {
                IVsPersistDocData d = (IVsPersistDocData)Marshal.GetObjectForIUnknown(pPunkDocData);
                string s;
                int cancelled;
                int hResult = d.SaveDocData(VSSAVEFLAGS.VSSAVE_SilentSave, out s, out cancelled);
                Marshal.ThrowExceptionForHR(hResult);
            }

            IVsRunningDocumentTable.UnlockDocument((uint)_VSRDTFLAGS.RDT_NoLock, pdwCookie);            
        }

        public static void SilentlyReloadFile(string path) {
            if (IVsRunningDocumentTable == null)
                throw new InvalidOperationException("RDTManager class has not been initialized - set IVsRunningDocumentTable instance.");

            IVsHierarchy ppHier;
            uint pitemid;
            IntPtr pPunkDocData;
            uint pdwCookie;
            IVsRunningDocumentTable.FindAndLockDocument((uint)_VSRDTFLAGS.RDT_NoLock, path,
                out ppHier, out pitemid, out pPunkDocData, out pdwCookie);
            
            if (pPunkDocData != IntPtr.Zero) {
                IVsPersistDocData d = (IVsPersistDocData)Marshal.GetObjectForIUnknown(pPunkDocData);
                int hResult = d.ReloadDocData(0);
                Marshal.ThrowExceptionForHR(hResult);            
            }

            IVsRunningDocumentTable.UnlockDocument((uint)_VSRDTFLAGS.RDT_NoLock, pdwCookie);            
        }
    }
}
