using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel.Design;
using System.Resources;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using VisualLocalizer.Library;
using System.Drawing.Drawing2D;
using VisualLocalizer.Editor.UndoUnits;
using System.IO;
using EnvDTE;
using VSLangProj;
using VisualLocalizer.Components;
using EnvDTE80;
using System.Drawing.Imaging;
using VisualLocalizer.Gui;
using System.Collections.Specialized;
using VisualLocalizer.Library;
using System.Text.RegularExpressions;

namespace VisualLocalizer.Editor {

    [Flags]
    internal enum REMOVEKIND { REMOVE = 1, EXCLUDE = 2, DELETE_FILE = 4 }
    
    internal sealed class ResXEditorControl : TableLayoutPanel,IEditorControl {

        private ResXStringGrid stringGrid;
        private ResXTabControl tabs;
        private ToolStrip toolStrip;
        private ToolStripMenuItem removeExcludeItem, removeDeleteItem;
        internal ToolStripSplitButton removeButton, inlineButton;
        private ToolStripDropDownButton viewButton;
        private ResXImagesList imagesListView;
        private ResXIconsList iconsListView;
        private ResXSoundsList soundsListView;
        private ResXFilesList filesListView;
        private TabPage stringTab, imagesTab, iconsTab, soundsTab, filesTab;
        private ToolStripComboBox codeGenerationBox;        
        private bool readOnly;

        private readonly string[] IMAGE_FILE_EXT = { ".png", ".gif", ".bmp", ".jpg", ".jpeg", ".tif", ".tiff" };
        private readonly string[] ICON_FILE_EXT = { ".ico" };
        private readonly string[] SOUND_FILE_EXT = { ".wav" };
        private readonly string[] TEXT_FILE_EXT = { ".txt" };

        public event EventHandler DataChanged;
        public event Action<REMOVEKIND> RemoveRequested;
        public event Action<View> ViewKindChanged;
        public KeyValueConflictResolver conflictResolver;

        public void Init<T>(AbstractSingleViewEditor<T> editor) where T : Control, IEditorControl, new() {
            this.Editor = editor as ResXEditor;

            this.Dock = DockStyle.Fill;
            this.DoubleBuffered = true;
            this.conflictResolver = new KeyValueConflictResolver(true, false);

            initTabControl();
            initToolStrip();

            this.RowCount = 2;
            this.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            this.ColumnCount = 1;
            this.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            this.Controls.Add(toolStrip, 0, 0);
            this.Controls.Add(tabs, 0, 1);
        }
        
        public ResXEditor Editor {
            get;
            set;
        }

