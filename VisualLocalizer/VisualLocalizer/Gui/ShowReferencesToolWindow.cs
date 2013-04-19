using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using VisualLocalizer.Components;
using Microsoft.VisualStudio.TextManager.Interop;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.Shell.Interop;
using VisualLocalizer.Commands;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.OLE.Interop;

namespace VisualLocalizer.Gui {

    /// <summary>
    /// Represents "Show references" invokeable from ResX editor toolwindow
    /// </summary>
    [Guid("93C7C5D5-111D-4492-8E8E-F54ACD1F9A7F")]
    internal sealed class ShowReferencesToolWindow : AbstractCodeToolWindow<BatchInlineToolGrid> {
        
        /// <summary>
        /// Creates new instance
        /// </summary>
        public ShowReferencesToolWindow() {
            this.Caption = "Show references - Visual Localizer"; // window title            
            this.BitmapResourceID = 501;
            this.BitmapIndex = 0;        
        }       

        /// <summary>
        /// When window is closed
        /// </summary>        
        protected override void OnWindowHidden(object sender, EventArgs e) {            
            panel.Clear();
        }

        /// <summary>
        /// Set content of toolwindow
        /// </summary>        
        public void SetData(List<CodeReferenceResultItem> list) {
            if (list == null) throw new ArgumentNullException("list");
            panel.Columns[panel.CheckBoxColumnName].Visible = false;
            panel.ContextMenuEnabled = false;
            panel.LockFiles = false;
            panel.SetData(list);
        }
    }
}
