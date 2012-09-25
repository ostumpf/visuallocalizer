using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Resources;
using VisualLocalizer.Editor;
using System.Collections;
using System.IO;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using Microsoft.VisualStudio;

namespace VisualLocalizer.Components {
    internal class VLDocumentViewsManager : DocumentViewsManager {

        private static HashSet<string> lockedDocuments, lockedDocumentsWaiting;
        private static IVsRunningDocumentTable IVsRunningDocumentTable;
        private static EnvDTE80.DTE2 DTE;

        static VLDocumentViewsManager() {
            IVsRunningDocumentTable = (IVsRunningDocumentTable)Package.GetGlobalService(typeof(SVsRunningDocumentTable));
            DTE = (EnvDTE80.DTE2)Package.GetGlobalService(typeof(EnvDTE.DTE));

            lockedDocuments = new HashSet<string>();
            lockedDocumentsWaiting = new HashSet<string>();

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

        public static void SetFileReadonly(string path, bool setreadonly) {
            if (RDTManager.IsFileOpen(path)) {
                IVsWindowFrame frame = DocumentViewsManager.GetWindowFrameForFile(path, false);
                object docData;
                int hr = frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out docData);
                Marshal.ThrowExceptionForHR(hr);

                if (docData is ResXEditor) {
                    (docData as ResXEditor).ReadOnly = setreadonly;                    
                } else {
                    Document document = DTE.Documents.Item(path);
                    document.ReadOnly = setreadonly;
                }
                if (setreadonly) {
                    if (!lockedDocuments.Contains(path)) lockedDocuments.Add(path);
                } else {
                    lockedDocuments.Remove(path);
                }
            } else {
                if (setreadonly) {
                    if (!lockedDocumentsWaiting.Contains(path)) lockedDocumentsWaiting.Add(path);
                } else {
                    lockedDocumentsWaiting.Remove(path);
                }
            }
        }

        public static bool IsFileLocked(string path) {
            return lockedDocuments.Contains(path) || lockedDocumentsWaiting.Contains(path);
        }

        public static void ReleaseLocks() {
            while (lockedDocuments.Count > 0) {
                string path = lockedDocuments.First();
                lockedDocuments.Remove(path);
                SetFileReadonly(path, false);
            }
                
            lockedDocumentsWaiting.Clear();
        }

        public static void SaveDataToBuffer(Dictionary<string, ResXDataNode> data, string file) {
            IVsWindowFrame frame = GetWindowFrameForFile(file, false);
            if (frame == null) throw new InvalidOperationException("Cannot get window frame - is file opened?");

            object docData;
            int hr = frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out docData);
            Marshal.ThrowExceptionForHR(hr);

            if (docData is ResXEditor) {
                ResXEditor editor = docData as ResXEditor;
                editor.UIControl.SetData(data);
                editor.IsDirty = true;
            } else {
                IVsTextLines textLines = GetTextLinesFrom(docData);
                ResXResourceWriter writer = null;
                MemoryStream stream = null;
                try {
                    stream = new MemoryStream();
                    writer = new ResXResourceWriter(stream);
                    foreach (var pair in data) {
                        writer.AddResource(pair.Key, pair.Value);
                    }
                    writer.Generate();
                    writer.Close();

                    byte[] buffer = stream.ToArray();
                    string text = Encoding.UTF8.GetString(buffer, 3, buffer.Length - 3);

                    int lastLine, lastLineIndex;
                    hr = textLines.GetLastLineIndex(out lastLine, out lastLineIndex);
                    Marshal.ThrowExceptionForHR(hr);

                    TextSpan[] spans = null;
                    hr = textLines.ReplaceLines(0, 0, lastLine, lastLineIndex, Marshal.StringToBSTR(text), text.Length, spans);
                    Marshal.ThrowExceptionForHR(hr);

                    IOleUndoManager manager;
                    hr = textLines.GetUndoManager(out manager);
                    Marshal.ThrowExceptionForHR(hr);

                    manager.RemoveTopFromUndoStack(1);
                } finally {
                    if (stream != null) stream.Close();
                }
            }

            
        }

        public static void LoadDataFromBuffer(ref Dictionary<string,ResXDataNode> data, string file) {            
            IVsWindowFrame frame = GetWindowFrameForFile(file, false);
            if (frame == null) throw new InvalidOperationException("Cannot get window frame - is file opened?");

            object docData;
            int hr = frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out docData);
            Marshal.ThrowExceptionForHR(hr);

            if (docData is ResXEditor) {
                ResXEditor editor = docData as ResXEditor;
                data = editor.UIControl.GetData(false);
            } else {
                IVsTextLines textLines = GetTextLinesFrom(docData);

                int lastLine, lastLineIndex;
                hr = textLines.GetLastLineIndex(out lastLine, out lastLineIndex);
                Marshal.ThrowExceptionForHR(hr);

                string textBuffer = "";
                hr = textLines.GetLineText(0, 0, lastLine, lastLineIndex, out textBuffer);
                Marshal.ThrowExceptionForHR(hr);

                ResXResourceReader reader = null;
                try {
                    data = new Dictionary<string, ResXDataNode>();
                    reader = ResXResourceReader.FromFileContents(textBuffer);
                    reader.UseResXDataNodes = true;
                    foreach (DictionaryEntry entry in reader) {
                        data.Add(entry.Key.ToString(), entry.Value as ResXDataNode);
                    }
                } finally {
                    if (reader != null) reader.Close();
                }
            }
            
        }

        private static IVsTextLines GetTextLinesFrom(object docData) {
            IVsTextLines textLines = null;
            var buffer = docData as VsTextBuffer;

            if (buffer == null) {
                var bufferProvider = docData as IVsTextBufferProvider;

                if (bufferProvider != null) {
                    int hr = bufferProvider.GetTextBuffer(out textLines);
                    Marshal.ThrowExceptionForHR(hr);
                }
            } else {
                textLines = (IVsTextLines)buffer;
            }
            if (textLines == null) throw new InvalidOperationException("Cannot retrieve file buffer.");

            return textLines;
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
                    pFrame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out o);
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