        private void initToolStrip() {
            ToolStripManager.Renderer = new ToolStripProfessionalRenderer(new VsColorTable());

            toolStrip = new ToolStrip();
            toolStrip.Dock = DockStyle.Top;

            ToolStripSplitButton addButton = new ToolStripSplitButton("&Add Resource");
            addButton.ButtonClick += new EventHandler(addExistingResources);
            addButton.DropDownItems.Add("Existing File", null, new EventHandler(addExistingResources));
            addButton.DropDownItems.Add(new ToolStripSeparator());
            ToolStripMenuItem newItem = new ToolStripMenuItem("New");
            newItem.DropDownItems.Add("String",null,new EventHandler(addNewString));
            newItem.DropDownItems.Add("Icon", null, new EventHandler((o, e) => { addNewImage(typeof(Icon), iconsListView, "Icons"); }));
            newItem.DropDownItems.Add("Image", null, new EventHandler((o, e) => { addNewImage(typeof(Bitmap), imagesListView, "Images"); }));
            addButton.DropDownItems.Add(newItem);
            toolStrip.Items.Add(addButton);

            ToolStripDropDownButton mergeButton = new ToolStripDropDownButton("&Merge with ResX File");
            mergeButton.DropDownItems.Add("Merge && &Preserve Both", null, new EventHandler(mergeButton_ButtonClick));
            mergeButton.DropDownItems.Add("Merge && &Delete Source", null, new EventHandler(mergeButton_ButtonClick));
            toolStrip.Items.Add(mergeButton);

            toolStrip.Items.Add(new ToolStripSeparator());

            removeButton = new ToolStripSplitButton("&Remove Resources");
            removeButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            removeExcludeItem = new ToolStripMenuItem("Remove && Exclude from Project");
            removeDeleteItem = new ToolStripMenuItem("Remove && Delete File");
            removeButton.DropDownItems.Add(removeExcludeItem);
            removeButton.DropDownItems.Add(removeDeleteItem);
            removeButton.ButtonClick += new EventHandler((o, e) => { NotifyRemoveRequested(REMOVEKIND.REMOVE); });
            removeDeleteItem.Click += new EventHandler((o, e) => { NotifyRemoveRequested(REMOVEKIND.REMOVE | REMOVEKIND.DELETE_FILE); });
            removeExcludeItem.Click += new EventHandler((o, e) => { NotifyRemoveRequested(REMOVEKIND.REMOVE | REMOVEKIND.EXCLUDE); });
            toolStrip.Items.Add(removeButton);

            inlineButton = new ToolStripSplitButton("&Inline resources");
            inlineButton.ButtonClick += new EventHandler(inlineButton_ButtonClick);
            inlineButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            inlineButton.DropDownItems.Add("Inline && &remove", null, new EventHandler(inlineAndRemoveButton_ButtonClick)); 
            toolStrip.Items.Add(inlineButton);

            toolStrip.Items.Add(new ToolStripSeparator());

            viewButton = new ToolStripDropDownButton("&View");
            ToolStripMenuItem viewDetailsItem = new ToolStripMenuItem("Details");
            viewDetailsItem.CheckState = CheckState.Unchecked;
            viewDetailsItem.CheckOnClick = true;
            viewDetailsItem.CheckStateChanged += new EventHandler(ViewCheckStateChanged);
            viewDetailsItem.Tag = View.Details;
            viewButton.DropDownItems.Add(viewDetailsItem);
            ToolStripMenuItem viewListItem = new ToolStripMenuItem("List");
            viewListItem.CheckState = CheckState.Unchecked;
            viewListItem.CheckOnClick = true;
            viewListItem.CheckStateChanged += new EventHandler(ViewCheckStateChanged);
            viewListItem.Tag = View.List;
            viewButton.DropDownItems.Add(viewListItem);
            ToolStripMenuItem viewIconsItem = new ToolStripMenuItem("Icons");
            viewIconsItem.CheckState = CheckState.Checked;
            viewIconsItem.CheckOnClick = true;
            viewIconsItem.CheckStateChanged += new EventHandler(ViewCheckStateChanged);
            viewIconsItem.Tag = View.LargeIcon;
            viewButton.DropDownItems.Add(viewIconsItem);
            toolStrip.Items.Add(viewButton);

            toolStrip.Items.Add(new ToolStripSeparator());

            ToolStripLabel codeGenerationLabel = new ToolStripLabel();
            codeGenerationLabel.Text = "Access Modifier:";
            toolStrip.Items.Add(codeGenerationLabel);

            codeGenerationBox = new ToolStripComboBox();
            codeGenerationBox.DropDownStyle = ComboBoxStyle.DropDownList;
            codeGenerationBox.FlatStyle = FlatStyle.Standard;
            codeGenerationBox.ComboBox.Items.Add("Internal");
            codeGenerationBox.ComboBox.Items.Add("Public");
            codeGenerationBox.ComboBox.Items.Add("No designer class");            
            codeGenerationBox.SelectedIndexChanged += new EventHandler(noFocusBoxSelectedIndexChanged);
            codeGenerationBox.SelectedIndexChanged += new EventHandler(codeGenerationBox_SelectedIndexChanged);
            codeGenerationBox.Margin = new Padding(2);
            if (!VisualLocalizerPackage.Instance.DTE.Solution.IsUserDefined()) codeGenerationBox.Enabled = false;

            toolStrip.Items.Add(codeGenerationBox);
        }
       
