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
using VisualLocalizer.Extensions;

namespace VisualLocalizer.Editor {    

    /// <summary>
    /// Represents String tab in the ResX editor
    /// </summary>
    internal sealed class ResXStringGrid : AbstractKeyValueGridView<ResXDataNode>, IDataTabItem {

        /// <summary>
        /// Issued when data changed in GUI and the document should be marked dirty
        /// </summary>
        public event EventHandler DataChanged;

        /// <summary>
        /// Issued when selected items collection changed and certain GUI elements should be enabled/disabled
        /// </summary>
        public event EventHandler ItemsStateChanged;
 
        public event Action<string, string> LanguagePairAdded;        

        private TextBox CurrentlyEditedTextBox;
        private ResXEditorControl editorControl;
        private ImageMenuItem editContextMenuItem, cutContextMenuItem, copyContextMenuItem, pasteContextMenuItem, deleteContextMenuItem,
            inlineContextMenu, translateMenu, inlineContextMenuItem, inlineRemoveContextMenuItem, showResultItemsMenuItem;
        
        public ResXStringGrid(ResXEditorControl editorControl) : base(false, editorControl.conflictResolver) {
            this.editorControl = editorControl;
            this.AllowUserToAddRows = true;            
            this.ShowEditingIcon = false;
            this.MultiSelect = true;
            this.Dock = DockStyle.Fill;            
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.None;
            this.ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable;            
            this.ScrollBars = ScrollBars.Both;
            this.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            this.AutoSize = true;            

            // to remove rows
            this.editorControl.RemoveRequested += new Action<REMOVEKIND>(EditorControl_RemoveRequested);

            // to change depending buttons and context menu state (enabled/disabled)
            this.SelectionChanged += new EventHandler((o, e) => { NotifyItemsStateChanged(); });
            this.NewRowNeeded += new DataGridViewRowEventHandler((o, e) => { NotifyItemsStateChanged(); });
            this.MouseDown += new MouseEventHandler(ResXStringGrid_MouseDown);
            this.editorControl.NewTranslatePairAdded+=new Action<TRANSLATE_PROVIDER>(EditorControl_TranslateRequested);
            this.editorControl.TranslateRequested+=new Action<TRANSLATE_PROVIDER,string,string>(EditorControl_TranslateRequested);
            this.editorControl.InlineRequested += new Action<INLINEKIND>(EditorControl_InlineRequested);
            this.Resize += new EventHandler(ResXStringGrid_Resize);
            this.ColumnWidthChanged += new DataGridViewColumnEventHandler(ResXStringGrid_ColumnWidthChanged);

            ResXStringGridRow rowTemplate = new ResXStringGridRow();
            rowTemplate.MinimumHeight = 24;
            this.RowTemplate = rowTemplate;

            // create context menu
            editContextMenuItem = new ImageMenuItem("Edit cell");
            editContextMenuItem.Shortcut = Shortcut.F2;
            editContextMenuItem.Click += new EventHandler((o, e) => { this.BeginEdit(true); });

            cutContextMenuItem = new ImageMenuItem("Cut");
            cutContextMenuItem.Image = Editor.cut;
            cutContextMenuItem.Shortcut = Shortcut.CtrlX;
            cutContextMenuItem.Click += new EventHandler((o, e) => { editorControl.ExecuteCut(); });

            copyContextMenuItem = new ImageMenuItem("Copy");
            copyContextMenuItem.Image = Editor.copy;
            copyContextMenuItem.Shortcut = Shortcut.CtrlC;
            copyContextMenuItem.Click += new EventHandler((o, e) => { editorControl.ExecuteCopy(); });

            pasteContextMenuItem = new ImageMenuItem("Paste");
            pasteContextMenuItem.Image = Editor.paste;
            pasteContextMenuItem.Shortcut = Shortcut.CtrlV;
            pasteContextMenuItem.Click += new EventHandler((o, e) => { editorControl.ExecutePaste(); });


            deleteContextMenuItem = new ImageMenuItem("Remove");
            deleteContextMenuItem.Image = Editor.remove;
            deleteContextMenuItem.Shortcut = Shortcut.Del;            
            deleteContextMenuItem.Click += new EventHandler((o, e) => { EditorControl_RemoveRequested(REMOVEKIND.REMOVE); });

            inlineContextMenu = new ImageMenuItem("Inline");

            inlineContextMenuItem = new ImageMenuItem("Inline");
            inlineContextMenuItem.Shortcut = Shortcut.CtrlI;
            inlineContextMenuItem.Click += new EventHandler((o, e) => { EditorControl_InlineRequested(INLINEKIND.INLINE); });

            inlineRemoveContextMenuItem = new ImageMenuItem("Inline & remove");
            inlineRemoveContextMenuItem.Shortcut = Shortcut.CtrlShiftI;            
            inlineRemoveContextMenuItem.Click += new EventHandler((o, e) => { EditorControl_InlineRequested(INLINEKIND.INLINE | INLINEKIND.REMOVE); });

            inlineContextMenu.MenuItems.Add(inlineContextMenuItem);
            inlineContextMenu.MenuItems.Add(inlineRemoveContextMenuItem);

            translateMenu = new ImageMenuItem("Translate");
            translateMenu.Image = Editor.translate;
            foreach (ToolStripMenuItem item in editorControl.translateButton.DropDownItems) {
                MenuItem mItem = new MenuItem();
                mItem.Tag = item.Tag;
                mItem.Text = item.Text;
                translateMenu.MenuItems.Add(mItem);
            }

            showResultItemsMenuItem = new ImageMenuItem("Show references");
            showResultItemsMenuItem.Image = Editor.search;
            showResultItemsMenuItem.Shortcut = Shortcut.CtrlF;
            showResultItemsMenuItem.Click += new EventHandler(showResultItemsMenuItem_Click);

            ContextMenu contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add(editContextMenuItem);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(cutContextMenuItem);
            contextMenu.MenuItems.Add(copyContextMenuItem);
            contextMenu.MenuItems.Add(pasteContextMenuItem);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(showResultItemsMenuItem);
            contextMenu.MenuItems.Add(inlineContextMenu);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(translateMenu);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(deleteContextMenuItem);
            contextMenu.Popup += new EventHandler(ContextMenu_Popup);
            this.ContextMenu = contextMenu;

            this.ColumnHeadersHeight = 24;            

            UpdateContextItemsEnabled();
        }

