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

    /// <summary>
    /// Represents undo unit for the action of removing string resources from the grid
    /// </summary>
    [Guid("1E58253F-ED20-4b1b-8E52-06E4D0A023B6")]
    internal sealed class RemoveStringsUndoUnit : AbstractUndoUnit {

        private List<ResXStringGridRow> Elements { get; set; }
        private ResXStringGrid Grid { get; set; }
        private KeyValueIdentifierConflictResolver ConflictResolver { get; set; }
        private ResXEditorControl Control { get; set; }

        public RemoveStringsUndoUnit(ResXEditorControl control, List<ResXStringGridRow> elements, ResXStringGrid grid, KeyValueIdentifierConflictResolver conflictResolver) {
            if (control == null) throw new ArgumentNullException("control");
            if (elements == null) throw new ArgumentNullException("elements");
            if (grid == null) throw new ArgumentNullException("grid");
            if (conflictResolver == null) throw new ArgumentNullException("conflictResolver");

            this.Elements = elements;
            this.Grid = grid;
            this.ConflictResolver = conflictResolver;
            this.Control = control;
        }

        public override void Undo() {
            try {
                Grid.SuspendLayout();
                
                // insert rows back to their positions
                foreach (var element in Elements.Where((el) => { return el != null; }).OrderBy((el) => { return el.IndexAtDeleteTime; })) {
                    Grid.Rows.Insert(element.IndexAtDeleteTime, element);
                    Grid.ValidateRow(element); // revalidate
                }
                Grid.ResumeLayout();
                Grid.NotifyDataChanged();

                VLOutputWindow.VisualLocalizerPane.WriteLine("Re-added {0} string rows", Elements.Count);
                Grid.SetContainingTabPageSelected();
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        public override void Redo() {
            try {
                Grid.SuspendLayout();
                foreach (var element in Elements.Where((el) => { return el != null; }).OrderByDescending((el) => { return el.IndexAtDeleteTime; })) {
                    ResXStringGridRow row = Grid.Rows[element.IndexAtDeleteTime] as ResXStringGridRow;

                    // unregister from the conflict resolver
                    ConflictResolver.TryAdd(row.Key, null, row, Control.Editor.ProjectItem, null);
                    row.Cells[Grid.KeyColumnName].Tag = null;

                    // remvoe the row
                    Grid.Rows.RemoveAt(element.IndexAtDeleteTime);
                }
                Grid.ResumeLayout();
                Grid.NotifyDataChanged();

                VLOutputWindow.VisualLocalizerPane.WriteLine("Re-deleted {0} string rows", Elements.Count);
                Grid.SetContainingTabPageSelected();
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        public override string GetUndoDescription() {
            return string.Format("Removed {0} string element(s)", Elements.Count);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
        
    }

}
