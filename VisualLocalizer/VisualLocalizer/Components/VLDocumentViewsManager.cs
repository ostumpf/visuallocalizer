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
using EnvDTE80;

namespace VisualLocalizer.Components {

    /// <summary>
    /// Provides functionality for managing files and documents in Visual Studio.
    /// </summary>
    internal class VLDocumentViewsManager : DocumentViewsManager {

        /// <summary>
        /// Set of opened locked documents, keys are file paths
        /// </summary>
        private static HashSet<string> lockedDocuments;

        /// <summary>
        /// Set of closed locked documents (waiting to be locked after opening), keys are file paths
        /// </summary>
        private static HashSet<string> lockedDocumentsWaiting;

        /// <summary>
        /// List of invisible editor windows that were opened in order to obtain their code model
        /// </summary>
        private static Dictionary<string, object> invisibleWindows;        

        /// <summary>
        /// Instance of the DTE2 object
        /// </summary>
        private static EnvDTE80.DTE2 DTE;

        /// <summary>
        /// Issued whenever is file in VS closed
        /// </summary>
        public static event Action<string> FileClosed;

        /// <summary>
        /// True if the FileClosed should not be issued even though file was closed
        /// </summary>
        private static bool suppressFileClosedEvent;

        static VLDocumentViewsManager() {
            IVsRunningDocumentTable IVsRunningDocumentTable = (IVsRunningDocumentTable)Package.GetGlobalService(typeof(SVsRunningDocumentTable));
            DTE = (EnvDTE80.DTE2)Package.GetGlobalService(typeof(EnvDTE.DTE));
            if (IVsRunningDocumentTable == null || DTE == null) throw new InvalidOperationException("Cannot consume VLDocumentViewsManager services.");

            lockedDocuments = new HashSet<string>();
            lockedDocumentsWaiting = new HashSet<string>();
            invisibleWindows = new Dictionary<string, object>();

            uint evCookie;
            // register file open and close events
            RDTEvents rdtEvents = new RDTEvents();
            rdtEvents.FileOpened += new Action<string>(RdtEvents_FileOpened);
            rdtEvents.FileClosed += new Action<string>(RdtEvents_FileClosed);
            rdtEvents.FileClosed+=new Action<string>(NotifyFileClosed);

            int hr = IVsRunningDocumentTable.AdviseRunningDocTableEvents(rdtEvents, out evCookie);
            Marshal.ThrowExceptionForHR(hr);
        }

        /// <summary>
        /// Called when file is closed; if it was locked, it is added to the set of documents waiting to be locked again on next open
        /// </summary>        
        private static void RdtEvents_FileClosed(string path) {
            if (path == null) throw new ArgumentNullException("path");

            if (lockedDocuments.Contains(path)) {
                lockedDocuments.Remove(path);
                lockedDocumentsWaiting.Add(path);
            }
        }

        /// <summary>
        /// Called when file is opened; set of documents waiting to be locked is checked and document locked if appropriate
        /// </summary>        
        private static void RdtEvents_FileOpened(string path) {
            if (path == null) throw new ArgumentNullException("path");

            if (lockedDocumentsWaiting.Contains(path)) {
                lockedDocumentsWaiting.Remove(path);
                SetFileReadonly(path, true);
            }
        }

        /// <summary>
        /// Adds a window to the list of invisible windows, opened in the background to obtain file code model
        /// </summary>        
        public static void AddInvisibleWindow(string path, object author) {
            if (path == null) throw new ArgumentNullException("path");

            if (!invisibleWindows.ContainsKey(path)) {
                invisibleWindows.Add(path, author);
            }
        }

        /// <summary>
        /// Closes all invisible windows
        /// </summary>        
        public static void CloseInvisibleWindows(object ofAuthor, bool saveIfClosed) {
            List<string> toDelete = new List<string>();
            try {
                suppressFileClosedEvent = true;

                foreach (var pair in invisibleWindows) {
                    if (pair.Value == ofAuthor || ofAuthor == null) {
                        IVsWindowFrame frame = GetWindowFrameForFile(pair.Key, false);
                        if (frame != null) {
                            var window = VsShellUtilities.GetWindowObject(frame);
                            if (!window.Visible) {
                                try {
                                    window.Detach();                                    
                                } catch { }
                                window.Close(saveIfClosed ? vsSaveChanges.vsSaveChangesYes : vsSaveChanges.vsSaveChangesNo);
                                VLOutputWindow.VisualLocalizerPane.WriteLine("\tClosing invisible window: " + Path.GetFileName(pair.Key));
                            }
                        }
                        toDelete.Add(pair.Key);
                    }
                }
                foreach (string path in toDelete) invisibleWindows.Remove(path);
            } finally {
                suppressFileClosedEvent = false;
            }
        }