        public ResXEditorControl EditorControl {
            get {
                return editorControl;
            }
        }

        #region IDataTabItem members

        /// <summary>
        /// Returns current working data
        /// </summary>
        /// <param name="throwExceptions">False if no exceptions should be thrown on errors (used by reference lookuper thread)</param>
        public Dictionary<string, ResXDataNode> GetData(bool throwExceptions) {
            EndEdit(); // cancel editting a cell

            Dictionary<string, ResXDataNode> data = new Dictionary<string, ResXDataNode>(RowCount);
            foreach (ResXStringGridRow row in Rows) {
                if (!string.IsNullOrEmpty(row.ErrorText)) {
                    if (throwExceptions) {
                        throw new Exception(row.ErrorText);
                    } else {
                        if (row.DataSourceItem != null) { // save under fake key (it may be null)
                            string rndFile = Path.GetRandomFileName().CreateIdentifier(LANGUAGE.CSHARP);
                            ResXDataNode newNode = new ResXDataNode(rndFile, row.DataSourceItem.GetValue<string>());
                            
                            // save all data in the comment
                            newNode.Comment = CreateMangledComment(row); // mangles all resource data to comment
                            data.Add(newNode.Name.ToLower(), newNode);
                        }
                    }
                } else if (row.DataSourceItem != null) {
                    if (row.DataSourceItem.GetValue<string>() == null) {
                        string cmt = row.DataSourceItem.Comment;
                        row.DataSourceItem = new ResXDataNode(row.DataSourceItem.Name, "");
                        row.DataSourceItem.Comment = cmt;
                    }
                    data.Add(row.DataSourceItem.Name.ToLower(), row.DataSourceItem);
                    if (!CanContainItem(row.DataSourceItem) && throwExceptions) 
                        throw new Exception("Error saving '" + row.DataSourceItem.Name + "' - cannot preserve type."); 
                }
            }

            return data;
        }

        /// <summary>
        /// Returns true if given node's type matches the type of items this control holds
        /// </summary>
        public bool CanContainItem(ResXDataNode node) {
            if (node == null) throw new ArgumentNullException("node");
            return node.HasValue<string>(); // only strings are allows
        }

        /// <summary>
        /// Begins batch adding items
        /// </summary>
        public void BeginAdd() {
            base.SetData(null);
            this.SuspendLayout();
            Rows.Clear();            
        }

        /// <summary>
        /// Adds given resource to the control
        /// </summary>   
        public IKeyValueSource Add(string key, ResXDataNode value) {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

            ResXStringGridRow row = new ResXStringGridRow();
            PopulateRow(row, value);

            Rows.Add(row);
            Validate(row);

            if (row.ErrorMessages.Count > 0) {
                row.Status = KEY_STATUS.ERROR;
            } else {
                row.Status = KEY_STATUS.OK;
                row.LastValidKey = row.Key;
            }

            return row;
        }

        /// <summary>
        /// Ends batch adding items and refreshes the view
        /// </summary>
        public void EndAdd() {
            if (SortedColumn != null) { // reset sorting
                SortedColumn.HeaderCell.SortGlyphDirection = SortOrder.None;
            }
            this.ResumeLayout();                               
        }

        /// <summary>
        /// Returns status for Cut and Copy commands, based on currently selected items
        /// </summary>
        public COMMAND_STATUS CanCutOrCopy {
            get {
                return (HasSelectedItems && !IsEditing && !ReadOnly) ? COMMAND_STATUS.ENABLED : COMMAND_STATUS.DISABLED;
            }
        }

        /// <summary>
        /// Returns status for Paste command, based on currently selected items
        /// </summary>
        public COMMAND_STATUS CanPaste {
            get {
                return (Clipboard.ContainsText() && !IsEditing && !ReadOnly) ? COMMAND_STATUS.ENABLED : COMMAND_STATUS.DISABLED;
            }
        }

