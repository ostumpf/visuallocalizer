using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using VisualLocalizer.Components;

namespace VisualLocalizer.Editor.UndoUnits {

    /// <summary>
    /// Undo unit for "Make external" action on list view's items
    /// </summary>
    [Guid("548ED4D1-72DF-4484-8B9D-F7300ED8483D")]
    internal sealed class ListViewMakeExternalUndoUnit : AbstractUndoUnit {
        private List<ListViewKeyItem> Items { get; set; }
        private AbstractListView ListView { get; set; }
        private bool ReferenceExisting { get; set; }

        public ListViewMakeExternalUndoUnit(AbstractListView listView, List<ListViewKeyItem> items, bool referenceExisting) {
            if (listView == null) throw new ArgumentNullException("listView");
            if (items == null) throw new ArgumentNullException("items");

            this.Items = items;
            this.ListView = listView;
            this.ReferenceExisting = referenceExisting;
        }

        public override void Undo() {
            if (ListView.EditorControl.Editor.ReadOnly) throw new Exception("Cannot perform this operation - the document is readonly.");

            try {
                ListView.MakeResourcesEmbedded(Items, !ReferenceExisting, false);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        public override void Redo() {
            if (ListView.EditorControl.Editor.ReadOnly) throw new Exception("Cannot perform this operation - the document is readonly.");

            try {
                ListView.MakeResourcesExternal(Items, ReferenceExisting, false);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        public override string GetUndoDescription() {
            return string.Format("Make resources ({0}) external", Items.Count);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
