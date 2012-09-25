using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using VisualLocalizer.Library;
using System.Resources;

namespace VisualLocalizer.Editor.UndoUnits {

    [Guid("E00A8D51-409A-453b-83AB-B63B5B79AC76")]
    internal sealed class StringChangeCommentUndoUnit : AbstractUndoUnit {

        public string Key { get; private set; }
        public string OldComment { get; private set; }
        public string NewComment { get; private set; }        
        public CodeDataGridViewRow<ResXDataNode> SourceRow { get; private set; }
        public ResXStringGrid Grid { get; private set; }

        public StringChangeCommentUndoUnit(CodeDataGridViewRow<ResXDataNode> sourceRow, ResXStringGrid grid, string key, string oldComment, string newComment) {
            this.SourceRow = sourceRow;
            this.Grid = grid;
            this.Key = key;
            this.OldComment = oldComment;
            this.NewComment = newComment;
        }

        public override void Undo() {
            ChangeComment(NewComment, OldComment);   
        }

        public override void Redo() {
            ChangeComment(OldComment, NewComment);
        }

        private void ChangeComment(string from, string to) {
            SourceRow.DataSourceItem.Comment = to;
            SourceRow.Cells[Grid.CommentColumnName].Tag = from;
            SourceRow.Cells[Grid.CommentColumnName].Value = to;
            Grid.ValidateRow(SourceRow);
            Grid.NotifyDataChanged();
        }



        public override string GetUndoDescription() {
            return string.Format("Comment of \"{0}\" changed", Key);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
