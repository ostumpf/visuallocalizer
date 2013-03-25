using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Resources;
using VisualLocalizer.Library;
using VisualLocalizer.Commands;
using VisualLocalizer.Components;

namespace VisualLocalizer.Editor.UndoUnits {

    /// <summary>
    /// Represents action of renaming key of string resource in editor
    /// </summary>
    [Guid("A524A5E7-EF67-4b42-BBB1-25706700A1AD")]
    internal sealed class StringRenameKeyUndoUnit : RenameKeyUndoUnit {

        public StringRenameKeyUndoUnit(ResXStringGridRow sourceRow, ResXEditorControl control, string oldKey, string newKey) 
            : base(oldKey, newKey) {
            if (sourceRow == null) throw new ArgumentNullException("sourceRow");
            if (control == null) throw new ArgumentNullException("control");

            this.SourceRow = sourceRow;
            this.Control = control;
        }

        public ResXStringGridRow SourceRow { get; private set; }
        public ResXEditorControl Control { get; private set; }
       
        public override void Undo() {                       
            UpdateSourceReferences(NewKey, OldKey);
        }

        public override void Redo() {
            UpdateSourceReferences(OldKey, NewKey);
        }

        private void UpdateSourceReferences(string from, string to) {
            try {                
                // suspend the reference lookuper thread
                Control.ReferenceCounterThreadSuspended = true;
                Control.UpdateReferencesCount(SourceRow);

                ChangeColumnValue(from, to);

                // if the rows has no errors, perform pseudo-refactoring
                if (SourceRow.ErrorMessages.Count == 0) {
                    int errors = 0;
                    int count = SourceRow.CodeReferences.Count;
                    SourceRow.CodeReferences.ForEach((item) => { item.KeyAfterRename = to; });

                    BatchReferenceReplacer replacer = new BatchReferenceReplacer(SourceRow.CodeReferences);
                    replacer.Inline(SourceRow.CodeReferences, true, ref errors);

                    VLOutputWindow.VisualLocalizerPane.WriteLine("Renamed {0} key references in code", count);
                }
                
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                MessageBox.ShowException(ex);
            } finally {
                Control.ReferenceCounterThreadSuspended = false;
            }
        }

        private void ChangeColumnValue(string from, string to) {
            if (!string.IsNullOrEmpty(to)) {                
                SourceRow.DataSourceItem.Name = to;
                SourceRow.Status = ResXStringGridRow.STATUS.OK;
            } else {
                SourceRow.Status = ResXStringGridRow.STATUS.KEY_NULL;
            }
            
            ResXStringGrid grid = (ResXStringGrid)SourceRow.DataGridView;
            SourceRow.Cells[grid.KeyColumnName].Tag = from;
            SourceRow.Cells[grid.KeyColumnName].Value = to;
            grid.ValidateRow(SourceRow);
            grid.NotifyDataChanged();
            grid.SetContainingTabPageSelected();

            if (SourceRow.ErrorMessages.Count == 0) SourceRow.LastValidKey = to;
        }
    }
}
