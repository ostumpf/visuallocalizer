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
    
    /// <summary>
    /// Content of the "Batch inline" toolwindow.
    /// </summary>
    internal sealed class BatchInlineToolGrid : AbstractCheckedGridView<CodeReferenceResultItem>,IHighlightRequestSource {
        
        /// <summary>
        /// Issued when row is double-clicked - causes corresponding block of code in the code window to be selected
        /// </summary>
        public event EventHandler<CodeResultItemEventArgs> HighlightRequired;

        /// <summary>
        /// Determines whether source and destination files are locked during SetData()
        /// </summary>
        public bool LockFiles {
            get;
            set;
        }

        private bool _ContextMenuEnabled;
        private ContextMenu contextMenu;

        /// <summary>
        /// True if SetData() was already called
        /// </summary>
        public bool SetDataFinished {
            get;
            private set;
        }

        /// <summary>
        /// True if context menu is enabled
        /// </summary>
        public bool ContextMenuEnabled {
            get { return _ContextMenuEnabled; }
            set {
                _ContextMenuEnabled = value;
                this.ContextMenu = value ? contextMenu : new ContextMenu();
            }
        }

        public BatchInlineToolGrid() : base(SettingsObject.Instance.ShowContextColumn) {
            this.MultiSelect = true;
            this.MouseUp += new MouseEventHandler(OnContextMenuShow);

            // create context menu with option to (un)check selected rows
            contextMenu = new ContextMenu();

            MenuItem stateMenu = new MenuItem("State");
            stateMenu.MenuItems.Add("Checked", new EventHandler((o, e) => {
                try {
                    SetCheckStateOfSelected(true);
                } catch (Exception ex) {
                    VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                    VisualLocalizer.Library.MessageBox.ShowException(ex);
                }
            }));
            stateMenu.MenuItems.Add("Unchecked", new EventHandler((o, e) => {
                try {
                    SetCheckStateOfSelected(false);
                } catch (Exception ex) {
                    VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                    VisualLocalizer.Library.MessageBox.ShowException(ex);
                }
            }));
            contextMenu.MenuItems.Add(stateMenu);            
            
            this.ContextMenu = contextMenu;
            
            SettingsObject.Instance.RevalidationRequested += new Action(Instance_RevalidationRequested);           
        }
      
        /// <summary>
        /// Updates visibility of the checkbox column after modified in the settings
        /// </summary>
        private void Instance_RevalidationRequested() {
            if (!this.Visible) return;
            if (!string.IsNullOrEmpty(ContextColumnName) && Columns.Contains(ContextColumnName)) {
                Columns[ContextColumnName].Visible = SettingsObject.Instance.ShowContextColumn;
            }
        }

        /// <summary>
        /// Initializes grid columns
        /// </summary>
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
            valueColumn.MinimumWidth = 50;
            valueColumn.Width = 250;
            valueColumn.HeaderText = "Value";
            valueColumn.Name = "Value";
            valueColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            this.Columns.Insert(3, valueColumn);

            DataGridViewTextBoxColumn sourceFileColumn = new DataGridViewTextBoxColumn();
            sourceFileColumn.MinimumWidth = 50;
            sourceFileColumn.Width = 250;
            sourceFileColumn.HeaderText = "Source File";
            sourceFileColumn.Name = "Source";
            sourceFileColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            this.Columns.Insert(4, sourceFileColumn);

            DataGridViewTextBoxColumn destinationColumn = new DataGridViewTextBoxColumn();
            destinationColumn.MinimumWidth = 50;
            destinationColumn.Width = 250;
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

        /// <summary>
        /// Sets grid content
        /// </summary>        
        public override void SetData(List<CodeReferenceResultItem> value) {
            if (value == null) throw new ArgumentNullException("value");
            try {
                Rows.Clear();
                errorRows.Clear();
                this.SuspendLayout();

                // adjust "context" column visibility according to the settings
                if (Columns.Contains(ContextColumnName)) Columns[ContextColumnName].Visible = SettingsObject.Instance.ShowContextColumn;

                // create new row for each result item
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
                    if (LockFiles) VLDocumentViewsManager.SetFileReadonly(item.DestinationItem.InternalProjectItem.GetFullPath(), true); // lock selected destination file
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

                // perform sorting
                if (SortedColumn != null) {
                    Sort(SortedColumn, SortOrder == SortOrder.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending);
                }
            } finally {
                SetDataFinished = true;
            }
        }

        /// <summary>
        /// Restores the removed rows
        /// </summary>
        public override List<DataGridViewRow> RestoreRemovedRows() {
            ClearSelection();
            previouslySelectedRow = null;

            List<DataGridViewRow> list = base.RestoreRemovedRows();

            if (!string.IsNullOrEmpty(ContextColumnName) && Columns.Contains(ContextColumnName) && list != null) {
                foreach (DataGridViewCheckedRow<CodeReferenceResultItem> row in list) {
                    if (row.Index == -1) continue;

                    DataGridViewDynamicWrapCell c = (DataGridViewDynamicWrapCell)row.Cells[ContextColumnName];
                    c.SetWrapContents(false);
                }
            }

            // perform sorting
            if (SortedColumn != null) {
                Sort(SortedColumn, SortOrder == SortOrder.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending);
            }

            return list;
        }

        /// <summary>
        /// Returns item initialized with values from provided row
        /// </summary>
        protected override CodeReferenceResultItem GetResultItemFromRow(DataGridViewRow row) {
            var typedRow = row as DataGridViewCheckedRow<CodeReferenceResultItem>;
            CodeReferenceResultItem item = typedRow.DataSourceItem;
            item.MoveThisItem = (bool)(typedRow.Cells[CheckBoxColumnName].Value);

            typedRow.DataSourceItem = item;
            return item;
        }

        /// <summary>
        /// Returns name of the column with checkbox
        /// </summary>
        public override string CheckBoxColumnName {
            get { return "InlineThisItem"; }
        }

        /// <summary>
        /// Removes current data from the grid
        /// </summary>
        public void Clear() {
            Rows.Clear();
            if (errorRows != null) errorRows.Clear();
        }
    }
}
