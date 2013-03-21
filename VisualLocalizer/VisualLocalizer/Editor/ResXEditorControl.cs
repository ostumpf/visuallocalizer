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
using System.Text.RegularExpressions;
using System.Collections;
using Microsoft.VisualStudio.OLE.Interop;
using VisualLocalizer.Translate;
using VisualLocalizer.Settings;
using VisualLocalizer.Extensions;
using VisualLocalizer.Commands;

namespace VisualLocalizer.Editor {

    [Flags]
    internal enum REMOVEKIND { REMOVE = 1, EXCLUDE = 2, DELETE_FILE = 4 }

    [Flags]
    internal enum INLINEKIND { INLINE = 1, REMOVE = 2 }

    internal sealed class ResXEditorControl : TableLayoutPanel,IEditorControl {

        private ResXStringGrid stringGrid;
        private ResXTabControl tabs;
        private ToolStrip toolStrip;
        private ToolStripMenuItem removeExcludeItem, removeDeleteItem;
        private ToolStripSplitButton removeButton, inlineButton, addButton;
        private ToolStripButton profferKeysButton, updateKeysButton;
        private ToolStripDropDownButton viewButton, mergeButton;
        internal ToolStripDropDownButton translateButton;
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
        public event Action<TRANSLATE_PROVIDER> NewTranslatePairAdded;
        public event Action<TRANSLATE_PROVIDER, string, string> TranslateRequested;
        public event Action<INLINEKIND> InlineRequested;

        public KeyValueIdentifierConflictResolver conflictResolver;

        private ReferenceLister referenceLister;
        public bool ReferenceCounterThreadSuspended = false;
        private System.Threading.Thread referenceUpdaterThread;

        public void Init<T>(AbstractSingleViewEditor<T> editor) where T : Control, IEditorControl, new() {
            this.Editor = editor as ResXEditor;

            this.referenceLister = new ReferenceLister();

            this.referenceUpdaterThread = new System.Threading.Thread(ReferenceLookuperThread);
            this.referenceUpdaterThread.IsBackground = true;
            this.referenceUpdaterThread.Priority = System.Threading.ThreadPriority.BelowNormal;

            this.Dock = DockStyle.Fill;
            this.DoubleBuffered = true;
            this.conflictResolver = new KeyValueIdentifierConflictResolver(true, false);

            initToolStrip();
            initTabControl();            

            this.RowCount = 2;
            this.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            this.ColumnCount = 1;
            this.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            this.Controls.Add(toolStrip, 0, 0);
            this.Controls.Add(tabs, 0, 1);

            SettingsObject.Instance.RevalidationRequested += new Action(Instance_RevalidationRequested);
        }       

        public ResXEditor Editor {
            get;
            set;
        }

