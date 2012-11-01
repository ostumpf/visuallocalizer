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

    [Guid("A524A5E7-EF67-4b42-BBB1-25706700A1AD")]
    internal sealed class StringRenameKeyUndoUnit : RenameKeyUndoUnit {

        public StringRenameKeyUndoUnit(ResXStringGridRow sourceRow, ResXStringGrid grid, string oldKey, string newKey) 
            : base(oldKey, newKey) {
            this.SourceRow = sourceRow;
            this.Grid = grid;
        }

        public ResXStringGridRow SourceRow { get; private set; }
        public ResXStringGrid Grid { get; private set; }
       
        public override void Undo() {                       
            UpdateSourceReferences(NewKey, OldKey);
        }

        public override void Redo() {
            UpdateSourceReferences(OldKey, NewKey);
        }

        private void UpdateSourceReferences(string from, string to) {
            try {                
                Grid.ReferenceCounterThreadSuspended = true;
                Grid.UpdateReferencesCount(SourceRow);

                ChangeColumnValue(from, to);

                if (SourceRow.ErrorSet.Count == 0) {
                    int errors = 0;
                    int count = SourceRow.CodeReferences.Count;
                    SourceRow.CodeReferences.ForEach((item) => { item.KeyAfterRename = to; });

                    BatchReferenceReplacer replacer = new BatchReferenceReplacer(SourceRow.CodeReferences);
                    replacer.Inline(SourceRow.CodeReferences, true, ref errors);

                    VLOutputWindow.VisualLocalizerPane.WriteLine("Renamed {0} key references in code", count);
                }
                
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                VisualLocalizer.Library.MessageBox.ShowError(text);
            } finally {
                Grid.ReferenceCounterThreadSuspended = false;
            }
        }

        private void ChangeColumnValue(string from, string to) {
            if (!string.IsNullOrEmpty(to)) {                
                SourceRow.DataSourceItem.Name = to;
                SourceRow.Status = ResXStringGridRow.STATUS.OK;
            } else {
                SourceRow.Status = ResXStringGridRow.STATUS.KEY_NULL;
            }
            SourceRow.Cells[Grid.KeyColumnName].Tag = from;
            SourceRow.Cells[Grid.KeyColumnName].Value = to;
            Grid.ValidateRow(SourceRow);
            Grid.NotifyDataChanged();
            Grid.SetContainingTabPageSelected();

            if (SourceRow.ErrorSet.Count == 0) SourceRow.LastValidKey = to;
        }
    }
}
