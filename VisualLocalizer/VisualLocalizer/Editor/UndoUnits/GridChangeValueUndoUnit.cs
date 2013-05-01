using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using VisualLocalizer.Library;
using System.Resources;
using VisualLocalizer.Components;
using VisualLocalizer.Library.Components;
using System.ComponentModel;

namespace VisualLocalizer.Editor.UndoUnits {

    /// <summary>
    /// Undo unit for modifying value of a string resource
    /// </summary>
    [Guid("9F4FDA14-9B4C-4151-9FC9-194FF0A90705")]
    internal sealed class GridChangeValueUndoUnit : AbstractUndoUnit {

        public string Key { get; private set; }
        public string OldValue { get; private set; }
        public string NewValue { get; private set; }
        public string Comment { get; private set; }
        public ResXStringGridRow SourceRow { get; private set; }
        public AbstractResXEditorGrid Grid { get; private set; }

        public GridChangeValueUndoUnit(ResXStringGridRow sourceRow, AbstractResXEditorGrid grid, string key, string oldValue, string newValue, string comment) {
            if (sourceRow == null) throw new ArgumentNullException("sourceRow");
            if (grid == null) throw new ArgumentNullException("grid");

            this.SourceRow = sourceRow;
            this.Grid = grid;
            this.Key = key;
            this.OldValue = oldValue;
            this.NewValue = newValue;
            this.Comment = comment;
        }

        public override void Undo() {
            ChangeColumnValue(NewValue, OldValue);
        }

        public override void Redo() {
            ChangeColumnValue(OldValue, NewValue);
        }

        private void ChangeColumnValue(string from, string to) {
            if (Grid.EditorControl.Editor.ReadOnly) throw new Exception("Cannot perform this operation - the document is readonly.");
            
            try {
                string newKey;
                if (!string.IsNullOrEmpty(Key)) {
                    newKey = Key;
                    SourceRow.Status = KEY_STATUS.OK;
                } else {
                    newKey = "A";
                    SourceRow.Status = KEY_STATUS.ERROR;
                }

                if (Grid is ResXStringGrid) {
                    SourceRow.DataSourceItem = new ResXDataNode(newKey, to);
                } else {
                    ResXDataNode newNode = null;
                    try {
                        newNode = new ResXDataNode(newKey, TypeDescriptor.GetConverter(((ResXOthersGridRow)SourceRow).DataType).ConvertFromString(to));
                    } catch { }
                    if (newNode != null) SourceRow.DataSourceItem = newNode;
                }

                SourceRow.DataSourceItem.Comment = Comment;
                SourceRow.Cells[Grid.ValueColumnName].Tag = from;
                SourceRow.Cells[Grid.ValueColumnName].Value = to;
                Grid.ValidateRow(SourceRow);
                Grid.NotifyDataChanged();
                Grid.SetContainingTabPageSelected();

                if (SourceRow.ErrorMessages.Count > 0) {
                    SourceRow.Status = KEY_STATUS.ERROR;
                } else {
                    SourceRow.Status = KEY_STATUS.OK;
                    SourceRow.LastValidKey = SourceRow.Key;
                }

                VLOutputWindow.VisualLocalizerPane.WriteLine("Edited value of \"{0}\"", Key);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }

        public override string GetUndoDescription() {
            return string.Format("Value of \"{0}\" changed from \"{1}\" to \"{2}\"", Key, OldValue, NewValue);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
