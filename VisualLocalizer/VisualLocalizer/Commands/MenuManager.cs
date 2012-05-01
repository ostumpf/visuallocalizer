using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Design;
using EnvDTE;
using VisualLocalizer.Commands;
using VisualLocalizer.Editor;
using VSLangProj;

namespace VisualLocalizer.Commands {
    internal sealed class MenuManager {

        private VisualLocalizerPackage package;
        
        public MenuManager(VisualLocalizerPackage package) {
            this.package = package;            

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
        }

        internal void ConfigureMenuCommand(Guid guid, int id,EventHandler invokeHandler,EventHandler queryStatusHandler) {            
            CommandID cmdid = new CommandID(guid, id);
            OleMenuCommand cmd = new OleMenuCommand(invokeHandler, cmdid);
            cmd.BeforeQueryStatus += queryStatusHandler;
            package.menuService.AddCommand(cmd);
        }



        private void codeMenuQueryStatus(object sender, EventArgs args) {
            bool ok = package.DTE.ActiveDocument.FullName.ToLowerInvariant().EndsWith(".cs");
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
                        ok = ok && item.get_FileNames(i).ToLowerInvariant().EndsWith(".cs");
                        ok = ok && item.ContainingProject.Kind == VSLangProj.PrjKind.prjKindCSharpProject;
                    }
                } else if (o.Object is Project) {
                    Project proj = (Project)o.Object;
                    ok = ok && proj.Kind == VSLangProj.PrjKind.prjKindCSharpProject;
                }                
            }

            (sender as OleMenuCommand).Visible = ok;
        }

        private void moveToResourcesClick(object sender, EventArgs args) {
            try {
                MoveToResourcesCommand.Handle(package);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteLine("{0} while processing command: {1}",ex.GetType().Name,ex.Message);
            }
        }

        private void inlineClick(object sender, EventArgs args) {
            
        }
    }
}