        private void initTabControl() {
            tabs = new ResXTabControl();
            tabs.Dock = DockStyle.Fill;
            tabs.ItemSize = new Size(25, 80);
            tabs.Alignment = TabAlignment.Left;
            tabs.SizeMode = TabSizeMode.Fixed;
            tabs.SelectedIndexChanged += new EventHandler(UpdateToolStripButtonsEnable);

            stringTab = new TabPage("Strings");
            stringTab.BorderStyle = BorderStyle.None;
            stringGrid = new ResXStringGrid(this);
            stringGrid.Dock = DockStyle.Fill;
            stringGrid.BackColor = Color.White;
            stringGrid.ScrollBars = ScrollBars.Vertical;
            stringGrid.DataChanged += new EventHandler((o, args) => { DataChanged(o, args); });
            stringGrid.BorderStyle = BorderStyle.None;
            stringGrid.ItemsStateChanged += new EventHandler(UpdateToolStripButtonsEnable);
            stringGrid.Name = "Content";
            stringTab.Controls.Add(stringGrid);
            tabs.TabPages.Add(stringTab);

            imagesListView = new ResXImagesList(this);
            imagesTab = CreateItemTabPage("Images", imagesListView);
            tabs.TabPages.Add(imagesTab);

            iconsListView = new ResXIconsList(this);
            iconsTab = CreateItemTabPage("Icons", iconsListView);
            tabs.TabPages.Add(iconsTab);

            soundsListView = new ResXSoundsList(this);
            soundsTab = CreateItemTabPage("Sounds", soundsListView);
            tabs.TabPages.Add(soundsTab);

            filesListView = new ResXFilesList(this);
            filesTab = CreateItemTabPage("Files", filesListView);
            tabs.TabPages.Add(filesTab);
        }

        #region public members

        public IDataTabItem GetContentFromTabPage(TabPage page) {
            if (page == null) return null;
            Control content = page.Controls.ContainsKey("Content") ? page.Controls["Content"] : null;
            if (content is IDataTabItem && content != null)
                return content as IDataTabItem;
            else
                return null;
        }

        public void SetData(Dictionary<string, ResXDataNode> data) {
            codeGenerationBox.Tag = SELECTION_CHANGE_INITIATOR.INITIALIZER;
            codeGenerationBox.SelectedItem = GetResXCodeGenerationMode();

            List<IDataTabItem> dataTabItems = new List<IDataTabItem>();

            foreach (TabPage page in this.tabs.TabPages) {
                IDataTabItem content = GetContentFromTabPage(page);
                if (content != null) {
                    IDataTabItem tabItem = content as IDataTabItem;

                    dataTabItems.Add(tabItem);
                    tabItem.BeginAdd();
                }
            }

            foreach (var pair in data) {
                foreach (var item in dataTabItems) {
                    if (item.CanContainItem(pair.Value)) {
                        item.Add(pair.Key, pair.Value, true);
                        break;
                    }
                }
            }

            foreach (IDataTabItem tabItem in dataTabItems)
                tabItem.EndAdd();
        }

        public Dictionary<string, ResXDataNode> GetData(bool throwExceptions) {
            Dictionary<string, ResXDataNode> data = new Dictionary<string, ResXDataNode>();

            foreach (TabPage page in this.tabs.TabPages) {
                IDataTabItem content = GetContentFromTabPage(page);
                if (content != null) {
                    foreach (var pair in content.GetData(throwExceptions))
                        data.Add(pair.Key, pair.Value);
                }
            }
            return data;
        }

