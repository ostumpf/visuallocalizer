using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace VisualLocalizer.Editor.UndoUnits {

    [Guid("406668D9-6E03-47c8-B838-FA4EE1EF1896")]
    internal sealed class ListViewRenameKeyUndoUnit : RenameKeyUndoUnit {

        private ListViewKeyItem Item { get; set; }
        private AbstractListView ListView { get; set; }

        public ListViewRenameKeyUndoUnit(AbstractListView listView, ListViewKeyItem item, string oldKey, string newKey)
            : base(oldKey, newKey) {
            this.Item = item;
            this.ListView = listView;
        }

        public override void Undo() {
            Item.Text = OldKey;
            Item.BeforeEditValue = NewKey;
            Item.AfterEditValue = OldKey;
            ListView.Validate(Item);
            ListView.NotifyDataChanged();
        }

        public override void Redo() {
            Item.Text = NewKey;
            Item.BeforeEditValue = OldKey;
            Item.AfterEditValue = NewKey;
            ListView.Validate(Item);
            ListView.NotifyDataChanged();
        }
    }
}
