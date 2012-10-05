using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Gui;
using VisualLocalizer.Components;
using System.Windows.Forms;
using System.Resources;
using System.ComponentModel.Design;
using VisualLocalizer.Library;
using System.IO;

namespace VisualLocalizer.Editor {

    internal sealed class ResXStringGridRow : CodeDataGridViewRow<ResXDataNode> {
        public enum STATUS { OK, KEY_NULL }

        public ResXStringGridRow() {
            Status = STATUS.OK;
        }

        public STATUS Status { get; set; }
    }

    internal sealed class ResXStringGrid : AbstractKeyValueGridView<ResXDataNode> {

        public event EventHandler DataChanged;
        public event Action<ResXStringGridRow, string> StringKeyRenamed;
        public event Action<ResXStringGridRow, string, string> StringValueChanged;
        public event Action<ResXStringGridRow, string, string> StringCommentChanged;
        
        public ResXStringGrid(ResXEditorControl editorControl) {
            this.AllowUserToAddRows = true;            
            this.ShowEditingIcon = false;
            this.MultiSelect = true;
            this.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            editorControl.RemoveRequested += new Action<REMOVEKIND>(editorControl_RemoveRequested);

            ResXStringGridRow rowTemplate = new ResXStringGridRow();
            this.RowTemplate = rowTemplate;

            this.ColumnHeadersHeight = 24;
        }

        private void editorControl_RemoveRequested(REMOVEKIND flags) {
            if (!this.Visible) return;
            if (this.SelectedRows.Count == 0) return;
            if ((flags | REMOVEKIND.REMOVE) != REMOVEKIND.REMOVE) throw new ArgumentException("Cannot delete or exclude strings.");

            if ((flags & REMOVEKIND.REMOVE) == REMOVEKIND.REMOVE) {
                foreach (ResXStringGridRow row in SelectedRows) {
                    TrySetValue(row.DataSourceItem.Name, null, row);
                    Rows.Remove(row);
                }
                NotifyDataChanged();
            }            
        }

        protected override void InitializeColumns() {            
            DataGridViewTextBoxColumn keyColumn = new DataGridViewTextBoxColumn();
            keyColumn.MinimumWidth = 180;
            keyColumn.HeaderText = "Resource Key";
            keyColumn.Name = KeyColumnName;
            this.Columns.Add(keyColumn);

            DataGridViewTextBoxColumn valueColumn = new DataGridViewTextBoxColumn();
            valueColumn.MinimumWidth = 250;
            valueColumn.HeaderText = "Resource Value";
            valueColumn.Name = ValueColumnName;
            valueColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;            
            valueColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.Columns.Add(valueColumn);

            DataGridViewTextBoxColumn commentColumn = new DataGridViewTextBoxColumn();
            commentColumn.MinimumWidth = 180;
            commentColumn.HeaderText = "Comment";
            commentColumn.Name = CommentColumnName;
            commentColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            this.Columns.Add(commentColumn);
        }

        private void PopulateRow(ResXStringGridRow row, ResXDataNode node) {
            string name, value, comment;
            if (node.Comment.StartsWith("@@")) {
                string p = node.Comment.Substring(2);
                int at = p.IndexOf('@');

                name = p.Substring(0, at);
                value = node.GetStringValue();
                comment = p.Substring(at + 1);
            } else {
                name = node.Name;
                value = node.GetStringValue();
                comment = node.Comment;
            }

            DataGridViewTextBoxCell keyCell = new DataGridViewTextBoxCell();
            keyCell.Value = name;

            DataGridViewTextBoxCell valueCell = new DataGridViewTextBoxCell();
            valueCell.Value = value;

            DataGridViewTextBoxCell commentCell = new DataGridViewTextBoxCell();
            commentCell.Value = comment;

            row.Cells.Add(keyCell);
            row.Cells.Add(valueCell);
            row.Cells.Add(commentCell);
            row.DataSourceItem = node;

            row.MinimumHeight = 25;
        }

        public Dictionary<string, ResXDataNode> GetData(bool throwExceptions) {
            Dictionary<string, ResXDataNode> data = new Dictionary<string, ResXDataNode>(RowCount);

            foreach (ResXStringGridRow row in Rows) {
                if (!string.IsNullOrEmpty(row.ErrorText)) {
                    if (throwExceptions) {
                        throw new Exception(row.ErrorText);
                    } else {
                        if (row.DataSourceItem != null) {
                            string rndFile = Path.GetRandomFileName();
                            ResXDataNode newNode = new ResXDataNode(rndFile.Replace('@','_'), row.DataSourceItem.GetStringValue());
                            newNode.Comment = string.Format("@@{0}@{1}", row.DataSourceItem.Name, row.DataSourceItem.Comment);
                            data.Add(newNode.Name.ToLower(), newNode); 
                        }
                    }
                } else if (row.DataSourceItem != null) { 
                    data.Add(row.DataSourceItem.Name.ToLower(), row.DataSourceItem); 
                }
            }

            return data;
        }