        public void SetReadOnly(bool readOnly) {
            toolStrip.Enabled = !readOnly;
            stringGrid.ReadOnly = readOnly;
            this.readOnly = readOnly;
            UpdateToolStripButtonsEnable(null, null);
        }

        public bool ExecutePaste() {
            try {
                if (Clipboard.ContainsFileDropList()) {
                    StringCollection files = Clipboard.GetFileDropList();
                    string[] f = new string[files.Count];
                    for (int i = 0; i < files.Count; i++)
                        f[i] = files[i];
                    addExistingFiles(f);

                    return true;
                } else if (Clipboard.ContainsText()) {
                    stringGrid.AddClipboardText(Clipboard.GetText());
                    tabs.SelectedTab = stringTab;
                    return true;
                } else return false;
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                VisualLocalizer.Library.MessageBox.ShowError(text);
            }
            return false;
        }

        public bool ExecuteCopy() {
            IDataTabItem content = GetContentFromTabPage(tabs.SelectedTab);
            if (content != null)
                return content.Copy();
            else
                return false;
        }

        public bool ExecuteCut() {
            IDataTabItem content = GetContentFromTabPage(tabs.SelectedTab);
            if (content != null)
                return content.Cut();
            else
                return false;
        }

        public bool ExecuteSelectAll() {
            IDataTabItem content = GetContentFromTabPage(tabs.SelectedTab);
            if (content != null)
                return content.SelectAllItems();
            else
                return false;
        }

        public COMMAND_STATUS CanCutOrCopy {
            get {
                IDataTabItem content = GetContentFromTabPage(tabs.SelectedTab);
                if (content != null)
                    return content.CanCutOrCopy;
                else
                    return COMMAND_STATUS.UNSUPPORTED;
            }
        }

        public COMMAND_STATUS CanPaste {
            get {
                IDataTabItem content = GetContentFromTabPage(tabs.SelectedTab);
                if (content != null)
                    return content.CanPaste;
                else
                    return COMMAND_STATUS.UNSUPPORTED;
            }
        }

        public COMMAND_STATUS CanDelete {
            get {
                IDataTabItem content = GetContentFromTabPage(tabs.SelectedTab);
                if (content != null)
                    return content.HasSelectedItems && !content.IsEditing ? COMMAND_STATUS.ENABLED : COMMAND_STATUS.DISABLED;
                else
                    return COMMAND_STATUS.UNSUPPORTED;
            }
        }

        public COMMAND_STATUS CanSelectAll {
            get {
                IDataTabItem content = GetContentFromTabPage(tabs.SelectedTab);
                if (content != null)
                    return content.HasItems && !content.IsEditing ? COMMAND_STATUS.ENABLED : COMMAND_STATUS.DISABLED;
                else
                    return COMMAND_STATUS.UNSUPPORTED;
            }
        }

        public void NotifyRemoveRequested(REMOVEKIND kind) {
            if (RemoveRequested != null) RemoveRequested(kind);
        }

        #endregion

        #region private - adding resources        

        private void addExistingResources(object sender, EventArgs e) {
            try {
                string imageFilter = "*" + string.Join(";*", IMAGE_FILE_EXT);
                string iconFilter = "*" + string.Join(";*", ICON_FILE_EXT);
                string soundFilter = "*" + string.Join(";*", SOUND_FILE_EXT);
                uint selectedFilter = (uint)Math.Max(0, tabs.SelectedIndex - 1);

                string[] files = VisualLocalizer.Library.MessageBox.SelectFilesViaDlg("Select files", Path.GetDirectoryName(Editor.FileName),
                    string.Format("Image files({0})\0{0}\0Icon files({1})\0{1}\0Sound files({2})\0{2}\0", imageFilter, iconFilter, soundFilter), selectedFilter, OPENFILENAME.OFN_ALLOWMULTISELECT);
                if (files == null) return;

                addExistingFiles(files);
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                VisualLocalizer.Library.MessageBox.ShowError(text);
            }
        }

