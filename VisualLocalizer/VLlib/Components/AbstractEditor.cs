using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE;

namespace VisualLocalizer.Library.Components {
   
    /// <summary>
    /// Generic implementation of IVsEditorFactory, used by VS to create new instances of editors.
    /// </summary>
    /// <typeparam name="EditorType">Type of the editor</typeparam>
    /// <typeparam name="ControlType">Type of the editor control</typeparam>
    public abstract class EditorFactory<EditorType, ControlType> : IVsEditorFactory
        where EditorType : AbstractSingleViewEditor<ControlType>,new()
        where ControlType : Control, IEditorControl, new() {

        private ServiceProvider serviceProvider;

        /// <summary>
        /// Closes the factory.
        /// </summary>        
        int IVsEditorFactory.Close() {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Creates new editor instance
        /// </summary>      
        int IVsEditorFactory.CreateEditorInstance(uint grfCreateDoc, string pszMkDocument, string pszPhysicalView, IVsHierarchy pvHier, uint itemid, IntPtr punkDocDataExisting, out IntPtr ppunkDocView, out IntPtr ppunkDocData, out string pbstrEditorCaption, out Guid pguidCmdUI, out int pgrfCDW) {
            ppunkDocView = IntPtr.Zero;
            ppunkDocData = IntPtr.Zero;
            pguidCmdUI = GetType().GUID;
            pgrfCDW = 0;
            pbstrEditorCaption = null;

            // Validate inputs
            if ((grfCreateDoc & (VSConstants.CEF_OPENFILE | VSConstants.CEF_SILENT)) == 0) {
                return VSConstants.E_INVALIDARG;
            }
            if (punkDocDataExisting != IntPtr.Zero) {
                return VSConstants.VS_E_INCOMPATIBLEDOCDATA;
            }

            // Create the Document (codeWindowHost)
            EditorType editor = new EditorType();
            ppunkDocView = Marshal.GetIUnknownForObject(editor);
            ppunkDocData = Marshal.GetIUnknownForObject(editor);
            pbstrEditorCaption = Caption;

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Matches the logical view to the physical view
        /// </summary> 
        int IVsEditorFactory.MapLogicalView(ref Guid rguidLogicalView, out string pbstrPhysicalView) {
            return MapLogicalView(rguidLogicalView, out pbstrPhysicalView);
        }        

        /// <summary>
        /// Sets service provider
        /// </summary>        
        int IVsEditorFactory.SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp) {
            serviceProvider = new ServiceProvider(psp);          

            return VSConstants.S_OK;
        }
        
        /// <summary>
        /// Matches the logical view to the physical view
        /// </summary>        
        public virtual int MapLogicalView(Guid rguidLogicalView, out string pbstrPhysicalView) {
            pbstrPhysicalView = null;
            // we support only a single physical view
            if (VSConstants.LOGVIEWID_Primary == rguidLogicalView)
                return VSConstants.S_OK;        // primary view uses NULL as pbstrPhysicalView
            else
                return VSConstants.E_NOTIMPL;
        }

        /// <summary>
        /// Caption for the editor
        /// </summary>
        public virtual string Caption {
            get {
                return string.Empty;
            }
        }

    }

    /// <summary>
    /// Represents base class for simple single view editor of files
    /// </summary>
    /// <typeparam name="T">Type of editor contorl</typeparam>
    public abstract class AbstractSingleViewEditor<T> :
        WindowPane, 
        IOleCommandTarget, 
        IVsPersistDocData2,
        IPersistFileFormat,
        IVsFileChangeEvents,
        IExtensibleObject,
        IVsStatusbarUser
        where T : Control, IEditorControl, new() {
                         
        private uint vsFileChangeCookie;
        private bool fileChangedTimerSet;
        private Timer reloadTimer;

        /// <summary>
        /// Index to the format list of the currently edited file
        /// </summary>
        protected uint formatIndex = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractSingleViewEditor{T}"/> class.
        /// </summary>
        public AbstractSingleViewEditor()
            : base(null) {
            
            UIControl = new T();            
            UIControl.Init(this);            
        }        

        #region WindowPane members


        /// <summary>
        /// Retrieves the window associated with this window pane.
        /// </summary>        
        public override IWin32Window Window {
            get {
                return UIControl as IWin32Window;
            }
        }

        #endregion

        #region IOleCommandTarget members

        /// <summary>
        /// Executes a Visual Studio command
        /// </summary>
        /// <param name="pguidCmdGroup">GUID of the command</param>
        /// <param name="nCmdID">ID of the command</param>
        /// <param name="nCmdexecopt">Execution options</param>
        /// <param name="pvaIn">Input object</param>
        /// <param name="pvaOut">Output object</param>        
        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
            bool ok = false;
            if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97) {
                switch ((VSConstants.VSStd97CmdID)nCmdID) {
                    case VSConstants.VSStd97CmdID.Paste:
                        ok = ExecutePaste();
                        break;
                    case VSConstants.VSStd97CmdID.Cut:
                        ok = ExecuteCut();
                        break;
                    case VSConstants.VSStd97CmdID.Copy:
                        ok = ExecuteCopy();
                        break;
                    default:
                        ok = ExecuteSystemCommand((VSConstants.VSStd97CmdID)nCmdID);
                        break;
                }                 
            } else {
                ok = ExecuteCommand(pguidCmdGroup, nCmdID);
            }
            
            if (ok)
                return VSConstants.S_OK;
            else
                return (int)(Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED);
        }

