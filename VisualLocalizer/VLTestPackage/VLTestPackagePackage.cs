// VsPkg.cs : Implementation of VLTestPackage
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
using EnvDTE;

namespace OndrejStumpf.VLTestPackage
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the registration utility (regpkg.exe) that this class needs
    // to be registered as package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // A Visual Studio component can be registered under different regitry roots; for instance
    // when you debug your package you want to register it in the experimental hive. This
    // attribute specifies the registry root to use if no one is provided to regpkg.exe with
    // the /root switch.
    [DefaultRegistryRoot("Software\\Microsoft\\VisualStudio\\9.0")]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration(false, "#110", "#112", "1.0", IconResourceID = 400)]
    // In order be loaded inside Visual Studio in a machine that has not the VS SDK installed, 
    // package needs to have a valid load key (it can be requested at 
    // http://msdn.microsoft.com/vstudio/extend/). This attributes tells the shell that this 
    // package has a load key embedded in its resources.
    [ProvideLoadKey("Standard", "1.0", "Visual Localizer Test Package", "Ondřej Štumpf", 1)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidTestPackagePkgString)]
    public sealed class VLTestPackagePackage : Package
    {

        private DTE ideObject;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public VLTestPackagePackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
            ideObject = (DTE)Package.GetGlobalService(typeof(DTE));
        }



        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs) {
                // Create the command for the menu item.
                CommandID batchMoveCommand = new CommandID(GuidList.guidTestPackageCmdSet, (int)PkgCmdIDList.batchMoveMenuItem);
                OleMenuCommand batchMenuItem = new OleMenuCommand(MenuItemCallback, batchMoveCommand);
                mcs.AddCommand(batchMenuItem);

                CommandID topMenuCommand = new CommandID(GuidList.guidTestPackageCmdSet, (int)PkgCmdIDList.visualLocalizerTopMenu);
                OleMenuCommand topMenu = new OleMenuCommand(null, topMenuCommand);
                topMenu.BeforeQueryStatus += new EventHandler(topMenu_BeforeQueryStatus);
                mcs.AddCommand(topMenu);


                CommandID codeMenuCommand = new CommandID(GuidList.guidTestPackageCmdSet, (int)PkgCmdIDList.visualLocalizerCodeMenu);
                OleMenuCommand codeMenu = new OleMenuCommand(null, codeMenuCommand);
                mcs.AddCommand(codeMenu);
            }
        }

        void topMenu_BeforeQueryStatus(object sender, EventArgs e) {
            EnvDTE.UIHierarchy uih = (UIHierarchy)ideObject.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Object;

            Array selectedItems = (Array)uih.SelectedItems;
            bool ok = true;
            foreach (UIHierarchyItem o in selectedItems) {
                if (o.Object is ProjectItem) {
                    ProjectItem item = (ProjectItem)o.Object;
                    for (short i = 0; i < item.FileCount; i++)
                        ok = ok && item.get_FileNames(i).EndsWith(".cs");
                }
                Trace.WriteLine(Microsoft.VisualBasic.Information.TypeName(o.Object));
            }

            (sender as OleMenuCommand).Visible = ok;
        }
    
        private void MenuItemCallback(object sender, EventArgs e) {
            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));

            Guid clsid = Guid.Empty;
            int result;
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "command: {0}", this.ToString()));

            uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       "Testing Package",
                       string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.ToString()),
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       OLEMSGICON.OLEMSGICON_INFO,
                       0,        // false
                       out result);
        }
    }
}