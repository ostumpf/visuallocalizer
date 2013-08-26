using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VisualLocalizer.Library;
using System.Resources;
using EnvDTE;
using VisualLocalizer.Components;
using System.IO;
using System.Collections.Specialized;
using System.Drawing;
using VisualLocalizer.Editor.UndoUnits;
using VisualLocalizer.Gui;
using System.ComponentModel.Design;
using System.Drawing.Imaging;
using VisualLocalizer.Commands;
using VisualLocalizer.Extensions;
using System.Collections;
using VisualLocalizer.Components.Code;
using VisualLocalizer.Library.Gui;
using VisualLocalizer.Library.Components;
using VisualLocalizer.Library.Extensions;
using VisualLocalizer.Commands.Inline;

namespace VisualLocalizer.Editor {

    /// <summary>
    /// Base class for controls displaying image, icon, sound and file resources in editor
    /// </summary>
    internal abstract class AbstractListView : ListView, IDataTabItem {
        /// <summary>
        /// Issued when data changed in GUI and the document should be marked dirty
        /// </summary>
        public event EventHandler DataChanged;

        /// <summary>
        /// Issued when selected items collection changed and certain GUI elements should be enabled/disabled
        /// </summary>
        public event EventHandler ItemsStateChanged;
       
        /// <summary>
        /// Parent editor control
        /// </summary>
        protected ResXEditorControl editorControl;        

        /// <summary>
        /// Key names conflict resolver common for the editor instance
        /// </summary>
        protected KeyValueIdentifierConflictResolver conflictResolver;

        protected ListViewKeyItem CurrentlyEditedItem;
        protected ImageMenuItem renameContextMenuItem, editCommentContextMenuItem, openContextMenuItem, cutContextMenuItem,
            copyContextMenuItem, pasteContextMenuItem, deleteContextMenu, deleteContextMenuItem, deleteExcludeContextMenuItem,
            deleteRemoveContextMenuItem, makeEmbeddedMenuItem, makeExternalMenuItem, showResultItemsMenuItem;
        
        /// <summary>
        /// Used during the "make resource linked" process
        /// </summary>
        private Dictionary<string, ListViewKeyItem> externalizedResourcesMap = new Dictionary<string, ListViewKeyItem>();

        /// <summary>
        /// True if existing items should be searched first and referenced on Add event; used in MakeResourcesExternal
        /// </summary>
        protected bool referenceExistingOnAdd = false;

        /// <summary>
        /// Currently sorted column
        /// </summary>
        protected int sortColumn = -1;