        /// <summary>
        /// Copies selected rows to clipboard, using both CSV format and tab-separated format in order to cooperate with Excel and text editors
        /// </summary>
        public bool Copy() {
            DataObject dataObject = new DataObject();

            StringBuilder tabbedContent = new StringBuilder();
            StringBuilder csvContent = new StringBuilder();
            foreach (DataGridViewRow row in SelectedRows) {
                if (row.IsNewRow) continue;
                tabbedContent.AppendFormat("{0}\t{1}\t{2}" + Environment.NewLine, (string)row.Cells[KeyColumnName].Value, (string)row.Cells[ValueColumnName].Value, (string)row.Cells[CommentColumnName].Value);
                csvContent.AppendFormat("{0};{1};{2}" + Environment.NewLine, (string)row.Cells[KeyColumnName].Value, (string)row.Cells[ValueColumnName].Value, (string)row.Cells[CommentColumnName].Value);
            }
            dataObject.SetText(tabbedContent.ToString());

            MemoryStream ms = new MemoryStream(Encoding.Default.GetBytes(csvContent.ToString()));
            dataObject.SetData(DataFormats.CommaSeparatedValue, ms);

            Clipboard.SetDataObject(dataObject, true);
            return true;
        }

        /// <summary>
        /// Performs Cut command
        /// </summary>    
        public bool Cut() {
            bool ok = Copy(); // perform copy
            if (!ok) return false;

            EditorControl_RemoveRequested(REMOVEKIND.REMOVE);  // remove the rows          

            return true;
        }

        /// <summary>
        /// Returns true if this list is not empty
        /// </summary>
        public bool HasItems {
            get {
                return Rows.Count > 1;
            }
        }

        /// <summary>
        /// Returns true if there are selected items in this list
        /// </summary>
        public bool HasSelectedItems {
            get {
                return (SelectedRows.Count > 1 || (SelectedRows.Count == 1 && !SelectedRows[0].IsNewRow));
            }
        }

        /// <summary>
        /// Gets/sets whether this control is readonly
        /// </summary>
        public bool DataReadOnly {
            get {
                return ReadOnly;
            }
            set {
                this.ReadOnly = value;
            }
        }

        /// <summary>
        /// Performs Select All command
        /// </summary> 
        public bool SelectAllItems() {
            foreach (DataGridViewRow row in Rows)
                if (!row.IsNewRow) row.Selected = true;

            return true;
        }

        /// <summary>
        /// Returns true if a resource is being edited
        /// </summary>
        public bool IsEditing {
            get {
                return IsCurrentCellInEditMode;
            }
        }

        /// <summary>
        /// Fires DataChanged() event
        /// </summary>
        public void NotifyDataChanged() {
            if (DataChanged != null) DataChanged(this, null);
        }

        /// <summary>
        /// Fires ItemsStateChanged() event
        /// </summary>
        public void NotifyItemsStateChanged() {
            if (ItemsStateChanged != null) ItemsStateChanged(this.Parent, null);
        }

        /// <summary>
        /// Selects this tab
        /// </summary>
        public void SetContainingTabPageSelected() {
            TabPage page = Parent as TabPage;
            if (page == null) return;

            TabControl tabControl = page.Parent as TabControl;
            if (tabControl == null) return;

            tabControl.SelectedTab = page;
        }

        #endregion

        #region AbstractKeyValueGridView members        

        /// <summary>
        /// This method is not supported, data are added using the Add() method
        /// </summary>
        public override void SetData(List<ResXDataNode> list) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Validate the key/value pair of the row
        /// </summary>   
        protected override void Validate(DataGridViewKeyValueRow<ResXDataNode> row) {
            if (row == null) throw new ArgumentNullException("row");

            string key = row.Key;
            string value = row.Value;

            string originalValue = (string)row.Cells[KeyColumnName].Tag;
            editorControl.conflictResolver.TryAdd(originalValue, key, row, editorControl.Editor.ProjectItem, null);
            if (originalValue == null) row.Cells[KeyColumnName].Tag = key;

            row.UpdateErrorSetDisplay();
        }

        /// <summary>
        /// No checkbox is displayed
        /// </summary>
        public override string CheckBoxColumnName {
            get { return null; }
        }

        /// <summary>
        /// Returns name of the column used to hold key
        /// </summary>
        public override string KeyColumnName {
            get { return "KeyColumn"; }
        }

        /// <summary>
        /// Returns name of the column used to hold value
        /// </summary>
        public override string ValueColumnName {
            get { return "ValueColumn"; }
        }        

        #endregion        

        #region protected members - virtual

        /// <summary>
        /// Initializes grid columns
        /// </summary>
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

