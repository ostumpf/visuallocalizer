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
using VisualLocalizer.Editor.UndoUnits;

namespace VisualLocalizer.Editor {    

    internal sealed class ResXStringGrid : AbstractKeyValueGridView<ResXDataNode>, IDataTabItem {

        public event EventHandler DataChanged;
        public event EventHandler ItemsStateChanged;
                
        private TextBox CurrentlyEditedTextBox;
        private ResXEditorControl editorControl;
        private MenuItem editContextMenuItem, cutContextMenuItem, copyContextMenuItem, pasteContextMenuItem, deleteContextMenuItem,
            inlineContextMenuItem;

        public ResXStringGrid(ResXEditorControl editorControl) : base(editorControl.conflictResolver) {
            this.editorControl = editorControl;
            this.AllowUserToAddRows = true;            
            this.ShowEditingIcon = false;
            this.MultiSelect = true;
            this.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            this.editorControl.RemoveRequested += new Action<REMOVEKIND>(editorControl_RemoveRequested);
            this.SelectionChanged += new EventHandler((o, e) => { NotifyItemsStateChanged(); });
            this.NewRowNeeded += new DataGridViewRowEventHandler((o, e) => { NotifyItemsStateChanged(); });
            this.MouseDown += new MouseEventHandler(ResXStringGrid_MouseDown);

            ResXStringGridRow rowTemplate = new ResXStringGridRow();
            rowTemplate.MinimumHeight = 24;
            this.RowTemplate = rowTemplate;

            editContextMenuItem = new MenuItem("Edit cell");
            editContextMenuItem.Click += new EventHandler((o, e) => { this.BeginEdit(true); });

            cutContextMenuItem = new MenuItem("Cut");
            cutContextMenuItem.Click += new EventHandler((o, e) => { editorControl.ExecuteCut(); });

            copyContextMenuItem = new MenuItem("Copy");
            copyContextMenuItem.Click += new EventHandler((o, e) => { editorControl.ExecuteCopy(); });

            pasteContextMenuItem = new MenuItem("Paste");
            pasteContextMenuItem.Click += new EventHandler((o, e) => { editorControl.ExecutePaste(); });

            deleteContextMenuItem = new MenuItem("Remove");
            deleteContextMenuItem.Click += new EventHandler((o, e) => { editorControl_RemoveRequested(REMOVEKIND.REMOVE); }); 

            inlineContextMenuItem = new MenuItem("Inline");

            ContextMenu contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add(editContextMenuItem);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(cutContextMenuItem);
            contextMenu.MenuItems.Add(copyContextMenuItem);
            contextMenu.MenuItems.Add(pasteContextMenuItem);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(inlineContextMenuItem);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(deleteContextMenuItem);
            contextMenu.Popup += new EventHandler(contextMenu_Popup);
            this.ContextMenu = contextMenu;

            this.ColumnHeadersHeight = 24;
        }
                
        #region IDataTabItem members

        public Dictionary<string, ResXDataNode> GetData(bool throwExceptions) {
            EndEdit();

            Dictionary<string, ResXDataNode> data = new Dictionary<string, ResXDataNode>(RowCount);
            foreach (ResXStringGridRow row in Rows) {
                if (!string.IsNullOrEmpty(row.ErrorText)) {
                    if (throwExceptions) {
                        throw new Exception(row.ErrorText);
                    } else {
                        if (row.DataSourceItem != null) {
                            string rndFile = Path.GetRandomFileName();
                            ResXDataNode newNode = new ResXDataNode(rndFile.Replace('@', '_'), row.DataSourceItem.GetValue<string>());
                            newNode.Comment = string.Format("@@{0}@{1}", row.Status == ResXStringGridRow.STATUS.KEY_NULL ? "" : row.DataSourceItem.Name, row.DataSourceItem.Comment);
                            data.Add(newNode.Name.ToLower(), newNode);
                        }
                    }
                } else if (row.DataSourceItem != null) {
                    data.Add(row.DataSourceItem.Name.ToLower(), row.DataSourceItem);
                }
            }

            return data;
        }        

        public bool CanContainItem(ResXDataNode node) {
            return node.HasValue<string>();
        }

        public void BeginAdd() {
            base.SetData(null);
            this.SuspendLayout();
            Rows.Clear();
        }

