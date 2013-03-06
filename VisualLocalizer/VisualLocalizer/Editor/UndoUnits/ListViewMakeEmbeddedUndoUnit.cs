using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;

namespace VisualLocalizer.Editor.UndoUnits {

    [Guid("DD50A387-8EAF-4451-9595-5C1674CAC03B")]
    internal sealed class ListViewMakeEmbeddedUndoUnit : AbstractUndoUnit {
        private List<ListViewKeyItem> Items { get; set; }
        private AbstractListView ListView { get; set; }
        private bool Deleted { get; set; }

        public ListViewMakeEmbeddedUndoUnit(AbstractListView listView, List<ListViewKeyItem> items, bool delete) {
            this.Items = items;
            this.ListView = listView;
            this.Deleted = delete;
        }
        
        public override void Undo() {
            ListView.MakeResourcesExternal(Items, !Deleted, false);
        }

        public override void Redo() {
            ListView.MakeResourcesEmbedded(Items, Deleted, false);
        }

        public override string GetUndoDescription() {
            return string.Format("Make resources ({0}) embedded{1}", Items.Count, Deleted ? " and delete originals":"");
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
