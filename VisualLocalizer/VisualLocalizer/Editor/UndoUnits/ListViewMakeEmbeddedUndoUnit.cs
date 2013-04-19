using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using VisualLocalizer.Components;

namespace VisualLocalizer.Editor.UndoUnits {

    /// <summary>
    /// Undo unit for "Make embedded" action on list view's resources
    /// </summary>
    [Guid("DD50A387-8EAF-4451-9595-5C1674CAC03B")]
    internal sealed class ListViewMakeEmbeddedUndoUnit : AbstractUndoUnit {
        private List<ListViewKeyItem> Items { get; set; }
        private AbstractListView ListView { get; set; }
        private bool Deleted { get; set; }

        public ListViewMakeEmbeddedUndoUnit(AbstractListView listView, List<ListViewKeyItem> items, bool delete) {
            if (listView == null) throw new ArgumentNullException("listView");
            if (items == null) throw new ArgumentNullException("items");

            this.Items = items;
            this.ListView = listView;
            this.Deleted = delete;
        }
        
        public override void Undo() {
            if (ListView.EditorControl.Editor.ReadOnly) throw new Exception("Cannot perform this operation - the document is readonly.");

            try {
                ListView.MakeResourcesExternal(Items, !Deleted, false);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        public override void Redo() {
            if (ListView.EditorControl.Editor.ReadOnly) throw new Exception("Cannot perform this operation - the document is readonly.");

            try {
                ListView.MakeResourcesEmbedded(Items, Deleted, false);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        public override string GetUndoDescription() {
            return string.Format("Make resources ({0}) embedded{1}", Items.Count, Deleted ? " and delete originals":"");
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