        /// <summary>
        /// Issues the FileClosed event
        /// </summary>        
        private static void NotifyFileClosed(string file) {
            if (!suppressFileClosedEvent && FileClosed != null) FileClosed(file);
        }

        /// <summary>
        /// Modifes lock on the document. This lock persists if document is closed and reopened.
        /// </summary>
        /// <param name="path">File path</param>
        /// <param name="setreadonly">True to set document readonly</param>
        public static void SetFileReadonly(string path, bool setreadonly) {
            if (path == null) throw new ArgumentNullException("path");

            if (RDTManager.IsFileOpen(path)) { // file is open
                IVsWindowFrame frame = DocumentViewsManager.GetWindowFrameForFile(path, false);
                object docData;
                int hr = frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out docData); // get document buffer
                Marshal.ThrowExceptionForHR(hr);

                if (docData is ResXEditor) { // file is opened in ResXEditor
                    (docData as ResXEditor).ReadOnly = setreadonly; // use custom method to set it readonly                   
                } else { // file is opened in VS editor
                    Document document = DTE.Documents.Item(path);
                    document.ReadOnly = setreadonly;
                }
                // add/remove the document from locked documents list
                if (setreadonly) {
                    if (!lockedDocuments.Contains(path)) lockedDocuments.Add(path);
                } else {
                    lockedDocuments.Remove(path);
                }
            } else { // file is closed
                if (setreadonly) { // add file to waiting documents set
                    if (!lockedDocumentsWaiting.Contains(path)) lockedDocumentsWaiting.Add(path);
                } else { // remove the file from waiting documents
                    lockedDocumentsWaiting.Remove(path);
                }
            }
        }

        /// <summary>
        /// Returns true if given file was locked using SetFileReadonly method
        /// </summary>
        public static bool IsFileLocked(string path) {
            if (path == null) throw new ArgumentNullException("path");

            return lockedDocuments.Contains(path) || lockedDocumentsWaiting.Contains(path);
        }       

        /// <summary>
        /// Releases locks from all files previously locked by SetFileReadonly method
        /// </summary>
        public static void ReleaseLocks() {
            while (lockedDocuments.Count > 0) {
                string path = lockedDocuments.First();
                lockedDocuments.Remove(path);
                SetFileReadonly(path, false);
            }
                
            lockedDocumentsWaiting.Clear();
        }

        /// <summary>
        /// Replaces opened file's buffer with given data
        /// </summary>        
        public static void SaveDataToBuffer(Dictionary<string, ResXDataNode> data, string file) {
            if (data == null) throw new ArgumentNullException("data");
            if (file == null) throw new ArgumentNullException("file");

            object docData = GetDocData(file); // get document buffer

            if (docData is ResXEditor) { // opened in VL editor
                ResXEditor editor = docData as ResXEditor;
                editor.UIControl.SetData(data);
                editor.IsDirty = true;
            } else { // opened in default VS editor
                IVsTextLines textLines = GetTextLinesFrom(docData); // get document line buffer
                ResXResourceWriter writer = null;
                MemoryStream stream = null;
                try {
                    // create new memory stream and fill it with new ResX content
                    stream = new MemoryStream();
                    writer = new ResXResourceWriter(stream);
                    writer.BasePath = Path.GetDirectoryName(file);
                    foreach (var pair in data) {
                        writer.AddResource(pair.Key, pair.Value);
                    }
                    writer.Generate();
                    writer.Close();

                    // replace current buffer with the memory stream content, leaving no undo units
                    SaveStreamToBuffer(stream, textLines, true);
                } finally {
                    if (stream != null) stream.Close();
                }
            }            
        }

