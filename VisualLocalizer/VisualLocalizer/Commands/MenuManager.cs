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

namespace VisualLocalizer.Commands {
    internal sealed class MenuManager {

        private VisualLocalizerPackage package;
        private MoveToResourcesCommand moveToResourcesCommand;
        private InlineCommand inlineCommand;

        public MenuManager(VisualLocalizerPackage package) {
            this.package = package;
            this.moveToResourcesCommand = new MoveToResourcesCommand(package);
            this.inlineCommand = new InlineCommand(package);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.CodeMenu, null,
                new EventHandler(codeMenuQueryStatus));

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.SolExpMenu, null,
                new EventHandler(solExpMenuQueryStatus));

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.MoveCodeMenuItem,
                new EventHandler(moveToResourcesClick),null);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.InlineCodeMenuItem,
                new EventHandler(inlineClick), null);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.BatchMoveCodeMenuItem,
                new EventHandler(batchMoveCodeClick), null);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.BatchMoveSolExpMenuItem,
                new EventHandler(batchMoveSolExpClick), null);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.BatchInlineCodeMenuItem,
                new EventHandler(batchInlineCodeClick), null);

            ConfigureMenuCommand(typeof(Guids.VLCommandSet).GUID,
                PackageCommandIDs.BatchInlineSolExpMenuItem,
                new EventHandler(batchInlineSolExpClick), null);
        }

        internal void ConfigureMenuCommand(Guid guid, int id,EventHandler invokeHandler,EventHandler queryStatusHandler) {            
            CommandID cmdid = new CommandID(guid, id);
            OleMenuCommand cmd = new OleMenuCommand(invokeHandler, cmdid);
            cmd.BeforeQueryStatus += queryStatusHandler;
            package.menuService.AddCommand(cmd);
        }

        private void codeMenuQueryStatus(object sender, EventArgs args) {
            bool ok = package.DTE.ActiveDocument.FullName.ToLowerInvariant().EndsWith(StringConstants.CsExtension);
            ok = ok && package.DTE.ActiveDocument.ProjectItem != null;
            ok = ok && package.DTE.ActiveDocument.ProjectItem.ContainingProject != null;
            ok = ok && package.DTE.ActiveDocument.ProjectItem.ContainingProject.Kind == VSLangProj.PrjKind.prjKindCSharpProject;
            (sender as OleMenuCommand).Supported = ok;
        }

        private void solExpMenuQueryStatus(object sender, EventArgs args) {
            Array selectedItems = (Array)package.UIHierarchy.SelectedItems;
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
                
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                MessageBox.ShowError(text);
            }
        }

        private void batchMoveSolExpClick(object sender, EventArgs args) {
            try {
                
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
