using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;

namespace VisualLocalizer.Library.Attributes {

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ProvideMarkerAttribute : RegistrationAttribute {        

        public override void Register(RegistrationAttribute.RegistrationContext context) {
            Key key = null;
            try {
                key = context.CreateKey(String.Format(@"Text Editor\External Markers\{{{0}}}", MarkerGuid));
                key.SetValue("", DisplayName);
                key.SetValue("DisplayName", DisplayName);
                key.SetValue("Package", Package.GUID.ToString("B"));
                key.SetValue("Service", Service.GUID.ToString("B"));
            } finally {
                if (key != null) key.Close();
            }
        }

        public override void Unregister(RegistrationAttribute.RegistrationContext context) {
            context.RemoveKey(String.Format(@"Text Editor\External Markers\{{{0}}}", MarkerGuid));
        }

        public string DisplayName {
            get;
            set;
        }

        public Type Package {
            get;
            set;
        }

        public Type Service {
            get;
            set;
        }

        public string MarkerGuid {
            get;
            set;
        }
    }
}
