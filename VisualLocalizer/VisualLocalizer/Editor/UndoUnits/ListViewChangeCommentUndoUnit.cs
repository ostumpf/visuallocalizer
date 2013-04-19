using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using VisualLocalizer.Components;

namespace VisualLocalizer.Editor.UndoUnits {

    /// <summary>
    /// Undo unit for comment change in editor's list view
    /// </summary>
    [Guid("2CB8F2A1-BD3D-4d9f-BD21-FB3B06047F2F")]
    internal sealed class ListViewChangeCommentUndoUnit : AbstractUndoUnit {

        private string Key { get; set; }
        private string OldComment { get; set; }
        private string NewComment { get; set; }
        private ListViewKeyItem Item { get; set; }

        public ListViewChangeCommentUndoUnit(ListViewKeyItem item, string oldComment, string newComment, string key) {
            this.Key = key;
            this.OldComment = oldComment;
            this.NewComment = newComment;
            this.Item = item;
        }

        public override void Undo() {
            if (((AbstractListView)Item.ListView).EditorControl.Editor.ReadOnly) throw new Exception("Cannot perform this operation - the document is readonly.");

            try {
                Item.DataNode.Comment = OldComment;
                Item.SubItems["Comment"].Text = OldComment;

                VLOutputWindow.VisualLocalizerPane.WriteLine("Edited comment of \"{0}\"", Key);
                if (Item.AbstractListView != null) Item.AbstractListView.SetContainingTabPageSelected();
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        public override void Redo() {
            if (((AbstractListView)Item.ListView).EditorControl.Editor.ReadOnly) throw new Exception("Cannot perform this operation - the document is readonly.");

            try {
                Item.DataNode.Comment = NewComment;
                Item.SubItems["Comment"].Text = NewComment;

                VLOutputWindow.VisualLocalizerPane.WriteLine("Edited comment of \"{0}\"", Key);
                if (Item.AbstractListView != null) Item.AbstractListView.SetContainingTabPageSelected();
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        public override string GetUndoDescription() {
            return string.Format("Changed comment of \"{0}\"", Key);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
