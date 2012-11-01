using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using VisualLocalizer.Library.Attributes;
using VisualLocalizer.Editor;
using VisualLocalizer.Library;
using VisualLocalizer.Commands;
using VisualLocalizer.Components;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;
using VisualLocalizer.Gui;
using VisualLocalizer.Settings;

namespace VisualLocalizer
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [DefaultRegistryRoot("Software\\Microsoft\\VisualStudio\\9.0")]
    [InstalledProductRegistration(false, "#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideLoadKey("Standard", "1.0", "Visual Localizer", "Ondrej Stumpf", 111)]
    [ProvideMenuResource(1000, 1)]
    [ProvideAutoLoad("{f1536ef8-92ec-443c-9ed7-fdadf150da82}")]  
    [ProvideOutputWindow(typeof(VisualLocalizerPackage),
        typeof(Guids.VisualLocalizerWindowPane),
        "#110",
        ClearWithSolution=true,InitiallyInvisible=false)]
    [ProvideEditorFactory(typeof(ResXEditorFactory), 113, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorExtension(typeof(ResXEditorFactory), StringConstants.ResXExtension, 100)]
    [ProvideEditorLogicalView(typeof(ResXEditorFactory), "58F7A940-4755-4382-BCA6-ED89F035491E")]
    [ProvideToolWindow(typeof(BatchMoveToResourcesToolWindow),MultiInstances=false,
        Style = VsDockStyle.Tabbed, Orientation = ToolWindowOrientation.Bottom, Window = ToolWindowGuids.Outputwindow)]
    [ProvideToolWindow(typeof(BatchInlineToolWindow), MultiInstances = false, 
        Style = VsDockStyle.Tabbed, Orientation = ToolWindowOrientation.Bottom, Window = ToolWindowGuids.Outputwindow)]
    
    [ProvideProfile(typeof(GeneralSettingsManager), "VisualLocalizer", "GeneralSettings", 114, 115, false, DescriptionResourceID = 116)]
    [ProvideProfile(typeof(FilterSettingsManager), "VisualLocalizer", "FilterSettings", 117, 118, true, DescriptionResourceID = 119,
        AlternateParent = "VisualLocalizer_GeneralSettings")]
    [ProvideProfile(typeof(EditorSettingsManager), "VisualLocalizer", "Editor", 127, 128, true, DescriptionResourceID = 129,
        AlternateParent = "VisualLocalizer_GeneralSettings")]    
    [ProvideOptionPage(typeof(FilterSettingsManager), "VisualLocalizer", "BatchToolWindows", 123, 124, false)]    
    [ProvideOptionPage(typeof(EditorSettingsManager), "VisualLocalizer", "Editor", 123, 126, false)]

    [Guid("68c95c48-9295-49a0-a2ed-81da6e651374")]
    public sealed class VisualLocalizerPackage : Package
    {
        internal MenuManager menuManager;
        internal EnvDTE80.DTE2 DTE;
        internal EnvDTE.UIHierarchy UIHierarchy;
        internal OleMenuCommandService menuService;
        internal IVsUIShell uiShell;
        internal IVsRunningDocumentTable ivsRunningDocumentTable;
        internal IVsUIHierWinClipboardHelper clipboardHelper;

        private static VisualLocalizerPackage instance;

        protected override void Initialize() {
            instance = this;
                        
            try {
                base.Initialize();

                ActivityLogger.Source = "Visual Localizer";                
                VLOutputWindow.VisualLocalizerPane.WriteLine("Visual Localizer is being initialized...");
                           
                InitBaseServices();
                new GeneralSettingsManager().LoadSettingsFromStorage();

                menuManager = new MenuManager();
                RegisterEditorFactory(new ResXEditorFactory());
               
                VLOutputWindow.VisualLocalizerPane.WriteLine("Initialization completed");
                VLOutputWindow.General.WriteLine("Visual Localizer is up and running");
            } catch (Exception ex) {
                MessageBox.ShowError(string.Format("{1} during initialization: {0}\n{2}", ex.Message, ex.GetType(), ex.StackTrace));
            }
        }

        private void InitBaseServices() {
            DTE = (EnvDTE80.DTE2)GetService(typeof(EnvDTE.DTE));            
            UIHierarchy = (EnvDTE.UIHierarchy)DTE.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Object;
            menuService = (OleMenuCommandService)GetService(typeof(IMenuCommandService));
            uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            ivsRunningDocumentTable = (IVsRunningDocumentTable)GetService(typeof(SVsRunningDocumentTable));
            clipboardHelper = (IVsUIHierWinClipboardHelper)GetService(typeof(SVsUIHierWinClipboardHelper));

            if (DTE == null || UIHierarchy == null || menuService == null || uiShell == null || ivsRunningDocumentTable == null || clipboardHelper==null)
                throw new Exception("Error during initialization of base services.");
        }
       
        public static VisualLocalizerPackage Instance {
            get {
                return instance;
            }
        }

        private static VS_VERSION? version = null;
        public static VS_VERSION VisualStudioVersion {
            get {
                if (!version.HasValue) {
                    IVsShell shell = (IVsShell)Package.GetGlobalService(typeof(SVsShell));
                    object o;
                    int hr = shell.GetProperty((int)__VSSPROPID2.VSSPROPID_SqmRegistryRoot, out o);
                    Marshal.ThrowExceptionForHR(hr);
                    string registry = o.ToString();

                    if (registry.EndsWith("9.0\\SQM")) {
                        version = VS_VERSION.VS2008;
                    } else if (registry.EndsWith("10.0\\SQM")) {
                        version = VS_VERSION.VS2010;
                    } else if (registry.EndsWith("11.0\\SQM")) {
                        version = VS_VERSION.VS2011;
                    } else {
                        version = VS_VERSION.UNKNOWN;
                    }
                }
                return version.Value;
            }
        }
    }

    public enum VS_VERSION { VS2008, VS2010, VS2011, UNKNOWN }
}