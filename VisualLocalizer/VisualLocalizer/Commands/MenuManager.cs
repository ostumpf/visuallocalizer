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
using VisualLocalizer.Translate;

namespace VisualLocalizer.Commands {

    /// <summary>
    /// Provides basic handling of menu item events (clicks).
    /// </summary>
    internal sealed class MenuManager {

        private CSharpMoveToResourcesCommand csharpMoveToResourcesCommand;
        private AspNetMoveToResourcesCommand aspNetMoveToResourcesCommand;
        private CSharpInlineCommand csharpInlineCommand;
        private AspNetInlineCommand aspNetInlineCommand;
        private BatchMoveCommand batchMoveCommand;
        private BatchInlineCommand batchInlineCommand;
        private GlobalTranslateCommand globalTranslateCommand;
        private bool globalTranslateEnabled, batchOperationsEnabled;
        public static bool OperationInProgress;

        public MenuManager() {
            this.csharpInlineCommand = new CSharpInlineCommand();
            this.aspNetInlineCommand = new AspNetInlineCommand();
            this.batchMoveCommand = new BatchMoveCommand();
            this.batchInlineCommand = new BatchInlineCommand();
            this.csharpMoveToResourcesCommand = new CSharpMoveToResourcesCommand();
            this.aspNetMoveToResourcesCommand = new AspNetMoveToResourcesCommand();
            this.globalTranslateCommand = new GlobalTranslateCommand();

            // registers context menu in code windows
            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.CodeMenu, null,
                new EventHandler(codeMenuQueryStatus),VisualLocalizerPackage.Instance.menuService);