        /// <summary>
        /// Creates new instance
        /// </summary>        
        public AbstractListView(ResXEditorControl editorControl) {
            if (editorControl == null) throw new ArgumentNullException("editorControl");

            this.conflictResolver = editorControl.conflictResolver;
            this.editorControl = editorControl;
            this.Dock = DockStyle.Fill;
            this.MultiSelect = true;
            this.View = View.LargeIcon;
            this.FullRowSelect = true;
            this.GridLines = true;
            this.HeaderStyle = ColumnHeaderStyle.Clickable;
            this.HideSelection = true;
            this.LabelEdit = true;
            this.ShowItemToolTips = true;            
            this.TileSize = new System.Drawing.Size(70, 70);
            this.AfterLabelEdit += new LabelEditEventHandler(AbstractListView_AfterLabelEdit);
            this.BeforeLabelEdit += new LabelEditEventHandler(AbstractListView_BeforeLabelEdit);
            this.SelectedIndexChanged += new EventHandler((o, e) => { NotifyItemsStateChanged(); });
            this.MouseDoubleClick += new MouseEventHandler(AbstractListView_MouseDoubleClick);
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(AbstractListView_DragEnter);
            this.DragDrop += new DragEventHandler(AbstractListView_DragDrop);
            editorControl.ViewKindChanged += new Action<View>(ViewKindChanged);
            editorControl.RemoveRequested += new Action<REMOVEKIND>(EditorControl_RemoveRequested);

            // create context menu
            renameContextMenuItem = new ImageMenuItem("Rename");
            renameContextMenuItem.Shortcut = Shortcut.F2;
            renameContextMenuItem.Click += new EventHandler((o, e) => { SelectedItems[0].BeginEdit(); });

            editCommentContextMenuItem = new ImageMenuItem("Edit comment");
            editCommentContextMenuItem.Shortcut = Shortcut.F3;
            editCommentContextMenuItem.Click += new EventHandler(EditCommentContextMenuItem_Click);

            openContextMenuItem = new ImageMenuItem("Open");
            openContextMenuItem.Image = Editor.open;
            openContextMenuItem.Shortcut = Shortcut.F11;
            openContextMenuItem.Click += new EventHandler(OpenContextMenuItem_Click);
            
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

            deleteContextMenu = new ImageMenuItem("Remove");
            deleteContextMenu.Image = Editor.remove;

            deleteContextMenuItem = new ImageMenuItem("Remove resource(s)");
            deleteContextMenuItem.Shortcut = Shortcut.Del; 
            deleteContextMenuItem.Click += new EventHandler((o, e) => { EditorControl_RemoveRequested(REMOVEKIND.REMOVE); });

            deleteExcludeContextMenuItem = new ImageMenuItem("Remove & exclude resource(s)");
            deleteExcludeContextMenuItem.Click += new EventHandler((o, e) => { EditorControl_RemoveRequested(REMOVEKIND.REMOVE | REMOVEKIND.EXCLUDE); });
            deleteExcludeContextMenuItem.Shortcut = Shortcut.CtrlE;

            deleteRemoveContextMenuItem = new ImageMenuItem("Remove & delete resource(s)");
            deleteRemoveContextMenuItem.Click+=new EventHandler((o, e) => { EditorControl_RemoveRequested(REMOVEKIND.REMOVE | REMOVEKIND.DELETE_FILE); });
            deleteRemoveContextMenuItem.Shortcut = Shortcut.ShiftDel;

            makeEmbeddedMenuItem = new ImageMenuItem("Make this resource(s) embedded");
            makeEmbeddedMenuItem.Image = Editor.embedded;
            makeEmbeddedMenuItem.Click += new EventHandler(MakeEmbeddedMenuItem_Click);

            makeExternalMenuItem = new ImageMenuItem("Make this resource(s) linked");
            makeExternalMenuItem.Image = Editor.external;
            makeExternalMenuItem.Click += new EventHandler(MakeExternalMenuItem_Click);

            showResultItemsMenuItem = new ImageMenuItem("Show references");
            showResultItemsMenuItem.Image = Editor.search;
            showResultItemsMenuItem.Shortcut = Shortcut.CtrlF;
            showResultItemsMenuItem.Click += new EventHandler(showResultItemsMenuItem_Click);

            deleteContextMenu.MenuItems.Add(deleteContextMenuItem);
            deleteContextMenu.MenuItems.Add(deleteExcludeContextMenuItem);
            deleteContextMenu.MenuItems.Add(deleteRemoveContextMenuItem);

            ContextMenu contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add(openContextMenuItem);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(cutContextMenuItem);
            contextMenu.MenuItems.Add(copyContextMenuItem);
            contextMenu.MenuItems.Add(pasteContextMenuItem);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(renameContextMenuItem);
            contextMenu.MenuItems.Add(editCommentContextMenuItem);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(showResultItemsMenuItem);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(makeEmbeddedMenuItem);
            contextMenu.MenuItems.Add(makeExternalMenuItem);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(deleteContextMenu);
            contextMenu.Popup += new EventHandler(ContextMenu_Popup);
            this.ContextMenu = contextMenu;

            InitializeColumns();

            this.ColumnClick += new ColumnClickEventHandler(AbstractListView_ColumnClick);
        }
        

        /// <summary>
        /// Parent editor control
        /// </summary>
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
            Focus();

            Dictionary<string, ResXDataNode> data = new Dictionary<string, ResXDataNode>(Items.Count);
            foreach (ListViewKeyItem item in Items) {    
                // if item has at least one error message and messages should be thrown --> report error
                if (item.ErrorMessages.Count > 0 && throwExceptions) throw new Exception(item.ErrorMessages.First());
                if (item.Key == null && throwExceptions) throw new Exception("Key cannot be null");

                ResXDataNode node;
                if (item.DataNode.FileRef == null) { // resource value is embedded
                    try {
                        node = new ResXDataNode(item.Key, item.DataNode.GetValue((ITypeResolutionService)null));
                    } catch (NullReferenceException ex) {
                        if (throwExceptions) throw new Exception("Value of '" + item.Key + "' is empty.", ex);
                        node = null;
                    }
                } else { // resource value is external
                    node = new ResXDataNode(item.Key, item.DataNode.FileRef);
                }
                if (node != null) {
                    node.Comment = item.SubItems[CommentColumnName].Text;
                    data.Add(item.Key.ToLower(), node);

                    if (!CanContainItem(node) && throwExceptions)
                        throw new Exception("Error saving '" + node.Name + "' - cannot preserve type."); 
                }
            }

            return data;
        }

        /// <summary>
        /// Begins batch adding items
        /// </summary>
        public void BeginAdd() {
            this.Items.Clear(); // remove existing items
            this.SuspendLayout();

            // create new images lists
            this.LargeImageList = new ImageList(); 
            this.SmallImageList = new ImageList();            
        }

        /// <summary>
        /// Ends batch adding items and refreshes the view
        /// </summary>
        public void EndAdd() {
            this.LargeImageList.ImageSize = new System.Drawing.Size(100, 100);
            this.ResumeLayout();
        }

