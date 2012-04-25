using System;
using Microsoft.VisualStudio.Shell;

namespace VisualLocalizer.Library.Attributes {
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true,Inherited=true)]
    public sealed class ProvideOutputWindowAttribute : RegistrationAttribute {

        public override void Register(RegistrationContext context) {
            Key key = null;
            try {
                key = context.CreateKey(String.Format(@"OutputWindow\{{{0}}}", OutputWindowGuid));
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
            context.RemoveKey(String.Format(@"OutputWindow\{{{0}}}", OutputWindowGuid));
        }

        public Type Package {
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

        public string ShowOutputFromText {
            get;
            set;
        }

        public string OutputWindowGuid {
            get;
            set;
        }
    }
}
