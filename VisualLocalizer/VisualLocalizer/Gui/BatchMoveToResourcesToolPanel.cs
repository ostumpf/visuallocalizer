using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using EnvDTE;
using System.Drawing;
using System.Windows.Forms.VisualStyles;
using System.Collections;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;

namespace VisualLocalizer.Gui {
    internal sealed class BatchMoveToResourcesToolPanel : AbstractKeyValueGridView<CodeStringResultItem>, IHighlightRequestSource {

        public event EventHandler<CodeResultItemEventArgs> HighlightRequired;
        private Dictionary<Project, DataGridViewComboBoxCell.ObjectCollection> destinationItemsCache = new Dictionary<Project, DataGridViewComboBoxCell.ObjectCollection>();
        private Dictionary<string, ResXProjectItem> resxItemsCache = new Dictionary<string, ResXProjectItem>();        
        private List<ResXProjectItem> loadedItems = new List<ResXProjectItem>();
        private bool valueAdded = false;

        public BatchMoveToResourcesToolPanel() {                        
            this.EditingControlShowing += new DataGridViewEditingControlShowingEventHandler(BatchMoveToResourcesToolPanel_EditingControlShowing);
            this.CellValidating += new DataGridViewCellValidatingEventHandler(BatchMoveToResourcesToolPanel_CellValidating);
            this.CellDoubleClick += new DataGridViewCellEventHandler(OnRowDoubleClick);
        }

        #region public members

        public void Unload() {
            foreach (var item in loadedItems)
                item.Unload();
            loadedItems.Clear();
        }

