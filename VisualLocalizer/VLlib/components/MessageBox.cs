using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VisualLocalizer.Library {
    public static class MessageBox {

        static MessageBox() {
            UIShell = (IVsUIShell)Package.GetGlobalService(typeof(SVsUIShell));
        }

        public static IVsUIShell UIShell {
            get;
            private set;
        }

        public static DialogResult Show(string message) {
            return Show(message, null, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_INFO);
        }

        public static DialogResult Show(string message,string title) {
            return Show(message, title, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_INFO);
        }

        public static DialogResult ShowError(string message) {
            return Show(message, null, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_CRITICAL);
        }

        public static DialogResult Show(string message,string title,OLEMSGBUTTON buttons,OLEMSGDEFBUTTON defaultButton,OLEMSGICON icon) {
            if (UIShell == null)
                throw new InvalidOperationException("MessageBox is not sufficiently initialized.");

            int result;
            Guid g = Guid.Empty;
            int hr = UIShell.ShowMessageBox(0, ref g, title, message, null, 0, buttons, defaultButton, icon, 1, out result);
            Marshal.ThrowExceptionForHR(hr);

            return ConvertFromIntToDialogResult(result);
        }

        private static DialogResult ConvertFromIntToDialogResult(int code) {
            switch (code) {
                case 1:
                    return DialogResult.OK;
                case 2:
                    return DialogResult.Cancel;
                case 3:
                    return DialogResult.Abort;
                case 4:
                    return DialogResult.Retry;
                case 5:
                    return DialogResult.Ignore;
                case 6:
                    return DialogResult.Yes;
                default:
                    return DialogResult.No;
            }
        }
    }
}