        public IKeyValueSource Add(string key, ResXDataNode value, bool showThumbnails) {
            ResXStringGridRow row = new ResXStringGridRow();
            PopulateRow(row, value);

            Rows.Add(row);
            Validate(row);

            return row;
        }

        public void EndAdd() {
            this.ResumeLayout();
            this.OnResize(null);
        }

        public COMMAND_STATUS CanCutOrCopy {
            get {
                return HasSelectedItems && !IsEditing ? COMMAND_STATUS.ENABLED : COMMAND_STATUS.DISABLED;
            }
        }

        public COMMAND_STATUS CanPaste {
            get {
                return Clipboard.ContainsText() && !IsEditing ? COMMAND_STATUS.ENABLED : COMMAND_STATUS.DISABLED;
            }
        }

        public bool Copy() {
            StringBuilder content = new StringBuilder();
            foreach (DataGridViewRow row in SelectedRows) {
                if (row.IsNewRow) continue;
                content.AppendFormat("{0},{1},{2};", (string)row.Cells[KeyColumnName].Value, (string)row.Cells[ValueColumnName].Value, (string)row.Cells[CommentColumnName].Value);
            }
            Clipboard.SetText(content.ToString(), TextDataFormat.UnicodeText);
            return true;
        }

        public bool Cut() {
            bool ok = Copy();
            if (!ok) return false;

            editorControl_RemoveRequested(REMOVEKIND.REMOVE);            

            return true;
        }

        public bool HasItems {
            get {
                return Rows.Count > 1;
            }
        }

        public bool HasSelectedItems {
            get {
                return (SelectedRows.Count > 1 || (SelectedRows.Count == 1 && !SelectedRows[0].IsNewRow));
            }
        }

        public bool SelectAllItems() {
            foreach (DataGridViewRow row in Rows)
                if (!row.IsNewRow) row.Selected = true;

            return true;
        }

        public bool IsEditing {
            get {
                return IsCurrentCellInEditMode;
            }
        }

        #endregion

        #region AbstractKeyValueGridView members        

        public override void SetData(List<ResXDataNode> list) {
            throw new NotImplementedException();
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

        #endregion        

        #region protected members - virtual

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

            DataGridViewTextBoxColumn referencesColumn = new DataGridViewTextBoxColumn();
            referencesColumn.MinimumWidth = 40;
            referencesColumn.HeaderText = "References";
            referencesColumn.Name = ReferencesColumnName;
            referencesColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            this.Columns.Add(referencesColumn);
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
            if (e.Control is TextBox) {
                CurrentlyEditedTextBox = e.Control as TextBox;
            }
            NotifyItemsStateChanged();
        }