        private void addExistingFiles(IEnumerable<string> files) {
            Project project = null;
            bool userDefinedSolution=VisualLocalizerPackage.Instance.DTE.Solution.IsUserDefined();
            if (userDefinedSolution) {
                project = VisualLocalizerPackage.Instance.DTE.Solution.FindProjectItem(Editor.FileName).ContainingProject;
            }

            ProjectItem imagesFolder = null, iconsFolder = null, soundFolder = null, filesFolder = null;
            List<ListViewKeyItem> newItems = new List<ListViewKeyItem>();

            foreach (string file in files) {
                string extension = Path.GetExtension(file);
                if (!string.IsNullOrEmpty(extension)) extension = extension.ToLower();

                if (IMAGE_FILE_EXT.Contains(extension)) {
                    if (imagesFolder == null && project != null) imagesFolder = project.AddResourceDir("Images");
                    newItems.Add(addExistingItem(imagesListView, imagesFolder, file, typeof(Bitmap), true));
                    tabs.SelectedTab = imagesTab;
                } else if (ICON_FILE_EXT.Contains(extension)) {
                    if (iconsFolder == null && project != null) iconsFolder = project.AddResourceDir("Icons");
                    newItems.Add(addExistingItem(iconsListView, iconsFolder, file, typeof(Icon), true));
                    tabs.SelectedTab = iconsTab;
                } else if (SOUND_FILE_EXT.Contains(extension)) {
                    if (soundFolder == null && project != null) soundFolder = project.AddResourceDir("Sounds");
                    newItems.Add(addExistingItem(soundsListView, soundFolder, file, typeof(MemoryStream), true));
                    tabs.SelectedTab = soundsTab;
                } else {
                    if (filesFolder == null && project != null) filesFolder = project.AddResourceDir("Others");
                    if (TEXT_FILE_EXT.Contains(extension)) {
                        newItems.Add(addExistingItem(filesListView, filesFolder, file, typeof(string), true));
                    } else {
                        newItems.Add(addExistingItem(filesListView, filesFolder, file, typeof(byte[]), true));
                    }
                    tabs.SelectedTab = filesTab;
                }
            }

            if (newItems.Count > 0) {
                ListViewItemsAddUndoUnit unit = new ListViewItemsAddUndoUnit(newItems, conflictResolver);
                Editor.AddUndoUnit(unit);
            }
        }

        private ListViewKeyItem addExistingItem(AbstractListView list, ProjectItem folder, string file, Type type, bool showThumbnails) {
            string fileName = Path.GetFileName(file);            
            bool fileExists,sameTargetDir;
            Action conflictResolveAction=null;
            ListViewKeyItem addedListItem;

            if (folder == null) {
                fileExists = false;
                sameTargetDir = true;
            } else {
                string fileDir = folder.Properties.Item("FullPath").Value.ToString();
                string localFile = Path.Combine(fileDir, fileName);
                fileExists = File.Exists(localFile);
                sameTargetDir = Path.GetFullPath(localFile) == Path.GetFullPath(file);
                if (fileExists) {
                    if (folder.ProjectItems.ContainsItem(fileName)) {
                        ProjectItem conflictItem = folder.ProjectItems.Item(fileName);
                        conflictResolveAction = new Action(() => { conflictItem.Delete(); });
                    } else {
                        conflictResolveAction = new Action(() => { File.Delete(localFile); });
                    }
                }
            }

            if (fileExists) {
                if (sameTargetDir) {
                    string copyFileName = GenerateCopyFileName(file);
                    File.Copy(file, copyFileName);

                    ProjectItem newItem = folder.ProjectItems.AddFromFile(copyFileName);
                    newItem.Properties.Item("BuildAction").Value = prjBuildAction.prjBuildActionNone;

                    addedListItem = addExistingItem(list, copyFileName, type, showThumbnails);
                } else {
                    DialogResult result = VisualLocalizer.Library.MessageBox.Show(string.Format("Item \"{0}\" already exists. Do you want to overwrite the file?", fileName), null, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_QUERY);
                    if (result == DialogResult.Yes) {
                        if (conflictResolveAction != null) {
                            conflictResolveAction();
                        }
                        string fullPath;

                        if (folder != null) {
                            ProjectItem newItem = folder.ProjectItems.AddFromFileCopy(file);
                            newItem.Properties.Item("BuildAction").Value = prjBuildAction.prjBuildActionNone;
                            fullPath = newItem.Properties.Item("FullPath").Value.ToString();
                        } else {
                            fullPath = file;
                        }

                        if (list.Items.ContainsKey(fileName)) {
                            addedListItem = list.UpdateDataOf(fileName);
                        } else {
                            addedListItem = addExistingItem(list, fullPath, type, showThumbnails);
                        }

                        list.Refresh();
                        list.NotifyDataChanged();
                    } else addedListItem = null;
                }
            } else {
                if (folder == null) {
                    addedListItem = addExistingItem(list, file, type, showThumbnails);
                } else {
                    ProjectItem newItem = folder.ProjectItems.AddFromFileCopy(file);
                    newItem.Properties.Item("BuildAction").Value = prjBuildAction.prjBuildActionNone;
                    string fullPath = newItem.Properties.Item("FullPath").Value.ToString();

                    addedListItem = addExistingItem(list, fullPath, type, showThumbnails);
                }
            }
            return addedListItem;
        }

