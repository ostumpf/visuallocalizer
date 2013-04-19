using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using VisualLocalizer.Library;
using VisualLocalizer.Extensions;
using System.Resources;
using System.Collections;
using VSLangProj;
using EnvDTE;
using VisualLocalizer.Editor.UndoUnits;
using System.IO;
using VisualLocalizer.Components;
using System.Windows.Forms;
using System.ComponentModel.Design;
using Microsoft.VisualStudio;

namespace VisualLocalizer.Editor {

    /// <summary>
    /// Editor factory creating instances of ResX editor, with ResXEditorControl being displayed as content
    /// </summary>
    [Guid("163D9FB6-68C6-4801-9CA0-3C53241D7855")]
    internal sealed class ResXEditorFactory : EditorFactory<ResXEditor,ResXEditorControl> {
    }

    /// <summary>
    /// Editor for ResX files
    /// </summary>
    internal sealed class ResXEditor : AbstractSingleViewEditor<ResXEditorControl> {        

        public ResXEditor() { 
            // register DataChanged event, which is issued from underlaying GUI content whenever the document should be marked as dirty           
            UIControl.DataChanged += new EventHandler(UIControl_DataChanged);            
        }

        private void UIControl_DataChanged(object sender, EventArgs e) {
            IsDirty = true;            
        }

        /// <summary>
        /// ResX project item corresponding to the edited file in Solution Explorer's file hierarchy
        /// </summary>
        public ResXProjectItem ProjectItem {
            get;
            private set;
        }

        /// <summary>
        /// URI of this file
        /// </summary>
        public Uri FileUri {
            get;
            private set;
        }

        /// <summary>
        /// Loads data from specified ResX file
        /// </summary>        
        public override void LoadFile(string path) {            
            base.LoadFile(path);
            
            ResXResourceReader reader = null;
            try {
                // initialize corresponding project item instance
                ProjectItem item = VisualLocalizerPackage.Instance.DTE.Solution.FindProjectItem(FileName); 
                ProjectItem = ResXProjectItem.ConvertToResXItem(item, item.ContainingProject);

                Dictionary<string, ResXDataNode> data = new Dictionary<string, ResXDataNode>();
                
                reader = new ResXResourceReader(path);
                reader.UseResXDataNodes = true;
                reader.BasePath = Path.GetDirectoryName(path);

                foreach (DictionaryEntry pair in reader) {
                    data.Add(pair.Key.ToString(), pair.Value as ResXDataNode);
                }

                // display data in GUI
                UIControl.SetData(data);

                VLOutputWindow.VisualLocalizerPane.WriteLine("Opened file \"{0}\"", path);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
                throw;
            } finally {
                if (reader != null) reader.Close();
            }
        }

        /// <summary>
        /// Saves current data from GUI to specified file
        /// </summary>        
        public override void SaveFile(string path, uint format) {
            base.SaveFile(path, format);

            ResXResourceWriter writer = null;
            try {
                // get data from GUI
                Dictionary<string,ResXDataNode> data = UIControl.GetData(true);
                writer = new ResXResourceWriter(path);
                writer.BasePath = Path.GetDirectoryName(path);

                foreach (var o in data) {
                    writer.AddResource(o.Value);
                }
                writer.Generate();

                VLOutputWindow.VisualLocalizerPane.WriteLine("Saved file \"{0}\"", path);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
                throw;
            } finally {
                if (writer != null) writer.Close();
            }
        }

        /// <summary>
        /// Gets/Sets readonly state of this document
        /// </summary>
        public override bool ReadOnly {
            get {
                return base.ReadOnly;
            }
            set {
                base.ReadOnly = value;
                UIControl.SetReadOnly(value); // update GUI                       
            }
        }

        /// <summary>
        /// Returns list of formats separated with \0 displayed in "Save as" dialog
        /// </summary>        
        public override string GetFormatList() {
            return "Managed Resource File (*.resx)\0*.resx\0";
        }

        /// <summary>
        /// Path of the edited file
        /// </summary>
        public override string FileName {
            get {
                return base.FileName;
            }
            protected set {
                base.FileName = value;
                FileUri = new Uri(value);
            }
        }        

        /// <summary>
        /// Returns extension which will be used to save the files
        /// </summary>
        public override string Extension {            
            get { return StringConstants.ResXExtension; }
        }

        public override Guid EditorFactoryGuid {
            get { return typeof(ResXEditorFactory).GUID; }
        }

        /// <summary>
        /// Paste command was triggered from Visual Studio
        /// </summary>        
        public override bool ExecutePaste() {
            return UIControl.ExecutePaste();
        }

        /// <summary>
        /// Copy command was triggered from Visual Studio
        /// </summary>        
        public override bool ExecuteCopy() {
            return UIControl.ExecuteCopy();
        }

        /// <summary>
        /// Cut command was triggered from Visual Studio
        /// </summary>        
        public override bool ExecuteCut() {
            return UIControl.ExecuteCut();
        }

        /// <summary>
        /// Returns status of the Cut command
        /// </summary>
        public override COMMAND_STATUS IsCutSupported {
            get {
                return UIControl.CanCutOrCopy;
            }
        }

        /// <summary>
        /// Returns status of the Paste command
        /// </summary>
        public override COMMAND_STATUS IsPasteSupported {
            get {
                return UIControl.CanPaste;
            }
        }

        /// <summary>
        /// Returns status of the Copy command
        /// </summary>
        public override COMMAND_STATUS IsCopySupported {
            get {
                return UIControl.CanCutOrCopy;
            }
        }

        /// <summary>
        /// Gets the system command status.
        /// </summary>
        /// <param name="cmdID">The CMD ID.</param>
        public override COMMAND_STATUS GetSystemCommandStatus(Microsoft.VisualStudio.VSConstants.VSStd97CmdID cmdID) {
            switch (cmdID) {
                case Microsoft.VisualStudio.VSConstants.VSStd97CmdID.Delete:
                    return UIControl.CanDelete; // returns status of Delete command
                case Microsoft.VisualStudio.VSConstants.VSStd97CmdID.SelectAll:
                    return UIControl.CanSelectAll; // returns status of SelectAll command
                default:
                    return base.GetSystemCommandStatus(cmdID);
            }            
        }

        /// <summary>
        /// Executes the system command.
        /// </summary>
        /// <param name="cmdID">The CMD ID.</param>        
        public override bool ExecuteSystemCommand(Microsoft.VisualStudio.VSConstants.VSStd97CmdID cmdID) {
            switch (cmdID) {
                case Microsoft.VisualStudio.VSConstants.VSStd97CmdID.Delete:
                    UIControl.NotifyRemoveRequested(REMOVEKIND.REMOVE); // executes Delete command
                    return true;
                case Microsoft.VisualStudio.VSConstants.VSStd97CmdID.SelectAll:
                    return UIControl.ExecuteSelectAll(); // executes SelectAll command
                default:
                    return base.ExecuteSystemCommand(cmdID);
            }               
        }
    }


}