        /// <summary>
        /// Replaces given IVsTextLines with given MemoryStream content
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="textLines"></param>
        /// <param name="removeFromUndoStack">True if new undo units (created by this operation) should be removed</param>
        public static void SaveStreamToBuffer(MemoryStream stream, IVsTextLines textLines, bool removeFromUndoStack) {
            if (stream == null) throw new ArgumentNullException("stream");
            if (textLines == null) throw new ArgumentNullException("textLines");

            byte[] buffer = stream.ToArray();
            string text = Encoding.UTF8.GetString(buffer, 3, buffer.Length - 3); // get ResX file text

            int lastLine, lastLineIndex;
            int hr = textLines.GetLastLineIndex(out lastLine, out lastLineIndex);
            Marshal.ThrowExceptionForHR(hr);

            TextSpan[] spans = null;
            // replace current buffer text with new text
            hr = textLines.ReplaceLines(0, 0, lastLine, lastLineIndex, Marshal.StringToBSTR(text), text.Length, spans);
            Marshal.ThrowExceptionForHR(hr);

            if (removeFromUndoStack) {
                IOleUndoManager manager;
                // previous operation created undo unit - remove it
                hr = textLines.GetUndoManager(out manager);
                Marshal.ThrowExceptionForHR(hr);

                manager.RemoveTopFromUndoStack(1);
            }
        }

        /// <summary>
        /// Returns content of opened ResX file's buffer
        /// </summary>
        /// <param name="data">Content of the buffer</param>
        /// <param name="file">File path</param>
        public static void LoadDataFromBuffer(ref Dictionary<string,ResXDataNode> data, string file) {
            if (file == null) throw new ArgumentNullException("file");

            object docData = GetDocData(file); // get document's buffer

            if (docData is ResXEditor) {
                ResXEditor editor = docData as ResXEditor;
                data = editor.UIControl.GetData(false);
            } else {
                IVsTextLines textLines = GetTextLinesFrom(docData); // get text buffer
                string textBuffer = GetTextFrom(textLines); // get plain text
                
                ResXResourceReader reader = null;
                try {
                    data = new Dictionary<string, ResXDataNode>();
                    
                    // use reader to parse given ResX text
                    reader = ResXResourceReader.FromFileContents(textBuffer);
                    reader.BasePath = Path.GetDirectoryName(file);
                    reader.UseResXDataNodes = true;
                    foreach (DictionaryEntry entry in reader) {
                        data.Add(entry.Key.ToString().ToLower(), entry.Value as ResXDataNode);
                    }
                } finally {
                    if (reader != null) reader.Close();
                }
            }
            
        }

        /// <summary>
        /// Returns content of the given text buffer in a string
        /// </summary>       
        public static string GetTextFrom(IVsTextLines textLines) {
            if (textLines == null) throw new ArgumentNullException("textLines");

            int lastLine, lastLineIndex;
            int hr = textLines.GetLastLineIndex(out lastLine, out lastLineIndex);
            Marshal.ThrowExceptionForHR(hr);

            string textBuffer = "";
            hr = textLines.GetLineText(0, 0, lastLine, lastLineIndex, out textBuffer);
            Marshal.ThrowExceptionForHR(hr);

            return textBuffer;
        }

        /// <summary>
        /// Returns document data object for given opened file
        /// </summary>        
        public static object GetDocData(string file) {
            if (file == null) throw new ArgumentNullException("file");

            IVsWindowFrame frame = GetWindowFrameForFile(file, false);
            if (frame == null) throw new InvalidOperationException("Cannot get window frame - is file opened?");

            object docData;
            int hr = frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out docData);
            Marshal.ThrowExceptionForHR(hr);

            return docData;
        }

        /// <summary>
        /// Returns text buffer object for given document data object
        /// </summary>        
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

        /// <summary>
        /// Implementation of IVsRunningDocTableEvents, exposing FileOpened and FileClosed events
        /// </summary>
        private class RDTEvents : IVsRunningDocTableEvents {

            public event Action<string> FileOpened;
            public event Action<string> FileClosed;

            public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) {
                return VSConstants.S_OK;
            }

            public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) {
                object o;
                int hr = pFrame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out o);
                Marshal.ThrowExceptionForHR(hr);
                
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
                    int hr = pFrame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out o);
                    Marshal.ThrowExceptionForHR(hr);

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