        /// <summary>
        /// Processes keys used for navigating in the <see cref="T:System.Windows.Forms.DataGridView" />.
        /// </summary>
        /// <param name="e">Contains information about the key that was pressed.</param>
        /// <returns>
        /// true if the key was processed; otherwise, false.
        /// </returns>
        protected override bool ProcessDataGridViewKey(KeyEventArgs e) {
            if (this.IsCurrentCellInEditMode && this.EditingControl is TextBox) {
                TextBox box = this.EditingControl as TextBox;
                if (e.KeyData == Keys.Home || e.KeyData == Keys.End) { // Home and End are enabled within the cell
                    return false;
                } else if (e.KeyData == Keys.Enter) { // pressing enter adds new row to the edited content
                    int selectionStart = box.SelectionStart;
                    box.Text = box.Text.Remove(selectionStart, box.SelectionLength).Insert(selectionStart, Environment.NewLine);
                    box.SelectionStart = selectionStart + Environment.NewLine.Length;
                    box.ScrollToCaret();
                    return true;
                } else return base.ProcessDataGridViewKey(e);
            } else return base.ProcessDataGridViewKey(e);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.DataGridView.EditingControlShowing" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.DataGridViewEditingControlShowingEventArgs" /> that contains information about the editing control.</param>
        protected override void OnEditingControlShowing(DataGridViewEditingControlShowingEventArgs e) {
            try {
                base.OnEditingControlShowing(e);
                if (!CurrentCellAddress.IsEmpty && CurrentCellAddress.X == 1 || CurrentCellAddress.X == 2 && e.Control is TextBox) {
                    // edited is value or comment - enable multiline content
                    TextBox box = e.Control as TextBox;
                    box.AcceptsReturn = true;
                    box.Multiline = true;
                    box.WordWrap = true;
                }
                if (e.Control is TextBox) {
                    CurrentlyEditedTextBox = e.Control as TextBox;
                }
                UpdateContextItemsEnabled();
                NotifyItemsStateChanged();
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Called before editting a cell - suspends reference lookuper thread and sets current key as last valid
        /// </summary>        
        protected override void OnCellBeginEdit(DataGridViewCellCancelEventArgs e) {            
            try {
                if (Columns[e.ColumnIndex].Name == KeyColumnName && e.RowIndex>=0) {
                    ResXStringGridRow row = (ResXStringGridRow)Rows[e.RowIndex];
                    if (row.CodeReferenceContainsReadonly) {
                        e.Cancel = true;
                        VisualLocalizer.Library.MessageBox.ShowError("This operation cannot be executed, because some of the references are located in readonly files.");
                        return;
                    }
                }

                base.OnCellBeginEdit(e);

                if (e.ColumnIndex == 0) {
                    ResXStringGridRow row = (ResXStringGridRow)Rows[e.RowIndex];
                    
                    editorControl.ReferenceCounterThreadSuspended = true; 
                    editorControl.UpdateReferencesCount(row);
                }                
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Called after editting a cell
        /// </summary>        
        protected override void OnCellEndEdit(DataGridViewCellEventArgs e) {
            try {
                if (e.RowIndex == Rows.Count - 1) return;

                base.OnCellEndEdit(e);                
                
                if (e.ColumnIndex >= 0 && e.RowIndex >= 0) {
                    ResXStringGridRow row = Rows[e.RowIndex] as ResXStringGridRow;
                    bool isNewRow = false;
                    if (row.DataSourceItem == null) { // last empty row was edited, new row has been added
                        isNewRow = true;
                        row.DataSourceItem = new ResXDataNode("(new)", string.Empty);                        
                    }
                    ResXDataNode node = row.DataSourceItem;

                    if (Columns[e.ColumnIndex].Name == KeyColumnName) { // key was edited
                        string newKey = (string)row.Cells[KeyColumnName].Value;
                      
                        if (isNewRow) {
                            SetNewKey(row, newKey);
                            row.Cells[ReferencesColumnName].Value = "?";
                            StringRowAdded(row);
                            NotifyDataChanged();
                        } else if (string.Compare(newKey, node.Name) != 0) {
                            // key has changed
                            StringKeyRenamed(row, newKey);
                            SetNewKey(row, newKey);
                            NotifyDataChanged();
                        }                     
                    } else if (Columns[e.ColumnIndex].Name == ValueColumnName) { // value was edited
                        string newValue = (string)row.Cells[ValueColumnName].Value;
                        if (newValue == null) newValue = string.Empty;

                        if (isNewRow) {
                            row.Status = KEY_STATUS.ERROR;
                            row.Cells[ReferencesColumnName].Value = "?";
                            StringRowAdded(row);
                            NotifyDataChanged();
                        } else if (string.Compare(newValue, node.GetValue<string>()) != 0) {
                            // value has changed
                            StringValueChanged(row, node.GetValue<string>(), newValue);
                            NotifyDataChanged();

                            string key = (string)row.Cells[KeyColumnName].Value;
                            ResXDataNode newNode;
                            if (string.IsNullOrEmpty(key)) {
                                newNode = new ResXDataNode("A", newValue);
                                row.Status = KEY_STATUS.ERROR;
                            } else {
                                newNode = new ResXDataNode(key, newValue);
                                row.Status = KEY_STATUS.OK;
                                row.LastValidKey = key;
                            }

                            newNode.Comment = (string)row.Cells[CommentColumnName].Value;
                            row.DataSourceItem = newNode;
                        }
                    } else { // comment was edited
                        string newComment = (string)row.Cells[CommentColumnName].Value;
                        if (isNewRow) {
                            row.Status = KEY_STATUS.ERROR;
                            row.Cells[ReferencesColumnName].Value = "?";
                            StringRowAdded(row);
                            NotifyDataChanged();
                        } else if (string.Compare(newComment, node.Comment) != 0) {
                            StringCommentChanged(row, node.Comment, newComment);
                            NotifyDataChanged();

                            node.Comment = newComment;
                        }
                    }
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            } finally {
                editorControl.ReferenceCounterThreadSuspended = false;
                NotifyItemsStateChanged();
            }
        }
        
        /// <summary>
        /// This method is not supported, data are obtained using the GetData() method
        /// </summary>        
        protected override ResXDataNode GetResultItemFromRow(DataGridViewRow row) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.PreviewKeyDown" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.PreviewKeyDownEventArgs" /> that contains the event data.</param>
        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e) {
            try {
                UpdateContextItemsEnabled();
                base.OnPreviewKeyDown(e);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        #endregion

        #region public members
        
        /// <summary>
        /// Called after comment was changed - adds undo unit
        /// </summary>        
        public void StringCommentChanged(ResXStringGridRow row, string oldComment, string newComment) {
            string key = row.Status == KEY_STATUS.ERROR ? null : row.DataSourceItem.Name;
            StringChangeCommentUndoUnit unit = new StringChangeCommentUndoUnit(row, this, key, oldComment, newComment);
            editorControl.Editor.AddUndoUnit(unit);

            VLOutputWindow.VisualLocalizerPane.WriteLine("Edited comment of \"{0}\"", key);
        }

        /// <summary>
        /// Called after value was changed - adds undo unit
        /// </summary> 
        public void StringValueChanged(ResXStringGridRow row, string oldValue, string newValue) {
            string key = row.Status == KEY_STATUS.ERROR ? null : row.DataSourceItem.Name;
            StringChangeValueUndoUnit unit = new StringChangeValueUndoUnit(row, this, key, oldValue, newValue, row.DataSourceItem.Comment);
            editorControl.Editor.AddUndoUnit(unit);

            VLOutputWindow.VisualLocalizerPane.WriteLine("Edited value of \"{0}\"", key);
        }

        /// <summary>
        /// Called after key was changed - adds undo unit and performs refactoring of code
        /// </summary> 
        public void StringKeyRenamed(ResXStringGridRow row, string newKey) {
            string oldKey = row.Status == KEY_STATUS.ERROR ? null : row.DataSourceItem.Name;
            
            StringRenameKeyUndoUnit unit = new StringRenameKeyUndoUnit(row, editorControl, oldKey, newKey);
            editorControl.Editor.AddUndoUnit(unit);

            if (VisualLocalizerPackage.Instance.DTE.Solution.ContainsProjectItem(editorControl.Editor.ProjectItem.InternalProjectItem)) {
                // obtain ResX project item
                ResXProjectItem resxItem = editorControl.Editor.ProjectItem;
                resxItem.ResolveNamespaceClass(resxItem.InternalProjectItem.ContainingProject.GetResXItemsAround(false, true));

                if (row.ConflictItems.Count == 0 && resxItem != null && !resxItem.IsCultureSpecific() && !string.IsNullOrEmpty(newKey)) {
                    int errors = 0;
                    int count = row.CodeReferences.Count;
                    
                    // set new key
                    row.CodeReferences.ForEach((item) => { item.KeyAfterRename = newKey.CreateIdentifier(resxItem.DesignerLanguage); });
                    
                    // run the replacer
                    try {
                        editorControl.ReferenceCounterThreadSuspended = true;
                        BatchReferenceReplacer replacer = new BatchReferenceReplacer();
                        replacer.Inline(row.CodeReferences, true, ref errors);
                    } finally {
                        editorControl.ReferenceCounterThreadSuspended = false;
                    }
                    VLOutputWindow.VisualLocalizerPane.WriteLine("Renamed {0} key references in code, {1} errors", count, errors);
                }                
            }            
        }

        /// <summary>
        /// Called when new row was added to the grid
        /// </summary>        
        public void StringRowAdded(ResXStringGridRow row) {
            if (row == null) throw new ArgumentNullException("row");

            StringRowsAdded(new List<ResXStringGridRow>() { row });
        }

        /// <summary>
        /// Called when new rows were added to the grid - creates appropriate undo unit
        /// </summary>        
        public void StringRowsAdded(List<ResXStringGridRow> rows) {
            if (rows == null) throw new ArgumentNullException("rows");

            StringRowAddUndoUnit unit = new StringRowAddUndoUnit(editorControl, rows, this, editorControl.conflictResolver);
            editorControl.Editor.AddUndoUnit(unit);
        }

        /// <summary>
        /// Adds given clipboard text to the grid - values separated with , and rows separated with ; format is expected
        /// </summary>
        /// <param name="text"></param>
        public void AddClipboardText(string text, bool isCsv) {
            if (text == null) throw new ArgumentNullException("text");

            string[] rows = text.Split(new string[] { Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries); // get the rows
            List<ResXStringGridRow> addedRows = new List<ResXStringGridRow>();
            foreach (string row in rows) {
                string[] columns = row.Split(isCsv ? ';' : '\t'); // get the columns
                if (columns.Length == 0) continue;

                string key = columns.Length >= 1 ? columns[0].CreateIdentifier(editorControl.Editor.ProjectItem.DesignerLanguage) : ""; // modify key so that is a valid identifier
                string value = columns.Length >= 2 ? columns[1] : "";
                string comment = columns.Length >= 3 ? columns[2] : "";

                ResXDataNode node = new ResXDataNode(key, value); // create new resource
                node.Comment = comment;

                ResXStringGridRow newRow = Add(key, node) as ResXStringGridRow; // add a row with the resource
                addedRows.Add(newRow);   
            }

            if (addedRows.Count > 0) {
                StringRowsAdded(addedRows);
                NotifyDataChanged();
                NotifyItemsStateChanged();

                VLOutputWindow.VisualLocalizerPane.WriteLine("Added {0} new rows from clipboard", addedRows.Count);
            }
        }

        /// <summary>
        /// Public wrapper for Validate(ResXStringGridRow) method
        /// </summary>        
        public void ValidateRow(ResXStringGridRow row) {
            if (row == null) throw new ArgumentNullException("row");
            Validate(row);
        }        

        public string CommentColumnName {
            get { return "Comment"; }
        }

        public string ReferencesColumnName {
            get { return "References"; }
        }

        /// <summary>
        /// Returns true if all selected rows' code references were succesfully looked up
        /// </summary>
        public bool AreReferencesKnownOnSelected {
            get {
                bool ok = true;
                foreach (DataGridViewRow row in SelectedRows) {
                    object o = row.Cells[ReferencesColumnName].Value;
                    if (o == null) { // reference count column must have a value
                        ok = false;
                        break;
                    }

                    string s = o.ToString();
                    int iv;
                    ok = ok && !string.IsNullOrEmpty(s) && int.TryParse(s, out iv); // the value must be a number
                }
                return ok;
            }
        }

        #endregion

        #region private members        

        /// <summary>
        /// Collects code references to all selected rows and displays them in the tool window
        /// </summary>       
        private void showResultItemsMenuItem_Click(object sender, EventArgs e) {
            try {
                List<CodeReferenceResultItem> selected = new List<CodeReferenceResultItem>();
                foreach (ResXStringGridRow row in SelectedRows) {
                    if (row.IsNewRow) continue;
                    if (string.IsNullOrEmpty(row.ErrorText)) selected.AddRange(row.CodeReferences);
                }
                editorControl.ShowReferences(selected);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }      

        /// <summary>
        /// Serializes resource data to a string
        /// </summary>        
        private string CreateMangledComment(ResXStringGridRow row) {
            if (row == null) throw new ArgumentNullException("row");

            return string.Format("@@@{0}-@-{1}-@-{2}", (int)row.Status, row.DataSourceItem.Name, row.DataSourceItem.Comment);
        }   

        /// <summary>
        /// Parses given string created by CreateMangledComment() method and returns stored data
        /// </summary>        
        private string[] GetMangledCommentData(string comment) {
            if (comment == null) throw new ArgumentNullException("comment");

            string p = comment.Substring(3); // remove @@@
            string[] data = p.Split(new string[] { "-@-" }, StringSplitOptions.None);
            if (data.Length != 3) throw new InvalidOperationException("Mangled comment is invalid: " + comment);

            return data;
        }

        /// <summary>
        /// Populates given row with data from given ResX node
        /// </summary>        
        private void PopulateRow(ResXStringGridRow row, ResXDataNode node) {
            if (row == null) throw new ArgumentNullException("row");
            if (node == null) throw new ArgumentNullException("node");

            string name, value, comment;
            if (node.Comment.StartsWith("@@@")) { // it's a mangled comment (row was not valid when saving)
                string[] data = GetMangledCommentData(node.Comment);

                row.Status = (KEY_STATUS)int.Parse(data[0]);
                name = data[1];
                comment = data[2];
                value = node.GetValue<string>();

                // set key
                if (row.Status == KEY_STATUS.OK) {
                    node.Name = name;
                } else {
                    name = string.Empty;
                }

                node.Comment = comment;
            } else { // the node is ok
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

        /// <summary>
        /// Updates row's status according to a new key
        /// </summary>        
        private void SetNewKey(ResXStringGridRow row, string newKey) {
            if (row == null) throw new ArgumentNullException("row");

            if (string.IsNullOrEmpty(newKey)) {
                row.Status = KEY_STATUS.ERROR;                
            } else {
                row.Status = KEY_STATUS.OK;
                row.DataSourceItem.Name = newKey;
                row.LastValidKey = newKey;
            }
        }

        /// <summary>
        /// Called when Remove button was clicked in editor's toolbar
        /// </summary>
        /// <param name="flags">Bitmask of REMOVEKIND values, settings parameters for the action</param>
        /// <param name="addUndoUnit">True if undo unit should be added for the operation</param>
        /// <param name="undoUnit">Created undo unit (if any)</param>
        private void EditorControl_RemoveRequested(REMOVEKIND flags, bool addUndoUnit, out RemoveStringsUndoUnit undoUnit) {
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
                            // remove the row from the conflict resolver
                            editorControl.conflictResolver.TryAdd(row.Key, null, row, editorControl.Editor.ProjectItem, null);

                            row.Cells[KeyColumnName].Tag = null;
                            row.IndexAtDeleteTime = row.Index;
                            copyRows.Add(row);
                            Rows.Remove(row);
                            dataChanged = true;
                        }
                    }

                    if (dataChanged) {
                        // create and add the undo unit
                        undoUnit = new RemoveStringsUndoUnit(editorControl, copyRows, this, editorControl.conflictResolver);
                        if (addUndoUnit) {                            
                            editorControl.Editor.AddUndoUnit(undoUnit);
                        }

                        NotifyItemsStateChanged();
                        NotifyDataChanged();

                        VLOutputWindow.VisualLocalizerPane.WriteLine("Removed {0} rows", copyRows.Count);
                    }
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        private void EditorControl_RemoveRequested(REMOVEKIND flags) {
            RemoveStringsUndoUnit u;
            EditorControl_RemoveRequested(flags, true, out u);
        }

        /// <summary>
        /// Handles display of the context menu
        /// </summary>        
        private void ResXStringGrid_MouseDown(object sender, MouseEventArgs e) {
            try {
                if (e.Button == MouseButtons.Right && !IsEditing) {
                    HitTestInfo info = this.HitTest(e.X, e.Y);
                    if (info != null && info.ColumnIndex >= 0 && info.RowIndex >= 0 && info.RowIndex != Rows.Count - 1) {
                        if (SelectedRows.Count == 0) { // set current row as selected
                            Rows[info.RowIndex].Selected = true;
                            CurrentCell = Rows[info.RowIndex].Cells[info.ColumnIndex];
                        } else {
                            if (!Rows[info.RowIndex].Selected) { // if unselected row was clicked
                                ClearSelection();
                                Rows[info.RowIndex].Selected = true; // set it as the only selected one
                                CurrentCell = Rows[info.RowIndex].Cells[info.ColumnIndex];
                            }
                        }
                        
                        this.ContextMenu.Show(this, e.Location);
                    }
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }        

        /// <summary>
        /// Updates context menu item's state (disabled/enabled)
        /// </summary>
        private void UpdateContextItemsEnabled() {
            cutContextMenuItem.Enabled = this.CanCutOrCopy == COMMAND_STATUS.ENABLED;
            copyContextMenuItem.Enabled = this.CanCutOrCopy == COMMAND_STATUS.ENABLED;
            deleteContextMenuItem.Enabled = SelectedRows.Count >= 1 && !ReadOnly && !IsEditing; 
            editContextMenuItem.Enabled = SelectedRows.Count == 1 && !CurrentCell.ReadOnly && !ReadOnly && !Columns[CurrentCellAddress.X].ReadOnly;
            inlineContextMenu.Enabled = SelectedRows.Count >= 1 && !ReadOnly && !IsEditing && AreReferencesKnownOnSelected;            
            pasteContextMenuItem.Enabled = this.CanPaste == COMMAND_STATUS.ENABLED;
            translateMenu.Enabled = SelectedRows.Count >= 1 && !ReadOnly && !IsEditing;
            showResultItemsMenuItem.Enabled = SelectedRows.Count >= 1 && !IsEditing && AreReferencesKnownOnSelected;   
        }

        /// <summary>
        /// Called before displaying the context menu
        /// </summary>        
        private void ContextMenu_Popup(object sender, EventArgs e) {
            try {
                UpdateContextItemsEnabled();

                foreach (MenuItem menuItem in translateMenu.MenuItems) { // for each translation provider
                    menuItem.MenuItems.Clear(); // clear current language pair menu items
                    TRANSLATE_PROVIDER provider = (TRANSLATE_PROVIDER)menuItem.Tag;

                    // if the provider is Bing, AppId is required
                    bool enabled = true;
                    if (provider == TRANSLATE_PROVIDER.BING) {
                        enabled = !string.IsNullOrEmpty(SettingsObject.Instance.BingAppId);
                    }

                    menuItem.Enabled = enabled;

                    // add current language pairs from settings
                    foreach (var pair in SettingsObject.Instance.LanguagePairs) {
                        MenuItem newItem = new MenuItem(pair.ToString());
                        newItem.Tag = pair;
                        newItem.Click += new EventHandler((o, args) => {
                            SettingsObject.LanguagePair sentPair = (o as MenuItem).Tag as SettingsObject.LanguagePair;
                            EditorControl_TranslateRequested(provider, sentPair.FromLanguage, sentPair.ToLanguage);
                        });
                        newItem.Enabled = enabled;
                        menuItem.MenuItems.Add(newItem);
                    }

                    // add option to add a new language pair
                    MenuItem addItem = new MenuItem("New language pair...", new EventHandler((o, args) => {
                        EditorControl_TranslateRequested(provider);
                    }));
                    addItem.Enabled = enabled;
                    menuItem.MenuItems.Add(addItem);
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Called when "New language pair..." menu item was clicked
        /// </summary>        
        private void EditorControl_TranslateRequested(TRANSLATE_PROVIDER provider) {
            try {
                NewLanguagePairWindow win = new NewLanguagePairWindow(true); // select or create new language pair
                if (win.ShowDialog() == DialogResult.OK) {
                    if (win.AddToList && LanguagePairAdded != null) {
                        LanguagePairAdded(win.SourceLanguage, win.TargetLanguage); // add the language pair to the settings list
                    }
                    EditorControl_TranslateRequested(provider, win.SourceLanguage, win.TargetLanguage); // perform translation
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Translate selected rows
        /// </summary>
        /// <param name="provider">Translation provider to handle the process</param>
        /// <param name="from">Source language (can be null)</param>
        /// <param name="to">Target language</param>
        private void EditorControl_TranslateRequested(TRANSLATE_PROVIDER provider, string from, string to) {
            try {                
                List<AbstractTranslateInfoItem> data = new List<AbstractTranslateInfoItem>();
                AddToTranslationList(SelectedRows, data); // collect data to translate

                TranslationHandler.Translate(data, provider, from, to);

                foreach (AbstractTranslateInfoItem item in data) {
                    item.ApplyTranslation(); // modify the editor's data
                }
            } catch (Exception ex) {
                Dictionary<string, string> add = null;
                if (ex is CannotParseResponseException) {
                    CannotParseResponseException cpex = ex as CannotParseResponseException;
                    add = new Dictionary<string, string>();
                    add.Add("Full response:", cpex.FullResponse);
                }

                VLOutputWindow.VisualLocalizerPane.WriteException(ex, add);
                VisualLocalizer.Library.MessageBox.ShowException(ex, add);
            }
        }

        /// <summary>
        /// Extracts data from specified list for translation
        /// </summary>        
        public void AddToTranslationList(IEnumerable list, List<AbstractTranslateInfoItem> data) {
            if (list == null) throw new ArgumentNullException("list");
            if (data == null) throw new ArgumentNullException("data");

            foreach (ResXStringGridRow row in list) {
                if (!row.IsNewRow) {
                    if (!string.IsNullOrEmpty(row.Key)) {
                        StringGridTranslationInfoItem item = new StringGridTranslationInfoItem();
                        item.Row = row;
                        item.Value = row.Value;
                        item.ValueColumnName = ValueColumnName;
                        data.Add(item);
                    }
                }
            }            
        }

        /// <summary>
        /// Called from editor when Inline operation is requested
        /// </summary>
        /// <param name="kind">Bitmask of INLINEKIND parameters</param>
        private void EditorControl_InlineRequested(INLINEKIND kind) {
            bool readonlyExists = false;
            foreach (ResXStringGridRow row in SelectedRows) {
                if (row.IsNewRow) continue;

                readonlyExists = readonlyExists || row.CodeReferenceContainsReadonly;
                if (readonlyExists) break;
            }
            if (readonlyExists) {
                VisualLocalizer.Library.MessageBox.ShowError("This operation cannot be executed, because some of the references are located in readonly files.");
                return;
            }

            // show confirmation
            DialogResult result = VisualLocalizer.Library.MessageBox.Show("This operation is irreversible, cannot be undone globally, only using undo managers in open files. Do you want to proceed?",
                null, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND, OLEMSGICON.OLEMSGICON_WARNING);
            
            if (result == DialogResult.Yes) {
                try {
                    editorControl.ReferenceCounterThreadSuspended = true; // suspend reference lookuper thread

                    if ((kind & INLINEKIND.INLINE) == INLINEKIND.INLINE) {
                        editorControl.UpdateReferencesCount((IEnumerable)SelectedRows); // update references for specified rows manually

                        List<CodeReferenceResultItem> totalList = new List<CodeReferenceResultItem>();

                        foreach (ResXStringGridRow row in SelectedRows) { 
                            if (!row.IsNewRow) {
                                totalList.AddRange(row.CodeReferences);
                            }
                        }
                        BatchInliner inliner = new BatchInliner();
                        
                        // run inliner
                        int errors = 0;
                        inliner.Inline(totalList, false, ref errors);
                        VLOutputWindow.VisualLocalizerPane.WriteLine("Inlining of selected rows finished - found {0} references, {1} finished successfuly", totalList.Count, totalList.Count - errors);
                    }
                    
                    if ((kind & INLINEKIND.REMOVE) == INLINEKIND.REMOVE) {
                        // remove the rows if requested
                        RemoveStringsUndoUnit removeUnit = null;
                        EditorControl_RemoveRequested(REMOVEKIND.REMOVE, false, out removeUnit);
                    }

                    StringInlinedUndoItem undoItem = new StringInlinedUndoItem(SelectedRows.Count);
                    editorControl.Editor.AddUndoUnit(undoItem);
                } catch (Exception ex) {
                    VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                    VisualLocalizer.Library.MessageBox.ShowException(ex);
                } finally {
                    editorControl.ReferenceCounterThreadSuspended = false;
                }
            }
        }

        /// <summary>
        /// Handles column width change
        /// </summary>        
        private void ResXStringGrid_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e) {
            if (ignoreColumnWidthChange) return;
            if (e.Column.Name == ReferencesColumnName) return;

            ResizeColumnsFavore(Columns[e.Column.Index + 1].Name);
        }

        private void ResXStringGrid_Resize(object sender, EventArgs e) {
            ResizeColumnsFavore(ValueColumnName);
        }

        private bool ignoreColumnWidthChange;

        /// <summary>
        /// Resizes the columns to fit in the given space, leaving the extra space to the specified column
        /// </summary>        
        private void ResizeColumnsFavore(string columnName) {
            int restWidth = 0;
            foreach (DataGridViewColumn col in Columns)
                if (col.Name != columnName) restWidth += col.Width;

            ignoreColumnWidthChange = true;
            Columns[columnName].Width = this.ClientSize.Width - restWidth - this.RowHeadersWidth;
            ignoreColumnWidthChange = false;
        }

        #endregion

    }

   
}