        private string GenerateCopyFileName(string file) {
            string dir = Path.GetDirectoryName(file);
            string name = Path.GetFileNameWithoutExtension(file);
            string ext = Path.GetExtension(file);
            string newName = file;

            while (File.Exists(newName)) {
                Match match = Regex.Match(name, "(.*)(\\d{1,})");
                if (match.Success) {
                    int i = int.Parse(match.Groups[2].Value);
                    i++;
                    name = string.Format("{0}{1}", match.Groups[1].Value, i);
                } else {
                    name += "1";
                }
                newName = Path.Combine(dir, name + ext);
            }

            return Path.GetFullPath(newName);
        }

        private ListViewKeyItem addExistingItem(AbstractListView list, string fullPath, Type type, bool showThumbnails) {                        
            string name = Path.GetFileNameWithoutExtension(fullPath).CreateIdentifier();

            ResXDataNode node = new ResXDataNode(name, new ResXFileRef(fullPath, type.AssemblyQualifiedName));
            ListViewKeyItem newItem = list.Add(name, node, showThumbnails) as ListViewKeyItem;
            list.NotifyDataChanged();
            list.NotifyItemsStateChanged();

            return newItem;
        }

        private void addNewString(object sender, EventArgs e) {
            tabs.SelectedTab = stringTab;
            stringGrid.ClearSelection();

            DataGridViewCell cell = stringGrid.Rows[stringGrid.Rows.Count - 1].Cells[stringGrid.KeyColumnName];            
            cell.Value = "new value";            

            stringGrid.CurrentCell = cell;
            stringGrid.BeginEdit(true);
            stringGrid.EndEdit();
            stringGrid.BeginEdit(true);

            stringGrid.NotifyDataChanged();
        }

