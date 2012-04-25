using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;

namespace OndrejStumpf.VisualLocalizer
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [DefaultRegistryRoot("Software\\Microsoft\\VisualStudio\\9.0")]
    [InstalledProductRegistration(false, "#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideLoadKey("Standard", "1.0", "Visual Localizer", "Ondřej Štumpf", 1)]
    [Guid(GuidList.guidVisualLocalizerPkgString)]
    public sealed class VisualLocalizerPackage : Package
    {
        public VisualLocalizerPackage() {

        }

        protected override void Initialize()
        {
            base.Initialize();
        }
       
    }
}