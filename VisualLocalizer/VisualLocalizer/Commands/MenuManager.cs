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

namespace VisualLocalizer.Commands {
    internal sealed class MenuManager {

        private MoveToResourcesCommand moveToResourcesCommand;
        private InlineCommand inlineCommand;
        private BatchMoveCommand batchMoveCommand;

        public MenuManager() {            
            this.moveToResourcesCommand = new MoveToResourcesCommand();
            this.inlineCommand = new InlineCommand();
            this.batchMoveCommand = new BatchMoveCommand();

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
                PackageCommandIDs.ShowToolWindowItem,
                new EventHandler(showToolWindowClick), null, VisualLocalizerPackage.Instance.menuService);            
        }

        internal static void ConfigureMenuCommand(Guid guid, int id,EventHandler invokeHandler,
            EventHandler queryStatusHandler, OleMenuCommandService menuService) {   
         
            CommandID cmdid = new CommandID(guid, id);
            OleMenuCommand cmd = new OleMenuCommand(invokeHandler, cmdid);
            cmd.BeforeQueryStatus += queryStatusHandler;
            menuService.AddCommand(cmd);
        }

        internal BatchMoveToResourcesToolWindow ShowToolWindow() {
            BatchMoveToResourcesToolWindow pane = (BatchMoveToResourcesToolWindow)VisualLocalizerPackage.Instance.FindToolWindow(typeof(BatchMoveToResourcesToolWindow), 0, true);

            if (pane != null && pane.Frame != null) {
                ((IVsWindowFrame)pane.Frame).SetProperty((int)__VSFPROPID.VSFPROPID_IsWindowTabbed, true);
                ((IVsWindowFrame)pane.Frame).SetProperty((int)__VSFPROPID.VSFPROPID_FrameMode, VSFRAMEMODE.VSFM_Dock);
                ((IVsWindowFrame)pane.Frame).Show();
            }

            return pane;
        }

        private void codeMenuQueryStatus(object sender, EventArgs args) {
            bool ok = VisualLocalizerPackage.Instance.DTE.ActiveDocument.FullName.ToLowerInvariant().EndsWith(StringConstants.CsExtension);
            ok = ok && VisualLocalizerPackage.Instance.DTE.ActiveDocument.ProjectItem != null;
            ok = ok && VisualLocalizerPackage.Instance.DTE.ActiveDocument.ProjectItem.ContainingProject != null;
            ok = ok && VisualLocalizerPackage.Instance.DTE.ActiveDocument.ProjectItem.ContainingProject.Kind == VSLangProj.PrjKind.prjKindCSharpProject;
            (sender as OleMenuCommand).Supported = ok;
        }

        private void solExpMenuQueryStatus(object sender, EventArgs args) {
            Array selectedItems = (Array)VisualLocalizerPackage.Instance.UIHierarchy.SelectedItems;
            bool ok = selectedItems.Length > 0;

            foreach (UIHierarchyItem o in selectedItems) {
                if (o.Object is ProjectItem) {
                    ProjectItem item = (ProjectItem)o.Object;
                    for (short i = 0; i < item.FileCount; i++) {
                        ok = ok && item.get_FileNames(i).ToLowerInvariant().EndsWith(StringConstants.CsExtension);
                        ok = ok && item.ContainingProject.Kind == VSLangProj.PrjKind.prjKindCSharpProject;
                    }
                } else if (o.Object is Project) {
                    Project proj = (Project)o.Object;
                    ok = ok && proj.Kind == VSLangProj.PrjKind.prjKindCSharpProject;
                } else throw new Exception("Unexpected project item type: "+o.Object.GetVisualBasicType());               
            }

            (sender as OleMenuCommand).Visible = ok;
        }

        private void showToolWindowClick(object sender, EventArgs args) {
            ShowToolWindow();
        }

        private void moveToResourcesClick(object sender, EventArgs args) {
            try {
                moveToResourcesCommand.Process();
            } catch (Exception ex) {
                string text=string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);
                
                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                MessageBox.ShowError(text);
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
                batchMoveCommand.Process();
                BatchMoveToResourcesToolWindow win = ShowToolWindow();
                if (win != null) {
                    win.SetData(batchMoveCommand.Results); 
                } else throw new Exception("Unable to display tool window.");
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                MessageBox.ShowError(text);
            }
        }

        private void batchMoveSolExpClick(object sender, EventArgs args) {
            try {
                batchMoveCommand.Process((Array)VisualLocalizerPackage.Instance.UIHierarchy.SelectedItems);
                BatchMoveToResourcesToolWindow win = ShowToolWindow();
                if (win != null) {
                    win.SetData(batchMoveCommand.Results); 
                } else throw new Exception("Unable to display tool window.");
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                MessageBox.ShowError(text);
            }
        }

        private void batchInlineCodeClick(object sender, EventArgs args) {
            try {
               
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                MessageBox.ShowError(text);
            }
        }

        private void batchInlineSolExpClick(object sender, EventArgs args) {
            try {

            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                MessageBox.ShowError(text);
            }
        }
    }
}
