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

namespace VisualLocalizer.Library {
    public static class RDTManager {

        private static IVsRunningDocumentTable IVsRunningDocumentTable;
        private static EnvDTE80.DTE2 DTE;
        private static IVsTextManager textManager;
        private static List<string> lockedDocuments, lockedDocumentsWaiting;

        static RDTManager() {
            IVsRunningDocumentTable = (IVsRunningDocumentTable)Package.GetGlobalService(typeof(SVsRunningDocumentTable));
            DTE = (EnvDTE80.DTE2)Package.GetGlobalService(typeof(EnvDTE.DTE));
            textManager = (IVsTextManager)Package.GetGlobalService(typeof(SVsTextManager));
            lockedDocuments = new List<string>();
            lockedDocumentsWaiting = new List<string>();

            uint evCookie;
            RDTEvents rdtEvents = new RDTEvents();
            rdtEvents.FileOpened += new Action<string>(rdtEvents_FileOpened);
            rdtEvents.FileClosed += new Action<string>(rdtEvents_FileClosed);
            IVsRunningDocumentTable.AdviseRunningDocTableEvents(rdtEvents, out evCookie);
        }

        private static void rdtEvents_FileClosed(string path) {
            if (lockedDocuments.Contains(path)) {
                lockedDocuments.Remove(path);
                lockedDocumentsWaiting.Add(path);
            }
        }

        private static void rdtEvents_FileOpened(string path) {
            if (lockedDocumentsWaiting.Contains(path)) {
                lockedDocumentsWaiting.Remove(path);
                SetFileReadonly(path, true);
            }
        }

        public static void SetFileReadonly(string path, bool read) {
            if (IsFileOpen(path)) {
                DTE.Documents.Item(path).ReadOnly = read;
                if (read) {
                    if (!lockedDocuments.Contains(path)) lockedDocuments.Add(path);
                } else {
                    lockedDocuments.Remove(path);
                }
            } else {
                if (read) {
                    if (!lockedDocumentsWaiting.Contains(path)) lockedDocumentsWaiting.Add(path);
                } else {
                    lockedDocumentsWaiting.Remove(path);
                }
            }
        }

        public static void ReleaseLocks() {
            lockedDocuments.ForEach((path) => { SetFileReadonly(path, false); });
        }

        public static void SilentlyModifyFile(string path, Action<string> modify) {
            SetIgnoreFileChanges(path, true);

            modify(path);
            
            SilentlyReloadFile(path); 
            SetIgnoreFileChanges(path, false);            
        }

        public static void SetIgnoreFileChanges(string path, bool ignore) {
            IVsHierarchy ppHier;
            uint pitemid;
            IntPtr pPunkDocData;
            uint pdwCookie;
            IVsRunningDocumentTable.FindAndLockDocument((uint)_VSRDTFLAGS.RDT_NoLock, path,
                out ppHier, out pitemid, out pPunkDocData, out pdwCookie);
            if (pPunkDocData != IntPtr.Zero) {
                IVsFileChangeEx fileChange = (IVsFileChangeEx)Package.GetGlobalService(typeof(SVsFileChangeEx));
                IVsDocDataFileChangeControl changeControl = (IVsDocDataFileChangeControl)Marshal.GetObjectForIUnknown(pPunkDocData);

                if (ignore) {
                    fileChange.IgnoreFile(0, path, 1);                    
                    changeControl.IgnoreFileChanges(1);
                } else {
                    fileChange.IgnoreFile(0, path, 0);
                    changeControl.IgnoreFileChanges(0);                    
                }
            }             
        }   

        public static void SilentlySaveFile(string path) {
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
        }

        public static void SilentlyReloadFile(string path) {
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

        public static bool IsFileSaved(string path) {
            bool open = DTE.get_IsOpenFile(null, path);
            return !open || DTE.Documents.Item(path).Saved;
        }

        public static bool IsFileOpen(string path) {            
            return DTE.get_IsOpenFile(null, path);
        }

        public static void SetFileSaved(string path,bool saved) {
            bool open = DTE.get_IsOpenFile(null, path);            
            if (open)
                DTE.Documents.Item(path).Saved = saved;
        }

        private class RDTEvents : IVsRunningDocTableEvents {

            public event Action<string> FileOpened;
            public event Action<string> FileClosed;

            public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) {
                return VSConstants.S_OK;
            }

            public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) {
                object o;
                pFrame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out o);
                string path = o.ToString();
                if (FileClosed != null) {
                    FileClosed(path);
                }
                return VSConstants.S_OK;
            }

            public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) {
                return VSConstants.S_OK;
            }

            public int OnAfterSave(uint docCookie) {
                return VSConstants.S_OK;
            }

            public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame) {
                if (fFirstShow == 1) {
                    object o;
                    pFrame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument,out o);
                    string path = o.ToString();
                    if (FileOpened != null) {
                        FileOpened(path);
                    }
                }
                return VSConstants.S_OK;
            }

            public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) {
                return VSConstants.S_OK;
            }
        }
    }    
}