        private void addNewImage(Type resourceType,AbstractListView listView,string resourceSubfolder) {
            NewImageWindow win = new NewImageWindow(resourceType == typeof(Icon));
            win.Owner = (Form)Form.FromHandle(new IntPtr(VisualLocalizerPackage.Instance.DTE.MainWindow.HWnd));
            
            if (win.ShowDialog(this) == DialogResult.OK) {
                try {
                    Solution solution = VisualLocalizerPackage.Instance.DTE.Solution;

                    string imageName = win.ImageName.ToLower();
                    bool hasExtension = false;
                    foreach (var item in win.ImageFormat.Extensions)
                        if (imageName.EndsWith(item)) hasExtension = true;
                    if (!hasExtension) {
                        imageName = win.ImageName + win.ImageFormat.Extensions[0];
                    } else {
                        imageName = win.ImageName;
                    }

                    ListViewKeyItem newItem;
                    if (!solution.IsUserDefined()) {
                        newItem = addNewImageNoSolution(imageName, resourceType, listView, resourceSubfolder, win);
                    } else {
                        newItem = addNewImageWithSolution(imageName, solution, resourceType, listView, resourceSubfolder, win);
                    }

                    ListViewNewItemCreateUndoUnit unit = new ListViewNewItemCreateUndoUnit(newItem, conflictResolver);
                    Editor.AddUndoUnit(unit);
                } catch (Exception ex) {
                    string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                    VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                    VisualLocalizer.Library.MessageBox.ShowError(text);
                }
            }
        }

        private ListViewKeyItem addNewImageWithSolution(string imageName, Solution solution, Type resourceType, AbstractListView listView, string resourceSubfolder, NewImageWindow win) {
            ProjectItem thisItem = solution.FindProjectItem(Editor.FileName);
            if (thisItem == null) {
                return addNewImageNoSolution(imageName, resourceType, listView, resourceSubfolder, win);
            } else {
                Project project = thisItem.ContainingProject;

                ProjectItem imagesFolder = project.AddResourceDir(resourceSubfolder);
                string imagesFolderPath = imagesFolder.Properties.Item("FullPath").Value.ToString();
                string newImagePath = Path.Combine(imagesFolderPath, imageName);

                if (imagesFolder.ProjectItems.ContainsItem(imageName))
                    throw new Exception(string.Format("File \"{0}\" already exists!", imageName));

                Bitmap bmp = new Bitmap(win.ImageWidth, win.ImageHeight);
                bmp.Save(newImagePath, win.ImageFormat.Value);
                bmp.Dispose();

                ProjectItem newImageItem = imagesFolder.ProjectItems.AddFromFile(newImagePath);
                Window newImageWindow = newImageItem.Open(null);
                if (newImageWindow != null) newImageWindow.Activate();

                newImageItem.Properties.Item("BuildAction").Value = prjBuildAction.prjBuildActionNone;

                return addExistingItem(listView, newImagePath, resourceType, false);
            }
        }

        private ListViewKeyItem addNewImageNoSolution(string imageName, Type resourceType, AbstractListView listView, string resourceSubfolder, NewImageWindow win) {
            string currentDir = Path.GetDirectoryName(Editor.FileName);
            string newImagePath = Path.Combine(currentDir, imageName);

            if (File.Exists(newImagePath))
                throw new Exception(string.Format("File \"{0}\" already exists!", imageName));

            Bitmap bmp = new Bitmap(win.ImageWidth, win.ImageHeight);
            bmp.Save(newImagePath, win.ImageFormat.Value);
            bmp.Dispose();

            Window newWindow = VisualLocalizerPackage.Instance.DTE.OpenFile(null, newImagePath);
            if (newWindow != null) newWindow.Activate();

            return addExistingItem(listView, newImagePath, resourceType, false);
        }

        #endregion

        #region private - listeners
     
        private void ViewCheckStateChanged(object sender, EventArgs e) {
            ToolStripMenuItem senderItem = sender as ToolStripMenuItem;
            if (senderItem.CheckState == CheckState.Unchecked) return;

            foreach (ToolStripMenuItem item in viewButton.DropDownItems)
                if (item != senderItem) item.CheckState = CheckState.Unchecked;

            notifyViewKindChanged((View)senderItem.Tag);
        }

        private void notifyViewKindChanged(View newView) {
            if (ViewKindChanged != null) ViewKindChanged(newView);
        }

        private void noFocusBoxSelectedIndexChanged(object sender, EventArgs e) {
            toolStrip.Focus();
        }        
      