        /// <summary>
        /// Determines status of commands (enabled, disabled, not supported)
        /// </summary>
        /// <param name="pguidCmdGroup">GUID of the command</param>
        /// <param name="cCmds">Commands count</param>
        /// <param name="prgCmds">Commands IDs</param>
        /// <param name="pCmdText">Command text</param>        
        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
            if (prgCmds == null || cCmds != 1)
                return VSConstants.E_INVALIDARG;

            COMMAND_STATUS status;
            if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97) {                  
                switch ((VSConstants.VSStd97CmdID)prgCmds[0].cmdID) {
                    case VSConstants.VSStd97CmdID.Paste: 
                        status = IsPasteSupported;
                        break;
                    case VSConstants.VSStd97CmdID.Cut:
                        status = IsCutSupported;
                        break;
                    case VSConstants.VSStd97CmdID.Copy:
                        status = IsCopySupported;
                        break;
                    case VSConstants.VSStd97CmdID.Save:
                        status = !ReadOnly ? COMMAND_STATUS.ENABLED : COMMAND_STATUS.DISABLED;
                        break;
                    case VSConstants.VSStd97CmdID.SaveProjectItem:
                        status = !ReadOnly ? COMMAND_STATUS.ENABLED : COMMAND_STATUS.DISABLED;
                        break;
                    case VSConstants.VSStd97CmdID.SaveSelection:
                        status = !ReadOnly ? COMMAND_STATUS.ENABLED : COMMAND_STATUS.DISABLED;
                        break;                    
                    default:
                        status = GetSystemCommandStatus((VSConstants.VSStd97CmdID)prgCmds[0].cmdID);
                        break;
                }                
            } else {
                status = GetCommandStatus(pguidCmdGroup, prgCmds[0].cmdID);
            }

            switch (status) {
                case COMMAND_STATUS.ENABLED:
                    prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
                    return VSConstants.S_OK;

                case COMMAND_STATUS.DISABLED:
                    prgCmds[0].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED);
                    return VSConstants.S_OK;

                case COMMAND_STATUS.UNSUPPORTED:
                    return (int)(Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED);

                default:
                    return (int)(Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED);
            }
        }

        #endregion

        #region IPersistFileFormat members

        /// <summary>
        /// Returns this class GUID
        /// </summary>        
        int IPersistFileFormat.GetClassID(out Guid pClassID) {
            return (this as IPersist).GetClassID(out pClassID);
        }

        /// <summary>
        /// Returns currently edited file
        /// </summary>
        /// <param name="ppszFilename">File path</param>
        /// <param name="pnFormatIndex">Format of the file (index to the list obtained by GetFormatList())</param>       
        int IPersistFileFormat.GetCurFile(out string ppszFilename, out uint pnFormatIndex) {
            pnFormatIndex = formatIndex;
            ppszFilename = FileName;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns list of supported formats, visible in the Save As... dialog. 
        /// </summary>        
        int IPersistFileFormat.GetFormatList(out string ppszFormatList) {
            ppszFormatList = GetFormatList();
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Creates new file with given format
        /// </summary>        
        int IPersistFileFormat.InitNew(uint nFormatIndex) {
            if (nFormatIndex != formatIndex) {
                return VSConstants.E_INVALIDARG;
            }
            
            CreateNew();

            IsDirty = false;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Determines whether currently edited file is dirty (modified and not saved)
        /// </summary>        
        int IPersistFileFormat.IsDirty(out int pfIsDirty) {
            pfIsDirty = IsDirty ? 1 : 0;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Loads the file
        /// </summary>
        /// <param name="fileToLoad">File path</param>
        /// <param name="grfMode">File opend mode</param>
        /// <param name="fReadOnly">True if the file should be opened as readonly</param>        
        int IPersistFileFormat.Load(string fileToLoad, uint grfMode, int fReadOnly) {
            if (fileToLoad == null) {
                return VSConstants.E_INVALIDARG;
            }

            Loading = true;
            try {
                FileName = fileToLoad;
                LoadFile(fileToLoad);
                IsDirty = false;
                ReadOnly = (File.GetAttributes(fileToLoad) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;                

                // Hook up to file change notifications
                if (String.IsNullOrEmpty(FileName) || 0 != String.Compare(FileName, fileToLoad, true, CultureInfo.CurrentCulture)) {                    
                    SetFileChangeNotification(FileName, true);

                    // Notify the load or reload
                    NotifyDocChanged();
                }
                return VSConstants.S_OK;
            } catch (Exception) {
                return VSConstants.E_ABORT;
            } finally {
                Loading = false;
            }            
        }

        /// <summary>
        /// Saves the file
        /// </summary>
        /// <param name="pszFilename">File path (may be different from current file path)</param>
        /// <param name="fRemember">True to execute Save As...</param>
        /// <param name="nFormatIndex">Format index</param>        
        int IPersistFileFormat.Save(string pszFilename, int fRemember, uint nFormatIndex) {            
            // --- If file is null or same --> SAVE
            try {
                if (pszFilename == null || pszFilename == FileName) {
                    SetFileChangeNotification(FileName, false);

                    SaveFile(FileName, nFormatIndex);
                    IsDirty = false;

                    SetFileChangeNotification(FileName, true);
                } else {
                    // --- If remember --> SaveAs
                    if (fRemember != 0) {
                        SetFileChangeNotification(pszFilename, false);

                        FileName = pszFilename;
                        SaveFile(FileName, nFormatIndex);
                        IsDirty = false;

                        SetFileChangeNotification(FileName, true);
                    } else {// --- Else, Save a Copy As
                        SetFileChangeNotification(pszFilename, false);

                        SaveFile(pszFilename, nFormatIndex);

                        SetFileChangeNotification(pszFilename, true);
                    }
                }
                return VSConstants.S_OK;
            } catch (Exception) {                
                return VSConstants.E_ABORT;
            }
            
        }

        /// <summary>
        /// Completes the file saving process
        /// </summary>
        /// <param name="pszFilename">Path of the saved file</param>        
        int IPersistFileFormat.SaveCompleted(string pszFilename) {
           return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns this class's GUID
        /// </summary>        
        int IPersist.GetClassID(out Guid pClassID) {
            pClassID = EditorFactoryGuid;
            return VSConstants.S_OK;
        }

        #endregion

        #region IVsPersistDocData2 members

        /// <summary>
        /// Closes the editor
        /// </summary>        
        public int Close() {
            SetFileChangeNotification(FileName, false);

            EditorClose();

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns this class's GUID
        /// </summary>        
        public int GetGuidEditorType(out Guid pClassID) {
            return ((IPersistFileFormat)this).GetClassID(out pClassID);
        }

        /// <summary>
        /// Determines whether currently edited file is modified but not saved
        /// </summary>        
        public int IsDocDataDirty(out int pfDirty) {
            return ((IPersistFileFormat)this).IsDirty(out pfDirty);
        }

        /// <summary>
        /// Determines whether file can be reloaded
        /// </summary>        
        public int IsDocDataReloadable(out int pfReloadable) {
            pfReloadable = 1;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Loads given file
        /// </summary>        
        public int LoadDocData(string pszMkDocument) {
            return ((IPersistFileFormat)this).Load(pszMkDocument, 0, 0);
        }

        /// <summary>
        /// Registers document buffer in the VS context
        /// </summary>  
        public int OnRegisterDocData(uint docCookie, IVsHierarchy pHierNew, uint itemidNew) {            
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Reloads document buffer
        /// </summary>        
        public int ReloadDocData(uint ignoreNextChange) {
            return ((IPersistFileFormat)this).Load(FileName, ignoreNextChange, 0);
        }

        /// <summary>
        /// Renames current document buffer
        /// </summary>
        public int RenameDocData(uint grfAttribs, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Saves the document
        /// </summary>
        /// <param name="saveFlag">Save flags</param>
        /// <param name="newFilePath">File path</param>
        /// <param name="saveCanceled">True if save was not successuful</param>        
        public int SaveDocData(VSSAVEFLAGS saveFlag, out string newFilePath, out int saveCanceled) {
            newFilePath = null;
            saveCanceled = 0;
            int hr = VSConstants.S_OK;

            switch (saveFlag) {
                case VSSAVEFLAGS.VSSAVE_Save:
                case VSSAVEFLAGS.VSSAVE_SilentSave: {
                        IVsQueryEditQuerySave2 queryEditQuerySave = (IVsQueryEditQuerySave2)Package.GetGlobalService(typeof(SVsQueryEditQuerySave));

                        // Call QueryEditQuerySave
                        uint result = 0;
                        hr = queryEditQuerySave.QuerySaveFile(
                            FileName,
                            // filename
                            0,
                            // flags
                            null,
                            // file attributes
                            out result); // result
                        if (ErrorHandler.Failed(hr))
                            return hr;

                        // Process according to result from QuerySave
                        switch ((tagVSQuerySaveResult)result) {
                            case tagVSQuerySaveResult.QSR_NoSave_Cancel:
                                // Note that this is also case tagVSQuerySaveResult.QSR_NoSave_UserCanceled because these
                                // two tags have the same value.
                                saveCanceled = ~0;
                                break;

                            case tagVSQuerySaveResult.QSR_SaveOK: {
                                    // Call the shell to do the save for us
                                    IVsUIShell uiShell = (IVsUIShell)Package.GetGlobalService(typeof(SVsUIShell));
                                    hr = uiShell.SaveDocDataToFile(saveFlag, (IPersistFileFormat)this, FileName, out newFilePath, out saveCanceled);
                                    if (ErrorHandler.Failed(hr))
                                        return hr;
                                }
                                break;

                            case tagVSQuerySaveResult.QSR_ForceSaveAs: {
                                    // Call the shell to do the SaveAS for us
                                    IVsUIShell uiShell = (IVsUIShell)Package.GetGlobalService(typeof(SVsUIShell));
                                    hr = uiShell.SaveDocDataToFile(VSSAVEFLAGS.VSSAVE_SaveAs, (IPersistFileFormat)this, FileName, out newFilePath, out saveCanceled);
                                    if (ErrorHandler.Failed(hr))
                                        return hr;
                                }
                                break;

                            case tagVSQuerySaveResult.QSR_NoSave_Continue:
                                // In this case there is nothing to do.
                                break;

                            default:
                                throw new COMException("Invalid QuerySave result.");
                        }
                        break;
                    }
                case VSSAVEFLAGS.VSSAVE_SaveAs:
                case VSSAVEFLAGS.VSSAVE_SaveCopyAs: {                       
                        // Call the shell to do the save for us
                        IVsUIShell uiShell = (IVsUIShell)Package.GetGlobalService(typeof(SVsUIShell));
                        hr = uiShell.SaveDocDataToFile(saveFlag, (IPersistFileFormat)this, FileName, out newFilePath, out saveCanceled);
                        if (ErrorHandler.Failed(hr))
                            return hr;
                        break;
                    }
                default:
                    throw new ArgumentException("Invalid VSSAVEFLAGS.");
            }


            return VSConstants.S_OK;
        }

        /// <summary>
        /// Saves unnamed document
        /// </summary>        
        public int SetUntitledDocPath(string pszDocDataPath) {
            return ((IPersistFileFormat)this).InitNew(formatIndex);
        }

        /// <summary>
        /// Returns true if document is readonly
        /// </summary>        
        public int IsDocDataReadOnly(out int pfReadOnly) {
            pfReadOnly = ReadOnly ? 1 : 0;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns true if document is dirty (modified and not saved)
        /// </summary>        
        public int SetDocDataDirty(int fDirty) {
            IsDirty = fDirty != 0;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Sets current document's readonly state
        /// </summary>        
        public int SetDocDataReadOnly(int fReadOnly) {
            ReadOnly = fReadOnly != 0;
            return VSConstants.S_OK;
        }


        #endregion

        #region IVsFileChangeEvents

        /// <summary>
        /// Directory name in which the file is located was changed
        /// </summary>        
        int IVsFileChangeEvents.DirectoryChanged(string pszDirectory) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Currently edited file was changed from outside VS environment
        /// </summary>        
        int IVsFileChangeEvents.FilesChanged(uint numberOfChanges, string[] filesChanged, uint[] typesOfChanges) {
            if (0 == numberOfChanges || null == filesChanged || null == typesOfChanges)
                return VSConstants.E_INVALIDARG;

            // accumulate the changes
            for (uint i = 0; i < numberOfChanges; i++) {
                if (!String.IsNullOrEmpty(filesChanged[i]) && String.Compare(filesChanged[i], FileName, true, CultureInfo.CurrentCulture) == 0) {
                  if (0 != (typesOfChanges[i] & (int)(_VSFILECHANGEFLAGS.VSFILECHG_Time | _VSFILECHANGEFLAGS.VSFILECHG_Size))) {
                        if (!fileChangedTimerSet) {
                            reloadTimer = new Timer();
                            fileChangedTimerSet = true;
                            reloadTimer.Interval = 1000;
                            reloadTimer.Tick += new EventHandler(ReloadTimer_Tick);
                            reloadTimer.Enabled = true;
                        }
                    }
                }
            }
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called 1s after the document was changed and therefore probably no other change will happen
        /// </summary>        
        private void ReloadTimer_Tick(object sender, EventArgs e) {
            reloadTimer.Enabled = false;           
            reloadTimer = null;

            FileChangedOutsideVS();
          
            fileChangedTimerSet = false;
        }        

        /// <summary>
        /// Sets the file change notification - whether VS should be informed about outside-VS file changes
        /// </summary>
        /// <param name="fileNameToNotify">File path</param>
        /// <param name="startNotify">True to turn notifications on</param>        
        private int SetFileChangeNotification(string fileNameToNotify, bool startNotify) {
            int result = VSConstants.E_FAIL;
            
            IVsFileChangeEx vsFileChangeEx = (IVsFileChangeEx)Package.GetGlobalService(typeof(SVsFileChangeEx));
            if (null == vsFileChangeEx)
                return VSConstants.E_UNEXPECTED;
            
            // Setup Notification if startNotify is TRUE, Remove if startNotify is FALSE.
            if (startNotify) {
                if (vsFileChangeCookie == VSConstants.VSCOOKIE_NIL) {
                    //Receive notifications if either the attributes of the file change or 
                    //if the size of the file changes or if the last modified time of the file changes
                    result = vsFileChangeEx.AdviseFileChange(fileNameToNotify,
                                                             (uint)(_VSFILECHANGEFLAGS.VSFILECHG_Attr | _VSFILECHANGEFLAGS.VSFILECHG_Size | _VSFILECHANGEFLAGS.VSFILECHG_Time),
                                                             (IVsFileChangeEvents)this,
                                                             out vsFileChangeCookie);
                    if (vsFileChangeCookie == VSConstants.VSCOOKIE_NIL) {
                        return VSConstants.E_FAIL;
                    }
                }
                result = VSConstants.S_OK;
            } else {
                if (vsFileChangeCookie != VSConstants.VSCOOKIE_NIL) {
                    //if we want to unadvise and the cookieTextViewEvents isnt null then unadvise changes
                    result = vsFileChangeEx.UnadviseFileChange(vsFileChangeCookie);
                    vsFileChangeCookie = VSConstants.VSCOOKIE_NIL;
                    result = VSConstants.S_OK;
                }
            }
            return result;
        }

        #endregion

        #region notifyDocChanged
        /// <summary>
        /// Gets an instance of the RunningDocumentTable (RDT) service which manages the set of currently open 
        /// documents in the environment and then notifies the client that an open document has changed
        /// </summary>
        private void NotifyDocChanged() {
            // Make sure that we have a file name
            if (FileName.Length == 0)
                return;

            // Get a reference to the Running Document Table
            IVsRunningDocumentTable runningDocTable = (IVsRunningDocumentTable)Package.GetGlobalService(typeof(SVsRunningDocumentTable));

            // Lock the document
            uint docCookie;
            IVsHierarchy hierarchy;
            uint itemID;
            IntPtr docData;
            int hr = runningDocTable.FindAndLockDocument(
                (uint)_VSRDTFLAGS.RDT_ReadLock,
                FileName,
                out hierarchy,
                out itemID,
                out docData,
                out docCookie
            );
            ErrorHandler.ThrowOnFailure(hr);

            // Send the notification
            hr = runningDocTable.NotifyDocumentChanged(docCookie, (uint)__VSRDTATTRIB.RDTA_DocDataReloaded);

            // Unlock the document.
            // Note that we have to unlock the document even if the previous call failed.
            runningDocTable.UnlockDocument((uint)_VSRDTFLAGS.RDT_ReadLock, docCookie);

            // Check ff the call to NotifyDocChanged failed.
            ErrorHandler.ThrowOnFailure(hr);
        }

        #endregion

        #region IExtensibleObject members

        /// <summary>
        /// Returns this class instance, which can be used by other VS packages
        /// </summary>
        /// <param name="Name">Should be "Document"</param>
        /// <param name="pParent">Parent object</param>
        /// <param name="ppDisp">Automation object</param>
        public void GetAutomationObject(string Name, IExtensibleObjectSite pParent, out object ppDisp) {
            if (!string.IsNullOrEmpty(Name) && !Name.Equals("Document", StringComparison.CurrentCultureIgnoreCase)) {
                ppDisp = null;
                return;
            }

            ppDisp = this;
        }
        #endregion


        #region IVsStatusBarUser members 
        
        /// <summary>
        /// Required by IVsStatusBarUser interface
        /// </summary>
        /// <returns></returns>
        public int SetInfo() {
            return VSConstants.S_OK;
        }

        #endregion

        /// <summary>
        /// True if the document is dirty
        /// </summary>
        public bool IsDirty {
            get;
            set;
        }

        /// <summary>
        /// Currently edited file path
        /// </summary>
        public virtual string FileName {
            get;
            protected set;
        }

        /// <summary>
        /// File extension supported by this editor
        /// </summary>
        public abstract string Extension {
            get;            
        }

        /// <summary>
        /// GUID of the factory producing this editor
        /// </summary>
        public abstract Guid EditorFactoryGuid {
            get;
        }

        /// <summary>
        /// GUI control
        /// </summary>
        public T UIControl {
            get;
            private set;
        }

        /// <summary>
        /// True if the editor is loading files (working cursor is displayed)
        /// </summary>
        public bool Loading {
            get;
            private set;
        }

        /// <summary>
        /// Executes VS command
        /// </summary>
        /// <param name="guidCmdGroup">GUID of the command</param>
        /// <param name="cmdID">ID of the command</param>
        /// <returns>True if the execution was actually performed</returns>
        public virtual bool ExecuteCommand(Guid guidCmdGroup, uint cmdID) {
            return false;
        }

        /// <summary>
        /// Executes VS command with classic VSConstants.GUID_VSStandardCommandSet97 GUID
        /// </summary>
        /// <param name="cmdID">Command ID</param>
        /// <returns>True if the execution was actually performed</returns>
        public virtual bool ExecuteSystemCommand(VSConstants.VSStd97CmdID cmdID) {            
            return false;
        }

        /// <summary>
        /// Gets status of a VS command
        /// </summary>
        /// <param name="guidCmdGroup">GUID of the command</param>
        /// <param name="cmdID">ID of the command</param>        
        public virtual COMMAND_STATUS GetCommandStatus(Guid guidCmdGroup, uint cmdID) {
            return COMMAND_STATUS.UNSUPPORTED;
        }

        /// <summary>
        /// Gets status of a VS command with classic VSConstants.GUID_VSStandardCommandSet97 GUID 
        /// </summary>        
        /// <param name="cmdID">ID of the command</param>
        public virtual COMMAND_STATUS GetSystemCommandStatus(VSConstants.VSStd97CmdID cmdID) {
            return COMMAND_STATUS.UNSUPPORTED;
        }

        /// <summary>
        /// Called when a new file was created
        /// </summary>
        public virtual void CreateNew() {
        }

        /// <summary>
        /// Returns format list displayd in the Save As... dialog.
        /// </summary>        
        public virtual string GetFormatList() {
            return string.Empty;
        }

        /// <summary>
        /// Loads a file with specified path
        /// </summary>        
        public virtual void LoadFile(string path) {
            IVsUIShell VsUiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            if (VsUiShell != null) {
                VsUiShell.SetWaitCursor();
            }
        }

        /// <summary>
        /// Saves current document's buffer in the specified path
        /// </summary>
        /// <param name="path">File path</param>
        /// <param name="format">Index in the format list, specifying format of the saved file</param>
        public virtual void SaveFile(string path, uint format) {
            IVsUIShell VsUiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            if (VsUiShell != null) {
                VsUiShell.SetWaitCursor();
            }
        }       

        /// <summary>
        /// Called when editor is closing
        /// </summary>
        public virtual void EditorClose() {
        }
        
        /// <summary>
        /// True if document's editing is disabled
        /// </summary>
        public virtual bool ReadOnly {
            get;
            set;
        }

        /// <summary>
        /// Displays a dialog offering user to select whether he wants to reload the outside-VS changes
        /// </summary>
        public virtual void FileChangedOutsideVS() {
            IVsQueryEditQuerySave2 queryEditQuerySave = (IVsQueryEditQuerySave2)GetService(typeof(SVsQueryEditQuerySave));

            // ---Now call the QueryEdit method to find the edit status of this file
            string[] documents = { FileName };
            uint result;
            uint outFlags;

            int hr = queryEditQuerySave.QueryEditFiles(
              0, // Flags
              1, // Number of elements in the array
              documents, // Files to edit
              null, // Input flags
              null, // Input array of VSQEQS_FILE_ATTRIBUTE_DATA
              out result, // result of the checkout
              out outFlags // Additional flags
              );

            string message = FileName + Environment.NewLine +
                Environment.NewLine + "File was changed outside the environment. Do you want to reload it?";

            string title = String.Empty;
            IVsUIShell VsUiShell = (IVsUIShell)Package.GetGlobalService(typeof(SVsUIShell));

            int r = (int)DialogResult.No;
            Guid tempGuid = Guid.Empty;
            if (VsUiShell != null) {
                //Show up a message box indicating that the file has changed outside of VS environment
                VsUiShell.ShowMessageBox(0,
                                         ref tempGuid,
                                         title,
                                         message,
                                         null,
                                         0,
                                         OLEMSGBUTTON.OLEMSGBUTTON_YESNOCANCEL,
                                         OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                                         OLEMSGICON.OLEMSGICON_QUERY,
                                         0,
                                         out r);
            }
            //if the user selects "Yes", reload the current file
            if (r == (int)DialogResult.Yes) {
                ((IVsPersistDocData)this).ReloadDocData(0);
            }
        }

        /// <summary>
        /// Adds an undo unit to current document's undo stack
        /// </summary>        
        public virtual void AddUndoUnit(IOleUndoUnit undoUnit) {
            UndoManager.Add(undoUnit);
        }

        /// <summary>
        /// Clears current document's undo stack
        /// </summary>
        public virtual void ClearUndoRedoStack() {
            UndoManager.Enable(0);
            UndoManager.Enable(1);
        }
      
        /// <summary>
        /// Returns IOleUndoManager for this document's buffer
        /// </summary>
        public IOleUndoManager UndoManager {
            get {
                return (IOleUndoManager)GetService(typeof(IOleUndoManager));
            }
        }

        /// <summary>
        /// Returns status of the Paste command
        /// </summary>
        public virtual COMMAND_STATUS IsPasteSupported {
            get {
                return COMMAND_STATUS.UNSUPPORTED;
            }
        }

        /// <summary>
        /// Returns status of the Copy command
        /// </summary>
        public virtual COMMAND_STATUS IsCopySupported {
            get {
                return COMMAND_STATUS.UNSUPPORTED;
            }
        }

        /// <summary>
        /// Returns status of the Cut command
        /// </summary>
        public virtual COMMAND_STATUS IsCutSupported {
            get {
                return COMMAND_STATUS.UNSUPPORTED;
            }
        }

        /// <summary>
        /// Executes the Paste command
        /// </summary>
        /// <returns>True if the command was executed successfuly</returns>
        public virtual bool ExecutePaste() {
            return true;
        }

        /// <summary>
        /// Executes the Cut command
        /// </summary>
        /// <returns>True if the command was executed successfuly</returns>
        public virtual bool ExecuteCut() {
            return true;
        }

        /// <summary>
        /// Executes the Copy command
        /// </summary>
        /// <returns>True if the command was executed successfuly</returns>
        public virtual bool ExecuteCopy() {
            return true;
        }
    
    }

    /// <summary>
    /// Status of a VS command in the file editor
    /// </summary>
    public enum COMMAND_STATUS { 
        /// <summary>
        /// Command can be executed
        /// </summary>
        ENABLED, 

        /// <summary>
        /// Command is supported, but cannot be executed right now
        /// </summary>
        DISABLED, 

        /// <summary>
        /// Command is not supported
        /// </summary>
        UNSUPPORTED 
    };
}
