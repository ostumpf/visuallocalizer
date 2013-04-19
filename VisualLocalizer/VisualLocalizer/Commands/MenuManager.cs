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
using System.Collections;

namespace VisualLocalizer.Commands {

    /// <summary>
    /// Provides basic handling of menu item events (clicks).
    /// </summary>
    internal sealed class MenuManager {

        private CSharpMoveToResourcesCommand csharpMoveToResourcesCommand;
        private AspNetMoveToResourcesCommand aspNetMoveToResourcesCommand;
        private VBMoveToResourcesCommand vbMoveToResourcesCommand;
        private CSharpInlineCommand csharpInlineCommand;
        private AspNetInlineCommand aspNetInlineCommand;
        private VBInlineCommand vbInlineCommand;
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
            this.vbMoveToResourcesCommand = new VBMoveToResourcesCommand();
            this.vbInlineCommand = new VBInlineCommand();

            // registers context menu in code windows
            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.CodeMenu, null,
                new EventHandler(CodeMenuQueryStatus),VisualLocalizerPackage.Instance.menuService);

            // registers context menu in Solution Explorer
            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.SolExpMenu, null,
                new EventHandler(SolExpMenuQueryStatus), VisualLocalizerPackage.Instance.menuService);
            
            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.MoveCodeMenuItem,
                new EventHandler(MoveToResourcesClick), null, VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.InlineCodeMenuItem,
                new EventHandler(InlineClick), null, VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.BatchMoveCodeMenuItem,
                new EventHandler(BatchMoveCodeClick), null, VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.BatchMoveSolExpMenuItem,
                new EventHandler(BatchMoveSolExpClick), new EventHandler(BatchSolExpQueryStatus), VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.BatchInlineCodeMenuItem,
                new EventHandler(BatchInlineCodeClick), null, VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.TranslateSolExpMenuItem,
                new EventHandler(TranslateSolExpClick), new EventHandler(TranslateSolExpQueryStatus), VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.BatchInlineSolExpMenuItem,
                new EventHandler(BatchInlineSolExpClick), new EventHandler(BatchSolExpQueryStatus), VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.BatchInlineSelectionCodeMenuItem,
                new EventHandler(BatchInlineSelectionCodeClick), new EventHandler(SelectionCodeQueryStatus), VisualLocalizerPackage.Instance.menuService);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.BatchMoveSelectionCodeMenuItem,
                new EventHandler(BatchMoveSelectionCodeClick), new EventHandler(SelectionCodeQueryStatus), VisualLocalizerPackage.Instance.menuService);

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
        public T ShowToolWindow<T>() where T:ToolWindowPane {
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
        private void CodeMenuQueryStatus(object sender, EventArgs args) {
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
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);           
            }
        }

        /// <summary>
        /// Determines whether active document's selection is non-empty and therefore "Batch move on selection" 
        /// and "Batch inline on selection" should be enabled in the context menu
        /// </summary>        
        private void SelectionCodeQueryStatus(object sender, EventArgs args) {
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
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
            }
        }
       
        /// <summary>
        /// Determines whether "Visual Localizer" menu should be visible in Solution Explorer's context menu.
        /// This happens only when all selected items are of known type.
        /// </summary>        
        private void SolExpMenuQueryStatus(object sender, EventArgs args) {
            try {
                if (VisualLocalizerPackage.Instance.UIHierarchy == null) {
                    VisualLocalizerPackage.Instance.UIHierarchy = (EnvDTE.UIHierarchy)VisualLocalizerPackage.Instance.DTE.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Object;                    
                }

                Array selectedItems = (Array)VisualLocalizerPackage.Instance.UIHierarchy.SelectedItems;
                bool menuOk = selectedItems.Length > 0;
                batchOperationsEnabled = menuOk;
                globalTranslateEnabled = menuOk;

                foreach (UIHierarchyItem o in selectedItems) {
                    if (o.Object is ProjectItem) {
                        ProjectItem item = (ProjectItem)o.Object;

                        bool isFolder = item.IsContainer();
                        bool isresx = item.IsItemResX();
                        bool canShow = item.CanShowCodeContextMenu();

                        menuOk = menuOk && (canShow || isresx || isFolder);
                        globalTranslateEnabled = globalTranslateEnabled && (isresx || isFolder);
                        batchOperationsEnabled = batchOperationsEnabled && (canShow || isFolder); 
                    } else if (o.Object is Project) {
                        Project proj = (Project)o.Object;
                        menuOk = menuOk && proj.IsKnownProjectType();
                    } else if (o.Object is Solution) {
                        menuOk = true;
                    } else throw new Exception("Unexpected project item type: " + o.Object.GetVisualBasicType());
                }

                (sender as OleMenuCommand).Visible = menuOk;
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
            }
        }

        private void TranslateSolExpQueryStatus(object sender, EventArgs args) {
            (sender as OleMenuCommand).Enabled = globalTranslateEnabled;
        }

        private void BatchSolExpQueryStatus(object sender, EventArgs args) {
            (sender as OleMenuCommand).Enabled = batchOperationsEnabled;
        }

        /// <summary>
        /// Handles "Move to resources" command from code context menu.
        /// </summary>        
        private void MoveToResourcesClick(object sender, EventArgs args) {
            bool enteredOk = false;
            try {
                if (OperationInProgress) throw new Exception("Cannot start operation 'Move to resources', because another operation is in progress.");
                enteredOk = true;

                Document doc = VisualLocalizerPackage.Instance.DTE.ActiveDocument;
                if (doc == null) throw new Exception("No active document.");

                if (doc.ProjectItem.GetFileType() == FILETYPE.ASPX) {
                    aspNetMoveToResourcesCommand.Process();
                } else if (doc.ProjectItem.GetFileType() == FILETYPE.CSHARP) {
                    csharpMoveToResourcesCommand.Process();
                } else if (doc.ProjectItem.GetFileType() == FILETYPE.VB) {
                    vbMoveToResourcesCommand.Process();
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                MessageBox.ShowException(ex);
            } finally {
                if (enteredOk) VLDocumentViewsManager.ReleaseLocks();
            }
        }

        /// <summary>
        /// Handles "Inline" command from code context menu.
        /// </summary>        
        private void InlineClick(object sender, EventArgs args) {
            bool enteredOk = false;
            try {
                if (OperationInProgress) throw new Exception("Cannot start operation 'Inline', because another operation is in progress.");
                enteredOk = true;

                Document doc = VisualLocalizerPackage.Instance.DTE.ActiveDocument;
                if (doc == null) throw new Exception("No active document.");

                if (doc.ProjectItem.GetFileType() == FILETYPE.ASPX) {
                    aspNetInlineCommand.Process();
                } else if (doc.ProjectItem.GetFileType() == FILETYPE.CSHARP) {
                    csharpInlineCommand.Process();
                } else if (doc.ProjectItem.GetFileType() == FILETYPE.VB) {
                    vbInlineCommand.Process();
                }                
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                MessageBox.ShowException(ex);
            } finally {
                if (enteredOk) VLDocumentViewsManager.ReleaseLocks();
            }
        }

        /// <summary>
        /// Handles "Batch move on document" command from code context menu.
        /// </summary>        
        private void BatchMoveCodeClick(object sender, EventArgs args) {
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
                if (OperationInProgress) VLDocumentViewsManager.ReleaseLocks();
                VLDocumentViewsManager.CloseInvisibleWindows(typeof(BatchMoveCommand), false);
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                MessageBox.ShowException(ex);
                OperationInProgress = false;
            }
        }

        /// <summary>
        /// Handles "Batch move" command from Solution Explorer's context menu.
        /// </summary>        
        private void BatchMoveSolExpClick(object sender, EventArgs args) {
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
                if (OperationInProgress) VLDocumentViewsManager.ReleaseLocks();
                VLDocumentViewsManager.CloseInvisibleWindows(typeof(BatchMoveCommand), false);
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                MessageBox.ShowException(ex);
                OperationInProgress = false;
            } 
        }

        /// <summary>
        /// Handles "Batch inline on document" command from code context menu.
        /// </summary>        
        private void BatchInlineCodeClick(object sender, EventArgs args) {
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
                if (OperationInProgress) VLDocumentViewsManager.ReleaseLocks();
                VLDocumentViewsManager.CloseInvisibleWindows(typeof(BatchInlineCommand), false);
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                MessageBox.ShowException(ex);
                OperationInProgress = false;
            }
        }

        /// <summary>
        /// Handles "Batch inline" command from Solution Explorer's context menu.
        /// </summary>        
        private void BatchInlineSolExpClick(object sender, EventArgs args) {
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
                if (OperationInProgress) VLDocumentViewsManager.ReleaseLocks();
                VLDocumentViewsManager.CloseInvisibleWindows(typeof(BatchInlineCommand), false);
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                MessageBox.ShowException(ex);
                OperationInProgress = false;
            } 
        }

        /// <summary>
        /// Handles "Translate resources" command from Solution Explorer's context menu.
        /// </summary>        
        private void TranslateSolExpClick(object sender, EventArgs args) {
            try {
                if (OperationInProgress) throw new Exception("Cannot start operation 'Global translate', because another operation is in progress.");
                OperationInProgress = true;

                globalTranslateCommand.Process((Array)VisualLocalizerPackage.Instance.UIHierarchy.SelectedItems);
            } catch (Exception ex) {
                if (OperationInProgress) VLDocumentViewsManager.ReleaseLocks();
                
                Dictionary<string, string> add = null;
                if (ex is CannotParseResponseException) {
                    CannotParseResponseException cpex = ex as CannotParseResponseException;                    
                    add = new Dictionary<string, string>();
                    add.Add("Full response:", cpex.FullResponse);
                }

                VLOutputWindow.VisualLocalizerPane.WriteException(ex, add);
                MessageBox.ShowException(ex, add);
                OperationInProgress = false;
            } 
        }

        /// <summary>
        /// Handles "Batch inline on selection" command from code context menu.
        /// </summary>        
        private void BatchInlineSelectionCodeClick(object sender, EventArgs args) {
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
                if (OperationInProgress) VLDocumentViewsManager.ReleaseLocks();
                VLDocumentViewsManager.CloseInvisibleWindows(typeof(BatchInlineCommand), false);
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                MessageBox.ShowException(ex);
                OperationInProgress = false;
            } 
        }

        /// <summary>
        /// Handles "Batch move on selection" command from code context menu.
        /// </summary>        
        private void BatchMoveSelectionCodeClick(object sender, EventArgs args) {
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
                if (OperationInProgress) VLDocumentViewsManager.ReleaseLocks();
                VLDocumentViewsManager.CloseInvisibleWindows(typeof(BatchMoveCommand), false);
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                MessageBox.ShowException(ex);
                OperationInProgress = false;
            }
        }
    }

   
}
