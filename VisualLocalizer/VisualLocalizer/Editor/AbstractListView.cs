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

namespace VisualLocalizer.Editor {
    internal abstract class AbstractListView : ListView, IDataTabItem {
        public event EventHandler DataChanged;
        public event EventHandler ItemsStateChanged;
       
        protected ResXEditorControl editorControl;        
        protected KeyValueIdentifierConflictResolver conflictResolver;
        protected ListViewKeyItem CurrentlyEditedItem;
        protected MenuItem renameContextMenuItem, editCommentContextMenuItem, openContextMenuItem, cutContextMenuItem,
            copyContextMenuItem, pasteContextMenuItem, deleteContextMenu, deleteContextMenuItem, deleteExcludeContextMenuItem, 
            deleteRemoveContextMenuItem, makeEmbeddedMenuItem, makeExternalMenuItem;
        protected bool referenceExistingOnAdd = false;

     

        public AbstractListView(ResXEditorControl editorControl) {
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
            this.AfterLabelEdit += new LabelEditEventHandler(ResXImagesList_AfterLabelEdit);
            this.BeforeLabelEdit += new LabelEditEventHandler(ResXImagesList_BeforeLabelEdit);
            this.SelectedIndexChanged += new EventHandler((o, e) => { NotifyItemsStateChanged(); });
            this.MouseDoubleClick += new MouseEventHandler(AbstractListView_MouseDoubleClick);
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(AbstractListView_DragEnter);
            this.DragDrop += new DragEventHandler(AbstractListView_DragDrop);
            editorControl.ViewKindChanged += new Action<View>(ViewKindChanged);
            editorControl.RemoveRequested += new Action<REMOVEKIND>(editorControl_RemoveRequested);

            renameContextMenuItem = new MenuItem("Rename");
            renameContextMenuItem.Shortcut = Shortcut.F2;
            renameContextMenuItem.Click += new EventHandler((o, e) => { SelectedItems[0].BeginEdit(); });

            editCommentContextMenuItem = new MenuItem("Edit comment");
            editCommentContextMenuItem.Shortcut = Shortcut.F3;
            editCommentContextMenuItem.Click += new EventHandler(editCommentContextMenuItem_Click);

            openContextMenuItem = new MenuItem("Open");
            openContextMenuItem.Shortcut = Shortcut.F11;
            openContextMenuItem.Click += new EventHandler(openContextMenuItem_Click);

            cutContextMenuItem = new MenuItem("Cut");
            cutContextMenuItem.Shortcut = Shortcut.CtrlX;
            cutContextMenuItem.Click += new EventHandler((o, e) => { editorControl.ExecuteCut(); });

            copyContextMenuItem = new MenuItem("Copy");
            copyContextMenuItem.Shortcut = Shortcut.CtrlC;
            copyContextMenuItem.Click += new EventHandler((o, e) => { editorControl.ExecuteCopy(); });

            pasteContextMenuItem = new MenuItem("Paste");
            pasteContextMenuItem.Shortcut = Shortcut.CtrlV;
            pasteContextMenuItem.Click += new EventHandler((o, e) => { editorControl.ExecutePaste(); });

            deleteContextMenu = new MenuItem("Remove");

            deleteContextMenuItem = new MenuItem("Remove resource(s)");
            deleteContextMenuItem.Shortcut = Shortcut.Del; 
            deleteContextMenuItem.Click += new EventHandler((o, e) => { editorControl_RemoveRequested(REMOVEKIND.REMOVE); });

            deleteExcludeContextMenuItem = new MenuItem("Remove && exclude resource(s)");
            deleteExcludeContextMenuItem.Click += new EventHandler((o, e) => { editorControl_RemoveRequested(REMOVEKIND.REMOVE | REMOVEKIND.EXCLUDE); });
            deleteExcludeContextMenuItem.Shortcut = Shortcut.CtrlE;

            deleteRemoveContextMenuItem = new MenuItem("Remove && delete resource(s)");
            deleteRemoveContextMenuItem.Click+=new EventHandler((o, e) => { editorControl_RemoveRequested(REMOVEKIND.REMOVE | REMOVEKIND.DELETE_FILE | REMOVEKIND.EXCLUDE); });
            deleteRemoveContextMenuItem.Shortcut = Shortcut.ShiftDel;

            makeEmbeddedMenuItem = new MenuItem("Make this resource(s) embedded");
            makeEmbeddedMenuItem.Click += new EventHandler(makeEmbeddedMenuItem_Click);

            makeExternalMenuItem = new MenuItem("Make this resource(s) external");
            makeExternalMenuItem.Click += new EventHandler(makeExternalMenuItem_Click);

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
            contextMenu.MenuItems.Add(makeEmbeddedMenuItem);
            contextMenu.MenuItems.Add(makeExternalMenuItem);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(deleteContextMenu);
            contextMenu.Popup += new EventHandler(contextMenu_Popup);
            this.ContextMenu = contextMenu;

            InitializeColumns();
        }                       