        /// <summary>
        /// Adds given resource to the control
        /// </summary>      
        public virtual IKeyValueSource Add(string key, ResXDataNode value) {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

            if (referenceExistingOnAdd) {
                ListViewKeyItem existingItem = externalizedResourcesMap[key]; // get existing item
                
                value.Comment = existingItem.DataNode.Comment;
                existingItem.DataNode = value; // set new data node as its value
                existingItem.AfterEditKey = existingItem.BeforeEditKey;

                return existingItem;
            } 

            ListViewKeyItem item = new ListViewKeyItem(this);                    
            item.DataNode = value;                        

            ListViewItem.ListViewSubItem subKey = new ListViewItem.ListViewSubItem();
            subKey.Name = "Path";            
            item.SubItems.Add(subKey);

            ListViewItem.ListViewSubItem subComment = new ListViewItem.ListViewSubItem();
            subComment.Name = CommentColumnName;                    
            item.SubItems.Add(subComment);

            ListViewItem.ListViewSubItem subReferences = new ListViewItem.ListViewSubItem();
            subReferences.Name = "References";
            subReferences.Text = "?";
            item.SubItems.Add(subReferences);

            Items.Add(item);
            item.Text = key;            

            return item;
        }

        /// <summary>
        /// Returns true if given node's type matches the type of items this control holds
        /// </summary>
        public abstract bool CanContainItem(ResXDataNode node);

        /// <summary>
        /// Returns status for Cut and Copy commands, based on currently selected items
        /// </summary>
        public COMMAND_STATUS CanCutOrCopy {
            get {
                return HasSelectedItems && !IsEditing && !DataReadOnly ? COMMAND_STATUS.ENABLED : COMMAND_STATUS.DISABLED;
            }
        }

        /// <summary>
        /// Returns status for Paste command, based on currently selected items
        /// </summary>
        public COMMAND_STATUS CanPaste {
            get {
                try {
                    IDataObject iData = Clipboard.GetDataObject();
                    return AcceptsClipboardData(iData) ? COMMAND_STATUS.ENABLED : COMMAND_STATUS.DISABLED;
                } catch (Exception ex) {
                    VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                    VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
                }
                return COMMAND_STATUS.DISABLED;
            }
        }

        /// <summary>
        /// Performs Copy command
        /// </summary>   
        public bool Copy() {
            if (CanCutOrCopy != COMMAND_STATUS.ENABLED) return false;

            bool allEmbedded = true;
            bool allExternal = true;

            foreach (ListViewKeyItem item in this.SelectedItems) {
                ResXFileRef fileRef = item.DataNode.FileRef;
                allEmbedded = allEmbedded && fileRef == null;
                allExternal = allExternal && fileRef != null;
            }
            if (!allEmbedded && !allExternal) { // cannot set mixed content to clipboard
                throw new Exception("Cannot copy both embedded and external resources at the same time.");
            } else {
                if (allExternal) {
                    // add file paths to clipboard
                    StringCollection list = new StringCollection();
                    foreach (ListViewKeyItem item in this.SelectedItems) {                        
                        string path = item.DataNode.FileRef.FileName;
                        list.Add(path);
                    }

                    Clipboard.SetFileDropList(list);
                } else { // add objects themselves to clipboard
                    List<object> list = new List<object>();
                    foreach (ListViewKeyItem item in this.SelectedItems) {                        
                        item.DataNode = new ResXDataNode(string.IsNullOrEmpty(item.Text) ? item.DataNode.Name : item.Text, item.DataNode.GetValue((ITypeResolutionService)null));
                        item.DataNode.Comment = item.SubItems[CommentColumnName].Text;
                        list.Add(item.DataNode);
                    }
                    Clipboard.SetDataObject(list, false);
                }
            }
            return true;
        }

        /// <summary>
        /// Performs Cut command
        /// </summary>
        public bool Cut() {
            bool ok = Copy(); // first copy selected items
            if (!ok) return false;

            EditorControl_RemoveRequested(REMOVEKIND.REMOVE); // then remove them
            
            return true;
        }

        /// <summary>
        /// Returns true if this list is not empty
        /// </summary>
        public bool HasItems {
            get {
                return Items.Count > 0;
            }
        }

        /// <summary>
        /// Returns true if there are selected items in this list
        /// </summary>
        public bool HasSelectedItems {
            get {
                return SelectedItems.Count > 0;
            }
        }

        /// <summary>
        /// Performs Select All command
        /// </summary>
        public bool SelectAllItems() {
            Focus();
            foreach (ListViewKeyItem item in Items)
                item.Selected = true;
            return true;
        }

        /// <summary>
        /// Returns true if a resource is being edited
        /// </summary>
        public bool IsEditing {
            get {
                return CurrentlyEditedItem != null;
            }
        }