            // registers context menu in Solution Explorer
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
                new EventHandler(batchMoveSolExpClick), new EventHandler(batchSolExpQueryStatus), VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.BatchInlineCodeMenuItem,
                new EventHandler(batchInlineCodeClick), null, VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.TranslateSolExpMenuItem,
                new EventHandler(translateSolExpClick), new EventHandler(translateSolExpQueryStatus), VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.BatchInlineSolExpMenuItem,
                new EventHandler(batchInlineSolExpClick), new EventHandler(batchSolExpQueryStatus), VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.BatchInlineSelectionCodeMenuItem,
                new EventHandler(batchInlineSelectionCodeClick), new EventHandler(selectionCodeQueryStatus), VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.BatchMoveSelectionCodeMenuItem,
                new EventHandler(batchMoveSelectionCodeClick), new EventHandler(selectionCodeQueryStatus), VisualLocalizerPackage.Instance.menuService);
        }

        /// <summary>
        /// Creates a command from specified GUID and ID and adds it to the menuService, registering queryStatusHandler
        /// as a method called before invocation of the command and invokeHandler as a handler of the command.
        /// </summary>      
        /// <returns>Newly created command.</returns>
        internal static OleMenuCommand ConfigureMenuCommand(Guid guid, int id, EventHandler invokeHandler,
            EventHandler queryStatusHandler, OleMenuCommandService menuService) {   
         
            CommandID cmdid = new CommandID(guid, id);
            OleMenuCommand cmd = new OleMenuCommand(invokeHandler, cmdid);
            
            cmd.BeforeQueryStatus += queryStatusHandler;
            menuService.AddCommand(cmd);

            return cmd;
        }

        /// <summary>
        /// Attempts to display tool windows specified by its type.
        /// </summary>
        /// <typeparam name="T">Type of the tool windows to display.</typeparam>
        /// <returns>Instance of the tool window.</returns>
        private T ShowToolWindow<T>() where T:ToolWindowPane {
            T pane = (T)VisualLocalizerPackage.Instance.FindToolWindow(typeof(T), 0, true);
            
            if (pane != null && pane.Frame != null) {
                IVsWindowFrame frame = (IVsWindowFrame)pane.Frame;                    
                frame.Show();                                
            }

            return pane;
        }      

        /// <summary>
        /// Determines whether "Visual Localizer" menu will be visible in the context menu of the active document.
        /// Modifies its sender accordingly.
        /// </summary>        
        private void codeMenuQueryStatus(object sender, EventArgs args) {
            try {
                Document doc = VisualLocalizerPackage.Instance.DTE.ActiveDocument;
                OleMenuCommand cmd = (OleMenuCommand)sender;

                if (doc == null || doc.ProjectItem == null) {
                    cmd.Enabled = false;
                    return;
                }

                bool supported = doc.ProjectItem.CanShowCodeContextMenu();

                cmd.Supported = supported;
                cmd.Visible = supported;
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);                
            }
        }

        /// <summary>
        /// Determines whether active document's selection is non-empty and therefore "Batch move on selection" 
        /// and "Batch inline on selection" should be enabled in the context menu
        /// </summary>        
        private void selectionCodeQueryStatus(object sender, EventArgs args) {
            try {
                OleMenuCommand cmd = (OleMenuCommand)sender;

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
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
            }
        }
       
        /// <summary>
        /// Determines whether "Visual Localizer" menu should be visible in Solution Explorer's context menu.
        /// This happens only when all selected items are of known type.
        /// </summary>        
        private void solExpMenuQueryStatus(object sender, EventArgs args) {
            try {
                Array selectedItems = (Array)VisualLocalizerPackage.Instance.UIHierarchy.SelectedItems;
                bool menuOk = selectedItems.Length > 0;
                batchOperationsEnabled = menuOk;
                globalTranslateEnabled = menuOk;

                foreach (UIHierarchyItem o in selectedItems) {
                    if (o.Object is ProjectItem) {
                        ProjectItem item = (ProjectItem)o.Object;

                        bool isFolder = item.IsContainer();
                        bool isresx = ResXProjectItem.IsItemResX(item);
                        bool canShow = item.CanShowCodeContextMenu();

                        menuOk = menuOk && (canShow || isresx || isFolder);
                        globalTranslateEnabled = globalTranslateEnabled && (isresx || isFolder);
                        batchOperationsEnabled = batchOperationsEnabled && (canShow || isFolder); 
                    } else if (o.Object is Project) {
                        Project proj = (Project)o.Object;
                        menuOk = menuOk && proj.IsKnownProjectType();                        
                    } else throw new Exception("Unexpected project item type: " + o.Object.GetVisualBasicType());
                }

                (sender as OleMenuCommand).Visible = menuOk;
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
            }
        }

        private void translateSolExpQueryStatus(object sender, EventArgs args) {
            (sender as OleMenuCommand).Enabled = globalTranslateEnabled;
        }

        private void batchSolExpQueryStatus(object sender, EventArgs args) {
            (sender as OleMenuCommand).Enabled = batchOperationsEnabled;
        }

        /// <summary>
        /// Handles "Move to resources" command from code context menu.
        /// </summary>        
        private void moveToResourcesClick(object sender, EventArgs args) {
            bool enteredOk = false;
            try {
                if (OperationInProgress) throw new Exception("Cannot start operation 'Move to resources', because another operation is in progress.");
                enteredOk = true;

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
                if (enteredOk) VLDocumentViewsManager.ReleaseLocks();
            }
        }

        /// <summary>
        /// Handles "Inline" command from code context menu.
        /// </summary>        
        private void inlineClick(object sender, EventArgs args) {
            bool enteredOk = false;
            try {
                if (OperationInProgress) throw new Exception("Cannot start operation 'Inline', because another operation is in progress.");
                enteredOk = true;

                Document doc = VisualLocalizerPackage.Instance.DTE.ActiveDocument;
                if (doc != null && doc.ProjectItem.GetFileType() == FILETYPE.ASPX) {
                    aspNetInlineCommand.Process();
                } else {
                    csharpInlineCommand.Process();
                }                
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                MessageBox.ShowError(text);
            } finally {
                if (enteredOk) VLDocumentViewsManager.ReleaseLocks();
            }
        }

        /// <summary>
        /// Handles "Batch move on document" command from code context menu.
        /// </summary>        
        private void batchMoveCodeClick(object sender, EventArgs args) {
            try {
                if (OperationInProgress) throw new Exception("Cannot start operation 'Batch move to resources', because another operation is in progress.");
                OperationInProgress = true;

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

        /// <summary>
        /// Handles "Batch move" command from Solution Explorer's context menu.
        /// </summary>        
        private void batchMoveSolExpClick(object sender, EventArgs args) {
            try {
                if (OperationInProgress) throw new Exception("Cannot start operation 'Batch move to resources', because another operation is in progress.");
                OperationInProgress = true;

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

        /// <summary>
        /// Handles "Batch inline on document" command from code context menu.
        /// </summary>        
        private void batchInlineCodeClick(object sender, EventArgs args) {
            try {
                if (OperationInProgress) throw new Exception("Cannot start operation 'Batch inline', because another operation is in progress.");
                OperationInProgress = true;

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

        /// <summary>
        /// Handles "Batch inline" command from Solution Explorer's context menu.
        /// </summary>        
        private void batchInlineSolExpClick(object sender, EventArgs args) {
            try {
                if (OperationInProgress) throw new Exception("Cannot start operation 'Batch inline', because another operation is in progress.");
                OperationInProgress = true;

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

        /// <summary>
        /// Handles "Translate resources" command from Solution Explorer's context menu.
        /// </summary>        
        private void translateSolExpClick(object sender, EventArgs args) {
            try {
                if (OperationInProgress) throw new Exception("Cannot start operation 'Global translate', because another operation is in progress.");
                OperationInProgress = true;

                globalTranslateCommand.Process((Array)VisualLocalizerPackage.Instance.UIHierarchy.SelectedItems);
            } catch (Exception ex) {
                string text = null;
                if (ex is CannotParseResponseException) {
                    CannotParseResponseException cpex = ex as CannotParseResponseException;
                    text = string.Format("Server response cannot be parsed: {0}.\nFull response:\n{1}", ex.Message, cpex.FullResponse);
                } else {
                    text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);
                }
                
                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                MessageBox.ShowError(text);
            } 
        }

        /// <summary>
        /// Handles "Batch inline on selection" command from code context menu.
        /// </summary>        
        private void batchInlineSelectionCodeClick(object sender, EventArgs args) {
            try {
                if (OperationInProgress) throw new Exception("Cannot start operation 'Batch inline', because another operation is in progress.");
                OperationInProgress = true;

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

        /// <summary>
        /// Handles "Batch move on selection" command from code context menu.
        /// </summary>        
        private void batchMoveSelectionCodeClick(object sender, EventArgs args) {
            try {
                if (OperationInProgress) throw new Exception("Cannot start operation 'Batch move to resources', because another operation is in progress.");
                OperationInProgress = true;

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
