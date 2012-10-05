using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio;
using System.IO;

namespace VisualLocalizer.Library {

    public static class OPENFILENAME {
        public static uint OFN_ALLOWMULTISELECT = 0x00000200;
        public static uint OFN_CREATEPROMPT = 0x00002000;
        public static uint OFN_DONTADDTORECENT = 0x02000000;
        public static uint OFN_FILEMUSTEXIST = 0x00001000;
        public static uint OFN_FORCESHOWHIDDEN = 0x10000000;
        public static uint OFN_OVERWRITEPROMPT = 0x00000002;
        public static uint OFN_PATHMUSTEXIST = 0x00000800;
        public static uint OFN_READONLY=0x00000001;
        public static uint OFN_SHAREAWARE = 0x00004000;
        public static uint OFN_SHOWHELP = 0x00000010;
        public static uint OFN_ENABLESIZING = 0x00800000;
        public static uint OFN_NOCHANGEDIR = 0x00000008;
        public static uint OFN_NONETWORKBUTTON = 0x00020000;
    }


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

        public static string[] SelectFilesViaDlg(string title,string initialDirectory, string filter,uint filterIndex,uint flags) {
            uint buffersize = 255;
            
            VSOPENFILENAMEW o = new VSOPENFILENAMEW();
            o.dwFlags = flags;
            o.pwzInitialDir = initialDirectory;
            o.pwzFilter = filter;
            o.pwzDlgTitle = title;
            o.nFilterIndex = filterIndex;
            o.nMaxFileName = buffersize;
            o.lStructSize = (uint)Marshal.SizeOf(typeof(VSOPENFILENAMEW));            
            o.pwzFileName = Marshal.StringToBSTR(new string('\0', (int)buffersize));
            
            IntPtr dialogOwner;
            int hr = UIShell.GetDialogOwnerHwnd(out dialogOwner);
            Marshal.ThrowExceptionForHR(hr);

            o.hwndOwner = dialogOwner;                        

            VSOPENFILENAMEW[] arr=new VSOPENFILENAMEW[1] {o};
            hr = UIShell.GetOpenFileNameViaDlg(arr);
            if (hr == VSConstants.OLE_E_PROMPTSAVECANCELLED) return null;
            Marshal.ThrowExceptionForHR(hr);

            string returnedData=Marshal.PtrToStringBSTR(arr[0].pwzFileName);
            string[] tokens = returnedData.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length <= 1) throw new Exception("Unexpected OpenFileDialog result.");

            string directory = tokens[0];
            string[] ret = new string[tokens.Length - 1];

            for (int i = 1; i < tokens.Length; i++) {
                ret[i - 1] = Path.Combine(directory, tokens[i]);
            }

            return ret;
        }
    }
}
