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
using System.IO;
using VisualLocalizer.Components;
using System.Windows.Forms;

namespace VisualLocalizer.Editor {
    [Guid("163D9FB6-68C6-4801-9CA0-3C53241D7855")]
    internal class ResXEditorFactory : EditorFactory<ResXEditor,ResXEditorControl> {
    }

    internal sealed class ResXEditor : AbstractSingleViewEditor<ResXEditorControl> {        

        public ResXEditor() {                        
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
                reader.BasePath = Path.GetDirectoryName(path);

                foreach (DictionaryEntry pair in reader) {
                    data.Add(pair.Key.ToString(), pair.Value as ResXDataNode);
                }

                UIControl.SetData(data);
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                VisualLocalizer.Library.MessageBox.ShowError(text);
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
                writer.BasePath = Path.GetDirectoryName(path);

                foreach (var o in data) {
                    writer.AddResource(o.Value);
                }              

            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                VisualLocalizer.Library.MessageBox.ShowError(text);
                throw;
            } finally {
                if (writer != null) writer.Close();
            }
        }

        public override bool ReadOnly {
            set {
                base.ReadOnly = value;
                UIControl.SetReadOnly(value);
                SetUndoManagerEnabled(!value);
            }
        }

        public override string GetFormatList() {
            return "Managed Resource File (*.resx)\0*.resx\0";
        }

        public override string FileName {
            get {
                return base.FileName;
            }
            protected set {
                base.FileName = value;
                FileUri = new Uri(value);
            }
        }

        public Uri FileUri {
            get;
            private set;
        }

        public override string Extension {            
            get { return StringConstants.ResXExtension; }
        }

        public override Guid EditorFactoryGuid {
            get { return typeof(ResXEditorFactory).GUID; }
        }
        
        public override bool ExecutePaste() {
            return UIControl.ExecutePaste();
        }

        public override bool ExecuteCopy() {
            return UIControl.ExecuteCopy();
        }

        public override bool ExecuteCut() {
            return UIControl.ExecuteCut();
        }

        public override COMMAND_STATUS IsCutSupported {
            get {
                return UIControl.CanCutOrCopy;
            }
        }

        public override COMMAND_STATUS IsPasteSupported {
            get {
                return UIControl.CanPaste;
            }
        }

        public override COMMAND_STATUS IsCopySupported {
            get {
                return UIControl.CanCutOrCopy;
            }
        }

        public override COMMAND_STATUS GetSystemCommandStatus(Microsoft.VisualStudio.VSConstants.VSStd97CmdID cmdID) {
            switch (cmdID) {
                case Microsoft.VisualStudio.VSConstants.VSStd97CmdID.Delete:
                    return UIControl.CanDelete;
                case Microsoft.VisualStudio.VSConstants.VSStd97CmdID.SelectAll:
                    return UIControl.CanSelectAll;
                default:
                    return base.GetSystemCommandStatus(cmdID);
            }            
        }

        public override bool ExecuteSystemCommand(Microsoft.VisualStudio.VSConstants.VSStd97CmdID cmdID) {
            switch (cmdID) {
                case Microsoft.VisualStudio.VSConstants.VSStd97CmdID.Delete:
                    UIControl.NotifyRemoveRequested(REMOVEKIND.REMOVE);
                    return true;
                case Microsoft.VisualStudio.VSConstants.VSStd97CmdID.SelectAll:
                    return UIControl.ExecuteSelectAll();
                default:
                    return base.ExecuteSystemCommand(cmdID);
            }               
        }
    }


}