        #region IDataTabItem members

        public Dictionary<string, ResXDataNode> GetData(bool throwExceptions) {            
            Focus();

            Dictionary<string, ResXDataNode> data = new Dictionary<string, ResXDataNode>(Items.Count);
            foreach (ListViewKeyItem item in Items) {                
                if (item.ErrorMessages.Count > 0 && throwExceptions) throw new Exception(item.ErrorMessages.First());
                
                ResXDataNode node;
                if (item.DataNode.FileRef == null) {
                    node = new ResXDataNode(item.Key, item.DataNode.GetValue((ITypeResolutionService)null));
                } else {
                    node = new ResXDataNode(item.Key, item.DataNode.FileRef);
                }
                
                node.Comment = item.SubItems["Comment"].Text;
                data.Add(item.Key, node);
            }

            return data;
        }        
        
        public void BeginAdd() {
            this.Items.Clear();
            this.SuspendLayout();

            this.LargeImageList = new ImageList();
            this.SmallImageList = new ImageList();            
        }
        
        public void EndAdd() {
            this.LargeImageList.ImageSize = new System.Drawing.Size(100, 100);
            this.ResumeLayout();
        }        
       
        public virtual IKeyValueSource Add(string key, ResXDataNode value, bool showThumbnails) {
            if (referenceExistingOnAdd) {
                ListViewKeyItem existingItem = ItemFromName(key);
                value.Comment = existingItem.DataNode.Comment;
                existingItem.DataNode = value;
                existingItem.SubItems["Path"].Text = Uri.UnescapeDataString(editorControl.Editor.FileUri.MakeRelativeUri(new Uri(value.FileRef.FileName)).ToString());

                return existingItem;
            }

            ListViewKeyItem item = new ListViewKeyItem(this);
            item.Text = key;            
            item.DataNode = value;
            item.Name = value.FileRef != null ? Path.GetFullPath(value.FileRef.FileName) : Path.GetRandomFileName();
            item.ImageKey = item.Name;            

            ListViewItem.ListViewSubItem subKey = new ListViewItem.ListViewSubItem();
            subKey.Name = "Path";
            if (value.FileRef != null) {
                subKey.Text = Uri.UnescapeDataString(editorControl.Editor.FileUri.MakeRelativeUri(new Uri(value.FileRef.FileName)).ToString());
            } else {
                subKey.Text = "(embedded)";
            }
            item.SubItems.Add(subKey);

            ListViewItem.ListViewSubItem subComment = new ListViewItem.ListViewSubItem();
            subComment.Name = "Comment";
            subComment.Text = value.Comment;            
            item.SubItems.Add(subComment);

            ListViewItem.ListViewSubItem subReferences = new ListViewItem.ListViewSubItem();
            subReferences.Name = "References";
            subReferences.Text = "?";
            item.SubItems.Add(subReferences);

            Items.Add(item);
            item.AfterEditValue = item.Text;
            
            Validate(item);
            NotifyItemsStateChanged();

            return item;
        }        

        public abstract bool CanContainItem(ResXDataNode node);

        public COMMAND_STATUS CanCutOrCopy {
            get {
                return HasSelectedItems && !IsEditing && !DataReadOnly ? COMMAND_STATUS.ENABLED : COMMAND_STATUS.DISABLED;
            }
        }

        public COMMAND_STATUS CanPaste {
            get {
                IDataObject iData = Clipboard.GetDataObject();
                return acceptsClipboardData(iData) ?  COMMAND_STATUS.ENABLED : COMMAND_STATUS.DISABLED;
            }
        }

