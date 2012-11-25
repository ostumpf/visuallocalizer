using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Design;
using VisualLocalizer.Commands;
using VisualLocalizer.Editor;
using VSLangProj;
using EnvDTE;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.Shell.Interop;
using VisualLocalizer.Gui;
using Microsoft.VisualStudio;
using VisualLocalizer.Extensions;

namespace VisualLocalizer.Commands {
    internal sealed class MenuManager {

        private CSharpMoveToResourcesCommand csharpMoveToResourcesCommand;
        private AspNetMoveToResourcesCommand aspNetMoveToResourcesCommand;
        private InlineCommand inlineCommand;
        private BatchMoveCommand batchMoveCommand;
        private BatchInlineCommand batchInlineCommand;

        public MenuManager() {
            this.inlineCommand = new InlineCommand();
            this.batchMoveCommand = new BatchMoveCommand();
            this.batchInlineCommand = new BatchInlineCommand();
            this.csharpMoveToResourcesCommand = new CSharpMoveToResourcesCommand();
            this.aspNetMoveToResourcesCommand = new AspNetMoveToResourcesCommand();            

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.CodeMenu, null,
                new EventHandler(codeMenuQueryStatus),VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.SolExpMenu, null,
                new EventHandler(solExpMenuQueryStatus), VisualLocalizerPackage.Instance.menuService);

           ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.MoveCodeMenuItem,
                new EventHandler(moveToResourcesClick), null, VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.InlineCodeMenuItem,
                new EventHandler(inlineClick), null, VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.BatchMoveCodeMenuItem,
                new EventHandler(batchMoveCodeClick), null, VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.BatchMoveSolExpMenuItem,
                new EventHandler(batchMoveSolExpClick), null, VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.BatchInlineCodeMenuItem,
                new EventHandler(batchInlineCodeClick), null, VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.BatchInlineSolExpMenuItem,
                new EventHandler(batchInlineSolExpClick), null, VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.BatchInlineSelectionCodeMenuItem,
                new EventHandler(batchInlineSelectionCodeClick), new EventHandler(selectionCodeQueryStatus), VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.BatchMoveSelectionCodeMenuItem,
                new EventHandler(batchMoveSelectionCodeClick), new EventHandler(selectionCodeQueryStatus), VisualLocalizerPackage.Instance.menuService);
        }

        internal static OleMenuCommand ConfigureMenuCommand(Guid guid, int id, EventHandler invokeHandler,
            EventHandler queryStatusHandler, OleMenuCommandService menuService) {   
         
            CommandID cmdid = new CommandID(guid, id);
            OleMenuCommand cmd = new OleMenuCommand(invokeHandler, cmdid);
            
            cmd.BeforeQueryStatus += queryStatusHandler;
            menuService.AddCommand(cmd);

            return cmd;
        }

        internal static T ShowToolWindow<T>() where T:ToolWindowPane {
            T pane = (T)VisualLocalizerPackage.Instance.FindToolWindow(typeof(T), 0, true);
            
            if (pane != null && pane.Frame != null) {
                IVsWindowFrame frame = (IVsWindowFrame)pane.Frame;                    
                frame.Show();                                
            }

            return pane;
        }      

        private void codeMenuQueryStatus(object sender, EventArgs args) {
            Document doc=VisualLocalizerPackage.Instance.DTE.ActiveDocument;
            
            OleMenuCommand cmd=(OleMenuCommand)sender;
            bool supported=doc.ProjectItem.CanShowCodeContextMenu();
            
            cmd.Supported = supported;
            cmd.Visible = supported;
        }

        private void selectionCodeQueryStatus(object sender, EventArgs args) {
            OleMenuCommand cmd = sender as OleMenuCommand;
            
            Document currentDocument = VisualLocalizerPackage.Instance.DTE.ActiveDocument;
            if (currentDocument == null) {
                cmd.Enabled = false;
                return;
            } 
            
            TextSelection selection = currentDocument.Selection as TextSelection;
            if (selection == null) {
                cmd.Enabled = false;
                return;
            }

            cmd.Enabled = !selection.IsEmpty;
            
        }

        private void solExpMenuQueryStatus(object sender, EventArgs args) {
            Array selectedItems = (Array)VisualLocalizerPackage.Instance.UIHierarchy.SelectedItems;
            bool ok = selectedItems.Length > 0;
      
            foreach (UIHierarchyItem o in selectedItems) {
                if (o.Object is ProjectItem) {
                    ProjectItem item = (ProjectItem)o.Object;                    
                    ok = ok && item.CanShowCodeContextMenu();
                } else if (o.Object is Project) {
                    Project proj = (Project)o.Object;
                    ok = ok && proj.IsKnownProjectType();
                } else throw new Exception("Unexpected project item type: "+o.Object.GetVisualBasicType());               
            }

            (sender as OleMenuCommand).Visible = ok;
        }
      
        private void moveToResourcesClick(object sender, EventArgs args) {
            try {
                Document doc = VisualLocalizerPackage.Instance.DTE.ActiveDocument;
                if (doc != null && doc.ProjectItem.GetFileType() == FILETYPE.ASPX) {
                    aspNetMoveToResourcesCommand.Process();
                } else {
                    csharpMoveToResourcesCommand.Process();
                }
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                MessageBox.ShowError(text);
            } finally {
                VLDocumentViewsManager.ReleaseLocks();
            }
        }

        private void inlineClick(object sender, EventArgs args) {
            try {
                inlineCommand.Process();
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                MessageBox.ShowError(text);
            }
        }

        private void batchMoveCodeClick(object sender, EventArgs args) {
            try {
                batchMoveCommand.Process(true);
                BatchMoveToResourcesToolWindow win = ShowToolWindow<BatchMoveToResourcesToolWindow>();
                if (win != null) {
                    win.SetData(batchMoveCommand.Results);
                } else throw new Exception("Unable to display tool window.");
                batchMoveCommand.Results.Clear();
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                MessageBox.ShowError(text);
            }
        }

        private void batchMoveSolExpClick(object sender, EventArgs args) {
            try {
                batchMoveCommand.Process((Array)VisualLocalizerPackage.Instance.UIHierarchy.SelectedItems, true);
                BatchMoveToResourcesToolWindow win = ShowToolWindow<BatchMoveToResourcesToolWindow>();
                if (win != null) {
                    win.SetData(batchMoveCommand.Results); 
                } else throw new Exception("Unable to display tool window.");
                batchMoveCommand.Results.Clear();
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                MessageBox.ShowError(text);
            } 
        }

        private void batchInlineCodeClick(object sender, EventArgs args) {
            try {
                batchInlineCommand.Process(true);
                BatchInlineToolWindow win = ShowToolWindow<BatchInlineToolWindow>();
                if (win != null) {
                    win.SetData(batchInlineCommand.Results);
                } else throw new Exception("Unable to display tool window.");
                batchInlineCommand.Results.Clear();
            } catch (Exception ex) {                
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                MessageBox.ShowError(text);
            } 
        }

        private void batchInlineSolExpClick(object sender, EventArgs args) {
            try {
                batchInlineCommand.Process((Array)VisualLocalizerPackage.Instance.UIHierarchy.SelectedItems, true);
                BatchInlineToolWindow win = ShowToolWindow<BatchInlineToolWindow>();
                if (win != null) {
                    win.SetData(batchInlineCommand.Results);
                } else throw new Exception("Unable to display tool window.");
                batchInlineCommand.Results.Clear();
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                MessageBox.ShowError(text);
            } 
        }

        private void batchInlineSelectionCodeClick(object sender, EventArgs args) {
            try {
                batchInlineCommand.ProcessSelection(true);
                BatchInlineToolWindow win = ShowToolWindow<BatchInlineToolWindow>();
                if (win != null) {
                    win.SetData(batchInlineCommand.Results);
                } else throw new Exception("Unable to display tool window.");
                batchInlineCommand.Results.Clear();
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                MessageBox.ShowError(text);
            }
        }

        private void batchMoveSelectionCodeClick(object sender, EventArgs args) {
            try {
                batchMoveCommand.ProcessSelection(true);
                BatchMoveToResourcesToolWindow win = ShowToolWindow<BatchMoveToResourcesToolWindow>();
                if (win != null) {
                    win.SetData(batchMoveCommand.Results);
                } else throw new Exception("Unable to display tool window.");
                batchMoveCommand.Results.Clear();
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                MessageBox.ShowError(text);
            }
        }
    }

   
}
