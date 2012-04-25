using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VisualLocalizer.Library;

namespace OndrejStumpf.VLTestingPackage {

    [Guid("B8CCEAF7-D0A3-4616-9166-957994728A6C")]
    class MyToolWindow : ToolWindow<MyToolWindowForm> {
        public MyToolWindow() {
            this.Caption = "My Tool Window Caption text";
        }
    }   

    class MyToolWindowForm : UserControl {
        public MyToolWindowForm() {
            Button b = new Button();
            b.Text = "TLACITKO";
            Controls.Add(b);
        }
    }

 }