        public bool Copy() {
            if (CanCutOrCopy != COMMAND_STATUS.ENABLED) return false;

            bool allEmbedded = true;
            bool allExternal = true;

            foreach (ListViewKeyItem item in this.SelectedItems) {
                ResXFileRef fileRef = item.DataNode.FileRef;
                allEmbedded = allEmbedded && fileRef == null;
                allExternal = allExternal && fileRef != null;
            }
            if (!allEmbedded && !allExternal) {
                throw new Exception("Cannot copy both embedded and external resources at the same time.");
            } else {
                if (allExternal) {
                    StringCollection list = new StringCollection();
                    foreach (ListViewKeyItem item in this.SelectedItems) {
                        ResXFileRef fileRef = item.DataNode.FileRef;
                        if (fileRef == null) continue;

                        string path = fileRef.FileName;
                        list.Add(path);
                    }

                    Clipboard.SetFileDropList(list);
                } else {
                    List<object> list = new List<object>();
                    foreach (ListViewKeyItem item in this.SelectedItems) {
                        list.Add(item.DataNode);
                    }
                    Clipboard.SetDataObject(list, false);
                }
            }
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
                return Items.Count > 0;
            }
        }

        public bool HasSelectedItems {
            get {
                return SelectedItems.Count > 0;
            }
        }

        public bool SelectAllItems() {
            Focus();
            foreach (ListViewKeyItem item in Items)
                item.Selected = true;
            return true;
        }      

        public bool IsEditing {
            get {
                return CurrentlyEditedItem != null;
            }
        }

        private bool _ReadOnly;
        public bool DataReadOnly {
            get {
                return _ReadOnly; 
            }
            set {
                _ReadOnly = value;
                LabelEdit = !value;
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

        public ListViewKeyItem ItemFromName(string name) {
            ListViewKeyItem existingItem = null;
            foreach (ListViewKeyItem i in Items)
                if (i.Text == name) {
                    existingItem = i;
                    break;
                }
            return existingItem;
        }

        #endregion

        #region protected members - virtual

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
            commentHeader.Name = "Comment";
            this.Columns.Add(commentHeader);

            ColumnHeader referencesHeader = new ColumnHeader();
            referencesHeader.Text = "References";
            referencesHeader.Width = 80;
            referencesHeader.Name = "References";
            referencesHeader.TextAlign = HorizontalAlignment.Center;
            this.Columns.Add(referencesHeader);
        }

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e) {
            contextMenu_Popup(null, null);
            base.OnPreviewKeyDown(e);
        }

        protected abstract string saveIntoTmpFile(ResXDataNode node, string directory);

        #endregion

        #region public members

        public void Validate(ListViewKeyItem item) {
            conflictResolver.TryAdd(item.BeforeEditValue, item.AfterEditValue, item, editorControl.Editor.ProjectItem, null);
            item.UpdateErrorSetDisplay();
        }

        public virtual ListViewKeyItem UpdateDataOf(string name) {
            if (!Items.ContainsKey(name)) return null;

            ListViewKeyItem item = Items[name] as ListViewKeyItem;
            item.SubItems["Comment"].Text = null;

            return item;
        }       

        public void ListItemKeyRenamed(ListViewKeyItem item) {
            ListViewRenameKeyUndoUnit unit = new ListViewRenameKeyUndoUnit(editorControl, this, item, item.BeforeEditValue, item.AfterEditValue);
            
            if (VisualLocalizerPackage.Instance.DTE.Solution.ContainsProjectItem(editorControl.Editor.ProjectItem.InternalProjectItem)) {
                ResXProjectItem resxItem = editorControl.Editor.ProjectItem;
                resxItem.ResolveNamespaceClass(resxItem.InternalProjectItem.ContainingProject.GetResXItemsAround(false, true));
                
                if (item.ErrorMessages.Count == 0 && resxItem != null && !resxItem.IsCultureSpecific()) {
                    int errors = 0;
                    int count = item.CodeReferences.Count;
                    item.CodeReferences.ForEach((i) => { i.KeyAfterRename = item.AfterEditValue; });

                    BatchReferenceReplacer replacer = new BatchReferenceReplacer(item.CodeReferences);
                    replacer.Inline(item.CodeReferences, true, ref errors);

                    VLOutputWindow.VisualLocalizerPane.WriteLine("Renamed {0} key references in code", count);
                }
            }

            editorControl.Editor.AddUndoUnit(unit);
        }

