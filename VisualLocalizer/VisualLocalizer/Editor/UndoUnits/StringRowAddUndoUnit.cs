using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using VisualLocalizer.Components;

namespace VisualLocalizer.Editor.UndoUnits {

    [Guid("A7F756F0-2926-4103-A4EA-0C5D3E8522C7")]
    internal sealed class StringRowAddUndoUnit : AbstractUndoUnit {

        private List<ResXStringGridRow> Rows { get; set; }
        private ResXStringGrid Grid { get; set; }
        private KeyValueIdentifierConflictResolver ConflictResolver { get; set; }
        private ResXEditorControl Control { get; set; }

        public StringRowAddUndoUnit(ResXEditorControl control, List<ResXStringGridRow> rows, ResXStringGrid grid, KeyValueIdentifierConflictResolver conflictResolver) {
            this.Rows = rows;
            this.Grid = grid;
            this.ConflictResolver = conflictResolver;
            this.Control = control;
        }

        public override void Undo() {
            Grid.SuspendLayout();
            foreach (var Row in Rows) {
                ConflictResolver.TryAdd(Row.Key, null, Row, Control.Editor.ProjectItem);
                Row.Cells[Grid.KeyColumnName].Tag = null;
                Grid.Rows.Remove(Row);
            }
            Grid.ResumeLayout();
            Grid.NotifyDataChanged();
            Grid.SetContainingTabPageSelected();
        }

        public override void Redo() {
            Grid.SuspendLayout();
            foreach (var Row in Rows) {
                Grid.Rows.Add(Row);
                Grid.ValidateRow(Row);
            }
            Grid.ResumeLayout();
            Grid.NotifyDataChanged();
            Grid.SetContainingTabPageSelected();
        }

        public override string GetUndoDescription() {
            return string.Format("Added {0} new row(s)", Rows.Count);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
