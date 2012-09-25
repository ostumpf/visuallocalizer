using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.TextManager.Interop;

namespace VisualLocalizer.Gui {
    internal sealed class BatchInlineToolPanel : AbstractCheckedGridView<CodeReferenceResultItem>,IHighlightRequestSource {

        public event EventHandler<CodeResultItemEventArgs> HighlightRequired;

        protected override void InitializeColumns() {
            base.InitializeColumns();
            this.CellDoubleClick += new DataGridViewCellEventHandler(OnRowDoubleClick);

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

        private void OnRowDoubleClick(object sender, DataGridViewCellEventArgs e) {
            if (HighlightRequired != null && e.RowIndex >= 0) {
                HighlightRequired(this, new CodeResultItemEventArgs() { 
                    Item = (Rows[e.RowIndex] as CodeDataGridViewRow<CodeReferenceResultItem>).DataSourceItem
                });
            }
        }

        public override void SetData(List<CodeReferenceResultItem> value) {
            Rows.Clear();
            ErrorRowsCount = 0;
            this.SuspendLayout();

            foreach (var item in value) {
                CodeDataGridViewRow<CodeReferenceResultItem> row = new CodeDataGridViewRow<CodeReferenceResultItem>();
                row.DataSourceItem = item;

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
            CheckHeader.Checked = true;
            this.ResumeLayout(true);
            this.OnResize(null);
        }

        protected override CodeReferenceResultItem GetResultItemFromRow(CodeDataGridViewRow<CodeReferenceResultItem> row) {
            CodeReferenceResultItem item = row.DataSourceItem;
            item.MoveThisItem = (bool)(row.Cells[CheckBoxColumnName].Value);

            row.DataSourceItem = item;
            if (item.MoveThisItem) {                                
                return item;
            } else {                
                return GetNextResultItem();
            }
        }

        public void SetItemFinished(bool ok, int newLength) {
            if (CurrentItemIndex == null || CurrentItemIndex < 0) throw new ArgumentException("currentItemIndex");

            if (ok) {
                AbstractResultItem resultItem = (Rows[CurrentItemIndex.Value] as CodeDataGridViewRow<AbstractResultItem>).DataSourceItem;
                TextSpan currentReplaceSpan = resultItem.ReplaceSpan;

                int diff = currentReplaceSpan.iEndLine - currentReplaceSpan.iStartLine;
                for (int i = CurrentItemIndex.Value + 1; i < Rows.Count; i++) {
                    AbstractResultItem item = (Rows[i] as CodeDataGridViewRow<AbstractResultItem>).DataSourceItem;
                    item.AbsoluteCharOffset += newLength - resultItem.AbsoluteCharLength;

                    if (item.ReplaceSpan.iStartLine > currentReplaceSpan.iEndLine) {
                        TextSpan newSpan = new TextSpan();
                        newSpan.iEndIndex = item.ReplaceSpan.iEndIndex;
                        newSpan.iStartIndex = item.ReplaceSpan.iStartIndex;
                        newSpan.iEndLine = item.ReplaceSpan.iEndLine - diff;
                        newSpan.iStartLine = item.ReplaceSpan.iStartLine - diff;
                        item.ReplaceSpan = newSpan;
                    } else if (item.ReplaceSpan.iStartLine == currentReplaceSpan.iEndLine) {
                        TextSpan newSpan = new TextSpan();
                        newSpan.iStartIndex = currentReplaceSpan.iStartIndex + newLength + item.ReplaceSpan.iStartIndex - currentReplaceSpan.iEndIndex;
                        if (item.ReplaceSpan.iEndLine == item.ReplaceSpan.iStartLine) {
                            newSpan.iEndIndex = newSpan.iStartIndex + item.ReplaceSpan.iEndIndex - item.ReplaceSpan.iStartIndex;
                        } else {
                            newSpan.iEndIndex = item.ReplaceSpan.iEndIndex;
                        }
                        newSpan.iEndLine = item.ReplaceSpan.iEndLine - diff;
                        newSpan.iStartLine = item.ReplaceSpan.iStartLine - diff;
                        item.ReplaceSpan = newSpan;
                    }
                }

                Rows.RemoveAt(CurrentItemIndex.Value);
                CheckedRowsCount--;
                UpdateCheckHeader();
            }
        }

        public override string CheckBoxColumnName {
            get { return "InlineThisItem"; }
        }
    }
}
