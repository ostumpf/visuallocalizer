using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;

namespace VisualLocalizer.Editor.UndoUnits {

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
            Item.DataNode.Comment = OldComment;
            Item.SubItems["Comment"].Text = OldComment;
        }

        public override void Redo() {
            Item.DataNode.Comment = NewComment;
            Item.SubItems["Comment"].Text = NewComment;
        }

        public override string GetUndoDescription() {
            return string.Format("Changed comment of \"{0}\"", Key);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
