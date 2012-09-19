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

namespace VisualLocalizer.Gui {
    internal sealed class BatchMoveToResourcesToolPanel : AbstractCodeToolWindowPanel {
        
        private Dictionary<Project, DataGridViewComboBoxCell.ObjectCollection> destinationItemsCache = new Dictionary<Project, DataGridViewComboBoxCell.ObjectCollection>();
        private Dictionary<string, ResXProjectItem> resxItemsCache = new Dictionary<string, ResXProjectItem>();        
        private List<ResXProjectItem> loadedItems = new List<ResXProjectItem>();
        private Dictionary<string, CodeDataGridViewRow> data = new Dictionary<string, CodeDataGridViewRow>();

        public BatchMoveToResourcesToolPanel() {                        
            this.EditingControlShowing += new DataGridViewEditingControlShowingEventHandler(BatchMoveToResourcesToolPanel_EditingControlShowing);
            this.CellValidating += new DataGridViewCellValidatingEventHandler(BatchMoveToResourcesToolPanel_CellValidating);            

            DataGridViewComboBoxColumn keyColumn = new DataGridViewComboBoxColumn();
            keyColumn.MinimumWidth = 150;
            keyColumn.HeaderText = "Resource Key";
            keyColumn.Name = "Key";
            this.Columns.Add(keyColumn);

            DataGridViewTextBoxColumn valueColumn = new DataGridViewTextBoxColumn();            
            valueColumn.MinimumWidth = 250;
            valueColumn.HeaderText = "Resource Value";
            valueColumn.Name = "Value";
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

        public void Unload() {
            foreach (var item in loadedItems)
                item.Unload();
            loadedItems.Clear();
        }

        protected override AbstractResultItem GetResultItemFromRow(CodeDataGridViewRow row) {
            CodeStringResultItem item = row.CodeResultItem as CodeStringResultItem;
            item.MoveThisItem = (bool)(row.Cells["MoveThisItem"].Value);
            if (item.MoveThisItem) {
                item.Key = row.Cells["Key"].Value.ToString();
                item.Value = row.Cells["Value"].Value.ToString();
                
                if (!string.IsNullOrEmpty(row.ErrorText))
                    throw new InvalidOperationException(string.Format("on key \"{0}\": \"{1}\"", item.Key, row.ErrorText));

                string dest = row.Cells["DestinationItem"].Value.ToString();
                if (resxItemsCache.ContainsKey(dest)) {
                    item.DestinationItem = resxItemsCache[dest];
                } else throw new InvalidOperationException(string.Format("Key \"{0}\" has no specified destination item.", item.Key));

                row.CodeResultItem = item;
                return item;
            } else {
                row.CodeResultItem = item;
                return GetNextResultItem();
            }                
        }

        public void SetData(List<CodeStringResultItem> value) {
            this.Rows.Clear();
            destinationItemsCache.Clear();
            resxItemsCache.Clear();
            loadedItems.Clear();
            data.Clear();
            ErrorRowsCount = 0;
            this.SuspendLayout();

            foreach (CodeStringResultItem item in value) {
                CodeDataGridViewRow row = new CodeDataGridViewRow();
                row.CodeResultItem = item;
              
                DataGridViewCheckBoxCell checkCell = new DataGridViewCheckBoxCell();
                checkCell.Value = item.MoveThisItem;                
                row.Cells.Add(checkCell);
                
                DataGridViewComboBoxCell keyCell = new DataGridViewComboBoxCell();
                foreach (string key in item.Value.CreateKeySuggestions(item.NamespaceElement == null  ? null : (item.NamespaceElement as CodeNamespace).FullName, item.ClassOrStructElementName, item.VariableElementName == null ? item.MethodElementName : item.VariableElementName)) {
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
                validate(row);
            }

            currentItemIndex = null;
            checkHeader.Checked = true;
            CheckedRowsCount = Rows.Count;
            this.ResumeLayout(true);
        }

        private void TrySetValue(string oldKey, string newKey, CodeDataGridViewRow row) {
            if (oldKey == newKey) {
                foreach (CodeDataGridViewRow c in data[newKey].DependantRows) {
                    if (c.Index!=row.Index) {                        
                        setConflictedRows(c, row, c.Cells["Value"].Value.ToString() != row.Cells["Value"].Value.ToString());                        
                    }
                }
                if (data[newKey].Index != row.Index) {
                    if (data[newKey].Cells["Value"].Value.ToString() != row.Cells["Value"].Value.ToString())
                        setConflictedRows(data[newKey], row, true);
                    else
                        setConflictedRows(data[newKey], row, false);
                }
            } else {
                if (oldKey != null && data.ContainsKey(oldKey)) {
                    if (data[oldKey].DependantRows.Count > 0) {
                        foreach (CodeDataGridViewRow c in data[oldKey].DependantRows) {
                            setConflictedRows(c, row, false);
                        }

                        if (data[oldKey].Index == row.Index) {
                            CodeDataGridViewRow replaceRow = data[oldKey].DependantRows[data[oldKey].DependantRows.Count - 1];
                            data[oldKey].DependantRows.RemoveAt(data[oldKey].DependantRows.Count - 1);
                            replaceRow.DependantRows = data[oldKey].DependantRows;
                            data[oldKey] = replaceRow;    
                        } else {
                            data[oldKey].DependantRows.Remove(row);
                            setConflictedRows(data[oldKey], row, false);
                        }                        
                    } else {
                        data.Remove(oldKey);
                    }
                }
                
                if (data.ContainsKey(newKey)) {
                    if (data[newKey].Index != row.Index && data[newKey].Cells["Value"].Value.ToString() != row.Cells["Value"].Value.ToString()) {                        
                        setConflictedRows(data[newKey], row, true);
                    }
                    foreach (CodeDataGridViewRow c in data[newKey].DependantRows) {
                        setConflictedRows(c, row, c.Cells["Value"].Value.ToString() != row.Cells["Value"].Value.ToString());
                    }
                    if (!data[newKey].DependantRows.Contains(row)) data[newKey].DependantRows.Add(row);
                } else {
                    data.Add(newKey, row);
                }
            }            
        }

        private void setConflictedRows(CodeDataGridViewRow row1, CodeDataGridViewRow row2, bool p) {
            object dest1 = row1.Cells["DestinationItem"].Value;
            object dest2 = row2.Cells["DestinationItem"].Value;
            p = p && (dest1==null || dest2==null || dest1.ToString()==dest2.ToString());

            if (p) {
                if (!row1.ConflictRows.Contains(row2)) row1.ConflictRows.Add(row2);
                if (!row2.ConflictRows.Contains(row1)) row2.ConflictRows.Add(row1);
            } else {
                row1.ConflictRows.Remove(row2);
                row2.ConflictRows.Remove(row1);
            }

            if (row1.ConflictRows.Count == 0) {
                row1.ErrorText = null;
            } else {
                row1.ErrorText = "Duplicate key entry";
            }

            if (row2.ConflictRows.Count == 0) {
                row2.ErrorText = null;
            } else {
                row2.ErrorText = "Duplicate key entry";
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
        
        private bool valueAdded = false;
        private void BatchMoveToResourcesToolPanel_CellValidating(object sender, DataGridViewCellValidatingEventArgs e) {
            valueAdded = false;
            if (e.ColumnIndex == 1) {
                var comboBoxCell = Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewComboBoxCell;
                if (!comboBoxCell.Items.Contains(e.FormattedValue)) {
                    comboBoxCell.Items.Insert(0, e.FormattedValue);
                    valueAdded = true;
                }
            }
        }

        private void BatchMoveToResourcesToolPanel_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e) {
            if (CurrentCellAddress.X == 1 && e.Control is ComboBox) {
                ComboBox box = e.Control as ComboBox;
                box.DropDownStyle = ComboBoxStyle.DropDown;                
            }            
        }

        protected override void OnCellEndEdit(object sender, DataGridViewCellEventArgs e) {
            base.OnCellEndEdit(sender, e);

            if (e.ColumnIndex == 1) {
                if (valueAdded) {
                    DataGridViewComboBoxCell cell = (DataGridViewComboBoxCell)Rows[e.RowIndex].Cells["Key"];
                    cell.Value = cell.Items[0];
                    valueAdded = false;
                }
            } else {
                Rows[e.RowIndex].Cells["Key"].Tag = Rows[e.RowIndex].Cells["Key"].Value;
            }
            if (e.ColumnIndex == 1 || e.ColumnIndex == 2 || e.ColumnIndex == 4) {                
                validate(e.RowIndex);
            }            
        }

        private void validate(CodeDataGridViewRow row) {
            string key = row.Cells["Key"].Value.ToString();           
            object dest=row.Cells["DestinationItem"].Value;
            string errorText = null;
            bool ok = true;

            if (dest == null) {
                ok = false;
                errorText = "No destination file selected";
            } else {
                ResXProjectItem resxItem = resxItemsCache[dest.ToString()];
                if (!resxItem.IsLoaded) {
                    resxItem.Load();
                    loadedItems.Add(resxItem);
                }                                                

                ok = !resxItem.ContainsKey(key);
                if (!ok) errorText = "Duplicate key entry - key is already present in resource file";
                ok = ok && key.IsValidIdentifier(ref errorText);                
            }

            if (ok) {
                row.ErrorText = null;
            } else {
                row.ErrorText = errorText;
            }

            if (ok) {
                object keyTag = row.Cells["Key"].Tag;
                TrySetValue(keyTag == null ? null : keyTag.ToString(), key, row);
                if (keyTag == null) row.Cells["Key"].Tag = key;                
            }
        }
     

        private void validate(int row) {
            validate(Rows[row] as CodeDataGridViewRow);
        }

    }

    
}
