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
using VisualLocalizer.Library.Components;


namespace VisualLocalizer {
    /// <summary>
    /// Base class for registration in host VS environment.
    /// </summary>

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [DefaultRegistryRoot("Software\\Microsoft\\VisualStudio\\9.0")]    

    // registers product info - name, icon, description (visible in Help/About)
    [InstalledProductRegistration(true, "#110", "#112", "1.0", IconResourceID = 400,LanguageIndependentName="Visual Localizer")]
    
    // necessary in order to run in hosting environment
    [ProvideLoadKey("Standard", "1.0", "Visual Localizer", "Ondrej Stumpf", 111)]
    
    // registers menu items
    [ProvideMenuResource(1000, 1)]

    // this GUID tells VS to load the package on startup
    [ProvideAutoLoad("{f1536ef8-92ec-443c-9ed7-fdadf150da82}")]  

    // creates "Visual Localizer" output pane in "Output" window
    [ProvideOutputWindow(typeof(VisualLocalizerPackage), typeof(Guids.VisualLocalizerWindowPane), "#110", ClearWithSolution=true,InitiallyInvisible=false)]
    
    // registers single-view editor of ResX files
    [ProvideEditorFactory(typeof(ResXEditorFactory), 113, TrustLevel = __VSEDITORTRUSTLEVEL.ETL_AlwaysTrusted)]
    [ProvideEditorExtension(typeof(ResXEditorFactory), StringConstants.ResXExtension, 100)]
    [ProvideEditorLogicalView(typeof(ResXEditorFactory), "58F7A940-4755-4382-BCA6-ED89F035491E")]
    
    // registers tool window displayed on "Batch move to resources" command
    [ProvideToolWindow(typeof(BatchMoveToResourcesToolWindow),MultiInstances=false,
        Style = VsDockStyle.Tabbed, Orientation = ToolWindowOrientation.Bottom, Window = ToolWindowGuids.Outputwindow)]

    // registers tool window displayed on "Batch inline" command
    [ProvideToolWindow(typeof(BatchInlineToolWindow), MultiInstances = false, 
        Style = VsDockStyle.Tabbed, Orientation = ToolWindowOrientation.Bottom, Window = ToolWindowGuids.Outputwindow)]

    // registers tool window used by the "Show references" command in ResX editor
    [ProvideToolWindow(typeof(ShowReferencesToolWindow), MultiInstances = false,
        Style = VsDockStyle.Tabbed, Orientation = ToolWindowOrientation.Bottom, Window = ToolWindowGuids.Outputwindow)]

    // registers settings (necessary in order to Import/Export settings work) - contains descriptions and names of nodes
    [ProvideProfile(typeof(GeneralSettingsManager), "VisualLocalizer", "GeneralSettings", 114, 115, false, DescriptionResourceID = 116)]
    [ProvideProfile(typeof(FilterSettingsManager), "VisualLocalizer", "FilterSettings", 117, 118, true, DescriptionResourceID = 119,
        AlternateParent = "VisualLocalizer_GeneralSettings")]
    [ProvideProfile(typeof(EditorSettingsManager), "VisualLocalizer", "Editor", 127, 128, true, DescriptionResourceID = 129,
        AlternateParent = "VisualLocalizer_GeneralSettings")]    
    
    // registers option pages accessible in Tools/Options
    [ProvideOptionPage(typeof(FilterSettingsManager), "VisualLocalizer", "BatchToolWindows", 123, 124, false)]    
    [ProvideOptionPage(typeof(EditorSettingsManager), "VisualLocalizer", "Editor", 123, 126, false)]

    [Guid("68c95c48-9295-49a0-a2ed-81da6e651374")]
    public sealed class VisualLocalizerPackage : Package,IVsInstalledProduct
    {
        internal MenuManager menuManager;
        internal EnvDTE80.DTE2 DTE;
        internal EnvDTE.UIHierarchy UIHierarchy;
        internal OleMenuCommandService menuService;        
       
        private static VisualLocalizerPackage instance;

        /// <summary>
        /// Run once on package startup.
        /// </summary>
        protected override void Initialize() {
            instance = this;
                        
            try {
                base.Initialize();

                ActivityLogger.Source = "Visual Localizer";                
                VLOutputWindow.VisualLocalizerPane.WriteLine("Visual Localizer is being initialized...");
                           
                InitBaseServices();
                
                // load settings from registry
                new GeneralSettingsManager().LoadSettingsFromStorage();

                // register handlers for menu items
                menuManager = new MenuManager();

                // register as an editor for ResX files
                RegisterEditorFactory(new ResXEditorFactory());
                
                VLOutputWindow.VisualLocalizerPane.WriteLine("Initialization completed");
                VLOutputWindow.General.WriteLine("Visual Localizer is up and running");
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Obtains services essential for the package to work properly
        /// </summary>
        private void InitBaseServices() {                       
            DTE = (EnvDTE80.DTE2)GetService(typeof(EnvDTE.DTE));
            try {
                UIHierarchy = (EnvDTE.UIHierarchy)DTE.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Object;
            } catch {
                UIHierarchy = null;
            }

            menuService = (OleMenuCommandService)GetService(typeof(IMenuCommandService));
            
            if (DTE == null || menuService == null)
                throw new Exception("Error during initialization of base services.");
        }
       
        /// <summary>
        /// Returns instance of this package
        /// </summary>
        public static VisualLocalizerPackage Instance {
            get {
                return instance;
            }
        }
        
        private static VS_VERSION? version = null;

        /// <summary>
        /// Returns version of hosting Visual Studio instance, calculated from registry values
        /// </summary>
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
                        version = VS_VERSION.VS2012;
                    } else {
                        version = VS_VERSION.UNKNOWN;
                    }
                }
                return version.Value;
            }
        }

        #region IVsInstalledProduct
           
        /// <summary>
        /// Returns ID of resource containing this project's logo displayed in the splash screen
        /// </summary>        
        public int IdBmpSplash(out uint pIdBmp) {
            pIdBmp = 400;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns ID of resource containing this project's logo displayed in the about box
        /// </summary> 
        public int IdIcoLogoForAboutbox(out uint pIdIco) {
            pIdIco = 400;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns name of this tool, displayed in the splash screen and about box
        /// </summary>        
        public int OfficialName(out string pbstrName) {
            pbstrName = "Visual Localizer";
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns description displayed in the about box
        /// </summary>        
        public int ProductDetails(out string pbstrProductDetails) {
            pbstrProductDetails = "Tool for advanced manipulation with resources.";
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns product version
        /// </summary>        
        public int ProductID(out string pbstrPID) {
            pbstrPID = "1.3";
            return VSConstants.S_OK;
        }

        #endregion
    }

    /// <summary>
    /// Possible versions of Visual Studio
    /// </summary>
    public enum VS_VERSION { 
        /// <summary>
        /// Microsoft Visual Studio 2008
        /// </summary>
        VS2008,

        /// <summary>
        /// Microsoft Visual Studio 2010
        /// </summary>
        VS2010,

        /// <summary>
        /// Microsoft Visual Studio 2012
        /// </summary>
        VS2012,

        /// <summary>
        /// Not possible to determine Visual Studio version
        /// </summary>
        UNKNOWN 
    }
}