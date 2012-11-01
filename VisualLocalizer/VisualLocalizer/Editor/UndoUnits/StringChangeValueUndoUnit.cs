using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using VisualLocalizer.Library;
using System.Resources;
using VisualLocalizer.Components;

namespace VisualLocalizer.Editor.UndoUnits {

    [Guid("9F4FDA14-9B4C-4151-9FC9-194FF0A90705")]
    internal sealed class StringChangeValueUndoUnit : AbstractUndoUnit {

        public string Key { get; private set; }
        public string OldValue { get; private set; }
        public string NewValue { get; private set; }
        public string Comment { get; private set; }
        public ResXStringGridRow SourceRow { get; private set; }
        public ResXStringGrid Grid { get; private set; }

        public StringChangeValueUndoUnit(ResXStringGridRow sourceRow, ResXStringGrid grid, string key, string oldValue,string newValue, string comment) {
            this.SourceRow = sourceRow;
            this.Grid = grid;
            this.Key = key;
            this.OldValue = oldValue;
            this.NewValue = newValue;
            this.Comment = comment;
        }

        public override void Undo() {
            ChangeColumnValue(NewValue, OldValue);
        }

        public override void Redo() {
            ChangeColumnValue(OldValue, NewValue);
        }

        private void ChangeColumnValue(string from, string to) {
            string newKey;
            if (!string.IsNullOrEmpty(Key)) {
                newKey = Key;
                SourceRow.Status = ResXStringGridRow.STATUS.OK;
            } else {
                newKey = "A";
                SourceRow.Status = ResXStringGridRow.STATUS.KEY_NULL;
            }
            SourceRow.DataSourceItem = new ResXDataNode(newKey, to);
            SourceRow.DataSourceItem.Comment = Comment;
            SourceRow.Cells[Grid.ValueColumnName].Tag = from;
            SourceRow.Cells[Grid.ValueColumnName].Value = to;
            Grid.ValidateRow(SourceRow);
            Grid.NotifyDataChanged();
            Grid.SetContainingTabPageSelected();

            VLOutputWindow.VisualLocalizerPane.WriteLine("Edited value of \"{0}\"", Key);
        }

        public override string GetUndoDescription() {
            return string.Format("Value of \"{0}\" changed from \"{1}\" to \"{2}\"", Key, OldValue, NewValue);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