        protected override void OnCellEndEdit(DataGridViewCellEventArgs e) {
            try {
                if (e.RowIndex == this.Rows.Count - 1) return; // last row edited - cancel
                base.OnCellEndEdit(e);

                if (e.ColumnIndex >= 0 && e.RowIndex >= 0) {
                    ResXStringGridRow row = Rows[e.RowIndex] as ResXStringGridRow;
                    bool isNewRow = false;
                    if (row.DataSourceItem == null) {
                        isNewRow = true;
                        row.DataSourceItem = new ResXDataNode("(new)", string.Empty);
                    }
                    ResXDataNode node = row.DataSourceItem;

                    if (Columns[e.ColumnIndex].Name == KeyColumnName) {
                        string newKey = (string)row.Cells[KeyColumnName].Value;                                       

                        if (isNewRow) {
                            setNewKey(row, newKey);
                            StringRowAdded(row);
                            NotifyDataChanged();
                        } else {
                            if (string.Compare(newKey, node.Name) != 0) {
                                StringKeyRenamed(row, newKey);
                                setNewKey(row, newKey);
                                NotifyDataChanged();
                            }
                        }         
                    } else if (Columns[e.ColumnIndex].Name == ValueColumnName) {
                        string newValue = (string)row.Cells[ValueColumnName].Value;
                        if (isNewRow) {
                            row.Status = ResXStringGridRow.STATUS.KEY_NULL;
                            StringRowAdded(row);
                            NotifyDataChanged();
                        } else {
                            if (string.Compare(newValue, node.GetValue<string>()) != 0) {
                                StringValueChanged(row, node.GetValue<string>(), newValue);
                                NotifyDataChanged();

                                string key = (string)row.Cells[KeyColumnName].Value;
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
                        }
                    } else {
                        string newComment = (string)row.Cells[CommentColumnName].Value;
                        if (isNewRow) {
                            row.Status = ResXStringGridRow.STATUS.KEY_NULL;
                            StringRowAdded(row);
                            NotifyDataChanged();
                        } else {
                            if (string.Compare(newComment, node.Comment) != 0) {
                                StringCommentChanged(row, node.Comment, newComment);
                                NotifyDataChanged();

                                node.Comment = newComment;
                            }
                        }
                    }
                }                
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                VisualLocalizer.Library.MessageBox.ShowError(text);
            }
            NotifyItemsStateChanged();
        }
        
        protected override ResXDataNode GetResultItemFromRow(DataGridViewRow row) {
            throw new NotImplementedException();
        }

        #endregion

        #region public members

        public void StringCommentChanged(ResXStringGridRow row, string oldComment, string newComment) {
            string key = row.Status == ResXStringGridRow.STATUS.KEY_NULL ? null : row.DataSourceItem.Name;
            StringChangeCommentUndoUnit unit = new StringChangeCommentUndoUnit(row, this, key, oldComment, newComment);
            editorControl.Editor.AddUndoUnit(unit);
        }

        public void StringValueChanged(ResXStringGridRow row, string oldValue, string newValue) {
            string key = row.Status == ResXStringGridRow.STATUS.KEY_NULL ? null : row.DataSourceItem.Name;
            StringChangeValueUndoUnit unit = new StringChangeValueUndoUnit(row, this, key, oldValue, newValue, row.DataSourceItem.Comment);
            editorControl.Editor.AddUndoUnit(unit);
        }

        public void StringKeyRenamed(ResXStringGridRow row, string newKey) {
            string oldKey = row.Status == ResXStringGridRow.STATUS.KEY_NULL ? null : row.DataSourceItem.Name;
            StringRenameKeyUndoUnit unit = new StringRenameKeyUndoUnit(row, this, oldKey, newKey);
            editorControl.Editor.AddUndoUnit(unit);
        }

        public void StringRowAdded(ResXStringGridRow row) {
            StringRowsAdded(new List<ResXStringGridRow>() { row });
        }

        public void StringRowsAdded(List<ResXStringGridRow> rows) {
            StringRowAddUndoUnit unit = new StringRowAddUndoUnit(rows, this, editorControl.conflictResolver);
            editorControl.Editor.AddUndoUnit(unit);
        }

        public void AddClipboardText(string text) {
            string[] rows = text.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            List<ResXStringGridRow> addedRows = new List<ResXStringGridRow>();
            foreach (string row in rows) {
                string[] columns = row.Split(',');
                if (columns.Length != 3) continue;

                string key = columns[0].CreateIdentifier();
                string value = columns[1];
                string comment = columns[2];

                ResXDataNode node = new ResXDataNode(key, value);
                node.Comment = comment;

                ResXStringGridRow newRow = Add(key, node, true) as ResXStringGridRow;
                addedRows.Add(newRow);   
            }

            if (addedRows.Count > 0) {
                StringRowsAdded(addedRows);
                NotifyDataChanged();
                NotifyItemsStateChanged();
            }
        }

        public void ValidateRow(ResXStringGridRow row) {
            Validate(row);
        }

        public void NotifyDataChanged() {
            if (DataChanged != null) DataChanged(this, null);
        }

        public string CommentColumnName {
            get { return "Comment"; }
        }

        public string ReferencesColumnName {
            get { return "References"; }
        }

        #endregion

        #region private members

        private void NotifyItemsStateChanged() {
            if (ItemsStateChanged != null) ItemsStateChanged(this.Parent, null);
        }

        private void PopulateRow(ResXStringGridRow row, ResXDataNode node) {
            string name, value, comment;
            if (node.Comment.StartsWith("@@")) {
                string p = node.Comment.Substring(2);
                int at = p.LastIndexOf('@');

                name = p.Substring(0, at);
                value = node.GetValue<string>();
                comment = p.Substring(at + 1);

                if (string.IsNullOrEmpty(name)) {
                    row.Status = ResXStringGridRow.STATUS.KEY_NULL;
                } else {
                    row.Status = ResXStringGridRow.STATUS.OK;
                    node.Name = name;
                }
                node.Comment = comment;
            } else {
                name = node.Name;
                value = node.GetValue<string>();
                comment = node.Comment;
            }

            DataGridViewTextBoxCell keyCell = new DataGridViewTextBoxCell();
            keyCell.Value = name;

            DataGridViewTextBoxCell valueCell = new DataGridViewTextBoxCell();
            valueCell.Value = value;

            DataGridViewTextBoxCell commentCell = new DataGridViewTextBoxCell();
            commentCell.Value = comment;

            DataGridViewTextBoxCell referencesCell = new DataGridViewTextBoxCell();
            referencesCell.Value = 0;            

            row.Cells.Add(keyCell);
            row.Cells.Add(valueCell);
            row.Cells.Add(commentCell);
            row.Cells.Add(referencesCell);
            row.DataSourceItem = node;

            referencesCell.ReadOnly = true;
            row.MinimumHeight = 25;
        }

        private void setNewKey(ResXStringGridRow row, string newKey) {
            if (string.IsNullOrEmpty(newKey)) {
                row.Status = ResXStringGridRow.STATUS.KEY_NULL;
            } else {
                row.Status = ResXStringGridRow.STATUS.OK;
                row.DataSourceItem.Name = newKey;
            }
        }

        private void editorControl_RemoveRequested(REMOVEKIND flags) {
            try {
                if (!this.Visible) return;
                if (this.SelectedRows.Count == 0) return;
                if ((flags | REMOVEKIND.REMOVE) != REMOVEKIND.REMOVE) throw new ArgumentException("Cannot delete or exclude strings.");

                if ((flags & REMOVEKIND.REMOVE) == REMOVEKIND.REMOVE) {
                    bool dataChanged = false;
                    List<ResXStringGridRow> copyRows = new List<ResXStringGridRow>(SelectedRows.Count);

                    foreach (ResXStringGridRow row in SelectedRows) {
                        if (!row.IsNewRow) {
                            ConflictResolver.TryAdd(row.Key, null, row);

                            row.Cells[KeyColumnName].Tag = null;
                            row.IndexAtDeleteTime = row.Index;
                            copyRows.Add(row);
                            Rows.Remove(row);  
                            dataChanged = true;
                        }                        
                    }

                    if (dataChanged) {
                        RemoveStringsUndoUnit undoUnit = new RemoveStringsUndoUnit(copyRows, this, editorControl.conflictResolver);
                        editorControl.Editor.AddUndoUnit(undoUnit);

                        NotifyItemsStateChanged();
                        NotifyDataChanged();
                    }
                }
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                VisualLocalizer.Library.MessageBox.ShowError(text);
            }
        }

        private void ResXStringGrid_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Right && !IsEditing) {
                HitTestInfo info = this.HitTest(e.X, e.Y);
                if (info != null && info.ColumnIndex >= 0 && info.RowIndex >= 0 && info.RowIndex != Rows.Count - 1) {
                    if (SelectedRows.Count == 0) {
                        Rows[info.RowIndex].Selected = true;                        
                    } else {
                        if (!Rows[info.RowIndex].Selected) {
                            ClearSelection();
                            Rows[info.RowIndex].Selected = true;
                        }
                    }
                    CurrentCell = Rows[info.RowIndex].Cells[info.ColumnIndex];
                    this.ContextMenu.Show(this, e.Location);
                }
            }
        }

        private void contextMenu_Popup(object sender, EventArgs e) {
            cutContextMenuItem.Enabled = this.CanCutOrCopy == COMMAND_STATUS.ENABLED;
            copyContextMenuItem.Enabled = this.CanCutOrCopy == COMMAND_STATUS.ENABLED;
            deleteContextMenuItem.Enabled = SelectedRows.Count >= 1;
            editContextMenuItem.Enabled = SelectedRows.Count == 1 && !CurrentCell.ReadOnly;
            inlineContextMenuItem.Enabled = false;
            pasteContextMenuItem.Enabled = this.CanPaste == COMMAND_STATUS.ENABLED;
        }

        #endregion
    }
}
