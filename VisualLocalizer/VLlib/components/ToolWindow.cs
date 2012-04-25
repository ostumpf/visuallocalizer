using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace VisualLocalizer.Library {
    public class ToolWindow<T> : ToolWindowPane 
        where T:UserControl,new() {
        
        private IWin32Window window;

        public ToolWindow()
            : base(null) {
            window = new T();
        }

        public override System.Windows.Forms.IWin32Window Window {
            get {
                return window;
            }
        }
    }
}
