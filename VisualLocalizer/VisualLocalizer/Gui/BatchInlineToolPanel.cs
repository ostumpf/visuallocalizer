using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.TextManager.Interop;
using VisualLocalizer.Settings;
using System.ComponentModel;

namespace VisualLocalizer.Gui {
    
    internal sealed class BatchInlineToolPanel : AbstractCheckedGridView<CodeReferenceResultItem>,IHighlightRequestSource {
        
        public event EventHandler<CodeResultItemEventArgs> HighlightRequired;

        public BatchInlineToolPanel() : base(SettingsObject.Instance.ShowFilterContext) {
            this.MultiSelect = true;
            this.MouseUp += new MouseEventHandler(OnContextMenuShow);

            ContextMenu contextMenu = new ContextMenu();

            MenuItem stateMenu = new MenuItem("State");
            stateMenu.MenuItems.Add("Checked", new EventHandler((o, e) => { setCheckStateOfSelected(true); }));
            stateMenu.MenuItems.Add("Unchecked", new EventHandler((o, e) => { setCheckStateOfSelected(false); }));
            contextMenu.MenuItems.Add(stateMenu);            

            this.ContextMenu = contextMenu;
        }

        protected override void InitializeColumns() {
            base.InitializeColumns();
            this.CellDoubleClick += new DataGridViewCellEventHandler(OnRowDoubleClick);

            DataGridViewTextBoxColumn referenceColumn = new DataGridViewTextBoxColumn();
            referenceColumn.MinimumWidth = 200;
            referenceColumn.HeaderText = "Reference Text";
            referenceColumn.Name = "ReferenceText";
            referenceColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            this.Columns.Insert(2, referenceColumn);

            DataGridViewTextBoxColumn valueColumn = new DataGridViewTextBoxColumn();
            valueColumn.MinimumWidth = 250;
            valueColumn.HeaderText = "Value";
            valueColumn.Name = "Value";
            valueColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            this.Columns.Insert(3, valueColumn);

            DataGridViewTextBoxColumn sourceFileColumn = new DataGridViewTextBoxColumn();
            sourceFileColumn.MinimumWidth = 250;
            sourceFileColumn.HeaderText = "Source File";
            sourceFileColumn.Name = "Source";
            sourceFileColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            this.Columns.Insert(4, sourceFileColumn);

            DataGridViewTextBoxColumn destinationColumn = new DataGridViewTextBoxColumn();
            destinationColumn.MinimumWidth = 250;
            destinationColumn.HeaderText = "Resource File";
            destinationColumn.Name = "Destination";
            destinationColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            this.Columns.Insert(5, destinationColumn);

            DataGridViewColumn column = new DataGridViewColumn();
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.Columns.Add(column);   

            DataGridViewCheckedRow<CodeReferenceResultItem> template=new DataGridViewCheckedRow<CodeReferenceResultItem>();
            template.MinimumHeight = 24;
            this.RowTemplate = template;
        }

        private void OnRowDoubleClick(object sender, DataGridViewCellEventArgs e) {
            if (HighlightRequired != null && e.RowIndex >= 0) {
                HighlightRequired(this, new CodeResultItemEventArgs() {
                    Item = (Rows[e.RowIndex] as DataGridViewCheckedRow<CodeReferenceResultItem>).DataSourceItem
                });
            }
        }

        public override void SetData(List<CodeReferenceResultItem> value) {
            Rows.Clear();
            errorRows.Clear();
            this.SuspendLayout();
            if (Columns.Contains(ContextColumnName)) Columns[ContextColumnName].Visible = SettingsObject.Instance.ShowFilterContext;

            foreach (var item in value) {
                DataGridViewCheckedRow<CodeReferenceResultItem> row = new DataGridViewCheckedRow<CodeReferenceResultItem>();
                row.DataSourceItem = item;

                DataGridViewCheckBoxCell checkCell = new DataGridViewCheckBoxCell();
                checkCell.Value = item.MoveThisItem;
                row.Cells.Add(checkCell);

                DataGridViewTextBoxCell lineCell = new DataGridViewTextBoxCell();
                lineCell.Value = item.ReplaceSpan.iStartLine + 1;
                row.Cells.Add(lineCell);

                DataGridViewTextBoxCell referenceCell = new DataGridViewTextBoxCell();
                referenceCell.Value = item.FullReferenceText;
                row.Cells.Add(referenceCell);

                DataGridViewTextBoxCell valueCell = new DataGridViewTextBoxCell();
                valueCell.Value = item.Value;
                row.Cells.Add(valueCell);               

                DataGridViewTextBoxCell sourceCell = new DataGridViewTextBoxCell();
                sourceCell.Value = item.SourceItem.Name;
                row.Cells.Add(sourceCell);

                DataGridViewTextBoxCell destinationCell = new DataGridViewTextBoxCell();
                destinationCell.Value = item.DestinationItem.ToString();
                VLDocumentViewsManager.SetFileReadonly(item.DestinationItem.InternalProjectItem.GetFullPath(), true);
                row.Cells.Add(destinationCell);
                
                DataGridViewDynamicWrapCell contextCell = new DataGridViewDynamicWrapCell();
                contextCell.Value = item.Context;
                contextCell.RelativeLine = item.ContextRelativeLine;
                contextCell.FullText = item.Context;
                contextCell.SetWrapContents(false);
                row.Cells.Add(contextCell);
                                   
                DataGridViewTextBoxCell cell = new DataGridViewTextBoxCell();
                row.Cells.Add(cell);                

                Rows.Add(row);

                referenceCell.ReadOnly = true;
                valueCell.ReadOnly = true;
                sourceCell.ReadOnly = true;
                destinationCell.ReadOnly = true;
                lineCell.ReadOnly = true;
                contextCell.ReadOnly = true;
            }

            CheckedRowsCount = Rows.Count;
            CheckHeader.Checked = true;
            this.ClearSelection();
            this.ResumeLayout(true);
            this.OnResize(null);
            NotifyErrorRowsChanged();

            if (SortedColumn != null) {
                Sort(SortedColumn, SortOrder == SortOrder.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending);
            }
        }

        protected override CodeReferenceResultItem GetResultItemFromRow(DataGridViewRow row) {
            var typedRow = row as DataGridViewCheckedRow<CodeReferenceResultItem>;
            CodeReferenceResultItem item = typedRow.DataSourceItem;
            item.MoveThisItem = (bool)(typedRow.Cells[CheckBoxColumnName].Value);

            typedRow.DataSourceItem = item;
            return item;
        }

        public override string CheckBoxColumnName {
            get { return "InlineThisItem"; }
        }
    }
}
