using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;

namespace VisualLocalizer.Editor.UndoUnits {

    [Guid("548ED4D1-72DF-4484-8B9D-F7300ED8483D")]
    internal sealed class ListViewMakeExternalUndoUnit : AbstractUndoUnit {
        private List<ListViewKeyItem> Items { get; set; }
        private AbstractListView ListView { get; set; }
        private bool ReferenceExisting { get; set; }

        public ListViewMakeExternalUndoUnit(AbstractListView listView, List<ListViewKeyItem> items, bool referenceExisting) {
            this.Items = items;
            this.ListView = listView;
            this.ReferenceExisting = referenceExisting;
        }

        public override void Undo() {
            ListView.MakeResourcesEmbedded(Items, !ReferenceExisting, false);
        }

        public override void Redo() {
            ListView.MakeResourcesExternal(Items, ReferenceExisting, false);
        }

        public override string GetUndoDescription() {
            return string.Format("Make resources ({0}) external", Items.Count);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
