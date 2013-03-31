using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using VisualLocalizer.Components;

namespace VisualLocalizer.Editor.UndoUnits {

    /// <summary>
    /// Undo unit for adding new items to the editor's list view
    /// </summary>
    [Guid("94CA2FEB-261E-465a-A2EA-0980C86AF2A2")]
    internal sealed class ListViewItemsAddUndoUnit : AbstractUndoUnit {

        private List<ListViewKeyItem> Items { get; set; }
        private ListViewRemoveItemsUndoUnit RemoveUnit { get; set; }

        public ListViewItemsAddUndoUnit(ResXEditorControl control, List<ListViewKeyItem> items, KeyValueIdentifierConflictResolver conflictResolver) {
            if (control == null) throw new ArgumentNullException("control");
            if (items == null) throw new ArgumentNullException("items");
            if (conflictResolver == null) throw new ArgumentNullException("conflictResolver");

            this.Items = items;            
    
            // create the reverse unit
            RemoveUnit = new ListViewRemoveItemsUndoUnit(control, items, conflictResolver);
        }

        public override void Undo() {
            try {
                foreach (var item in Items) {
                    item.RemoveKind = REMOVEKIND.REMOVE;
                    item.IndexAtDeleteTime = item.Index;
                }

                RemoveUnit.Redo();
                VLOutputWindow.VisualLocalizerPane.WriteLine("Removed {0} added files", Items.Count);

                if (Items.Count > 0 && Items[0].AbstractListView != null) Items[0].AbstractListView.SetContainingTabPageSelected();
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        public override void Redo() {
            try {
                foreach (var item in Items) {
                    item.RemoveKind = REMOVEKIND.REMOVE;
                    item.IndexAtDeleteTime = item.Index;
                }

                RemoveUnit.Undo();
                VLOutputWindow.VisualLocalizerPane.WriteLine("Re-added {0} files", Items.Count);

                if (Items.Count > 0 && Items[0].AbstractListView != null) Items[0].AbstractListView.SetContainingTabPageSelected();
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        public override string GetUndoDescription() {
            return string.Format("Added {0} media elements", Items.Count);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
