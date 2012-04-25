using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Windows.Forms;
using System.Drawing;
using System.Globalization;
using System.Diagnostics;
using VisualLocalizer.Library;
using System.IO;

namespace OndrejStumpf.VLTestingPackage {


    [Guid("842E809F-647A-4043-BE59-D48764931F65")]
    class MyEditorFactory : EditorFactory<MyEditor,EditorControl> {
       
    }

    
    class MyEditor : MonoEditor<EditorControl> {

        public MyEditor() {
            UIControl.ContentChanged += new EventHandler(Control_ContentChanged);
            
        }

        void Control_ContentChanged(object sender, EventArgs e) {
            if (!Loading)
                IsDirty = true;
        }

        public override string GetFormatList() {
            return "ResX file (*.resx)\n*.resx\nTXT file (*.txt)\n*.txt\n";
        }

        public override void LoadFile(string path) {
            string text=File.ReadAllText(path);
            this.UIControl.page.box.Text = text;
        }

        public override void SaveFile(string path, uint format) {
            Trace.WriteLine("saving " + format);
            File.WriteAllText(path, UIControl.page.box.Text);
        }

        public override string Extension {
            get { return ".resx"; }
        }

        public override Guid EditorFactoryGuid {
            get { return typeof(MyEditorFactory).GUID; }
        }
    }

    class EditorControl : UserControl {

        public Page page;

        public EditorControl() {
            TabControl tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
                       
            page = new Page();
            tabs.TabPages.Add(page);
            tabs.TabPages.Add("TEST");

            page.box.TextChanged += new EventHandler(delegate(object sender,EventArgs args) { 
                ContentChanged(sender,args); 
            });
          
            Controls.Add(tabs);
        }

        public event EventHandler ContentChanged;
    }
    class Page : TabPage {

        public TextBox box;

        public Page() {
            this.Text = "VL Editor";

            box = new TextBox();
            box.Multiline = true;
            box.Size = new Size(400, 200);
            box.ScrollBars = ScrollBars.Both;

            this.Controls.Add(box);
        }

    }

}
