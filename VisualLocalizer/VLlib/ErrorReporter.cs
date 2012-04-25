using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;

namespace VisualLocalizer.Library {
    public static class ErrorReporter {

        private static IVsUIShell uiShell;

        static ErrorReporter() {
            uiShell = (IVsUIShell)Package.GetGlobalService(typeof(SVsUIShell));
        }

        public static void Report(int hr) {
            if (hr == VSConstants.S_OK) hr = VSConstants.E_UNEXPECTED;
            Report(hr, "Operation cannot be completed.");
        }

        public static void Report(string message, params object[] args) {
            Report(VSConstants.E_UNEXPECTED, message, args);
        }

        public static void Report(int hr, string message, params object[] args) {
            uiShell.SetErrorInfo(hr, string.Format(message,args), 0, null, null);
            uiShell.ReportErrorInfo(hr);
        }
    }
}
