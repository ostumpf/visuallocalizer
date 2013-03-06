using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using VisualLocalizer.Components;

namespace VisualLocalizer.Editor.UndoUnits {

    [Guid("330CFB15-620A-4f2f-88D4-7E70B8BB3250")]
    internal sealed class ListViewNewItemCreateUndoUnit : AbstractUndoUnit {
        private ListViewKeyItem Item { get; set; }
        private ListViewRemoveItemsUndoUnit RemoveUnit { get; set; }

        public ListViewNewItemCreateUndoUnit(ResXEditorControl control, ListViewKeyItem item, KeyValueIdentifierConflictResolver conflictResolver) {
            this.Item = item;

            RemoveUnit = new ListViewRemoveItemsUndoUnit(control, new List<ListViewKeyItem>() { item }, conflictResolver);
        }

        public override void Undo() {
            Item.RemoveKind = REMOVEKIND.REMOVE;

            RemoveUnit.Redo();
            if (Item.AbstractListView != null) Item.AbstractListView.SetContainingTabPageSelected();
        }

        public override void Redo() {
            Item.RemoveKind = REMOVEKIND.REMOVE;

            RemoveUnit.Undo();
            if (Item.AbstractListView != null) Item.AbstractListView.SetContainingTabPageSelected();
        }

        public override string GetUndoDescription() {
            return "Created new media element";
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
