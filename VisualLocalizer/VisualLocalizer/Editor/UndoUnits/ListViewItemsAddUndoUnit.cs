using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;

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
        }

        public override void Redo() {
            foreach (var item in Items)
                item.RemoveKind = REMOVEKIND.REMOVE;

            RemoveUnit.Undo();
        }

        public override string GetUndoDescription() {
            return string.Format("Added {0} existing media elements", Items.Count);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
