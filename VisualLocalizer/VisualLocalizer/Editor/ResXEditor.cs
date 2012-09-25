using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using VisualLocalizer.Library;
using System.Resources;
using System.Collections;
using VSLangProj;
using EnvDTE;
using VisualLocalizer.Editor.UndoUnits;

namespace VisualLocalizer.Editor {
    [Guid("163D9FB6-68C6-4801-9CA0-3C53241D7855")]
    internal class ResXEditorFactory : EditorFactory<ResXEditor,ResXEditorControl> {
    }

    internal sealed class ResXEditor : AbstractSingleViewEditor<ResXEditorControl> {
        
        public ResXEditor() {            
            UIControl.Editor = this;
            UIControl.DataChanged += new EventHandler(UIControl_DataChanged);            
        }

        private void UIControl_DataChanged(object sender, EventArgs e) {
            IsDirty = true;            
        }
      
        public override void LoadFile(string path) {
            base.LoadFile(path);
            
            ResXResourceReader reader = null;
            try {
                Dictionary<string, ResXDataNode> data = new Dictionary<string, ResXDataNode>();

                reader = new ResXResourceReader(path);
                reader.UseResXDataNodes = true;
                foreach (DictionaryEntry pair in reader) {
                    data.Add(pair.Key.ToString(), pair.Value as ResXDataNode);
                }

                UIControl.SetData(data);
            } catch (Exception ex) {
                MessageBox.ShowError(ex.Message);
                throw;
            } finally {
                if (reader != null) reader.Close();
            }
        }

        public override void SaveFile(string path, uint format) {
            base.SaveFile(path, format);

            ResXResourceWriter writer = null;
            try {
                Dictionary<string,ResXDataNode> data = UIControl.GetData(true);
                writer = new ResXResourceWriter(path);
                
                foreach (var o in data) {
                    writer.AddResource(o.Value);
                }              

            } catch (Exception ex) {
                MessageBox.ShowError(ex.Message);
                throw;
            } finally {
                if (writer != null) writer.Close();
            }
        }

        public override bool ReadOnly {
            set {
                base.ReadOnly = value;
                UIControl.SetReadOnly(value);
            }
        }

        public override string GetFormatList() {
            return "Managed Resource File (*.resx)\n*.resx\n";
        }

        public override string Extension {            
            get { return StringConstants.ResXExtension; }
        }

        public override Guid EditorFactoryGuid {
            get { return typeof(ResXEditorFactory).GUID; }
        }
    }


}
