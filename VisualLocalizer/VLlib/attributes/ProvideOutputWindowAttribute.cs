using System;
using Microsoft.VisualStudio.Shell;

/// Contains attributes classes, assigneable to a package class and used by the RegPkg utilitiy
namespace VisualLocalizer.Library.Attributes {
    
    /// <summary>
    /// When applied on IVsPackage implementation class, registers new output window pane in VS Output window.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true,Inherited=true)]
    public sealed class ProvideOutputWindowAttribute : RegistrationAttribute {

        /// <summary>
        /// Attribute initialization
        /// </summary>
        /// <param name="package">Type of the package that requests registration (with defined GUID)</param>
        /// <param name="outputWindowGuid">GUID-carrying type of the output window pane</param>
        /// <param name="showOutputFromText">Window pane name</param>
        public ProvideOutputWindowAttribute(Type package, Type outputWindowGuid, string showOutputFromText) {
            if (package == null) throw new ArgumentNullException("package");
            if (outputWindowGuid == null) throw new ArgumentNullException("outputWindowGuid");
            if (showOutputFromText == null) throw new ArgumentNullException("showOutputFromText");

            this.Package = package;
            this.OutputWindowGuid = outputWindowGuid;
            this.ShowOutputFromText = showOutputFromText;
            this.Name = "";
            this.InitiallyInvisible = false;
            this.ClearWithSolution = false;
        }

        /// <summary>
        /// Registers the output window pane in \OutputWindow registry subkey of VS installation
        /// </summary>        
        public override void Register(RegistrationContext context) {
            Key key = null;
            try {
                key = context.CreateKey(String.Format(@"OutputWindow\{0}", OutputWindowGuid.GUID.ToString("B")));
                if (!string.IsNullOrEmpty(Name)) key.SetValue("Name", Name);
                key.SetValue("Package", Package.GUID.ToString("B"));
                key.SetValue("InitiallyInvisible", InitiallyInvisible ? 1:0);
                key.SetValue("ClearWithSolution", ClearWithSolution? 1:0);
                key.SetValue(null, ShowOutputFromText);
            } finally {
                if (key != null) key.Close();
            }
        }

        /// <summary>
        /// Unregister the window (remove key)
        /// </summary>        
        public override void Unregister(RegistrationContext context) {
            context.RemoveKey(String.Format(@"OutputWindow\{0}", OutputWindowGuid.GUID.ToString("B")));
        }

        /// <summary>
        /// Type of window owning package
        /// </summary>
        private Type Package {
            get;
            set;
        }

        /// <summary>
        /// Name - not visible to the user (use ShowOutputFromText)
        /// </summary>
        public string Name {
            get;
            set;
        }

        /// <summary>
        /// True if the window pane should be cleared when solution is closed
        /// </summary>
        public bool ClearWithSolution {
            get;
            set;
        }

        /// <summary>
        /// True if the window pane is invisible until activated
        /// </summary>
        public bool InitiallyInvisible {
            get;
            set;
        }

        /// <summary>
        /// Text that appears in output window combo box with other output window panes
        /// </summary>
        private string ShowOutputFromText {
            get;
            set;
        }

        /// <summary>
        /// GUID of the window pane
        /// </summary>
        private Type OutputWindowGuid {
            get;
            set;
        }
    }
}