        public override void SetData(List<CodeStringResultItem> value) {
            base.SetData(value);

            this.Rows.Clear();
            destinationItemsCache.Clear();
            resxItemsCache.Clear();
            loadedItems.Clear();
            ErrorRowsCount = 0;
            this.SuspendLayout();

            foreach (CodeStringResultItem item in value) {
                CodeDataGridViewRow<CodeStringResultItem> row = new CodeDataGridViewRow<CodeStringResultItem>();
                row.DataSourceItem = item;

                DataGridViewCheckBoxCell checkCell = new DataGridViewCheckBoxCell();
                checkCell.Value = item.MoveThisItem;
                row.Cells.Add(checkCell);

                DataGridViewComboBoxCell keyCell = new DataGridViewComboBoxCell();
                foreach (string key in item.Value.CreateKeySuggestions(item.NamespaceElement == null ? null : (item.NamespaceElement as CodeNamespace).FullName, item.ClassOrStructElementName, item.VariableElementName == null ? item.MethodElementName : item.VariableElementName)) {
                    keyCell.Items.Add(key);
                    if (keyCell.Value == null)
                        keyCell.Value = key;
                }

                row.Cells.Add(keyCell);

                DataGridViewTextBoxCell valueCell = new DataGridViewTextBoxCell();
                valueCell.Value = item.Value;
                row.Cells.Add(valueCell);

                DataGridViewTextBoxCell sourceCell = new DataGridViewTextBoxCell();
                sourceCell.Value = item.SourceItem.Name;
                row.Cells.Add(sourceCell);

                DataGridViewComboBoxCell destinationCell = new DataGridViewComboBoxCell();
                destinationCell.Items.AddRange(CreateDestinationOptions(destinationCell, item.SourceItem.ContainingProject));
                if (destinationCell.Items.Count > 0)
                    destinationCell.Value = destinationCell.Items[0].ToString();
                row.Cells.Add(destinationCell);

                DataGridViewTextBoxCell cell = new DataGridViewTextBoxCell();
                row.Cells.Add(cell);
                Rows.Add(row);

                valueCell.ReadOnly = false;
                sourceCell.ReadOnly = true;
                Validate(row);
            }

            CurrentItemIndex = null;
            CheckHeader.Checked = true;
            CheckedRowsCount = Rows.Count;
            this.ResumeLayout();
            this.OnResize(null);
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

        #endregion

        #region overridable members

        protected override void InitializeColumns() {
            base.InitializeColumns();

            DataGridViewComboBoxColumn keyColumn = new DataGridViewComboBoxColumn();
            keyColumn.MinimumWidth = 150;
            keyColumn.HeaderText = "Resource Key";
            keyColumn.Name = KeyColumnName;
            this.Columns.Add(keyColumn);

            DataGridViewTextBoxColumn valueColumn = new DataGridViewTextBoxColumn();
            valueColumn.MinimumWidth = 250;
            valueColumn.HeaderText = "Resource Value";
            valueColumn.Name = ValueColumnName;
            valueColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            this.Columns.Add(valueColumn);

            DataGridViewTextBoxColumn sourceColumn = new DataGridViewTextBoxColumn();
            sourceColumn.MinimumWidth = 150;
            sourceColumn.HeaderText = "Source File";
            sourceColumn.Name = "SourceItem";
            this.Columns.Add(sourceColumn);

            DataGridViewComboBoxColumn destinationColumn = new DataGridViewComboBoxColumn();
            destinationColumn.MinimumWidth = 250;
            destinationColumn.HeaderText = "Destination File";
            destinationColumn.Name = "DestinationItem";
            destinationColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            this.Columns.Add(destinationColumn);

            DataGridViewColumn column = new DataGridViewColumn();
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.Columns.Add(column);
        }

        protected override CodeStringResultItem GetResultItemFromRow(CodeDataGridViewRow<CodeStringResultItem> row) {
            CodeStringResultItem item = row.DataSourceItem;
            item.MoveThisItem = (bool)(row.Cells[CheckBoxColumnName].Value);
            if (item.MoveThisItem) {
                item.Key = (string)row.Cells[KeyColumnName].Value;
                item.Value = (string)row.Cells[ValueColumnName].Value;
                if (string.IsNullOrEmpty(item.Key) || item.Value == null)
                    throw new InvalidOperationException("Item key and value cannot be null");

                if (!string.IsNullOrEmpty(row.ErrorText))
                    throw new InvalidOperationException(string.Format("on key \"{0}\": \"{1}\"", item.Key, row.ErrorText));

                string dest = (string)row.Cells["DestinationItem"].Value;
                if (string.IsNullOrEmpty(dest))
                    throw new InvalidOperationException(string.Format("on key \"{0}\" - item destination cannot be null", item.Key));

                if (resxItemsCache.ContainsKey(dest)) {
                    item.DestinationItem = resxItemsCache[dest];
                } else throw new InvalidOperationException(string.Format("Key \"{0}\" has no specified destination item.", item.Key));

                row.DataSourceItem = item;
                return item;
            } else {
                row.DataSourceItem = item;
                return GetNextResultItem();
            }
        }

        protected override void OnCellEndEdit(DataGridViewCellEventArgs e) {
            if (Columns[e.ColumnIndex].Name == KeyColumnName) {
                if (valueAdded) {
                    DataGridViewComboBoxCell cell = (DataGridViewComboBoxCell)Rows[e.RowIndex].Cells[KeyColumnName];
                    cell.Value = cell.Items[0];
                    valueAdded = false;
                }
            }
            base.OnCellEndEdit(e);
        }

        protected override void Validate(CodeDataGridViewRow<CodeStringResultItem> row) {
            object dest = row.Cells["DestinationItem"].Value;
            bool existsSameValue = false; 
            string destError = "Destination file not set";
            if (dest == null) {
                row.ErrorSet.Add(destError);
            } else {
                row.ErrorSet.Remove(destError);

                ResXProjectItem resxItem = resxItemsCache[dest.ToString()];
                if (!resxItem.IsLoaded) {
                    resxItem.Load();
                    VLDocumentViewsManager.SetFileReadonly(resxItem.InternalProjectItem.Properties.Item("FullPath").Value.ToString(), true);
                    loadedItems.Add(resxItem);
                }

                string key = (string)row.Cells[KeyColumnName].Value;
                string value = (string)row.Cells[ValueColumnName].Value;
                if (key == null || value == null) return;

                string errorText = "Duplicate key entry - key is already present in resource file with different value";
                CONTAINS_KEY_RESULT keyConflict = resxItem.StringKeyInConflict(key, value);
                switch (keyConflict) {
                    case CONTAINS_KEY_RESULT.EXISTS_WITH_SAME_VALUE:                        
                        row.ErrorSet.Remove(errorText);
                        existsSameValue=true;
                        break;
                    case CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE:                        
                        row.ErrorSet.Add(errorText);
                        break;
                    case CONTAINS_KEY_RESULT.DOESNT_EXIST:                        
                        row.ErrorSet.Remove(errorText);
                        break;
                }                               
            }

            base.Validate(row);

            if (row.ErrorSet.Count == 0) {
                if (existsSameValue) {
                    row.DefaultCellStyle.BackColor = ExistingKeySameValueColor;
                } else {
                    row.DefaultCellStyle.BackColor = Color.White;
                }
            }
        }

        protected override void SetConflictedRows(CodeDataGridViewRow<CodeStringResultItem> row1, CodeDataGridViewRow<CodeStringResultItem> row2, bool p) {
            object dest1 = row1.Cells["DestinationItem"].Value;
            object dest2 = row2.Cells["DestinationItem"].Value;
            p = p && (dest1 == null || dest2 == null || dest1.ToString() == dest2.ToString());

            base.SetConflictedRows(row1, row2, p);
        }

        #endregion

        private void OnRowDoubleClick(object sender, DataGridViewCellEventArgs e) {
            if (HighlightRequired != null && e.RowIndex >= 0) {
                HighlightRequired(this, new CodeResultItemEventArgs() {
                    Item = (Rows[e.RowIndex] as CodeDataGridViewRow<CodeStringResultItem>).DataSourceItem
                });
            }
        }
        
        private DataGridViewComboBoxCell.ObjectCollection CreateDestinationOptions(DataGridViewComboBoxCell cell, Project project) {
            if (!destinationItemsCache.ContainsKey(project)) {
                List<ProjectItem> items = project.GetFiles(ResXProjectItem.IsItemResX, true);
                DataGridViewComboBoxCell.ObjectCollection resxItems = new DataGridViewComboBoxCell.ObjectCollection(cell);
                foreach (ProjectItem projectItem in items) {
                    var resxItem = ResXProjectItem.ConvertToResXItem(projectItem, project);
                    resxItems.Add(resxItem.ToString());
                    resxItemsCache.Add(resxItem.ToStringValue, resxItem);
                }
                destinationItemsCache.Add(project, resxItems);
            }

            return destinationItemsCache[project];
        }
                
        private void BatchMoveToResourcesToolPanel_CellValidating(object sender, DataGridViewCellValidatingEventArgs e) {
            valueAdded = false;
            if (Columns[e.ColumnIndex].Name == KeyColumnName) {
                var comboBoxCell = Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewComboBoxCell;
                if (!comboBoxCell.Items.Contains(e.FormattedValue)) {
                    comboBoxCell.Items.Insert(0, e.FormattedValue);
                    valueAdded = true;
                }
            }
        }

        protected override bool ProcessDataGridViewKey(KeyEventArgs e) {
            if (this.IsCurrentCellInEditMode) {
                if (e.KeyData == Keys.Left || e.KeyData == Keys.Right || e.KeyData==Keys.Home || e.KeyData==Keys.End) {
                    return false;
                } else return base.ProcessDataGridViewKey(e);
            } else return base.ProcessDataGridViewKey(e);
        }

        private void BatchMoveToResourcesToolPanel_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e) {
            if (CurrentCellAddress.X == 1 && e.Control is ComboBox) {
                ComboBox box = e.Control as ComboBox;
                box.DropDownStyle = ComboBoxStyle.DropDown;                     
            }            
        }
        
        public override string CheckBoxColumnName {
            get { return "MoveThisItem"; }
        }

        public override string KeyColumnName {
            get { return "Key"; }
        }

        public override string ValueColumnName {
            get { return "Value"; }
        }
    }

    
}
