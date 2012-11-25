using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using VisualLocalizer.Components;
using Microsoft.VisualStudio.TextManager.Interop;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.Shell.Interop;
using VisualLocalizer.Commands;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.OLE.Interop;
using System.IO;

namespace VisualLocalizer.Gui {

    [Guid("E7755751-5A96-451b-9DFF-DCA1422CCA0A")]
    internal sealed class BatchInlineToolWindow : AbstractCodeToolWindow<BatchInlineToolPanel> {

        public BatchInlineToolWindow() {
            this.Caption = "Batch Inline - Visual Localizer";
            this.ToolBar = new CommandID(typeof(VisualLocalizer.Guids.VLBatchInlineToolbarCommandSet).GUID, PackageCommandIDs.BatchInlineToolbarID);
            this.ToolBarLocation = (int)VSTWT_LOCATION.VSTWT_TOP;

            OleMenuCommandService menuService = (OleMenuCommandService)GetService(typeof(IMenuCommandService));
            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchInlineToolbarCommandSet).GUID, PackageCommandIDs.BatchInlineToolbarRunID,
                new EventHandler(runClick), null, menuService);

            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchInlineToolbarCommandSet).GUID, PackageCommandIDs.BatchInlineToolbarRemoveUncheckedID,
                new EventHandler(removeUnchecked), null, menuService);

            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchInlineToolbarCommandSet).GUID, PackageCommandIDs.BatchInlineToolbarPutBackUncheckedID,
                new EventHandler(restoreUnchecked), null, menuService);
        }

        protected override void OnWindowHidden(object sender, EventArgs e) {
            VLDocumentViewsManager.ReleaseLocks();
        }

        private void removeUnchecked(object sender, EventArgs e) {
            panel.RemoveUncheckedRows(true);
        }

        private void restoreUnchecked(object sender, EventArgs e) {
            panel.RestoreRemovedRows();
        }

        private void runClick(object sender, EventArgs e) {
            int checkedRows = panel.CheckedRowsCount;
            int rowCount = panel.Rows.Count;
            int rowErrors = 0;

            try {
                VLDocumentViewsManager.ReleaseLocks();
                BatchInliner inliner = new BatchInliner(panel.Rows);

                inliner.Inline(panel.GetData(), false, ref rowErrors);
               
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                VisualLocalizer.Library.MessageBox.ShowError(text);
            } finally {
                ((IVsWindowFrame)this.Frame).CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);

                VLOutputWindow.VisualLocalizerPane.Activate();
                VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Inline command completed - selected {0} rows of {1}, {2} rows processed successfully", checkedRows, rowCount, checkedRows - rowErrors);
            }
        }

        public void SetData(List<CodeReferenceResultItem> value) {
            panel.SetData(value);
        }
    }
}