        private void UpdateToolStripButtonsEnable(object sender, EventArgs e) {
            bool selectedString = tabs.SelectedTab.Text.Equals("Strings");
            IDataTabItem item = GetContentFromTabPage(tabs.SelectedTab);

            inlineButton.Enabled = selectedString && item.HasSelectedItems && !item.IsEditing && !readOnly;
            removeDeleteItem.Enabled = !selectedString && item.HasSelectedItems && !item.IsEditing && !readOnly;
            removeExcludeItem.Enabled = !selectedString && item.HasSelectedItems && !item.IsEditing && !readOnly;
            removeButton.Enabled = item.HasSelectedItems && !item.IsEditing && !readOnly;
            viewButton.Enabled = !selectedString && !item.IsEditing;
        }        

        private void inlineButton_ButtonClick(object sender, EventArgs e) {
            VisualLocalizer.Library.MessageBox.ShowError("Not yet implemented");
        }

        private void inlineAndRemoveButton_ButtonClick(object sender, EventArgs e) {
            VisualLocalizer.Library.MessageBox.ShowError("Not yet implemented");
        }

        private void mergeButton_ButtonClick(object sender, EventArgs e) {
            VisualLocalizer.Library.MessageBox.ShowError("Not yet implemented");
        }

        private string previousValue = null;       
        private void codeGenerationBox_SelectedIndexChanged(object sender, EventArgs e) {
            if (!VisualLocalizerPackage.Instance.DTE.Solution.IsUserDefined()) return;

            try {
                ProjectItem documentItem = VisualLocalizerPackage.Instance.DTE.Solution.FindProjectItem(Editor.FileName);
                if (documentItem == null) throw new Exception("Cannot find corresponding project item.");

                ToolStripComboBox box = sender as ToolStripComboBox; 

                string selectedValue = box.SelectedItem.ToString();
                if (selectedValue == "Internal") {
                    documentItem.Properties.Item("CustomTool").Value = StringConstants.InternalResXTool;
                } else if (selectedValue == "Public") {
                    documentItem.Properties.Item("CustomTool").Value = StringConstants.PublicResXTool;
                } else {
                    documentItem.Properties.Item("CustomTool").Value = null;
                }

                if (box.Tag == null) {
                    AccessModifierChangeUndoUnit undoUnit = new AccessModifierChangeUndoUnit(box, previousValue, selectedValue);
                    Editor.AddUndoUnit(undoUnit);                    
                }
                box.Tag = null;
                previousValue = selectedValue;
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                VisualLocalizer.Library.MessageBox.ShowError(text);
            }
        }

        #endregion

        private string GetResXCodeGenerationMode() {
            if (!VisualLocalizerPackage.Instance.DTE.Solution.IsUserDefined()) return null;

            try {
                ProjectItem documentItem = VisualLocalizerPackage.Instance.DTE.Solution.FindProjectItem(Editor.FileName);
                if (documentItem == null) throw new Exception("Cannot find corresponding project item.");

                string value = (string)documentItem.Properties.Item("CustomTool").Value;
                if (value == StringConstants.PublicResXTool) {
                    return "Public";
                } else if (value == StringConstants.InternalResXTool) {
                    return "Internal";
                } else {
                    return "No designer class";
                }
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                VisualLocalizer.Library.MessageBox.ShowError(text);
            }
            return null;
        }        

        private TabPage CreateItemTabPage(string header, AbstractListView content) {
            TabPage tab = new TabPage(header);
            tab.BorderStyle = BorderStyle.None;
            content.Dock = DockStyle.Fill;
            content.BackColor = Color.White;
            content.Name = "Content";
            content.DataChanged += new EventHandler((o, args) => { DataChanged(o, args); });
            content.ItemsStateChanged += new EventHandler(UpdateToolStripButtonsEnable);
            tab.Controls.Add(content);

            return tab;
        }             
    }

}
    
