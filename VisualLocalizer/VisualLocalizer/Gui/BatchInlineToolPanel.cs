using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VisualLocalizer.Components;

namespace VisualLocalizer.Gui {
    internal sealed class BatchInlineToolPanel : AbstractCodeToolWindowPanel {

        public BatchInlineToolPanel() {
            DataGridViewTextBoxColumn referenceColumn = new DataGridViewTextBoxColumn();
            referenceColumn.MinimumWidth = 200;
            referenceColumn.HeaderText = "Reference Text";
            referenceColumn.Name = "ReferenceText";
            referenceColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            this.Columns.Add(referenceColumn);

            DataGridViewTextBoxColumn valueColumn = new DataGridViewTextBoxColumn();
            valueColumn.MinimumWidth = 250;
            valueColumn.HeaderText = "Value";
            valueColumn.Name = "Value";
            valueColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            this.Columns.Add(valueColumn);

            DataGridViewTextBoxColumn sourceFileColumn = new DataGridViewTextBoxColumn();
            sourceFileColumn.MinimumWidth = 250;
            sourceFileColumn.HeaderText = "Source File";
            sourceFileColumn.Name = "Source";
            sourceFileColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            this.Columns.Add(sourceFileColumn);

            DataGridViewTextBoxColumn destinationColumn = new DataGridViewTextBoxColumn();
            destinationColumn.MinimumWidth = 250;
            destinationColumn.HeaderText = "Resource File";
            destinationColumn.Name = "Destination";
            destinationColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            this.Columns.Add(destinationColumn);

            DataGridViewColumn column = new DataGridViewColumn();
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.Columns.Add(column);   
        }   
     
        internal void SetData(List<CodeReferenceResultItem> value) {
            Rows.Clear();
            ErrorRowsCount = 0;
            this.SuspendLayout();

            foreach (var item in value) {
                CodeDataGridViewRow row = new CodeDataGridViewRow();
                row.CodeResultItem = item;

                DataGridViewCheckBoxCell checkCell = new DataGridViewCheckBoxCell();
                checkCell.Value = item.MoveThisItem;
                row.Cells.Add(checkCell);

                DataGridViewTextBoxCell referenceCell = new DataGridViewTextBoxCell();
                referenceCell.Value = item.ReferenceText;
                row.Cells.Add(referenceCell);

                DataGridViewTextBoxCell valueCell = new DataGridViewTextBoxCell();
                valueCell.Value = item.Value;
                row.Cells.Add(valueCell);

                DataGridViewTextBoxCell sourceCell = new DataGridViewTextBoxCell();
                sourceCell.Value = item.SourceItem.Name;
                row.Cells.Add(sourceCell);

                DataGridViewTextBoxCell destinationCell = new DataGridViewTextBoxCell();
                destinationCell.Value = item.DestinationItem.ToString();
                row.Cells.Add(destinationCell);

                DataGridViewTextBoxCell cell = new DataGridViewTextBoxCell();
                row.Cells.Add(cell);                

                Rows.Add(row);

                referenceCell.ReadOnly = true;
                valueCell.ReadOnly = true;
                sourceCell.ReadOnly = true;
                destinationCell.ReadOnly = true;
            }

            CheckedRowsCount = Rows.Count;
            checkHeader.Checked = true;
            this.ResumeLayout(true);
        }

        protected override AbstractResultItem GetResultItemFromRow(CodeDataGridViewRow row) {
            CodeReferenceResultItem item = row.CodeResultItem as CodeReferenceResultItem;
            item.MoveThisItem = (bool)(row.Cells["MoveThisItem"].Value);

            row.CodeResultItem = item;
            if (item.MoveThisItem) {                                
                return item;
            } else {                
                return GetNextResultItem();
            }
        }
    }
}