        public void SetData(Dictionary<string, ResXDataNode> newData) {
            base.SetData(null);
            this.SuspendLayout();
            Rows.Clear();            

            foreach (var pair in newData) {
                ResXStringGridRow row = new ResXStringGridRow();
                PopulateRow(row, pair.Value);
                
                Rows.Add(row);                
                Validate(row);
            }

            this.ResumeLayout();
            this.OnResize(null);
        }

        protected override bool ProcessDataGridViewKey(KeyEventArgs e) {
            if (this.IsCurrentCellInEditMode && this.EditingControl is TextBox) {
                TextBox box = this.EditingControl as TextBox;
                if (e.KeyData == Keys.Home || e.KeyData == Keys.End) {
                    return false;
                } else if (e.KeyData == Keys.Enter) {
                    int selectionStart = box.SelectionStart;
                    box.Text = box.Text.Remove(selectionStart, box.SelectionLength).Insert(selectionStart, Environment.NewLine);
                    box.SelectionStart = selectionStart + Environment.NewLine.Length;
                    box.ScrollToCaret();
                    return true;
                } else return base.ProcessDataGridViewKey(e);
            } else return base.ProcessDataGridViewKey(e);
        }

        protected override void OnEditingControlShowing(DataGridViewEditingControlShowingEventArgs e) {            
            base.OnEditingControlShowing(e);
            if (!CurrentCellAddress.IsEmpty && CurrentCellAddress.X == 1 || CurrentCellAddress.X == 2 && e.Control is TextBox) {
                TextBox box = e.Control as TextBox;
                box.AcceptsReturn = true;
                box.Multiline = true;
                box.WordWrap = true;
            }
        }
        
        protected override void OnCellEndEdit(DataGridViewCellEventArgs e) {
            base.OnCellEndEdit(e);

            if (e.ColumnIndex >= 0 && e.RowIndex >= 0) {
                ResXStringGridRow row = Rows[e.RowIndex] as ResXStringGridRow;
                if (row.DataSourceItem == null) row.DataSourceItem = new ResXDataNode("(new)", string.Empty);
                ResXDataNode node = row.DataSourceItem;

                if (Columns[e.ColumnIndex].Name == KeyColumnName) {
                    string newKey=(string)row.Cells[KeyColumnName].Value;
                    if (string.Compare(newKey, node.Name) != 0) {
                        NotifyStringKeyRenamed(row, newKey);
                        NotifyDataChanged();                        

                        if (!string.IsNullOrEmpty(newKey)) {
                            node.Name = newKey;
                            row.Status = ResXStringGridRow.STATUS.OK;
                        } else {
                            row.Status = ResXStringGridRow.STATUS.KEY_NULL;
                        }
                    }
                } else if (Columns[e.ColumnIndex].Name == ValueColumnName) {
                    string newValue=(string)row.Cells[ValueColumnName].Value;
                    if (string.Compare(newValue, node.GetStringValue()) != 0) {
                        NotifyStringValueChanged(row, node.GetStringValue(), newValue);
                        NotifyDataChanged();

                        string key=(string)row.Cells[KeyColumnName].Value;
                        ResXDataNode newNode;
                        if (string.IsNullOrEmpty(key)) {
                            newNode = new ResXDataNode("A", newValue);
                            row.Status = ResXStringGridRow.STATUS.KEY_NULL;
                        } else {
                            newNode = new ResXDataNode(key, newValue);
                            row.Status = ResXStringGridRow.STATUS.OK;
                        }

                        newNode.Comment = (string)row.Cells[CommentColumnName].Value;
                        row.DataSourceItem = newNode;                        
                    }
                } else {
                    string newComment = (string)row.Cells[CommentColumnName].Value;
                    if (string.Compare(newComment, node.Comment) != 0) {
                        NotifyStringCommentChanged(row, node.Comment, newComment);
                        NotifyDataChanged();
                        
                        node.Comment = newComment;                        
                    }
                }
            }           
        }

        public void ValidateRow(ResXStringGridRow row) {
            Validate(row);
        }

        public void NotifyDataChanged() {
            if (DataChanged != null) DataChanged(this, null);
        }

        public void NotifyStringKeyRenamed(ResXStringGridRow row, string newKey) {
            if (StringKeyRenamed != null) StringKeyRenamed(row, newKey);
        }

        public void NotifyStringCommentChanged(ResXStringGridRow row, string oldComment, string newComment) {
            if (StringCommentChanged != null) StringCommentChanged(row, oldComment, newComment);
        }

        public void NotifyStringValueChanged(ResXStringGridRow row, string oldValue, string newValue) {
            if (StringValueChanged != null) StringValueChanged(row, oldValue, newValue);
        }

        public override string CheckBoxColumnName {
            get { return null; }
        }

        public override string KeyColumnName {
            get { return "KeyColumn"; }
        }

        public override string ValueColumnName {
            get { return "ValueColumn"; }
        }

        public string CommentColumnName {
            get { return "Comment"; }
        }

        protected override ResXDataNode GetResultItemFromRow(CodeDataGridViewRow<ResXDataNode> row) {
            throw new NotImplementedException();
        }

        public override void SetData(List<ResXDataNode> list) {
            throw new NotImplementedException();
        }
    }
}
