using System;
using Microsoft.VisualStudio.Shell;

namespace VisualLocalizer.Library.Attributes {
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true,Inherited=true)]
    public sealed class ProvideOutputWindowAttribute : RegistrationAttribute {

        public ProvideOutputWindowAttribute(Type package, Type outputWindowGuid, string showOutputFromText) {
            this.Package = package;
            this.OutputWindowGuid = outputWindowGuid;
            this.ShowOutputFromText = showOutputFromText;
            this.Name = "";
            this.InitiallyInvisible = false;
            this.ClearWithSolution = false;
        }

        public override void Register(RegistrationContext context) {
            Key key = null;
            try {
                key = context.CreateKey(String.Format(@"OutputWindow\{0}", OutputWindowGuid.GUID.ToString("B")));
                key.SetValue("Name", Name);
                key.SetValue("Package", Package.GUID.ToString("B"));
                key.SetValue("InitiallyInvisible", InitiallyInvisible ? 1:0);
                key.SetValue("ClearWithSolution", ClearWithSolution? 1:0);
                key.SetValue(null, ShowOutputFromText);
            } finally {
                if (key != null) key.Close();
            }
        }

        public override void Unregister(RegistrationContext context) {
            context.RemoveKey(String.Format(@"OutputWindow\{0}", OutputWindowGuid.GUID.ToString("B")));
        }

        private Type Package {
            get;
            set;
        }

        public string Name {
            get;
            set;
        }

        public bool ClearWithSolution {
            get;
            set;
        }

        public bool InitiallyInvisible {
            get;
            set;
        }

        private string ShowOutputFromText {
            get;
            set;
        }

        private Type OutputWindowGuid {
            get;
            set;
        }
    }
}
