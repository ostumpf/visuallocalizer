﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using VisualLocalizer.Components;
using VisualLocalizer.Library.Components;

namespace VisualLocalizer.Editor.UndoUnits {

    /// <summary>
    /// Represents undo unit for adding new string resources
    /// </summary>
    [Guid("A7F756F0-2926-4103-A4EA-0C5D3E8522C7")]
    internal sealed class GridRowAddUndoUnit : AbstractUndoUnit {

        private List<ResXStringGridRow> Rows { get; set; }
        private AbstractResXEditorGrid Grid { get; set; }
        private KeyValueIdentifierConflictResolver ConflictResolver { get; set; }
        private ResXEditorControl Control { get; set; }

        public GridRowAddUndoUnit(ResXEditorControl control, List<ResXStringGridRow> rows, AbstractResXEditorGrid grid, KeyValueIdentifierConflictResolver conflictResolver) {
            if (control == null) throw new ArgumentNullException("control");
            if (grid == null) throw new ArgumentNullException("grid");
            if (rows == null) throw new ArgumentNullException("rows");
            if (conflictResolver == null) throw new ArgumentNullException("conflictResolver");

            this.Rows = rows;
            this.Grid = grid;
            this.ConflictResolver = conflictResolver;
            this.Control = control;
        }

        public override void Undo() {
            if (Grid.EditorControl.Editor.ReadOnly) throw new Exception("Cannot perform this operation - the document is readonly.");

            try {
                Grid.SuspendLayout();
                
                // remove the rows
                foreach (var Row in Rows) {
                    ConflictResolver.TryAdd(Row.Key, null, Row, Control.Editor.ProjectItem, null);
                    Row.Cells[Grid.KeyColumnName].Tag = null;
                    Grid.Rows.Remove(Row);
                }
                Grid.ResumeLayout();
                Grid.NotifyDataChanged();
                Grid.SetContainingTabPageSelected();
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }

        public override void Redo() {
            if (Grid.EditorControl.Editor.ReadOnly) throw new Exception("Cannot perform this operation - the document is readonly.");

            try {
                Grid.SuspendLayout();
                // re-add the rows
                foreach (var Row in Rows) {
                    Grid.Rows.Add(Row);
                    Grid.ValidateRow(Row);
                }
                Grid.ResumeLayout();
                Grid.NotifyDataChanged();
                Grid.SetContainingTabPageSelected();
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }

        public override string GetUndoDescription() {
            return string.Format("Added {0} new row(s)", Rows.Count);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