        private bool _ReadOnly;
        /// <summary>
        /// Gets/sets whether this control is readonly
        /// </summary>
        public bool DataReadOnly {
            get {
                return _ReadOnly; 
            }
            set {
                _ReadOnly = value;
                LabelEdit = !value;
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

        #region protected members - virtual

        /// <summary>
        /// Create the GUI
        /// </summary>
        protected virtual void InitializeColumns() {
            ColumnHeader keyHeader = new ColumnHeader();
            keyHeader.Text = "Resource Key";
            keyHeader.Width = 200;
            keyHeader.Name = "Key";
            this.Columns.Add(keyHeader);

            ColumnHeader fileHeader = new ColumnHeader();
            fileHeader.Text = "Corresponding File";
            fileHeader.Width = 220;
            fileHeader.Name = "Path";
            this.Columns.Add(fileHeader);

            ColumnHeader commentHeader = new ColumnHeader();
            commentHeader.Text = "Comment";
            commentHeader.Width = 200;
            commentHeader.Name = CommentColumnName;
            this.Columns.Add(commentHeader);

            ColumnHeader referencesHeader = new ColumnHeader();
            referencesHeader.Text = "References";
            referencesHeader.Width = 80;
            referencesHeader.Name = "References";
            referencesHeader.TextAlign = HorizontalAlignment.Center;
            this.Columns.Add(referencesHeader);
        }

        /// <summary>
        /// Updates context menu items enabled (to prevent multiple invocations of commands like CTRL+C)
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e) {            
            UpdateContextMenuItemsEnabled();
            base.OnPreviewKeyDown(e);
        }

        /// <summary>
        /// Saves given node's content into random file in specified directory and returns the file path
        /// </summary>        
        protected abstract string SaveIntoTmpFile(ResXDataNode node, string filename, string directory);

        #endregion

        #region public members

        /// <summary>
        /// Name of the "comment" column
        /// </summary>
        public string CommentColumnName {
            get { return "Comment"; }
        }

        /// <summary>
        /// Returns existing item with specified name
        /// </summary>        
        public ListViewKeyItem ItemFromName(string name) {
            if (name == null) throw new ArgumentNullException("name");

            ListViewKeyItem existingItem = null;
            foreach (ListViewKeyItem i in Items)
                if (i.Text == name) {
                    existingItem = i;
                    break;
                }
            return existingItem;
        }

        public bool ExistsReference(string path) {
            bool exists = false;
            foreach (ListViewKeyItem listViewItem in Items)
                if (string.Compare(Path.GetFullPath(listViewItem.ImageKey), Path.GetFullPath(path), true) == 0) {
                    exists = true;
                    break;
                }
            return exists;
        }

        /// <summary>
        /// Validates given item and sets according error messages
        /// </summary>        
        public void Validate(ListViewKeyItem item) {
            if (item == null) throw new ArgumentNullException("item");
            
            conflictResolver.TryAdd(item.BeforeEditKey, item.AfterEditKey, item, editorControl.Editor.ProjectItem, null);            
            item.UpdateErrorSetDisplay(); // update error messages display
        }

        /// <summary>
        /// Reloads displayed data from underlaying ResX node
        /// </summary>     
        public virtual void UpdateDataOf(ListViewKeyItem item, bool reloadImages) {
            if (item == null) throw new ArgumentNullException("item");

            item.SubItems["Comment"].Text = item.DataNode.Comment;            
            item.Name = Path.GetRandomFileName();
            item.ImageKey = item.DataNode.FileRef == null ? item.Name : item.DataNode.FileRef.FileName;
            item.AfterEditKey = item.Text;            

            if (item.DataNode.FileRef != null) { // resource is external
                item.SubItems["Path"].Text = Uri.UnescapeDataString(editorControl.Editor.FileUri.MakeRelativeUri(new Uri(item.DataNode.FileRef.FileName)).ToString());
            } else {
                item.SubItems["Path"].Text = "(embedded)";
            }
        }       

        /// <summary>
        /// Called after item's key changed - creates the undo unit and performs pseudo-refactoring in code
        /// </summary>        
        public void ListItemKeyRenamed(ListViewKeyItem item) {
            // create the undo unit
            ListViewRenameKeyUndoUnit unit = new ListViewRenameKeyUndoUnit(editorControl, this, item, item.BeforeEditKey, item.AfterEditKey);
            editorControl.Editor.AddUndoUnit(unit);
            
            if (VisualLocalizerPackage.Instance.DTE.Solution.ContainsProjectItem(editorControl.Editor.ProjectItem.InternalProjectItem)) {
                // create ResX project item
                ResXProjectItem resxItem = editorControl.Editor.ProjectItem;
                resxItem.ResolveNamespaceClass(resxItem.InternalProjectItem.ContainingProject.GetResXItemsAround(false, true));

                if (item.ConflictItems.Count == 0 && resxItem != null && !resxItem.IsCultureSpecific() && !string.IsNullOrEmpty(item.AfterEditKey)) {
                    int errors = 0;
                    int count = item.CodeReferences.Count;
                    item.CodeReferences.ForEach((i) => { i.KeyAfterRename = item.AfterEditKey.CreateIdentifier(resxItem.DesignerLanguage); ;  });

                    // run replacer
                    try {
                        editorControl.ReferenceCounterThreadSuspended = true;
                        BatchReferenceReplacer replacer = new BatchReferenceReplacer();
                        replacer.Inline(item.CodeReferences, true, ref errors);
                    } finally {
                        editorControl.ReferenceCounterThreadSuspended = false;
                    }

                    VLOutputWindow.VisualLocalizerPane.WriteLine("Renamed {0} key references in code, {1} errors occured", count, errors);
                }
            }            
        }

        /// <summary>
        /// Makes given list of external resources embedded
        /// </summary>
        /// <param name="list">List of items to modify</param>
        /// <param name="delete">True of original files should be deleted</param>
        /// <param name="addUndoUnit">True if undo unit should be added for the operation</param>
        public void MakeResourcesEmbedded(IEnumerable list, bool delete, bool addUndoUnit) {
            ListViewMakeEmbeddedUndoUnit undoUnit = null;
            try {
                if (list == null) throw new ArgumentNullException("list");
                CheckListItemsForErrors(list);

                List<ListViewKeyItem> undoList = new List<ListViewKeyItem>();
                undoUnit = new ListViewMakeEmbeddedUndoUnit(this, undoList, delete);
                    
                foreach (ListViewKeyItem item in list) {
                    // get value of the node
                    object value = item.DataNode.GetValue((ITypeResolutionService)null);

                    // get current node info
                    string path = item.DataNode.FileRef.FileName;
                    string name = item.Key;
                    string cmt = item.SubItems[CommentColumnName].Text;

                    // create new, embedded node
                    item.DataNode = new ResXDataNode(name, value);
                    item.DataNode.Comment = cmt;
                    item.SubItems["Path"].Text = "(embedded)";

                    VLOutputWindow.VisualLocalizerPane.WriteLine("Embedded resource \"{0}\" into ResX file", item.Key);

                    if (delete) {
                        ProjectItem projectItem = VisualLocalizerPackage.Instance.DTE.Solution.FindProjectItem(path);
                        if (projectItem != null) { // remove the item from project
                            item.NeighborItems = projectItem.Collection;
                            projectItem.Delete();
                        }
                        if (File.Exists(path)) File.Delete(path); // delete the file

                        VLOutputWindow.VisualLocalizerPane.WriteLine("Deleted referenced file \"{0}\"", Path.GetFileName(path));
                    }
                    undoList.Add(item);
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            } finally {
                if (addUndoUnit && undoUnit != null) {
                    editorControl.Editor.AddUndoUnit(undoUnit);
                }
                NotifyDataChanged();
            }
        }

        /// <summary>
        /// Makes given list of embedded resources external, i.e. moves the content to an external file
        /// </summary>
        /// <param name="list">List of items to modify</param>
        /// <param name="referenceExisting">True if existing resources in this list should be referenced</param>
        /// <param name="addUndoUnit">True if undo unit should be added for the operation</param>
        public void MakeResourcesExternal(IEnumerable list, bool referenceExisting, bool addUndoUnit) {
            ListViewMakeExternalUndoUnit undoUnit = null;
            try {
                if (!VisualLocalizerPackage.Instance.DTE.Solution.ContainsProjectItem(editorControl.Editor.ProjectItem.InternalProjectItem))
                    throw new Exception("This operation is not supported on documents that are not a part of a solution.");
                if (list == null) throw new ArgumentNullException("list");
                CheckListItemsForErrors(list);

                List<ListViewKeyItem> undoList = new List<ListViewKeyItem>();
                undoUnit = new ListViewMakeExternalUndoUnit(this, undoList, referenceExisting);
                
                externalizedResourcesMap.Clear();
                List<string> paths = new List<string>();
                string dir = Path.Combine(Path.GetTempPath(), "VisualLocalizer");
                Directory.CreateDirectory(dir); // create temporary directory

                foreach (ListViewKeyItem item in list) {
                    externalizedResourcesMap.Add(item.Key, item);

                    string path = SaveIntoTmpFile(item.DataNode, item.Key, dir); // move the content to temporary file
                    paths.Add(path);
                    
                    undoList.Add(item);
                    VLOutputWindow.VisualLocalizerPane.WriteLine("Moved resource \"{0}\" to external file", item.Key);
                }

                referenceExistingOnAdd = true;
                editorControl.AddExistingFiles(paths, FILES_ORIGIN.MAKE_EXTERNAL); // add the temporary files to the list, matching them to existing items
                referenceExistingOnAdd = false;

                editorControl.Editor.UndoManager.RemoveTopFromUndoStack(1); // previous operation (adding existing files) created an undo unit - remove it               
                
                Directory.Delete(dir, true); // delete temporary directory
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            } finally {                
                referenceExistingOnAdd = false;
                if (addUndoUnit && undoUnit != null) {
                    editorControl.Editor.AddUndoUnit(undoUnit);
                }
                NotifyDataChanged();
            }
        }
       
        #endregion

        #region protected non-virtual members

        protected void AbstractListView_ColumnClick(object sender, ColumnClickEventArgs e) {            
            if (e.Column != sortColumn) {                
                sortColumn = e.Column;                
                Sorting = SortOrder.Ascending;
            } else {
                if (Sorting == SortOrder.Ascending) {
                    Sorting = SortOrder.Descending;
                } else {
                    Sorting = SortOrder.Ascending;
                }
            }

            Sort();            
            this.ListViewItemSorter = new ListViewItemComparer(e.Column, Sorting);
            this.SetSortIcon(e.Column, Sorting);
        }

        /// <summary>
        /// Called on drop
        /// </summary>        
        private void AbstractListView_DragDrop(object sender, DragEventArgs e) {
            editorControl.ExecutePaste(e.Data);
        }

        /// <summary>
        /// Called on drag enter - set cursor based on content data
        /// </summary>        
        private void AbstractListView_DragEnter(object sender, DragEventArgs e) {
            try {
                e.Effect = AcceptsClipboardData(e.Data) ? DragDropEffects.All : DragDropEffects.None;
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }         

        /// <summary>
        /// Returns true if given clipboard data object contains data that can be added to the list
        /// </summary>
        private bool AcceptsClipboardData(IDataObject iData) {
            if (iData == null) throw new ArgumentNullException("iData");

            bool containsList = iData.GetDataPresent(typeof(List<object>)); // data copied from this list
            bool containsFiles = iData.GetDataPresent(StringConstants.FILE_LIST); // file list from Windows Explorer or similar
            bool containsSolExpList = iData.GetDataPresent(StringConstants.SOLUTION_EXPLORER_FILE_LIST); // file list from Solution Explorer

            return (containsFiles || containsSolExpList || containsList) && !IsEditing && !DataReadOnly;
        }

        /// <summary>
        /// If item was double-clicked, try opening it for edit
        /// </summary>        
        private void AbstractListView_MouseDoubleClick(object sender, MouseEventArgs e) {
            ListViewKeyItem item = this.GetItemAt(e.X, e.Y) as ListViewKeyItem;
            if (item != null) {
                OpenForEdit(item);
            }
        }

        /// <summary>
        /// Makes selected embedded resources linked
        /// </summary>        
        private void MakeExternalMenuItem_Click(object sender, EventArgs e) {
            MakeResourcesExternal((IEnumerable)SelectedItems, false, true);
        }
        
        /// <summary>
        /// Makes selected linked resources embedded
        /// </summary>        
        private void MakeEmbeddedMenuItem_Click(object sender, EventArgs e) {            
            var result = System.Windows.Forms.MessageBox.Show("Do you also want to delete all referenced files?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            bool delete = result == DialogResult.Yes;

            MakeResourcesEmbedded((IEnumerable)SelectedItems, delete, true);
        }        

        /// <summary>
        /// Edits comment of the resource
        /// </summary>        
        protected void EditCommentContextMenuItem_Click(object sender, EventArgs e) {
            try {
                if (SelectedItems.Count == 0) throw new IndexOutOfRangeException("No selected item.");

                ListViewKeyItem item = SelectedItems[0] as ListViewKeyItem;
                CommentWindow win = new CommentWindow(item.SubItems[CommentColumnName].Text); // display dialog

                if (win.ShowDialog() == DialogResult.OK) {
                    // create undo unit
                    ListViewChangeCommentUndoUnit unit = new ListViewChangeCommentUndoUnit(item, item.DataNode.Comment, win.Comment, item.Key);
                    editorControl.Editor.AddUndoUnit(unit);

                    // change the comment
                    item.DataNode.Comment = win.Comment;
                    item.SubItems["Comment"].Text = win.Comment;

                    NotifyDataChanged(); // set document dirty
                    VLOutputWindow.VisualLocalizerPane.WriteLine("Edited comment of \"{0}\"", item.Key);
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Opens selected item by standard VS editor
        /// </summary>        
        protected void OpenContextMenuItem_Click(object sender, EventArgs e) {
            if (SelectedItems.Count == 0) {
                OpenForEdit(null); // throws exception
            } else {
                ListViewKeyItem item = SelectedItems[0] as ListViewKeyItem;
                OpenForEdit(item);
            }
        }

        /// <summary>
        /// Attempts to open given resource item in an appropriate editor (not necessarily in VS)
        /// </summary>        
        private void OpenForEdit(ListViewKeyItem item) {
            try {
                if (item == null) throw new ArgumentNullException("item");                
                if (item.DataNode.FileRef == null) throw new InvalidOperationException("Cannot open item - editing of embedded resources is not supported.");
                if (!item.FileRefOk) throw new InvalidOperationException("Cannot open item - referenced file does not exist.");

                Window win = VisualLocalizerPackage.Instance.DTE.OpenFile(null, item.DataNode.FileRef.FileName);
                if (win != null) win.Activate();

                VLOutputWindow.VisualLocalizerPane.WriteLine("Opened resource file \"{0}\"", item.DataNode.FileRef.FileName);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);         
            }
        }

        /// <summary>
        /// Displays context menu
        /// </summary>        
        protected void ContextMenu_Popup(object sender, EventArgs e) {
            UpdateContextMenuItemsEnabled();
        }

        /// <summary>
        /// Updates state of context menu items (enabled/disabled)
        /// </summary>
        protected void UpdateContextMenuItemsEnabled() {
            renameContextMenuItem.Enabled = SelectedItems.Count == 1 && !DataReadOnly && !IsEditing;
            editCommentContextMenuItem.Enabled = SelectedItems.Count == 1 && !DataReadOnly && !IsEditing;
            openContextMenuItem.Enabled = SelectedItems.Count == 1 && !IsEditing;
            deleteContextMenu.Enabled = SelectedItems.Count >= 1 && !DataReadOnly && !IsEditing;

            bool allSelectedResourcesExternal = true;
            bool allSelectedResourcesEmbedded = true;
            foreach (ListViewKeyItem item in SelectedItems) {
                allSelectedResourcesExternal = allSelectedResourcesExternal && item.DataNode.FileRef != null;
                allSelectedResourcesEmbedded = allSelectedResourcesEmbedded && item.DataNode.FileRef == null;
            }
            deleteExcludeContextMenuItem.Enabled = allSelectedResourcesExternal;
            deleteRemoveContextMenuItem.Enabled = allSelectedResourcesExternal;

            makeEmbeddedMenuItem.Enabled = SelectedItems.Count >= 1 && !DataReadOnly && !IsEditing && allSelectedResourcesExternal;
            makeExternalMenuItem.Enabled = SelectedItems.Count >= 1 && !DataReadOnly && !IsEditing && allSelectedResourcesEmbedded;

            cutContextMenuItem.Enabled = this.CanCutOrCopy == COMMAND_STATUS.ENABLED;
            copyContextMenuItem.Enabled = this.CanCutOrCopy == COMMAND_STATUS.ENABLED;
            pasteContextMenuItem.Enabled = this.CanPaste == COMMAND_STATUS.ENABLED;

            showResultItemsMenuItem.Enabled = SelectedItems.Count >= 1 && !IsEditing && AreReferencesKnownOnSelected;
        }

        /// <summary>
        /// Collects references from every selected item and displays them in the tool window
        /// </summary>        
        protected void showResultItemsMenuItem_Click(object sender, EventArgs e) {
            try {
                List<CodeReferenceResultItem> selected = new List<CodeReferenceResultItem>();
                foreach (ListViewKeyItem item in SelectedItems) {
                    if (item.ErrorMessages.Count == 0) selected.AddRange(item.CodeReferences);
                }
                editorControl.ShowReferences(selected);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }                       

        /// <summary>
        /// Returns human readeble representation of file size
        /// </summary>        
        protected string GetFileSize(long bytes) {
            if (bytes < 1024) {
                return string.Format("{0} B", bytes);
            } else if (bytes < 1024 * 1024) {
                return string.Format("{0} kB", bytes / 1024);
            } else {
                return string.Format("{0} MB", bytes / (1024 * 1024));
            }
        }

        /// <summary>
        /// Handles before-edit event; saves current label as key and suspends reference lookuper thread
        /// </summary> 
        protected void AbstractListView_BeforeLabelEdit(object sender, LabelEditEventArgs e) {
            try {
                ListViewKeyItem item = (ListViewKeyItem)Items[e.Item];
                
                if (item.CodeReferenceContainsReadonly) {
                    e.CancelEdit = true;
                    VisualLocalizer.Library.Components.MessageBox.ShowError("This operation cannot be executed, because some of the references are located in readonly files.");
                    return;
                }                
                
                item.BeforeEditKey = item.Key;
                CurrentlyEditedItem = item;

                editorControl.ReferenceCounterThreadSuspended = true;
                editorControl.UpdateReferencesCount(item);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            } finally {
                NotifyItemsStateChanged();
            }
        }

        /// <summary>
        /// Handles after-edit event; adds undo unit, revalidates the item and resumes reference lookuper thread
        /// </summary>       
        protected void AbstractListView_AfterLabelEdit(object sender, LabelEditEventArgs e) {
            try {
                ListViewKeyItem item = (ListViewKeyItem)Items[e.Item];

                if (e.Label != null && string.Compare(e.Label, item.BeforeEditKey) != 0) { // value changed
                    item.AfterEditKey = e.Label;
                                        
                    Validate(item); // validation
                    ListItemKeyRenamed(item); // adds undo unit

                    if (item.ErrorMessages.Count > 0) {
                        item.Status = KEY_STATUS.ERROR;
                    } else {
                        item.Status = KEY_STATUS.OK;
                        item.LastValidKey = item.AfterEditKey;
                    }

                    NotifyDataChanged(); // document dirty
                    VLOutputWindow.VisualLocalizerPane.WriteLine("Renamed from \"{0}\" to \"{1}\"", item.BeforeEditKey, item.AfterEditKey);
                }
                CurrentlyEditedItem = null;                
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            } finally {
                editorControl.ReferenceCounterThreadSuspended = false;
                NotifyItemsStateChanged();
            }
        }

        /// <summary>
        /// Removes selected items from the list. The "Exclude" flag is not valid in ASP .NET website environment.
        /// </summary>
        /// <param name="remove">Bitmask of REMOVEKIND parameters</param>
        protected void EditorControl_RemoveRequested(REMOVEKIND remove) {
            try {
                if (!this.Visible) return;
                if (this.SelectedItems.Count == 0) return;

                if ((remove & REMOVEKIND.DELETE_FILE) == REMOVEKIND.DELETE_FILE) {
                    DialogResult result = System.Windows.Forms.MessageBox.Show("You are about to delete files from disk. This operation cannot be undone, do you really want to proceed?", "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                    if (result == DialogResult.No) return;
                }

                // if items should be excluded from the project
                if ((remove & REMOVEKIND.EXCLUDE) == REMOVEKIND.EXCLUDE && VisualLocalizerPackage.Instance.DTE.Solution != null) {
                    foreach (ListViewKeyItem item in SelectedItems) {
                        string file = item.DataNode.FileRef.FileName;
                        ProjectItem projectItem = VisualLocalizerPackage.Instance.DTE.Solution.FindProjectItem(file);
                        if (projectItem != null) {
                            item.NeighborItems = projectItem.Collection;                                                        
                            projectItem.Remove();                            
                        }
                    }
                }

                // if item's referenced files should be deleted from disk
                if ((remove & REMOVEKIND.DELETE_FILE) == REMOVEKIND.DELETE_FILE && VisualLocalizerPackage.Instance.DTE.Solution != null) {
                    foreach (ListViewKeyItem item in SelectedItems) {
                        string file = item.DataNode.FileRef.FileName;
                        ProjectItem projectItem = VisualLocalizerPackage.Instance.DTE.Solution.FindProjectItem(file);
                        if (projectItem != null) {
                            item.NeighborItems = projectItem.Collection;
                            projectItem.Delete();
                        }
                        if (File.Exists(file)) File.Delete(file);
                    }
                }
                
                // if items should be removed from the list (always)                
                if ((remove & REMOVEKIND.REMOVE) == REMOVEKIND.REMOVE) {
                    List<ListViewKeyItem> removedItems = new List<ListViewKeyItem>();
                    foreach (ListViewKeyItem item in SelectedItems) {
                        conflictResolver.TryAdd(item.Key, null, item, editorControl.Editor.ProjectItem, null); // remove from conflict resolver
                        if (!string.IsNullOrEmpty(item.ImageKey) && LargeImageList.Images.ContainsKey(item.ImageKey)) {
                            LargeImageList.Images.RemoveByKey(item.ImageKey);
                            SmallImageList.Images.RemoveByKey(item.ImageKey);
                        }
                        item.IndexAtDeleteTime = item.Index;
                        item.RemoveKind = remove;                        

                        removedItems.Add(item);
                        Items.Remove(item);
                    }

                    if (removedItems.Count > 0) {
                        ItemsRemoved(removedItems); // add undo unit
                        NotifyItemsStateChanged();
                        NotifyDataChanged(); // set document dirty

                        VLOutputWindow.VisualLocalizerPane.WriteLine("Removed {0} resource files", removedItems.Count);
                    }
                }                
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Adds "items removed" undo unit
        /// </summary>        
        protected void ItemsRemoved(List<ListViewKeyItem> list) {
            ListViewRemoveItemsUndoUnit unit = new ListViewRemoveItemsUndoUnit(editorControl, list, editorControl.conflictResolver);
            editorControl.Editor.AddUndoUnit(unit);
        }        

        /// <summary>
        /// Updates the view kind value
        /// </summary>        
        protected void ViewKindChanged(View view) {
            this.View = view;
        }

        /// <summary>
        /// Checks given list of list view items for error messages and throws an exception if some is found
        /// </summary>        
        protected void CheckListItemsForErrors(IEnumerable list) {
            foreach (ListViewKeyItem item in list) {
                if (item.ErrorMessages.Count > 0) throw new InvalidOperationException("Cannot execute this operation, because some of the selected items have errors.");
            }
        }

        /// <summary>
        /// Returns true if all selected items' code references were succesfully looked up
        /// </summary>
        protected bool AreReferencesKnownOnSelected {
            get {
                bool ok = true;
                foreach (ListViewKeyItem item in SelectedItems) {
                    string s = item.SubItems["References"].Text;
                    if (string.IsNullOrEmpty(s)) { // reference count column must have a value
                        ok = false;
                        break;
                    }

                    int iv;
                    ok = ok && !string.IsNullOrEmpty(s) && int.TryParse(s, out iv); // the value must be a number
                }
                return ok;
            }
        }

        #endregion

        
    }
    
}

/// <summary>
/// Handles sorting in a ListView
/// </summary>
public class ListViewItemComparer : IComparer {
    private int col;
    private SortOrder order;
    public ListViewItemComparer() {
        col = 0;
        order = SortOrder.Ascending;
    }
    public ListViewItemComparer(int column, SortOrder order) {
        col = column;
        this.order = order;
    }
    /// <summary>
    /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
    /// </summary>
    /// <param name="x">The first object to compare.</param>
    /// <param name="y">The second object to compare.</param>
    /// <returns>
    /// Value Condition Less than zero <paramref name="x" /> is less than <paramref name="y" />. Zero <paramref name="x" /> equals <paramref name="y" />. Greater than zero <paramref name="x" /> is greater than <paramref name="y" />.
    /// </returns>
    public int Compare(object x, object y)  {
        int returnVal= -1;
        returnVal = String.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text);        
        if (order == SortOrder.Descending)            
            returnVal *= -1;
        return returnVal;
    }
}