        private void initToolStrip() {
            ToolStripManager.Renderer = new ToolStripProfessionalRenderer(new VsColorTable());

            toolStrip = new ToolStrip();
            toolStrip.Dock = DockStyle.Top;

            addButton = new ToolStripSplitButton("&Add Resource");
            addButton.ButtonClick += new EventHandler(addExistingResources);
            addButton.DropDownItems.Add("Existing File", null, new EventHandler(addExistingResources));
            addButton.DropDownItems.Add(new ToolStripSeparator());
            ToolStripMenuItem newItem = new ToolStripMenuItem("New");
            newItem.DropDownItems.Add("String",null,new EventHandler(addNewString));
            newItem.DropDownItems.Add("Icon", null, new EventHandler((o, e) => { addNewImage(typeof(Icon), iconsListView, "Icons"); }));
            newItem.DropDownItems.Add("Image", null, new EventHandler((o, e) => { addNewImage(typeof(Bitmap), imagesListView, "Images"); }));
            addButton.DropDownItems.Add(newItem);
            toolStrip.Items.Add(addButton);

            mergeButton = new ToolStripDropDownButton("&Merge with ResX File");
            mergeButton.DropDownItems.Add("Merge && &Preserve Both", null, new EventHandler(mergeButton_PreserveClick));
            mergeButton.DropDownItems.Add("Merge && &Delete Source", null, new EventHandler(mergeButton_DeleteClick));
            toolStrip.Items.Add(mergeButton);

            toolStrip.Items.Add(new ToolStripSeparator());

            profferKeysButton = new ToolStripButton("Proffer");
            profferKeysButton.Click += new EventHandler(profferKeysButton_Click);
            toolStrip.Items.Add(profferKeysButton);

            updateKeysButton = new ToolStripButton("Synchronize");
            updateKeysButton.Click += new EventHandler(updateKeysButton_Click);
            toolStrip.Items.Add(updateKeysButton);

            toolStrip.Items.Add(new ToolStripSeparator());

            removeButton = new ToolStripSplitButton("&Remove");
            removeButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            removeExcludeItem = new ToolStripMenuItem("Remove && Exclude from Project");
            removeDeleteItem = new ToolStripMenuItem("Remove && Delete File");
            removeButton.DropDownItems.Add(removeExcludeItem);
            removeButton.DropDownItems.Add(removeDeleteItem);
            removeButton.ButtonClick += new EventHandler((o, e) => { NotifyRemoveRequested(REMOVEKIND.REMOVE); });
            removeDeleteItem.Click += new EventHandler((o, e) => { NotifyRemoveRequested(REMOVEKIND.REMOVE | REMOVEKIND.DELETE_FILE); });
            removeExcludeItem.Click += new EventHandler((o, e) => { NotifyRemoveRequested(REMOVEKIND.REMOVE | REMOVEKIND.EXCLUDE); });
            toolStrip.Items.Add(removeButton);

            inlineButton = new ToolStripSplitButton("&Inline");
            inlineButton.ButtonClick += new EventHandler(inlineButton_ButtonClick);
            inlineButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            inlineButton.DropDownItems.Add("Inline && &remove", null, new EventHandler(inlineAndRemoveButton_ButtonClick)); 
            toolStrip.Items.Add(inlineButton);

            translateButton = new ToolStripDropDownButton("&Translate");
            translateButton.DropDownOpening += new EventHandler(translateButton_DropDownOpening);

            foreach (TRANSLATE_PROVIDER prov in Enum.GetValues(typeof(TRANSLATE_PROVIDER))) {
                ToolStripMenuItem menuItem = new ToolStripMenuItem(prov.ToHumanForm());
                menuItem.Tag = prov;
                translateButton.DropDownItems.Add(menuItem);
            }
                    
            translateButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            toolStrip.Items.Add(translateButton);

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
            stringGrid.DataChanged += new EventHandler((o, args) => { DataChanged(o, args); });            
            stringGrid.ItemsStateChanged += new EventHandler(UpdateToolStripButtonsEnable);
            stringGrid.LanguagePairAdded += new Action<string, string>(stringGrid_LanguagePairAdded);
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
            if (Editor.ProjectItem ==null || (Editor.ProjectItem.InternalProjectItem.ContainingProject != null && Editor.ProjectItem.InternalProjectItem.ContainingProject.Kind.ToUpper() == StringConstants.WebSiteProject))
                codeGenerationBox.Enabled = false;

            codeGenerationBox.Tag = SELECTION_CHANGE_INITIATOR.INITIALIZER;
            codeGenerationBox.SelectedItem = GetResXCodeGenerationMode();

            List<IDataTabItem> dataTabItems = new List<IDataTabItem>();
            ReferenceCounterThreadSuspended = true;

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

            ReferenceCounterThreadSuspended = false;
            if (!referenceUpdaterThread.IsAlive) referenceUpdaterThread.Start(); 
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

        private void ReferenceLookuperThread() {
            bool init = true;
            while (!IsDisposed) {
                try {
                    if (init) {
                        UpdateReferencesCount();
                        init = false;
                    }
                    System.Threading.Thread.Sleep(SettingsObject.Instance.ReferenceUpdateInterval);
                    if (Visible && !IsDisposed && !ReferenceCounterThreadSuspended)
                        UpdateReferencesCount();
                } catch (Exception ex) {
                    VLOutputWindow.VisualLocalizerPane.WriteLine("{0} occured on reference lookuper thread: {1}", ex.GetType().Name, ex.Message);
                }
            }
            VLOutputWindow.VisualLocalizerPane.WriteLine("Reference lookuper thread of \"{0}\" terminated", Path.GetFileName(Editor.FileName));
        }

        public void UpdateReferencesCount() {
            ArrayList list = new ArrayList();            
            
            foreach (ResXStringGridRow row in stringGrid.Rows)
                if (!row.IsNewRow) list.Add(row);

            filesListView.Invoke(new Action<IList, IEnumerable>((l, s) => addRange(l, s)), list, filesListView.Items);
            imagesListView.Invoke(new Action<IList, IEnumerable>((l, s) => addRange(l, s)), list, imagesListView.Items);
            iconsListView.Invoke(new Action<IList, IEnumerable>((l, s) => addRange(l, s)), list, iconsListView.Items);
            soundsListView.Invoke(new Action<IList, IEnumerable>((l, s) => addRange(l, s)), list, soundsListView.Items);            

            UpdateReferencesCount(list);
        }

        private void addRange(IList list, IEnumerable source) {
            foreach (IReferencableKeyValueSource item in source)
                list.Add(item);
        }

        public void UpdateReferencesCount(IReferencableKeyValueSource src) {
            UpdateReferencesCount(new List<IReferencableKeyValueSource>() { src });
        }

        public void UpdateReferencesCount(IEnumerable items) {
            ResXProjectItem resxItem = Editor.ProjectItem;
            if (resxItem != null && resxItem.InternalProjectItem.ContainingProject != null && VisualLocalizerPackage.Instance.DTE.Solution.ContainsProjectItem(resxItem.InternalProjectItem)) {
                Project containingProject = resxItem.InternalProjectItem.ContainingProject;
                resxItem.ResolveNamespaceClass(containingProject.GetResXItemsAround(null, false, true));

                List<Project> projects = new List<Project>();
                
                projects.Add(containingProject);
                foreach (Project solutionProject in VisualLocalizerPackage.Instance.DTE.Solution.Projects) {
                    foreach (Project proj in solutionProject.GetReferencedProjects()) {
                        if (proj == containingProject) {
                            projects.Add(solutionProject);
                            break;
                        }
                    }
                }

                bool impliedDesignerItem = false;
                if (containingProject.Kind.ToUpper() == StringConstants.WebSiteProject) {
                    string relative = resxItem.InternalProjectItem.GetRelativeURL();
                    impliedDesignerItem = !string.IsNullOrEmpty(relative) && relative.StartsWith(StringConstants.GlobalWebSiteResourcesFolder);
                }

                if (resxItem.DesignerItem == null && !impliedDesignerItem) {
                    foreach (IReferencableKeyValueSource item in items) {
                        item.CodeReferences.Clear();
                        item.UpdateReferenceCount(false);
                    }
                } else {
                    Trie<CodeReferenceTrieElement> trie = new Trie<CodeReferenceTrieElement>();
                    foreach (IReferencableKeyValueSource item in items) {                        
                        string referenceKey;
                        if (item.ErrorMessages.Count == 0) {
                            referenceKey = item.Key;
                        } else {
                            referenceKey = "";
                            //referenceKey = item.LastValidKey;
                        }
                        var element = trie.Add(resxItem.Class + "." + referenceKey);
                        element.Infos.Add(new CodeReferenceInfo() { Origin = resxItem, Value = item.Value, Key = referenceKey });
                    }
                    trie.CreatePredecessorsAndShortcuts();

                    referenceLister.Process(projects, trie, resxItem);

                    foreach (IReferencableKeyValueSource item in items) {
                        item.CodeReferences.Clear();
                        /*item.CodeReferences.AddRange(referenceLister.Results.Where((i) => {
                            return i.Key == item.Key || (item.ErrorSet.Count > 0 && i.Key == item.LastValidKey);
                        }));*/
                        item.CodeReferences.AddRange(referenceLister.Results.Where((i) => {
                            return i.Key == item.Key;
                        }));
                        item.UpdateReferenceCount(true); 
                    }
                }
            }
        }


        public void SetReadOnly(bool readOnly) {
            foreach (TabPage page in tabs.TabPages) {
                IDataTabItem item = GetContentFromTabPage(page);
                item.DataReadOnly = readOnly;
            }
            this.readOnly = readOnly;
            UpdateToolStripButtonsEnable(null, null);
        }

        public bool ExecutePaste() {
            return ExecutePaste(Clipboard.GetDataObject());
        }

        public bool ExecutePaste(System.Windows.Forms.IDataObject iData) {
            try {
                if (iData.GetDataPresent(StringConstants.FILE_LIST)) {
                    string[] files = (string[])iData.GetData(StringConstants.FILE_LIST);                    
                    addExistingFiles(files);
                    return true;
                } else if (iData.GetDataPresent("Text") && !iData.GetDataPresent(StringConstants.SOLUTION_EXPLORER_FILE_LIST)) {
                    stringGrid.AddClipboardText((string)iData.GetData("Text"));
                    tabs.SelectedTab = stringTab;
                    return true;
                } else {                    
                    List<AbstractListView> dataTabItems = new List<AbstractListView>();
                    foreach (TabPage page in this.tabs.TabPages) {
                        IDataTabItem content = GetContentFromTabPage(page);
                        if (content != null && content is AbstractListView) {
                            dataTabItems.Add(content as AbstractListView);
                        }
                    }

                    if (iData.GetDataPresent(typeof(List<object>))) {
                        internalEmbeddedPaste((List<object>)iData.GetData(typeof(List<object>)), dataTabItems);                        
                        return true;
                    } else if (iData.GetDataPresent(StringConstants.SOLUTION_EXPLORER_FILE_LIST)) {
                        internalSolExpPaste((MemoryStream)iData.GetData(StringConstants.SOLUTION_EXPLORER_FILE_LIST), dataTabItems);                        
                        return true;
                    } else return false;
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            } 
            return false;
        }            

        public bool ExecuteCopy() {
            try {
                IDataTabItem content = GetContentFromTabPage(tabs.SelectedTab);
                if (content != null)
                    return content.Copy();
                else
                    return false;
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);

                return false;
            }            
        }

        public bool ExecuteCut() {
            try {
                IDataTabItem content = GetContentFromTabPage(tabs.SelectedTab);
                if (content != null)
                    return content.Cut();
                else
                    return false;
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);

                return false;
            }
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
                    return content.HasSelectedItems && !content.IsEditing && !content.DataReadOnly ? COMMAND_STATUS.ENABLED : COMMAND_STATUS.DISABLED;
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

        public void AddForTranslation(List<AbstractTranslateInfoItem> list) {
            stringGrid.AddToTranslationList(stringGrid.Rows, list);
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
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        internal void addExistingFiles(IEnumerable<string> files) {
            Project project = null;
            bool userDefinedSolution=VisualLocalizerPackage.Instance.DTE.Solution.ContainsProjectItem(Editor.ProjectItem.InternalProjectItem);
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
                ListViewItemsAddUndoUnit unit = new ListViewItemsAddUndoUnit(this, newItems, conflictResolver);
                Editor.AddUndoUnit(unit);

                VLOutputWindow.VisualLocalizerPane.WriteLine("Added {0} existing files", newItems.Count);
            }
        }

        private ListViewKeyItem addExistingItem(AbstractListView list, ProjectItem folder, string file, Type type, bool showThumbnails) {
            string fileName = Path.GetFileName(file);
            string localFile = null;
            bool localFileExists,fileSameAsLocalFile;            
            ListViewKeyItem addedListItem;

            if (folder == null) {
                localFileExists = false;
                fileSameAsLocalFile = true;
            } else {                
                localFile = Path.Combine(folder.GetFullPath(), fileName);
                localFileExists = File.Exists(localFile);
                fileSameAsLocalFile = string.Compare(Path.GetFullPath(localFile), Path.GetFullPath(file), true) == 0;             
            }

            if (localFileExists) {
                if (fileSameAsLocalFile) {
                    ListViewKeyItem existingListItem = list.ItemFromName(Path.GetFileNameWithoutExtension(file));
                    if (existingListItem == null) {
                        addedListItem = addExistingItem(list, file, type, showThumbnails);
                    } else {
                        string copyFileName = GenerateCopyFileName(file);
                        File.Copy(file, copyFileName);

                        ProjectItem newItem = folder.ProjectItems.AddFromFile(copyFileName);
                        setBuildAction(newItem, prjBuildAction.prjBuildActionNone);

                        addedListItem = addExistingItem(list, copyFileName, type, showThumbnails);    
                    }
                } else {
                    Action conflictResolveAction = null;
                    if (folder.ProjectItems.ContainsItem(localFile)) {
                        ProjectItem existingItem = folder.ProjectItems.Item(localFile);
                        conflictResolveAction = new Action(() => { existingItem.Delete(); });
                    } else {
                        conflictResolveAction = new Action(() => { File.Delete(localFile); });
                    }     

                    DialogResult result = VisualLocalizer.Library.MessageBox.Show(string.Format("Item \"{0}\" already exists. Do you want to overwrite the file?", fileName), null, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_QUERY);
                    string fullPath = localFile;
                    if (result == DialogResult.Yes) {
                        if (conflictResolveAction != null) {
                            conflictResolveAction();
                        }

                        if (folder != null) {
                            ProjectItem newItem = folder.ProjectItems.AddFromFileCopy(file);
                            setBuildAction(newItem, prjBuildAction.prjBuildActionNone);
                            fullPath = newItem.GetFullPath();
                        } else {
                            fullPath = file;
                        }
                    } else {
                        if (!folder.ProjectItems.ContainsItem(Path.GetFileName(localFile))) {
                            ProjectItem newItem = folder.ProjectItems.AddFromFile(localFile);
                            setBuildAction(newItem, prjBuildAction.prjBuildActionNone);
                        }
                    }

                    if (list.Items.ContainsKey(fullPath)) {
                        addedListItem = list.UpdateDataOf(fullPath);
                    } else {
                        addedListItem = addExistingItem(list, fullPath, type, showThumbnails);
                    }

                    list.Refresh();
                    list.NotifyDataChanged();                    
                }
            } else {
                if (folder == null) {
                    addedListItem = addExistingItem(list, file, type, showThumbnails);
                } else {
                    ProjectItem newItem = folder.ProjectItems.AddFromFileCopy(file);
                    setBuildAction(newItem, prjBuildAction.prjBuildActionNone);
                    string fullPath = newItem.GetFullPath();

                    addedListItem = addExistingItem(list, fullPath, type, showThumbnails);
                }
            }
            return addedListItem;
        }

        private void internalEmbeddedPaste(List<object> list, List<AbstractListView> dataTabItems) {
            ListViewItemsAddUndoUnit unit = null;
            List<ListViewKeyItem> newItems = null;
            try {
                newItems = new List<ListViewKeyItem>();
                unit = new ListViewItemsAddUndoUnit(this, newItems, conflictResolver);               

                foreach (ResXDataNode o in list) {
                    foreach (var item in dataTabItems) {
                        if (item.CanContainItem(o)) {
                            string name = GetNextCopyName(o.Name);
                            bool contains = true;
                            while (contains) {
                                contains = item.ItemFromName(name) != null;
                                if (contains) name = GetNextCopyName(name);
                            }

                            ListViewKeyItem newItem = (ListViewKeyItem)item.Add(name, o, true);
                            newItems.Add(newItem);

                            break;
                        }
                    }
                }
            } finally {
                if (unit != null) Editor.AddUndoUnit(unit);

                VLOutputWindow.VisualLocalizerPane.WriteLine("Pasted {0} embedded elements", newItems.Count);
                stringGrid.NotifyDataChanged();
            }
        }

        private void internalSolExpPaste(MemoryStream memoryStream, List<AbstractListView> dataTabItems) {
            byte[] buffer = new byte[memoryStream.Length];
            memoryStream.Read(buffer, 0, buffer.Length);

            string text = Encoding.UTF8.GetString(buffer);
            List<string> paths = new List<string>();
            StringBuilder dataBuilder = new StringBuilder();

            char prevChar = '?';
            foreach (char c in text) {
                if (c != '\0' || prevChar == '\0') dataBuilder.Append(c);
                prevChar=c;
            }

            string[] data = dataBuilder.ToString().Split(Path.GetInvalidPathChars(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string path in data) {
                if (File.Exists(path)) {
                    paths.Add(path);
                }
            }

            addExistingFiles(paths);
        }   

        private void setBuildAction(ProjectItem item, prjBuildAction prjBuildAction) {
            if (item == null) return;
            if (item.ContainingProject.Kind.ToUpperInvariant() == StringConstants.WebSiteProject) return;
            
            try {
                item.Properties.Item("BuildAction").Value = prjBuildAction;
            } catch (Exception) {
                VLOutputWindow.VisualLocalizerPane.WriteLine("Error setting BuildAction of " + item.Name);
            }
        }

        private string GenerateCopyFileName(string file) {
            string dir = Path.GetDirectoryName(file);
            string name = Path.GetFileNameWithoutExtension(file);
            string ext = Path.GetExtension(file);
            string newName = file;

            while (File.Exists(newName)) {
                name = GetNextCopyName(name);
                newName = Path.Combine(dir, name + ext);
            }

            return Path.GetFullPath(newName);
        }

        private string GetNextCopyName(string name) {
            Match match = Regex.Match(name, "(.*)(\\d{1,})$");
            if (match.Success) {
                int i = int.Parse(match.Groups[2].Value);
                i++;
                name = string.Format("{0}{1}", match.Groups[1].Value, i);
            } else {
                name += "1";
            }
            return name;
        }

        private ListViewKeyItem addExistingItem(AbstractListView list, string fullPath, Type type, bool showThumbnails) {                        
            string name = Path.GetFileNameWithoutExtension(fullPath).CreateIdentifier(Editor.ProjectItem.DesignerLanguage);

            ResXDataNode node = new ResXDataNode(name, new ResXFileRef(fullPath, type.AssemblyQualifiedName));
            ListViewKeyItem newItem = list.Add(name, node, showThumbnails) as ListViewKeyItem;
            list.NotifyDataChanged();
            list.NotifyItemsStateChanged();

            return newItem;
        }

        private void addNewString(object sender, EventArgs e) {
            tabs.SelectedTab = stringTab;
            stringGrid.ClearSelection();

            stringGrid.Rows.Add();

            DataGridViewCell cell = stringGrid.Rows[stringGrid.Rows.Count - 2].Cells[stringGrid.KeyColumnName]; 
            cell.Value = "(new)";
            cell.Selected = true;

            stringGrid.CurrentCell = cell;
            stringGrid.BeginEdit(true);
            cell.Tag = null;

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

                    // TODO - fileref
                    ListViewKeyItem newItem;
                    if (!solution.ContainsProjectItem(Editor.ProjectItem.InternalProjectItem)) {
                        newItem = addNewImageNoSolution(imageName, resourceType, listView, resourceSubfolder, win);
                    } else {
                        newItem = addNewImageWithSolution(imageName, solution, resourceType, listView, resourceSubfolder, win);
                    }

                    ListViewNewItemCreateUndoUnit unit = new ListViewNewItemCreateUndoUnit(this, newItem, conflictResolver);
                    Editor.AddUndoUnit(unit);

                    VLOutputWindow.VisualLocalizerPane.WriteLine("Created and added new object \"{0}\"", newItem.DataNode.FileRef.FileName);
                } catch (Exception ex) {
                    VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                    VisualLocalizer.Library.MessageBox.ShowException(ex);
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
                string imagesFolderPath = imagesFolder.GetFullPath();
                string newImagePath = Path.Combine(imagesFolderPath, imageName);

                if (imagesFolder.ProjectItems.ContainsItem(imageName))
                    throw new Exception(string.Format("File \"{0}\" already exists!", imageName));

                Bitmap bmp = new Bitmap(win.ImageWidth, win.ImageHeight);
                bmp.Save(newImagePath, win.ImageFormat.Value);
                bmp.Dispose();

                ProjectItem newImageItem = imagesFolder.ProjectItems.AddFromFile(newImagePath);
                Window newImageWindow = newImageItem.Open(null);
                if (newImageWindow != null) newImageWindow.Activate();

                setBuildAction(newImageItem, prjBuildAction.prjBuildActionNone);                

                return addExistingItem(listView, newImagePath, resourceType, true);
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

            return addExistingItem(listView, newImagePath, resourceType, true);
        }

        #endregion

        #region private - listeners

        private void Instance_RevalidationRequested() {
            foreach (ResXStringGridRow row in stringGrid.Rows) {
                if (row.IsNewRow) continue;
                row.Cells[stringGrid.KeyColumnName].Tag = row.Cells[stringGrid.KeyColumnName].Value;
                stringGrid.ValidateRow(row);
            }
            validateListView(imagesListView);
            validateListView(soundsListView);
            validateListView(iconsListView);
            validateListView(filesListView);
        }

        private void validateListView(AbstractListView view) {
            foreach (ListViewKeyItem item in view.Items) {
                item.BeforeEditValue = item.AfterEditValue = item.Key;
                view.Validate(item);
            }
        }

        private void updateKeysButton_Click(object sender, EventArgs e) {
            string myNeutralName = Editor.ProjectItem.GetCultureNeutralName();
            try {                
                ProjectItem parentItem = null;
                foreach (ProjectItem projectItem in Editor.ProjectItem.InternalProjectItem.Collection)
                    if (projectItem.Name == myNeutralName) {
                        parentItem = projectItem;
                        break;
                    }

                ResXProjectItem resxParent = ResXProjectItem.ConvertToResXItem(parentItem, parentItem.ContainingProject);

                int rowsAdded = 0;
                foreach (var pair in resxParent.GetAllStringReferences(false)) {
                    if (Editor.ProjectItem.GetKeyConflictType(pair.Key, pair.Value) == CONTAINS_KEY_RESULT.DOESNT_EXIST) {
                        ResXStringGridRow newRow = (ResXStringGridRow)stringGrid.Add(pair.Key, new ResXDataNode(pair.Key, pair.Value), true);
                        stringGrid.StringRowAdded(newRow);
                        rowsAdded++;
                    }
                }

                if (rowsAdded == 0) {
                    VLOutputWindow.VisualLocalizerPane.WriteLine("Synchronize successful - no updates from \"{0}\"", parentItem.Name);
                } else {
                    VLOutputWindow.VisualLocalizerPane.WriteLine("Synchronize successful - added {0} rows from \"{1}\"", rowsAdded, parentItem.Name);
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteLine("Synchronize error, from \"{0}\", text: {1}", myNeutralName, ex.Message);
            }            
        }

        private void profferKeysButton_Click(object sender, EventArgs e) {
            try {
                string childrenString = null;
                foreach (ProjectItem projectItem in Editor.ProjectItem.InternalProjectItem.Collection)
                    if (projectItem.IsCultureSpecificResX() && projectItem.GetResXCultureNeutralName() == Editor.ProjectItem.InternalProjectItem.Name) {
                        string fullPath = projectItem.GetFullPath();
                        bool wasUpdated = false;

                        if (RDTManager.IsFileOpen(fullPath)) {
                            Dictionary<string, ResXDataNode> data = null; 
                            VLDocumentViewsManager.LoadDataFromBuffer(ref data, fullPath);

                            foreach (var pair in stringGrid.GetData(true)) {                                
                                if (!data.ContainsKey(pair.Key)) {
                                    data.Add(pair.Key, pair.Value);
                                    wasUpdated = true;
                                }
                            }

                            VLDocumentViewsManager.SaveDataToBuffer(data, fullPath);
                        } else {
                            ResXProjectItem resxChild = ResXProjectItem.ConvertToResXItem(projectItem, projectItem.ContainingProject);
                            
                            resxChild.BeginBatch();
                            foreach (var pair in stringGrid.GetData(true)) {
                                if (resxChild.GetKeyConflictType(pair.Key,pair.Value.GetValue<string>())==CONTAINS_KEY_RESULT.DOESNT_EXIST) {
                                    resxChild.AddString(pair.Key, pair.Value.GetValue<string>());
                                    wasUpdated = true;
                                }
                            }
                            resxChild.EndBatch();
                        }

                        if (wasUpdated) {
                            if (string.IsNullOrEmpty(childrenString)) {
                                childrenString = projectItem.Name;
                            } else {
                                childrenString += ", " + projectItem.Name;
                            }
                        }
                    }

                if (string.IsNullOrEmpty(childrenString)) {
                    VLOutputWindow.VisualLocalizerPane.WriteLine("Proffer OK, no changes needed");
                } else {
                    VLOutputWindow.VisualLocalizerPane.WriteLine("Proffer OK, updated {0}", childrenString);
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteLine("Proffer error, text: {0}", ex.Message);
            }
        }   

        private void translateButton_DropDownOpening(object sender, EventArgs eargs) {
            foreach (ToolStripMenuItem menuItem in translateButton.DropDownItems) {
                menuItem.DropDownItems.Clear();
                TRANSLATE_PROVIDER provider = (TRANSLATE_PROVIDER)menuItem.Tag;

                bool enabled = true;
                if (provider == TRANSLATE_PROVIDER.BING) {
                    enabled = !string.IsNullOrEmpty(SettingsObject.Instance.BingAppId);
                }

                menuItem.Enabled = enabled;

                foreach (var pair in SettingsObject.Instance.LanguagePairs) {
                    ToolStripMenuItem newItem = new ToolStripMenuItem(pair.ToString());
                    newItem.Tag = pair;
                    newItem.Click += new EventHandler((o, e) => {
                        SettingsObject.LanguagePair sentPair = (o as ToolStripMenuItem).Tag as SettingsObject.LanguagePair;
                        notifyTranslateRequested(provider, sentPair.FromLanguage, sentPair.ToLanguage);
                    });
                    newItem.Enabled = enabled;
                    menuItem.DropDownItems.Add(newItem);
                }

                ToolStripMenuItem addItem = new ToolStripMenuItem("New language pair...", null, new EventHandler((o, e) => {
                    notifyNewTranslatePairAdded(provider);
                }));
                addItem.Enabled = enabled;
                menuItem.DropDownItems.Add(addItem);   
            }
        }

        private void stringGrid_LanguagePairAdded(string sourceLanguage, string targetLanguage) {
            SettingsObject.LanguagePair newPair = new SettingsObject.LanguagePair() {
                FromLanguage = sourceLanguage,
                ToLanguage = targetLanguage
            };

            bool contains = false;
            foreach (var pair in SettingsObject.Instance.LanguagePairs)
                if (pair.Equals(newPair)) {
                    contains = true;
                    break;
                }

            if (!contains) {
                SettingsObject.Instance.LanguagePairs.Add(newPair);
                SettingsObject.Instance.NotifyPropertyChanged(CHANGE_CATEGORY.EDITOR);

                VLOutputWindow.VisualLocalizerPane.WriteLine("Added new language pair \"{0}\"", newPair);
            } 
        }

        private void ViewCheckStateChanged(object sender, EventArgs e) {
            ToolStripMenuItem senderItem = sender as ToolStripMenuItem;
            if (senderItem.CheckState == CheckState.Unchecked) return;

            foreach (ToolStripMenuItem item in viewButton.DropDownItems)
                if (item != senderItem) item.CheckState = CheckState.Unchecked;

            notifyViewKindChanged((View)senderItem.Tag);
        }        

        private void noFocusBoxSelectedIndexChanged(object sender, EventArgs e) {
            toolStrip.Focus();
        }                

        private void inlineButton_ButtonClick(object sender, EventArgs e) {
            notifyInlineRequested(INLINEKIND.INLINE);
        }

        private void inlineAndRemoveButton_ButtonClick(object sender, EventArgs e) {
            notifyInlineRequested(INLINEKIND.INLINE | INLINEKIND.REMOVE);
        }

        private void mergeButton_PreserveClick(object sender, EventArgs e) {
            MergeWithFile(false);
        }

        private void mergeButton_DeleteClick(object sender, EventArgs e) {
            MergeWithFile(true);
        }
        
        private string previousValue = null;       
        private void codeGenerationBox_SelectedIndexChanged(object sender, EventArgs e) {
            if (!VisualLocalizerPackage.Instance.DTE.Solution.ContainsProjectItem(Editor.ProjectItem.InternalProjectItem)) return;
            if (!codeGenerationBox.Enabled) return;
            if (Editor.ProjectItem.InternalProjectItem.ContainingProject.Kind.ToUpperInvariant() == StringConstants.WebSiteProject) return;

            try {
                ProjectItem documentItem = VisualLocalizerPackage.Instance.DTE.Solution.FindProjectItem(Editor.FileName);
                if (documentItem == null) throw new Exception("Cannot find corresponding project item.");

                ToolStripComboBox box = sender as ToolStripComboBox;

                try {
                    string selectedValue = (string)box.SelectedItem;
                    if (selectedValue == "Internal") {
                        documentItem.Properties.Item("CustomTool").Value = StringConstants.InternalResXTool;
                    } else if (selectedValue == "Public") {
                        documentItem.Properties.Item("CustomTool").Value = StringConstants.PublicResXTool;
                    } else {
                        documentItem.Properties.Item("CustomTool").Value = "";
                    }

                    if (box.Tag == null) {
                        AccessModifierChangeUndoUnit undoUnit = new AccessModifierChangeUndoUnit(box, previousValue, selectedValue);
                        Editor.AddUndoUnit(undoUnit);
                    }
                    box.Tag = null;
                    previousValue = selectedValue;

                    VLOutputWindow.VisualLocalizerPane.WriteLine("Changed access modifier to \"{0}\"", selectedValue);
                } catch (ArgumentException) {
                    codeGenerationBox.Enabled = false;
                    VLOutputWindow.VisualLocalizerPane.WriteLine("Error occured while setting the CustomTool property. Please use Properties Window instead.");
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        #endregion

        private string GetResXCodeGenerationMode() {
            if (!VisualLocalizerPackage.Instance.DTE.Solution.ContainsProjectItem(Editor.ProjectItem.InternalProjectItem)) return null;
            if (!codeGenerationBox.Enabled) return null;

            try {
                ProjectItem documentItem = VisualLocalizerPackage.Instance.DTE.Solution.FindProjectItem(Editor.FileName);
                if (documentItem == null) throw new Exception("Cannot find corresponding project item.");

                string value = documentItem.GetCustomTool();
                if (value == StringConstants.PublicResXTool) {
                    return "Public";
                } else if (value == StringConstants.InternalResXTool) {
                    return "Internal";
                } else {
                    return "No designer class";
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
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

        private void MergeWithFile(bool deleteSource) {
            ResXResourceReader reader = null;
            try {
                string[] files = VisualLocalizer.Library.MessageBox.SelectFilesViaDlg("Select file", Path.GetDirectoryName(Editor.FileName),
                    "ResX file\0*.resx\0", 0, 0);
                if (files == null) return;
                if (files.Length != 1) throw new Exception("Exactly one file must be selected!");

                string file = files[0];
                if (Path.GetFullPath(file) == Path.GetFullPath(Editor.FileName)) throw new Exception("Cannot select same file as the one being edited!");

                if (deleteSource) {
                    DialogResult result = VisualLocalizer.Library.MessageBox.Show(string.Format("You have chosen to delete source file \"{0}\". Do you really want to do so?", Path.GetFileName(file)), null, OLEMSGBUTTON.OLEMSGBUTTON_YESNOCANCEL, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_THIRD, OLEMSGICON.OLEMSGICON_WARNING);
                    if (result == DialogResult.Cancel) return;
                    if (result == DialogResult.No) deleteSource = false;
                }

                List<IDataTabItem> dataTabItems = new List<IDataTabItem>();

                foreach (TabPage page in this.tabs.TabPages) {
                    IDataTabItem content = GetContentFromTabPage(page);
                    if (content != null) {
                        IDataTabItem tabItem = content as IDataTabItem;
                        dataTabItems.Add(tabItem);
                    }
                }

                IEnumerable enumarable;
                if (RDTManager.IsFileOpen(file)) {
                    Dictionary<string, ResXDataNode> data = null;
                    VLDocumentViewsManager.LoadDataFromBuffer(ref data, file);

                    enumarable = (IEnumerable)data;
                } else {
                    reader = new ResXResourceReader(file);
                    reader.BasePath = Path.GetDirectoryName(file);
                    reader.UseResXDataNodes = true;

                    enumarable = (IEnumerable)reader;
                }

                Stack<IOleUndoUnit> units = new Stack<IOleUndoUnit>();

                foreach (object o in enumarable) {
                    foreach (var item in dataTabItems) {
                        ResXDataNode node = (o is DictionaryEntry) ? ((DictionaryEntry)o).Value as ResXDataNode : ((KeyValuePair<string, ResXDataNode>)o).Value;
                        string key = (o is DictionaryEntry) ? ((DictionaryEntry)o).Key.ToString() : ((KeyValuePair<string, ResXDataNode>)o).Key;

                        if (item.CanContainItem(node)) {
                            IKeyValueSource newItem = item.Add(key, node, true);
                            if (newItem is ResXStringGridRow) {
                                StringRowAddUndoUnit undoUnit = new StringRowAddUndoUnit(this,
                                    new List<ResXStringGridRow>() { newItem as ResXStringGridRow }, stringGrid, conflictResolver);
                                units.Push(undoUnit);
                            } else {
                                ListViewItemsAddUndoUnit undoUnit = new ListViewItemsAddUndoUnit(this,
                                    new List<ListViewKeyItem>() { newItem as ListViewKeyItem }, conflictResolver);
                                units.Push(undoUnit);
                            }

                            item.NotifyDataChanged();
                            item.NotifyItemsStateChanged();
                            break;
                        }
                    }
                }                

                MergeUndoUnit unit = new MergeUndoUnit(Path.GetFileName(file), units);
                Editor.AddUndoUnit(unit);
                VLOutputWindow.VisualLocalizerPane.WriteLine("Merged files \"{0}\" and \"{1}\"", Editor.FileName, file);

                if (deleteSource) {
                    ProjectItem item = VisualLocalizerPackage.Instance.DTE.Solution.FindProjectItem(file);
                    if (item != null) item.Delete();
                    File.Delete(file);
                    VLOutputWindow.VisualLocalizerPane.WriteLine("Deleted file after merge: \"{1}\"", file);
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            } finally {
                if (reader != null) reader.Close();
            }
        }

        private void notifyViewKindChanged(View newView) {
            if (ViewKindChanged != null) ViewKindChanged(newView);
        }

        private void UpdateToolStripButtonsEnable(object sender, EventArgs e) {
            bool selectedString = stringGrid.Visible;
            IDataTabItem item = GetContentFromTabPage(tabs.SelectedTab);

            inlineButton.Enabled = selectedString && item.HasSelectedItems && !item.IsEditing && !readOnly && stringGrid.AreReferencesKnownOnSelected;
            removeDeleteItem.Enabled = !selectedString && item.HasSelectedItems && !item.IsEditing && !readOnly;
            removeExcludeItem.Enabled = !selectedString && item.HasSelectedItems && !item.IsEditing && !readOnly;
            removeButton.Enabled = item.HasSelectedItems && !item.IsEditing && !readOnly;
            viewButton.Enabled = !selectedString && !item.IsEditing;
            addButton.Enabled = !readOnly;
            translateButton.Enabled = selectedString && item.HasSelectedItems && !item.IsEditing && !readOnly;
            mergeButton.Enabled = !readOnly;

            if (Editor.ProjectItem != null) {
                bool specific = Editor.ProjectItem.IsCultureSpecific();

                if (specific) {                    
                    bool parentExists = Editor.ProjectItem.InternalProjectItem.Collection.ContainsItem(Editor.ProjectItem.GetCultureNeutralName());
                    updateKeysButton.Enabled = selectedString &&!item.IsEditing && !readOnly && parentExists;
                } else {
                    bool childExists = false;
                    foreach (ProjectItem projectItem in Editor.ProjectItem.InternalProjectItem.Collection)
                        if (projectItem.IsCultureSpecificResX() && projectItem.GetResXCultureNeutralName() == Editor.ProjectItem.InternalProjectItem.Name) {
                            childExists = true;
                            break;
                        }
                    profferKeysButton.Enabled = selectedString && !item.IsEditing && !readOnly && childExists;
                }

                profferKeysButton.Visible = !specific;
                updateKeysButton.Visible = specific;
            } else {
                profferKeysButton.Visible = false;                
                updateKeysButton.Visible = false;
            }            
        }

        private void notifyNewTranslatePairAdded(TRANSLATE_PROVIDER provider) {
            if (NewTranslatePairAdded != null) NewTranslatePairAdded(provider);
        }

        private void notifyTranslateRequested(TRANSLATE_PROVIDER provider, string fromLanguage, string toLanguage) {
            if (TranslateRequested != null) TranslateRequested(provider, fromLanguage, toLanguage);
        }

        private void notifyInlineRequested(INLINEKIND inlineKind) {
            if (InlineRequested != null) InlineRequested(inlineKind);
        }
    }

}
    