        public void MakeResourcesEmbedded(IEnumerable list, bool delete, bool addUndoUnit) {
            ListViewMakeEmbeddedUndoUnit undoUnit = null;
            try {
                List<ListViewKeyItem> undoList = new List<ListViewKeyItem>();
                undoUnit = new ListViewMakeEmbeddedUndoUnit(this, undoList, delete);

                foreach (ListViewKeyItem item in list) {
                    object value = item.DataNode.GetValue((ITypeResolutionService)null);
                    string path = item.DataNode.FileRef.FileName;
                    string name = item.DataNode.Name;
                    string cmt = item.DataNode.Comment;
                    item.DataNode = new ResXDataNode(name, value);
                    item.DataNode.Comment = cmt;
                    item.SubItems["Path"].Text = "(embedded)";

                    VLOutputWindow.VisualLocalizerPane.WriteLine("Embedded resource \"{0}\" into ResX file", item.Key);

                    if (delete) {
                        ProjectItem projectItem = VisualLocalizerPackage.Instance.DTE.Solution.FindProjectItem(path);
                        if (projectItem != null) {
                            item.NeighborItems = projectItem.Collection;
                            projectItem.Delete();
                        }
                        if (File.Exists(path)) File.Delete(path);

                        VLOutputWindow.VisualLocalizerPane.WriteLine("Deleted referenced file \"{0}\"", Path.GetFileName(path));
                    }
                    undoList.Add(item);
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            } finally {
                if (addUndoUnit && undoUnit != null) {
                    editorControl.Editor.AddUndoUnit(undoUnit);
                }
                NotifyDataChanged();
            }
        }

        public void MakeResourcesExternal(IEnumerable list, bool referenceExisting, bool addUndoUnit) {
            ListViewMakeExternalUndoUnit undoUnit = null;
            try {
                List<ListViewKeyItem> undoList = new List<ListViewKeyItem>();
                undoUnit = new ListViewMakeExternalUndoUnit(this, undoList, referenceExisting);

                List<string> paths = new List<string>();
                string dir = Path.Combine(Path.GetTempPath(), "VisualLocalizer");
                Directory.CreateDirectory(dir);

                foreach (ListViewKeyItem item in list) {
                    string path = saveIntoTmpFile(item.DataNode, dir);
                    paths.Add(path);

                    undoList.Add(item);
                    VLOutputWindow.VisualLocalizerPane.WriteLine("Moved resource \"{0}\" to external file", item.Key);
                }

                referenceExistingOnAdd = true;
                editorControl.addExistingFiles(paths);
                referenceExistingOnAdd = false;

                editorControl.Editor.UndoManager.RemoveTopFromUndoStack(1);                
                
                Directory.Delete(dir, true);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
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

        private void AbstractListView_DragDrop(object sender, DragEventArgs e) {
            editorControl.ExecutePaste(e.Data);
        }

        private void AbstractListView_DragEnter(object sender, DragEventArgs e) {
            e.Effect = acceptsClipboardData(e.Data) ? DragDropEffects.All : DragDropEffects.None;
        }         

        private bool acceptsClipboardData(IDataObject iData) {
            bool containsList = iData.GetDataPresent(typeof(List<object>));
            bool containsFiles = iData.GetDataPresent(StringConstants.FILE_LIST);
            bool containsSolExpList = iData.GetDataPresent(StringConstants.SOLUTION_EXPLORER_FILE_LIST);

            return (containsFiles || containsSolExpList || containsList) && !IsEditing && !DataReadOnly;
        }

        private void AbstractListView_MouseDoubleClick(object sender, MouseEventArgs e) {
            ListViewKeyItem item = this.GetItemAt(e.X, e.Y) as ListViewKeyItem;
            if (item != null) {
                openForEdit(item);
            }
        }

        private void makeExternalMenuItem_Click(object sender, EventArgs e) {
            MakeResourcesExternal((IEnumerable)SelectedItems, false, true);
        }
        
        private void makeEmbeddedMenuItem_Click(object sender, EventArgs e) {            
            var result = System.Windows.Forms.MessageBox.Show("Do you also want to delete all referenced files?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            bool delete = result == DialogResult.Yes;

            MakeResourcesEmbedded((IEnumerable)SelectedItems, delete, true);
        }        

        protected void editCommentContextMenuItem_Click(object sender, EventArgs e) {
            ListViewKeyItem item = SelectedItems[0] as ListViewKeyItem;
            CommentWindow win = new CommentWindow(item.DataNode.Comment);
            if (win.ShowDialog() == DialogResult.OK) {
                ListViewChangeCommentUndoUnit unit = new ListViewChangeCommentUndoUnit(item, item.DataNode.Comment, win.Comment, item.Key);
                editorControl.Editor.AddUndoUnit(unit);

                item.DataNode.Comment = win.Comment;
                item.SubItems["Comment"].Text = win.Comment;

                NotifyDataChanged();
                VLOutputWindow.VisualLocalizerPane.WriteLine("Edited comment of \"{0}\"", item.Key);
            }
        }

        protected void openContextMenuItem_Click(object sender, EventArgs e) {
            ListViewKeyItem item = SelectedItems[0] as ListViewKeyItem;
            openForEdit(item);
        }

        private void openForEdit(ListViewKeyItem item) {
            if (!item.FileRefOk) {
                VisualLocalizer.Library.MessageBox.ShowError("Cannot open item - referenced file does not exist.");
                return;
            }
            if (item.DataNode.FileRef == null) {
                VisualLocalizer.Library.MessageBox.ShowError("Cannot open item - editing of embedded resources is not supported.");
                return;
            }

            try {
                Window win = VisualLocalizerPackage.Instance.DTE.OpenFile(null, item.DataNode.FileRef.FileName);
                if (win != null) win.Activate();

                VLOutputWindow.VisualLocalizerPane.WriteLine("Opened resource file \"{0}\"", item.DataNode.FileRef.FileName);
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);                
            }
        }

        protected void contextMenu_Popup(object sender, EventArgs e) {
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
        }

        protected string GetFileSize(long bytes) {
            if (bytes < 1024) {
                return string.Format("{0} B", bytes);
            } else if (bytes < 1024 * 1024) {
                return string.Format("{0} kB", bytes / 1024);
            } else {
                return string.Format("{0} MB", bytes / (1024 * 1024));
            }
        }

        protected void ResXImagesList_BeforeLabelEdit(object sender, LabelEditEventArgs e) {
            try {
                ListViewKeyItem item = (ListViewKeyItem)Items[e.Item];
                item.BeforeEditValue = item.Key;
                CurrentlyEditedItem = item;
                NotifyItemsStateChanged();

                editorControl.ReferenceCounterThreadSuspended = true;
                editorControl.UpdateReferencesCount(item);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        protected void ResXImagesList_AfterLabelEdit(object sender, LabelEditEventArgs e) {
            try {
                ListViewKeyItem item = (ListViewKeyItem)Items[e.Item];

                if (e.Label != null && string.Compare(e.Label, item.BeforeEditValue) != 0) {
                    item.AfterEditValue = e.Label;
                    ListItemKeyRenamed(item);

                    Validate(item);
                    NotifyDataChanged();
                    VLOutputWindow.VisualLocalizerPane.WriteLine("Renamed from \"{0}\" to \"{1}\"", item.BeforeEditValue, item.AfterEditValue);
                }
                CurrentlyEditedItem = null;                
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            } finally {
                editorControl.ReferenceCounterThreadSuspended = false;
                NotifyItemsStateChanged();
            }
        }

        protected void editorControl_RemoveRequested(REMOVEKIND remove) {
            try {
                if (!this.Visible) return;
                if (this.SelectedItems.Count == 0) return;

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

                if ((remove & REMOVEKIND.REMOVE) == REMOVEKIND.REMOVE) {
                    List<ListViewKeyItem> removedItems = new List<ListViewKeyItem>();
                    foreach (ListViewKeyItem item in SelectedItems) {
                        conflictResolver.TryAdd(item.Key, null, item, editorControl.Editor.ProjectItem, null);
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
                        ItemsRemoved(removedItems);
                        NotifyItemsStateChanged();
                        NotifyDataChanged();

                        VLOutputWindow.VisualLocalizerPane.WriteLine("Removed {0} resource files", removedItems.Count);
                    }
                }                
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        protected void ItemsRemoved(List<ListViewKeyItem> list) {
            ListViewRemoveItemsUndoUnit unit = new ListViewRemoveItemsUndoUnit(editorControl, list, editorControl.conflictResolver);
            editorControl.Editor.AddUndoUnit(unit);
        }        

        protected void ViewKindChanged(View view) {
            this.View = view;
        }      

        #endregion
    }
}
