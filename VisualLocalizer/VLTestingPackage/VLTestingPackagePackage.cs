// VsPkg.cs : Implementation of VLTestingPackage
//

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using System.Reflection;
using Microsoft.VisualStudio;
using System.Windows.Forms;
using Microsoft.VisualStudio.TextManager.Interop;
using System.IO;
using System.Security.AccessControl;
using VisualLocalizer.Library.Attributes;
using EnvDTE;
using Microsoft.VisualStudio.Package;

namespace OndrejStumpf.VLTestingPackage
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [DefaultRegistryRoot("Software\\Microsoft\\VisualStudio\\9.0")]
    [InstalledProductRegistration(false, "#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideLoadKey("Standard", "1.0", "Visual Localizer Testing Package", "Ondrej Stumpf", 113)]
    
    [ProvideMenuResource(1000, 1)]
    [ProvideOutputWindow(ClearWithSolution = true, InitiallyInvisible = false, Name = "VL",
        ShowOutputFromText = "#111", Package = typeof(VLTestingPackagePackage), 
        OutputWindowGuid = GuidList.guidVLTestingPackageOutputWindow)]
    [ProvideAutoLoad("{f1536ef8-92ec-443c-9ed7-fdadf150da82}")]    
    
    [ProvideService(typeof(MarkerService),ServiceName="VL testing marker service")]
    [ProvideMarker(DisplayName="VL testing marker",Package=typeof(VLTestingPackagePackage),
        Service=typeof(MarkerService),MarkerGuid=GuidList.guidVLTestingPackageMarker)]

    [ProvideEditorFactory(typeof(MyEditorFactory), 200, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorExtension(typeof(MyEditorFactory), ".resx", 100)]
    [ProvideEditorLogicalView(typeof(MyEditorFactory), GuidList.guidVLTestingPackageLogicalView)]

    [ProvideToolWindow(typeof(MyToolWindow),Style=VsDockStyle.Tabbed,Orientation=ToolWindowOrientation.Bottom,Window=ToolWindowGuids.Outputwindow)]
  //  [ProvideToolWindowVisibility(typeof(MyToolWindow),"")]

    [Guid("4d84a08f-4147-4224-8a05-96b47e9d5f6a")]
    public sealed class VLTestingPackagePackage : Package,IVsSelectionEvents
    {
        
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public VLTestingPackagePackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
            
           
        }
        private EnvDTE.DTE ideObject;
        private EnvDTE.UIHierarchy uih;
        private EnvDTE.CommandEvents cmdPaste;
        private MarkerService markerService;
        private MyEditorFactory editor;

        protected override void Initialize()
        {
            Trace.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            
            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            // initialize marker service and lookup id assigned to my marker
            markerService = new MarkerService();
            Guid g=typeof(MarkerService).GUID;
            ((IServiceContainer)this).AddService(markerService.GetType(), markerService, true);

            IVsTextManager textManager = (IVsTextManager)GetService(typeof(SVsTextManager));
            int markerTypeID;
            Guid guid = new Guid(GuidList.guidVLTestingPackageMarker);
            int hr = textManager.GetRegisteredMarkerTypeID(ref guid, out markerTypeID);
            markerService.FormatMarker.Id = markerTypeID;

            
            if ( null != mcs )
            {

                ideObject = (EnvDTE.DTE)GetService(typeof(EnvDTE.DTE));
                uih = (UIHierarchy)ideObject.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Object;

                // track tab change
                IVsMonitorSelection s = GetService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
                
                uint u;
                s.AdviseSelectionEvents(this, out u);

               editor = new MyEditorFactory();
                RegisterEditorFactory(editor);
   
                // MUST BE SAVED, OR GC EATS IT!!!
                Command pastecmd = ideObject.Commands.Item("Edit.Paste", -1);                
                cmdPaste = ideObject.Events.get_CommandEvents(pastecmd.Guid, pastecmd.ID);                
                cmdPaste.BeforeExecute += new _dispCommandEvents_BeforeExecuteEventHandler(cmdPaste_BeforeExecute);                                

                CommandID batchMoveCommand = new CommandID(GuidList.guidVLTestingPackageCmdSet, (int)PkgCmdIDList.batchMoveMenuItem);                
                OleMenuCommand batchMenuItem = new OleMenuCommand(MenuItemCallback, batchMoveCommand);     
                  
                mcs.AddCommand(batchMenuItem);

                CommandID topMenuCommand = new CommandID(GuidList.guidVLTestingPackageCmdSet, (int)PkgCmdIDList.visualLocalizerTopMenu);
                OleMenuCommand topMenu = new OleMenuCommand(null, topMenuCommand);
                topMenu.BeforeQueryStatus += new EventHandler(topMenu_BeforeQueryStatus);
                mcs.AddCommand(topMenu);

                CommandID inlineCommand = new CommandID(GuidList.guidVLTestingPackageCmdSet, (int)PkgCmdIDList.inlineMenuItem);
                OleMenuCommand inlineItem = new OleMenuCommand(MarkerCallback, inlineCommand);
                mcs.AddCommand(inlineItem);

                CommandID codeMenuCommand = new CommandID(GuidList.guidVLTestingPackageCmdSet, (int)PkgCmdIDList.visualLocalizerCodeMenu);
                OleMenuCommand codeMenu = new OleMenuCommand(null, codeMenuCommand);
                codeMenu.BeforeQueryStatus += new EventHandler(codeMenu_BeforeQueryStatus);
                mcs.AddCommand(codeMenu);

                CommandID showToolCommand = new CommandID(GuidList.guidVLTestingPackageCmdSet, (int)PkgCmdIDList.showToolWindowItem);
                OleMenuCommand showToolItem = new OleMenuCommand(ShowCallback, showToolCommand);
                mcs.AddCommand(showToolItem);

            }

          
        }


        private void ShowCallback(object sender, EventArgs e) {
            ToolWindowPane pane=FindToolWindow(typeof(MyToolWindow), 0, true);
            
            if (pane != null && pane.Frame != null) {
                ((IVsWindowFrame)pane.Frame).SetProperty((int)__VSFPROPID.VSFPROPID_IsWindowTabbed, true);
                ((IVsWindowFrame)pane.Frame).SetProperty((int)__VSFPROPID.VSFPROPID_FrameMode,VSFRAMEMODE.VSFM_Dock);
                ((IVsWindowFrame)pane.Frame).Show();                
            }
        }

        void cmdPaste_BeforeExecute(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault) {
            Trace.WriteLine(Clipboard.GetText());

            VisualLocalizer.Library.OutputWindow.Build.OutputTaskItemString("moje zprava",
                VSTASKPRIORITY.TP_HIGH, VSTASKCATEGORY.CAT_MISC, "", 0, "", 10, "nejaky text");
            

            VisualLocalizer.Library.OutputWindow.GetActivatedPane(GuidList.guidVLTestingPackageOutputWindow).
                OutputString("PASTE: "+Clipboard.GetText()+"\n");
        }


        void codeMenu_BeforeQueryStatus(object sender, EventArgs e) {
            TextSelection selection = (TextSelection)ideObject.ActiveDocument.Selection;
            
            (sender as OleMenuCommand).Supported = ideObject.ActiveDocument.Name.ToLowerInvariant().EndsWith("cs");
        }

      

        string type(object o) {
            return Microsoft.VisualBasic.Information.TypeName(o);
        }

        void topMenu_BeforeQueryStatus(object sender, EventArgs e) {
            Array selectedItems = (Array)uih.SelectedItems;
            bool ok = true;
            foreach (UIHierarchyItem o in selectedItems) {
                if (o.Object is ProjectItem) {
                    ProjectItem item = (ProjectItem)o.Object;
                    for (short i = 0; i < item.FileCount; i++)
                        ok = ok && item.get_FileNames(i).ToLowerInvariant().EndsWith(".cs");
                } else if (o.Object is Project) {
                    Project proj = (Project)o.Object;
                    ok = ok && proj.Kind == VSLangProj.PrjKind.prjKindCSharpProject;
                }
                Trace.WriteLine(Microsoft.VisualBasic.Information.TypeName(o.Object));
            }

            (sender as OleMenuCommand).Visible = ok;
        }

        private void MarkerCallback(object sender, EventArgs e) {
            IVsTextManager textManager = (IVsTextManager)GetService(typeof(SVsTextManager));
            
            IVsTextView textView;
            textManager.GetActiveView(1, null, out textView);
                 
            IVsTextLines textLines;                        
            textView.GetBuffer(out textLines);            
            
            TextSpan[] spans=new TextSpan[1];
            textView.GetSelectionSpan(spans);            

            textLines.CreateLineMarker(MarkerService.Instance.FormatMarker.Id,
                                            spans[0].iStartLine,
                                            spans[0].iStartIndex,
                                            spans[0].iEndLine,
                                            spans[0].iEndIndex,
                                            null,
                                            null);
            
            // adding to the list
           /* Project p = ideObject.ActiveDocument.ProjectItem.ContainingProject;
            string projectDirectory=Path.GetDirectoryName(p.FileName);
            string vlDir = Path.Combine(projectDirectory, "VL");
            if (!Directory.Exists(vlDir)) {
                Directory.CreateDirectory(vlDir);
            }
            string vlFile = Path.Combine(vlDir, ideObject.ActiveDocument.Name);
            if (!File.Exists(vlFile)) {
                
            }*/

            // removing 
            /*
            IVsTextLineMarker marker;
            textLines.FindMarkerByLineIndex(markerTypeID, line, column, (uint)FINDMARKERFLAGS.FM_FORWARD, out marker);
            marker.Invalidate();*/            
           
        }

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       "Visual Localizer Testing Package",
                       string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback(), coming from GAC: {1}", 
                        this.ToString(), Assembly.GetExecutingAssembly().GlobalAssemblyCache),
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       OLEMSGICON.OLEMSGICON_INFO,
                       0,        // false
                       out result));
        }




        public int OnCmdUIContextChanged(uint dwCmdUICookie, int fActive) {
            

            return VSConstants.S_OK;
        }

        public int OnElementValueChanged(uint elementid, object varValueOld, object varValueNew) {
          //  Trace.WriteLine("Tab changed");
            return VSConstants.S_OK;
        }

        public int OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew) {
            
            return VSConstants.S_OK;
        }
    }

   
}