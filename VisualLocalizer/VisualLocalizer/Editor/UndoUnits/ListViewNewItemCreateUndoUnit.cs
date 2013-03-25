using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using VisualLocalizer.Components;

namespace VisualLocalizer.Editor.UndoUnits {

    /// <summary>
    /// Represents undoable action in which newly created resource was added to the file
    /// </summary>
    [Guid("330CFB15-620A-4f2f-88D4-7E70B8BB3250")]
    internal sealed class ListViewNewItemCreateUndoUnit : AbstractUndoUnit {
        private ListViewKeyItem Item { get; set; }
        private ListViewRemoveItemsUndoUnit RemoveUnit { get; set; }

        public ListViewNewItemCreateUndoUnit(ResXEditorControl control, ListViewKeyItem item, KeyValueIdentifierConflictResolver conflictResolver) {
            if (control == null) throw new ArgumentNullException("control");
            if (item == null) throw new ArgumentNullException("item");
            if (conflictResolver == null) throw new ArgumentNullException("conflictResolver");

            this.Item = item;

            RemoveUnit = new ListViewRemoveItemsUndoUnit(control, new List<ListViewKeyItem>() { item }, conflictResolver);
        }

        public override void Undo() {
            try {
                Item.RemoveKind = REMOVEKIND.REMOVE;

                RemoveUnit.Redo();
                if (Item.AbstractListView != null) Item.AbstractListView.SetContainingTabPageSelected();
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        public override void Redo() {
            try {
                Item.RemoveKind = REMOVEKIND.REMOVE;

                RemoveUnit.Undo();
                if (Item.AbstractListView != null) Item.AbstractListView.SetContainingTabPageSelected();
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        public override string GetUndoDescription() {
            return "Created new media element";
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
