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
    /// Undo unit for modifying type of a typed resource
    /// </summary>
    [Guid("D9615AF7-C8A9-4ced-A2CA-CF13808A4C67")]
    internal sealed class OthersChangeTypeUndoUnit : AbstractUndoUnit {

        public string Key { get; private set; }
        public string StringValue { get; private set; }
        public Type OldValue { get; private set; }
        public Type NewValue { get; private set; }
        public string Comment { get; private set; }
        public ResXOthersGridRow SourceRow { get; private set; }
        public ResXOthersGrid Grid { get; private set; }

        public OthersChangeTypeUndoUnit(ResXOthersGridRow sourceRow, ResXOthersGrid grid, string key, Type oldValue, Type newValue, string strValue, string comment) {
            if (sourceRow == null) throw new ArgumentNullException("sourceRow");
            if (grid == null) throw new ArgumentNullException("grid");
            if (oldValue == null) throw new ArgumentNullException("oldValue");
            if (newValue == null) throw new ArgumentNullException("newValue");

            this.SourceRow = sourceRow;
            this.Grid = grid;
            this.Key = key;
            this.OldValue = oldValue;
            this.NewValue = newValue;
            this.Comment = comment;
            this.StringValue = strValue;
        }

        public override void Undo() {
            ChangeColumnValue(NewValue, OldValue);
        }

        public override void Redo() {
            ChangeColumnValue(OldValue, NewValue);
        }

        private void ChangeColumnValue(Type from, Type to) {
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

                ResXDataNode newNode = null;
                try {
                    newNode = new ResXDataNode(newKey, TypeDescriptor.GetConverter(to).ConvertFromString(StringValue));
                } catch { }
                if (newNode != null) SourceRow.DataSourceItem = newNode;

                SourceRow.DataSourceItem.Comment = Comment;
                SourceRow.DataType = to;
                SourceRow.Cells[Grid.TypeColumnName].Tag = from;
                SourceRow.Cells[Grid.TypeColumnName].Value = to.FullName;
                Grid.ValidateRow(SourceRow);
                Grid.NotifyDataChanged();
                Grid.SetContainingTabPageSelected();

                if (SourceRow.ErrorMessages.Count > 0) {
                    SourceRow.Status = KEY_STATUS.ERROR;
                } else {
                    SourceRow.Status = KEY_STATUS.OK;
                    SourceRow.LastValidKey = SourceRow.Key;
                }

                VLOutputWindow.VisualLocalizerPane.WriteLine("Edited type of \"{0}\"", Key);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }

        public override string GetUndoDescription() {
            return string.Format("Type of \"{0}\" changed from \"{1}\" to \"{2}\"", Key, OldValue.Name, NewValue.Name);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
