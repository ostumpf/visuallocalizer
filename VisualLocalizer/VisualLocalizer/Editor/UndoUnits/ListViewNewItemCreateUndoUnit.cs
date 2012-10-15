using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;

namespace VisualLocalizer.Editor.UndoUnits {

    [Guid("330CFB15-620A-4f2f-88D4-7E70B8BB3250")]
    internal sealed class ListViewNewItemCreateUndoUnit : AbstractUndoUnit {
        private ListViewKeyItem Item { get; set; }
        private ListViewRemoveItemsUndoUnit RemoveUnit { get; set; }

        public ListViewNewItemCreateUndoUnit(ListViewKeyItem item, KeyValueConflictResolver conflictResolver) {
            this.Item = item;

            RemoveUnit = new ListViewRemoveItemsUndoUnit(new List<ListViewKeyItem>() { item }, conflictResolver);
        }

        public override void Undo() {
            Item.RemoveKind = REMOVEKIND.REMOVE;

            RemoveUnit.Redo();
        }

        public override void Redo() {
            Item.RemoveKind = REMOVEKIND.REMOVE;

            RemoveUnit.Undo();
        }

        public override string GetUndoDescription() {
            return "Created new media element";
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
