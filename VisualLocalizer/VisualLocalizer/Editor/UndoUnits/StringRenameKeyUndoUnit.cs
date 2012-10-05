using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Resources;
using VisualLocalizer.Library;

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
            ChangeColumnValue(NewKey, OldKey);
        }

        public override void Redo() {
            ChangeColumnValue(OldKey, NewKey);
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
        }
    }
}
