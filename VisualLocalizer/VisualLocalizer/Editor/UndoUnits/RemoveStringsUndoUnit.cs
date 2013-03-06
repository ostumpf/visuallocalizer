using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Resources;
using VisualLocalizer.Components;

namespace VisualLocalizer.Editor.UndoUnits {

    [Guid("1E58253F-ED20-4b1b-8E52-06E4D0A023B6")]
    internal sealed class RemoveStringsUndoUnit : AbstractUndoUnit {

        private List<ResXStringGridRow> Elements { get; set; }
        private ResXStringGrid Grid { get; set; }
        private KeyValueIdentifierConflictResolver ConflictResolver { get; set; }
        private ResXEditorControl Control { get; set; }

        public RemoveStringsUndoUnit(ResXEditorControl control, List<ResXStringGridRow> elements, ResXStringGrid grid, KeyValueIdentifierConflictResolver conflictResolver) {
            this.Elements = elements;
            this.Grid = grid;
            this.ConflictResolver = conflictResolver;
            this.Control = control;
        }

        public override void Undo() {
            Grid.SuspendLayout();
            foreach (var element in Elements.Where((el) => { return el != null; }).OrderBy((el) => { return el.IndexAtDeleteTime; })) {
                Grid.Rows.Insert(element.IndexAtDeleteTime, element);
                Grid.ValidateRow(element);
            }
            Grid.ResumeLayout();
            Grid.NotifyDataChanged();

            VLOutputWindow.VisualLocalizerPane.WriteLine("Re-added {0} string rows", Elements.Count);
            Grid.SetContainingTabPageSelected();
        }

        public override void Redo() {
            Grid.SuspendLayout();
            foreach (var element in Elements.Where((el) => { return el != null; }).OrderByDescending((el) => { return el.IndexAtDeleteTime; })) {
                ResXStringGridRow row = Grid.Rows[element.IndexAtDeleteTime] as ResXStringGridRow;
                ConflictResolver.TryAdd(row.Key, null, row, Control.Editor.ProjectItem);
                row.Cells[Grid.KeyColumnName].Tag = null;
                Grid.Rows.RemoveAt(element.IndexAtDeleteTime);
            }
            Grid.ResumeLayout();
            Grid.NotifyDataChanged();

            VLOutputWindow.VisualLocalizerPane.WriteLine("Re-deleted {0} string rows", Elements.Count);
            Grid.SetContainingTabPageSelected();
        }

        public override string GetUndoDescription() {
            return string.Format("Removed {0} string element(s)", Elements.Count);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
        
    }

   /* internal sealed class StringsElement {

        public StringsElement(int index, ResXDataNode DataNode, ResXStringGridRow.STATUS status) {
            this.Index = index;
            this.DataNode = DataNode;
            this.Status = status;
        }

        public ResXStringGridRow CreateRow() {
            ResXStringGridRow row = new ResXStringGridRow();

            DataGridViewTextBoxCell keyCell = new DataGridViewTextBoxCell();            
            if (Status == ResXStringGridRow.STATUS.OK) {
                keyCell.Value = DataNode.Name;
            } else {
                keyCell.Value = "";
            }
            DataGridViewTextBoxCell valueCell = new DataGridViewTextBoxCell();
            valueCell.Value = DataNode.GetValue<string>();

            DataGridViewTextBoxCell commentCell = new DataGridViewTextBoxCell();
            commentCell.Value = DataNode.Comment;
            
            row.Cells.Add(keyCell);
            row.Cells.Add(valueCell);
            row.Cells.Add(commentCell);
            row.DataSourceItem = DataNode;
            row.Status = Status;
            row.MinimumHeight = 25;

            return row;
        }

        public int Index { get; set; }
        public ResXDataNode DataNode { get; set; }
        public ResXStringGridRow.STATUS Status { get; set; }
    }*/
}
