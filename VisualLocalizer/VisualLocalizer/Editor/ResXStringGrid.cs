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
using VisualLocalizer.Translate;
using System.Globalization;
using VisualLocalizer.Settings;
using VisualLocalizer.Commands;
using EnvDTE;
using System.Collections;
using System.Drawing;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace VisualLocalizer.Editor {    

    internal sealed class ResXStringGrid : AbstractKeyValueGridView<ResXDataNode>, IDataTabItem {

        public event EventHandler DataChanged;
        public event EventHandler ItemsStateChanged;
        public event Action<string, string> LanguagePairAdded;        

        private TextBox CurrentlyEditedTextBox;
        private ResXEditorControl editorControl;
        private MenuItem editContextMenuItem, cutContextMenuItem, copyContextMenuItem, pasteContextMenuItem, deleteContextMenuItem,
            inlineContextMenu, translateMenu, inlineContextMenuItem, inlineRemoveContextMenuItem;
        private ReferenceLister referenceLister;
        public bool ReferenceCounterThreadSuspended = false;
        private System.Threading.Thread referenceUpdaterThread;

        public ResXStringGrid(ResXEditorControl editorControl) : base(false, editorControl.conflictResolver) {
            this.editorControl = editorControl;
            this.AllowUserToAddRows = true;            
            this.ShowEditingIcon = false;
            this.MultiSelect = true;
            this.Dock = DockStyle.Fill;            
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.None;

            this.ScrollBars = ScrollBars.Both;
            this.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            this.AutoSize = true;            

            this.editorControl.RemoveRequested += new Action<REMOVEKIND>(editorControl_RemoveRequested);
            this.SelectionChanged += new EventHandler((o, e) => { NotifyItemsStateChanged(); });
            this.NewRowNeeded += new DataGridViewRowEventHandler((o, e) => { NotifyItemsStateChanged(); });
            this.MouseDown += new MouseEventHandler(ResXStringGrid_MouseDown);
            this.editorControl.NewTranslatePairAdded+=new Action<TRANSLATE_PROVIDER>(editorControl_TranslateRequested);
            this.editorControl.TranslateRequested+=new Action<TRANSLATE_PROVIDER,string,string>(editorControl_TranslateRequested);
            this.editorControl.InlineRequested += new Action<INLINEKIND>(editorControl_InlineRequested);
            this.Resize += new EventHandler(ResXStringGrid_Resize);
            this.ColumnWidthChanged += new DataGridViewColumnEventHandler(ResXStringGrid_ColumnWidthChanged);

            ResXStringGridRow rowTemplate = new ResXStringGridRow();
            rowTemplate.MinimumHeight = 24;
            this.RowTemplate = rowTemplate;

            editContextMenuItem = new MenuItem("Edit cell");
            editContextMenuItem.Shortcut = Shortcut.F2;
            editContextMenuItem.Click += new EventHandler((o, e) => { this.BeginEdit(true); });

            cutContextMenuItem = new MenuItem("Cut");
            cutContextMenuItem.Shortcut = Shortcut.CtrlX;
            cutContextMenuItem.Click += new EventHandler((o, e) => { 
                editorControl.ExecuteCut();
            });

            copyContextMenuItem = new MenuItem("Copy");
            copyContextMenuItem.Shortcut = Shortcut.CtrlC;
            copyContextMenuItem.Click += new EventHandler((o, e) => { editorControl.ExecuteCopy(); });

            pasteContextMenuItem = new MenuItem("Paste");
            pasteContextMenuItem.Shortcut = Shortcut.CtrlV;
            pasteContextMenuItem.Click += new EventHandler((o, e) => { editorControl.ExecutePaste(); });

            
            deleteContextMenuItem = new MenuItem("Remove");
            deleteContextMenuItem.Shortcut = Shortcut.Del;            
            deleteContextMenuItem.Click += new EventHandler((o, e) => { editorControl_RemoveRequested(REMOVEKIND.REMOVE); }); 
            
            inlineContextMenu = new MenuItem("Inline");

            inlineContextMenuItem = new MenuItem("Inline");
            inlineContextMenuItem.Shortcut = Shortcut.CtrlI;
            inlineContextMenuItem.Click += new EventHandler((o, e) => { editorControl_InlineRequested(INLINEKIND.INLINE); }); 
            
            inlineRemoveContextMenuItem = new MenuItem("Inline && remove");
            inlineRemoveContextMenuItem.Shortcut = Shortcut.CtrlShiftI;            
            inlineRemoveContextMenuItem.Click += new EventHandler((o, e) => { editorControl_InlineRequested(INLINEKIND.INLINE | INLINEKIND.REMOVE); });

            inlineContextMenu.MenuItems.Add(inlineContextMenuItem);
            inlineContextMenu.MenuItems.Add(inlineRemoveContextMenuItem);

            translateMenu = new MenuItem("Translate");
            foreach (ToolStripMenuItem item in editorControl.translateButton.DropDownItems) {
                MenuItem mItem = new MenuItem();
                mItem.Tag = item.Tag;
                mItem.Text = item.Text;
                translateMenu.MenuItems.Add(mItem);
            }

            ContextMenu contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add(editContextMenuItem);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(cutContextMenuItem);
            contextMenu.MenuItems.Add(copyContextMenuItem);
            contextMenu.MenuItems.Add(pasteContextMenuItem);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(inlineContextMenu);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(translateMenu);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(deleteContextMenuItem);
            contextMenu.Popup += new EventHandler(contextMenu_Popup);
            this.ContextMenu = contextMenu;

            this.ColumnHeadersHeight = 24;

            referenceLister = new ReferenceLister();
            
            referenceUpdaterThread = new System.Threading.Thread(ReferenceLookuperThread);
            referenceUpdaterThread.IsBackground = true;
            referenceUpdaterThread.Priority = System.Threading.ThreadPriority.BelowNormal;

            UpdateContextItemsEnabled();
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
                            newNode.Comment = string.Format("@@@{0}-@-{1}-@-{2}", (int)row.Status, row.DataSourceItem.Name, row.DataSourceItem.Comment);
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
            ReferenceCounterThreadSuspended = true;
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
            ReferenceCounterThreadSuspended = false;
            if (!referenceUpdaterThread.IsAlive) referenceUpdaterThread.Start();         
        }

        public COMMAND_STATUS CanCutOrCopy {
            get {
                return (HasSelectedItems && !IsEditing && !ReadOnly) ? COMMAND_STATUS.ENABLED : COMMAND_STATUS.DISABLED;
            }
        }

        public COMMAND_STATUS CanPaste {
            get {
                return (Clipboard.ContainsText() && !IsEditing && !ReadOnly) ? COMMAND_STATUS.ENABLED : COMMAND_STATUS.DISABLED;
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

        public bool DataReadOnly {
            get {
                return ReadOnly;
            }
            set {
                this.ReadOnly = value;
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

        public void NotifyDataChanged() {
            if (DataChanged != null) DataChanged(this, null);
        }

        public void NotifyItemsStateChanged() {
            if (ItemsStateChanged != null) ItemsStateChanged(this.Parent, null);
        }

        public void SetContainingTabPageSelected() {
            TabPage page = Parent as TabPage;
            if (page == null) return;

            TabControl tabControl = page.Parent as TabControl;
            if (tabControl == null) return;

            tabControl.SelectedTab = page;
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
            ignoreColumnWidthChange = true;

            DataGridViewTextBoxColumn keyColumn = new DataGridViewTextBoxColumn();
            keyColumn.MinimumWidth = 50;
            keyColumn.Width = 180;
            keyColumn.HeaderText = "Resource Key";
            keyColumn.Name = KeyColumnName;
            keyColumn.Frozen = false;
            keyColumn.SortMode = DataGridViewColumnSortMode.Automatic;            
            this.Columns.Add(keyColumn);

            DataGridViewTextBoxColumn valueColumn = new DataGridViewTextBoxColumn();
            valueColumn.Width = 250;
            valueColumn.MinimumWidth = 50;
            valueColumn.HeaderText = "Resource Value";
            valueColumn.Name = ValueColumnName;
            valueColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;                        
            valueColumn.Frozen = false;
            valueColumn.SortMode = DataGridViewColumnSortMode.Automatic;            
            this.Columns.Add(valueColumn);

            DataGridViewTextBoxColumn commentColumn = new DataGridViewTextBoxColumn();
            commentColumn.MinimumWidth = 50;
            commentColumn.Width = 180;
            commentColumn.HeaderText = "Comment";
            commentColumn.Name = CommentColumnName;
            commentColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            commentColumn.Frozen = false;
            commentColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            this.Columns.Add(commentColumn);

            DataGridViewTextBoxColumn referencesColumn = new DataGridViewTextBoxColumn();
            referencesColumn.Width = 70;
            referencesColumn.MinimumWidth = 40;
            referencesColumn.HeaderText = "References";
            referencesColumn.Name = ReferencesColumnName;
            referencesColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            referencesColumn.Frozen = false;
            referencesColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            referencesColumn.ReadOnly = true;
            this.Columns.Add(referencesColumn);

            ignoreColumnWidthChange = false;
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

        protected override void OnCellBeginEdit(DataGridViewCellCancelEventArgs e) {
            base.OnCellBeginEdit(e);
            
            if (e.ColumnIndex == 0) {
                ResXStringGridRow row = (ResXStringGridRow)Rows[e.RowIndex];
                if (row.ErrorSet.Count == 0) row.LastValidKey = row.Key;

                ReferenceCounterThreadSuspended = true;
                UpdateReferencesCount(row);
            }         
        }

        protected override void OnCellEndEdit(DataGridViewCellEventArgs e) {
            try {
                if (e.RowIndex == Rows.Count - 1) return;

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
                        if (row.ErrorSet.Count == 0) row.LastValidKey = row.Key;

                        if (isNewRow) {
                            setNewKey(row, newKey);
                            row.Cells[ReferencesColumnName].Value = "?";
                            StringRowAdded(row);
                            NotifyDataChanged();
                        } else {
                            if (string.Compare(newKey, node.Name) != 0) {
                                StringKeyRenamed(row, newKey);
                                setNewKey(row, newKey);
                                NotifyDataChanged();
                            }
                        }

                        UpdateReferencesCount(row);
                    } else if (Columns[e.ColumnIndex].Name == ValueColumnName) {
                        string newValue = (string)row.Cells[ValueColumnName].Value;
                        if (isNewRow) {
                            row.Status = ResXStringGridRow.STATUS.KEY_NULL;
                            row.Cells[ReferencesColumnName].Value = "?";
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
                            row.Cells[ReferencesColumnName].Value = "?";
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
            } finally {
                ReferenceCounterThreadSuspended = false;
                NotifyItemsStateChanged();
            }
        }
        
        protected override ResXDataNode GetResultItemFromRow(DataGridViewRow row) {
            throw new NotImplementedException();
        }
        
        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e) {
            UpdateContextItemsEnabled();
            base.OnPreviewKeyDown(e);
        }

        #endregion

        #region public members

        public void UpdateReferencesCount(ResXStringGridRow row) {
            UpdateReferencesCount(new List<ResXStringGridRow>() { row });
        }

        public void UpdateReferencesCount(IList rows) {            
            ProjectItem thisItem = VisualLocalizerPackage.Instance.DTE.Solution.FindProjectItem(editorControl.Editor.FileName);
            if (thisItem != null && thisItem.ContainingProject!=null && VisualLocalizerPackage.Instance.DTE.Solution.IsUserDefined()) {
                ResXProjectItem resxItem = ResXProjectItem.ConvertToResXItem(thisItem, thisItem.ContainingProject);
                List<Project> projects = new List<Project>();

                projects.Add(thisItem.ContainingProject);
                foreach (Project solutionProject in VisualLocalizerPackage.Instance.DTE.Solution.Projects) {
                    foreach (Project proj in solutionProject.GetReferencedProjects()) {
                        if (proj == thisItem.ContainingProject) {
                            projects.Add(solutionProject);
                            break;
                        }
                    }
                }

                if (!editorControl.Editor.HasDesignerClass) {
                    foreach (ResXStringGridRow row in rows) {
                        if (row.IsNewRow) continue;
                        row.CodeReferences.Clear();                        
                        row.UpdateReferenceCount();
                    }
                } else {
                    Trie<CodeReferenceTrieElement> trie = new Trie<CodeReferenceTrieElement>();
                    foreach (ResXStringGridRow row in rows) {
                        if (row.IsNewRow) continue;
                        string referenceKey;
                        if (row.ErrorSet.Count == 0) {
                            referenceKey = row.Key;
                        } else {
                            referenceKey = row.LastValidKey;
                        }
                        var element = trie.Add(resxItem.Class + "." + referenceKey);
                        element.Infos.Add(new CodeReferenceInfo() { Origin = resxItem, Value = row.Value, Key = referenceKey });
                    }
                    trie.CreatePredecessorsAndShortcuts();

                    referenceLister.Process(projects, trie);

                    foreach (ResXStringGridRow row in rows) {
                        if (row.IsNewRow) continue;
                        row.CodeReferences.Clear();
                        row.CodeReferences.AddRange(referenceLister.Results.Where((item) => { 
                            return item.Key == row.Key || (row.ErrorSet.Count > 0 && item.Key == row.LastValidKey); 
                        }));
                        row.UpdateReferenceCount();                        
                    }
                }
            }
        }

        public void StringCommentChanged(ResXStringGridRow row, string oldComment, string newComment) {
            string key = row.Status == ResXStringGridRow.STATUS.KEY_NULL ? null : row.DataSourceItem.Name;
            StringChangeCommentUndoUnit unit = new StringChangeCommentUndoUnit(row, this, key, oldComment, newComment);
            editorControl.Editor.AddUndoUnit(unit);

            VLOutputWindow.VisualLocalizerPane.WriteLine("Edited comment of \"{0}\"", key);
        }

        public void StringValueChanged(ResXStringGridRow row, string oldValue, string newValue) {
            string key = row.Status == ResXStringGridRow.STATUS.KEY_NULL ? null : row.DataSourceItem.Name;
            StringChangeValueUndoUnit unit = new StringChangeValueUndoUnit(row, this, key, oldValue, newValue, row.DataSourceItem.Comment);
            editorControl.Editor.AddUndoUnit(unit);

            VLOutputWindow.VisualLocalizerPane.WriteLine("Edited value of \"{0}\"", key);
        }

        public void StringKeyRenamed(ResXStringGridRow row, string newKey) {
            string oldKey = row.Status == ResXStringGridRow.STATUS.KEY_NULL ? null : row.DataSourceItem.Name;
            StringRenameKeyUndoUnit unit = new StringRenameKeyUndoUnit(row, this, oldKey, newKey);            

            if (row.ErrorSet.Count == 0) {
                int errors=0;
                int count = row.CodeReferences.Count;
                row.CodeReferences.ForEach((item) => { item.KeyAfterRename = newKey; });

                BatchReferenceReplacer replacer = new BatchReferenceReplacer(row.CodeReferences);
                replacer.Inline(row.CodeReferences, true, ref errors);
                
                VLOutputWindow.VisualLocalizerPane.WriteLine("Renamed {0} key references in code", count);
            }

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

                VLOutputWindow.VisualLocalizerPane.WriteLine("Added {0} new rows from clipboard", addedRows.Count);
            }
        }

        public void ValidateRow(ResXStringGridRow row) {
            Validate(row);
        }        

        public string CommentColumnName {
            get { return "Comment"; }
        }

        public string ReferencesColumnName {
            get { return "References"; }
        }

        #endregion

        #region private members        

        private void ReferenceLookuperThread() {
            UpdateReferencesCount(Rows);
            while (!IsDisposed) {
                try {
                    System.Threading.Thread.Sleep(SettingsObject.Instance.ReferenceUpdateInterval);
                    if (Visible && !IsDisposed && !ReferenceCounterThreadSuspended)
                        UpdateReferencesCount(Rows);
                } catch (Exception ex) {
                    VLOutputWindow.VisualLocalizerPane.WriteLine("{0} occured on reference lookuper thread: {1}", ex.GetType().Name, ex.Message);
                    // termination of IDE
                }
            }
            VLOutputWindow.VisualLocalizerPane.WriteLine("Reference lookuper thread of \"{0}\" terminated", Path.GetFileName(editorControl.Editor.FileName));
        }

        private string[] GetMangledCommentData(string comment) {
            string p = comment.Substring(3);
            string[] data = p.Split(new string[] { "-@-" }, StringSplitOptions.None);
            return data;
        }

        private void PopulateRow(ResXStringGridRow row, ResXDataNode node) {
            string name, value, comment;
            if (node.Comment.StartsWith("@@@")) {
                string[] data = GetMangledCommentData(node.Comment);

                row.Status = (ResXStringGridRow.STATUS)int.Parse(data[0]);
                name = data[1];
                comment = data[2];
                value = node.GetValue<string>();

                if (row.Status == ResXStringGridRow.STATUS.OK) {
                    node.Name = name;
                } else {
                    name = string.Empty;
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
            referencesCell.Value = "?";            

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

        private void editorControl_RemoveRequested(REMOVEKIND flags, bool addUndoUnit, out RemoveStringsUndoUnit undoUnit) {
            undoUnit = null;
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
                        undoUnit = new RemoveStringsUndoUnit(copyRows, this, editorControl.conflictResolver);
                        if (addUndoUnit) {                            
                            editorControl.Editor.AddUndoUnit(undoUnit);
                        }

                        NotifyItemsStateChanged();
                        NotifyDataChanged();

                        VLOutputWindow.VisualLocalizerPane.WriteLine("Removed {0} rows", copyRows.Count);
                    }
                }
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                VisualLocalizer.Library.MessageBox.ShowError(text);
            }
        }

        private void editorControl_RemoveRequested(REMOVEKIND flags) {
            RemoveStringsUndoUnit u;
            editorControl_RemoveRequested(flags, true, out u);
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

        private void UpdateContextItemsEnabled() {
            cutContextMenuItem.Enabled = this.CanCutOrCopy == COMMAND_STATUS.ENABLED;
            copyContextMenuItem.Enabled = this.CanCutOrCopy == COMMAND_STATUS.ENABLED;
            deleteContextMenuItem.Enabled = SelectedRows.Count >= 1 && !ReadOnly && !IsEditing; ;
            editContextMenuItem.Enabled = SelectedRows.Count == 1 && !CurrentCell.ReadOnly && !ReadOnly;
            inlineContextMenu.Enabled = SelectedRows.Count >= 1 && !ReadOnly && !IsEditing;
            pasteContextMenuItem.Enabled = this.CanPaste == COMMAND_STATUS.ENABLED;
            translateMenu.Enabled = SelectedRows.Count >= 1 && !ReadOnly && !IsEditing;
        }

        private void contextMenu_Popup(object sender, EventArgs e) {
            UpdateContextItemsEnabled();

            foreach (MenuItem menuItem in translateMenu.MenuItems) {
                menuItem.MenuItems.Clear();
                TRANSLATE_PROVIDER provider = (TRANSLATE_PROVIDER)menuItem.Tag;

                bool enabled = true;
                if (provider == TRANSLATE_PROVIDER.BING) {
                    enabled = !string.IsNullOrEmpty(SettingsObject.Instance.BingAppId);
                }

                menuItem.Enabled = enabled;

                foreach (var pair in SettingsObject.Instance.LanguagePairs) {
                    MenuItem newItem = new MenuItem(pair.ToString());
                    newItem.Tag = pair;
                    newItem.Click += new EventHandler((o, args) => {
                        SettingsObject.LanguagePair sentPair = (o as MenuItem).Tag as SettingsObject.LanguagePair;
                        editorControl_TranslateRequested(provider, sentPair.FromLanguage, sentPair.ToLanguage);
                    });
                    newItem.Enabled = enabled;
                    menuItem.MenuItems.Add(newItem);
                }

                MenuItem addItem = new MenuItem("New language pair...", new EventHandler((o, args) => {
                    editorControl_TranslateRequested(provider);
                }));
                addItem.Enabled = enabled;
                menuItem.MenuItems.Add(addItem);
            }

        }

        private void editorControl_TranslateRequested(TRANSLATE_PROVIDER provider) {
            NewLanguagePairWindow win = new NewLanguagePairWindow(true);
            if (win.ShowDialog() == DialogResult.OK) {
                if (win.AddToList && LanguagePairAdded != null) {
                    LanguagePairAdded(win.SourceLanguage, win.TargetLanguage);
                }
                editorControl_TranslateRequested(provider, win.SourceLanguage, win.TargetLanguage);
            }
        }

        private void editorControl_TranslateRequested(TRANSLATE_PROVIDER provider, string from, string to) {
            ITranslatorService service = null;
            switch (provider) {
                case TRANSLATE_PROVIDER.BING:
                    service = BingTranslator.GetService(SettingsObject.Instance.BingAppId);
                    break;
                case TRANSLATE_PROVIDER.MYMEMORY:
                    service = MyMemoryTranslator.GetService();
                    break;
                case TRANSLATE_PROVIDER.GOOGLE:
                    service = GoogleTranslator.GetService();
                    break;
            }
            if (service == null) {
                VisualLocalizer.Library.MessageBox.ShowError("Cannot resolve translation provider!");
            } else {
                System.Threading.Thread translatingThread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(this.translatingThread));
                translatingThread.Start(new TranslatingThreadInfo() { FromLanguage = from, ToLanguage = to, Service = service });
            }
        }

        private void translatingThread(object o) {          
            uint statusBarCookie = 0;
            string statusBarText = "Translating...";
            uint completed = 0;

            try {
                TranslatingThreadInfo nfo = (TranslatingThreadInfo)o;
                editorControl.Editor.StatusBar.Progress(ref statusBarCookie, 1, statusBarText, 0, (uint)SelectedRows.Count);
                editorControl.Invoke(new Action(() => { 
                    editorControl.SetReadOnly(true);                    
                }));                
                
                foreach (ResXStringGridRow row in SelectedRows) {
                    if (!row.IsNewRow) {
                        string oldValue = (string)row.Cells[ValueColumnName].Value;
                        row.Cells[ValueColumnName].Value = nfo.Service.Translate(nfo.FromLanguage, nfo.ToLanguage, (string)row.Cells[ValueColumnName].Value);

                        StringValueChanged(row, oldValue, (string)row.Cells[ValueColumnName].Value);
                        VLOutputWindow.VisualLocalizerPane.WriteLine("Translated \"{0}\" as \"{1}\" ", oldValue, row.Cells[ValueColumnName].Value);
                    }
                    completed++;
                    editorControl.Editor.StatusBar.Progress(ref statusBarCookie, 1, statusBarText, completed, (uint)SelectedRows.Count);                    
                }
            } catch (Exception ex) {
                string text;
                if (ex is CannotParseResponseException) {
                    CannotParseResponseException cpex = ex as CannotParseResponseException;
                    text = string.Format("Server response cannot be parsed: {0}.\nFull response:\n{1}", ex.Message, cpex.FullResponse);
                } else {
                    text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);
                }
                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                editorControl.Invoke(new Action(() => { VisualLocalizer.Library.MessageBox.ShowError(text); }));
            } finally {
                editorControl.Invoke(new Action(() => { 
                    editorControl.SetReadOnly(false);                    
                }));
                if (completed > 0) NotifyDataChanged(); 
                editorControl.Editor.StatusBar.Progress(ref statusBarCookie, 1, statusBarText, (uint)SelectedRows.Count, (uint)SelectedRows.Count);
                System.Threading.Thread.Sleep(500);
                editorControl.Editor.StatusBar.Progress(ref statusBarCookie, 0, "Ready", (uint)SelectedRows.Count, (uint)SelectedRows.Count);
            }
        }

        private void editorControl_InlineRequested(INLINEKIND kind) {
            DialogResult result = VisualLocalizer.Library.MessageBox.Show("This operation is irreversible, cannot be undone globally, only using undo managers in open files. Do you want to proceed?",
                null, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND, OLEMSGICON.OLEMSGICON_WARNING);
            
            if (result == DialogResult.Yes) {
                try {
                    ReferenceCounterThreadSuspended = true;

                    if ((kind & INLINEKIND.INLINE) == INLINEKIND.INLINE) {
                        UpdateReferencesCount(SelectedRows);

                        List<CodeReferenceResultItem> totalList = new List<CodeReferenceResultItem>();

                        foreach (ResXStringGridRow row in SelectedRows) {
                            if (!row.IsNewRow) {
                                totalList.AddRange(row.CodeReferences);
                            }
                        }
                        BatchInliner inliner = new BatchInliner(totalList);

                        int errors = 0;
                        inliner.Inline(totalList, false, ref errors);
                        VLOutputWindow.VisualLocalizerPane.WriteLine("Inlining of selected rows finished - found {0} references, {1} finished successfuly", totalList.Count, totalList.Count - errors);
                    }
                    if ((kind & INLINEKIND.REMOVE) == INLINEKIND.REMOVE) {
                        RemoveStringsUndoUnit removeUnit = null;
                        editorControl_RemoveRequested(REMOVEKIND.REMOVE, false, out removeUnit);
                    }

                    StringInlinedUndoItem undoItem = new StringInlinedUndoItem(SelectedRows.Count);
                    editorControl.Editor.AddUndoUnit(undoItem);
                } catch (Exception ex) {
                    string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                    VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                    VisualLocalizer.Library.MessageBox.ShowError(text);
                } finally {
                    ReferenceCounterThreadSuspended = false;
                }
            }
        }

        private void ResXStringGrid_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e) {
            if (ignoreColumnWidthChange) return;
            if (e.Column.Name == ReferencesColumnName) return;

            resizeColumnsFavore(Columns[e.Column.Index + 1].Name);
        }

        private void ResXStringGrid_Resize(object sender, EventArgs e) {
            resizeColumnsFavore(ValueColumnName);
        }

        private bool ignoreColumnWidthChange;
        private void resizeColumnsFavore(string columnName) {
            int restWidth = 0;
            foreach (DataGridViewColumn col in Columns)
                if (col.Name != columnName) restWidth += col.Width;

            ignoreColumnWidthChange = true;
            Columns[columnName].Width = this.ClientSize.Width - restWidth - this.RowHeadersWidth;
            ignoreColumnWidthChange = false;
        }

        #endregion

        private class TranslatingThreadInfo {
            public ITranslatorService Service { get; set; }
            public string FromLanguage { get; set; }
            public string ToLanguage { get; set; }
        }
    }
}
