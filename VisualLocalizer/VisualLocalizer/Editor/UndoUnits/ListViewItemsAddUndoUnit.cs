using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using VisualLocalizer.Components;

namespace VisualLocalizer.Editor.UndoUnits {

    [Guid("94CA2FEB-261E-465a-A2EA-0980C86AF2A2")]
    internal sealed class ListViewItemsAddUndoUnit : AbstractUndoUnit {

        private List<ListViewKeyItem> Items { get; set; }
        private ListViewRemoveItemsUndoUnit RemoveUnit { get; set; }
        
        public ListViewItemsAddUndoUnit(List<ListViewKeyItem> items, KeyValueConflictResolver conflictResolver) {
            this.Items = items;            
    
            RemoveUnit = new ListViewRemoveItemsUndoUnit(items, conflictResolver);
        }

        public override void Undo() {
            foreach (var item in Items)
                item.RemoveKind = REMOVEKIND.REMOVE;

            RemoveUnit.Redo();
            VLOutputWindow.VisualLocalizerPane.WriteLine("Removed {0} added files", Items.Count);

            if (Items.Count > 0 && Items[0].AbstractListView != null) Items[0].AbstractListView.SetContainingTabPageSelected();
        }

        public override void Redo() {
            foreach (var item in Items)
                item.RemoveKind = REMOVEKIND.REMOVE;

            RemoveUnit.Undo();
            VLOutputWindow.VisualLocalizerPane.WriteLine("Re-added {0} existing files", Items.Count);

            if (Items.Count > 0 && Items[0].AbstractListView != null) Items[0].AbstractListView.SetContainingTabPageSelected();
        }

        public override string GetUndoDescription() {
            return string.Format("Added {0} existing media elements", Items.Count);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
