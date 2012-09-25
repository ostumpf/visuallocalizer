using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using VisualLocalizer.Library;
using System.Windows.Forms;
using System.Resources;

namespace VisualLocalizer.Library {
    public abstract class AbstractKeyValueGridView<ItemType>:AbstractCheckedGridView<ItemType> where ItemType:class {

        private Dictionary<string, CodeDataGridViewRow<ItemType>> RowData = new Dictionary<string, CodeDataGridViewRow<ItemType>>();        

        protected virtual void TrySetValue(string oldKey, string newKey, CodeDataGridViewRow<ItemType> row) {
            if (oldKey == newKey) {
                if (!string.IsNullOrEmpty(newKey)) {
                    foreach (CodeDataGridViewRow<ItemType> c in RowData[newKey].RowsWithSameKey) {
                        if (c.Index != row.Index) {
                            SetConflictedRows(c, row, c.Cells[ValueColumnName].Value.ToString() != row.Cells[ValueColumnName].Value.ToString());
                        }
                    }
                    if (RowData[newKey].Index != row.Index) {
                        if (RowData[newKey].Cells[ValueColumnName].Value.ToString() != row.Cells[ValueColumnName].Value.ToString())
                            SetConflictedRows(RowData[newKey], row, true);
                        else
                            SetConflictedRows(RowData[newKey], row, false);
                    }
                }
            } else {
                if (!string.IsNullOrEmpty(oldKey) && RowData.ContainsKey(oldKey)) {
                    if (RowData[oldKey].RowsWithSameKey.Count > 0) {
                        foreach (CodeDataGridViewRow<ItemType> c in RowData[oldKey].RowsWithSameKey) {
                            SetConflictedRows(c, row, false);
                        }

                        if (RowData[oldKey].Index == row.Index) {
                            CodeDataGridViewRow<ItemType> replaceRow = RowData[oldKey].RowsWithSameKey[RowData[oldKey].RowsWithSameKey.Count - 1];
                            RowData[oldKey].RowsWithSameKey.RemoveAt(RowData[oldKey].RowsWithSameKey.Count - 1);
                            replaceRow.RowsWithSameKey = RowData[oldKey].RowsWithSameKey;
                            RowData[oldKey] = replaceRow;
                        } else {
                            RowData[oldKey].RowsWithSameKey.Remove(row);
                            SetConflictedRows(RowData[oldKey], row, false);
                        }
                    } else {
                        RowData.Remove(oldKey);
                    }
                }

                if (!string.IsNullOrEmpty(newKey)) {
                    if (RowData.ContainsKey(newKey)) {
                        if (RowData[newKey].Index != row.Index && RowData[newKey].Cells[ValueColumnName].Value.ToString() != row.Cells[ValueColumnName].Value.ToString()) {
                            SetConflictedRows(RowData[newKey], row, true);
                        }
                        foreach (CodeDataGridViewRow<ItemType> c in RowData[newKey].RowsWithSameKey) {
                            SetConflictedRows(c, row, c.Cells[ValueColumnName].Value.ToString() != row.Cells[ValueColumnName].Value.ToString());
                        }
                        if (!RowData[newKey].RowsWithSameKey.Contains(row)) RowData[newKey].RowsWithSameKey.Add(row);
                    } else {
                        RowData.Add(newKey, row);
                    }
                }
            }
        }

        protected virtual void SetConflictedRows(CodeDataGridViewRow<ItemType> row1, CodeDataGridViewRow<ItemType> row2, bool p) {            
            if (p) {
                if (!row1.ConflictRows.Contains(row2)) row1.ConflictRows.Add(row2);
                if (!row2.ConflictRows.Contains(row1)) row2.ConflictRows.Add(row1);
            } else {
                row1.ConflictRows.Remove(row2);
                row2.ConflictRows.Remove(row1);
            }

            string errorText="Duplicate key entry";
            
            if (row1.ConflictRows.Count == 0) {
                row1.ErrorSet.Remove(errorText);
            } else {
                if (!row1.ErrorSet.Contains(errorText)) row1.ErrorSet.Add(errorText);
            }
            row1.ErrorSetUpdate();

            if (row2.ConflictRows.Count == 0) {
                row2.ErrorSet.Remove(errorText);
            } else {
                if (!row2.ErrorSet.Contains(errorText)) row2.ErrorSet.Add(errorText);
            }
            row2.ErrorSetUpdate();
        }

        protected override void OnCellEndEdit(DataGridViewCellEventArgs e) {
            base.OnCellEndEdit(e);

            if (Columns[e.ColumnIndex].Name != KeyColumnName) {
                Rows[e.RowIndex].Cells[KeyColumnName].Tag = Rows[e.RowIndex].Cells[KeyColumnName].Value;
            }
            if (Columns[e.ColumnIndex].Name != CheckBoxColumnName) {
                Validate(e.RowIndex);
            }       
        }

        protected virtual void Validate(CodeDataGridViewRow<ItemType> row) {
            string key = (string)row.Cells[KeyColumnName].Value;
            string value = (string)row.Cells[ValueColumnName].Value;
            
            string errorText = null;
            string identError = "Key is not valid C# identifier";
            if (!key.IsValidIdentifier(ref errorText)) {
                row.ErrorSet.Add(identError);
            } else {
                row.ErrorSet.Remove(identError);
            }
            
            object originalValue = row.Cells[KeyColumnName].Tag;
            TrySetValue((string)originalValue, key, row);
            if (originalValue == null) row.Cells[KeyColumnName].Tag = key;

            row.ErrorSetUpdate();
        }              

        protected void Validate(int rowIndex) {
            Validate(Rows[rowIndex] as CodeDataGridViewRow<ItemType>);
        }

        public override void SetData(List<ItemType> list) {
            RowData.Clear();
        }

        public abstract string KeyColumnName { get; }
        public abstract string ValueColumnName { get; }
    }
}
