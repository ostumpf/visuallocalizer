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

    /// <summary>
    /// Options for removing items
    /// </summary>
    [Flags]
    internal enum REMOVEKIND { 
        /// <summary>
        /// Item should be removed from the list/grid
        /// </summary>
        REMOVE = 1, 

        /// <summary>
        /// Referenced resource should be excluded from the project
        /// </summary>
        EXCLUDE = 2, 

        /// <summary>
        /// Referenced resource should be deleted from disk
        /// </summary>
        DELETE_FILE = 4 
    }

    /// <summary>
    /// Options for inlining items
    /// </summary>
    [Flags]
    internal enum INLINEKIND { 
        /// <summary>
        /// Item should be inlined
        /// </summary>
        INLINE = 1, 

        /// <summary>
        /// Item should be removed from the grid
        /// </summary>
        REMOVE = 2 
    }

    /// <summary>
    /// Used to determine origin of the added files
    /// </summary>
    internal enum FILES_ORIGIN { 
        /// <summary>
        /// File list in clipboard
        /// </summary>
        CLIPBOARD_REF, 

        /// <summary>
        /// Objects in clipboard
        /// </summary>
        CLIPBOARD_EMB, 

        /// <summary>
        /// Drag'n'drop from Solution Explorer
        /// </summary>
        SOLUTION_EXPLORER, 

        /// <summary>
        /// "Make external" command
        /// </summary>
        MAKE_EXTERNAL 
    }

    /// <summary>
    /// Represents GUI of the ResX editor
    /// </summary>
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

        /// <summary>
        /// Invoked when data changed in the editor
        /// </summary>
        public event EventHandler DataChanged;

        /// <summary>
        /// Invoked when "Remove" button on the toolbar was used
        /// </summary>
        public event Action<REMOVEKIND> RemoveRequested;

        /// <summary>
        /// Invoked when "View" was modified on the toolbar
        /// </summary>
        public event Action<View> ViewKindChanged;

        /// <summary>
        /// Invoked when "New language pair" menu item was clicked
        /// </summary>
        public event Action<TRANSLATE_PROVIDER> NewTranslatePairAdded;

        /// <summary>
        /// Invoked when "Translate" button was clicked on the toolbar
        /// </summary>
        public event Action<TRANSLATE_PROVIDER, string, string> TranslateRequested;

        /// <summary>
        /// Invoked when "Inline" button was clicked on the toolbar
        /// </summary>
        public event Action<INLINEKIND> InlineRequested;

        /// <summary>
        /// Key names conflict resolver used in whole editor instance
        /// </summary>
        public KeyValueIdentifierConflictResolver conflictResolver;

        /// <summary>
        /// ReferenceLister used to lookup references to resources in this file
        /// </summary>
        private ReferenceLister referenceLister;

        /// <summary>
        /// True if the reference lookuper should not be run
        /// </summary>
        public bool ReferenceCounterThreadSuspended = false;

        /// <summary>
        /// Background thread that handles the references lookup
        /// </summary>
        private System.Threading.Thread referenceUpdaterThread;

        /// <summary>
        /// Synchronization object for the reference lookup thread
        /// </summary>
        private static object LookuperThreadLockObject = new object();

        /// <summary>
        /// List of files that had to be force-opened to be searched for references and are now closed; their references are not deleted from each resource's list
        /// </summary>
        private HashSet<string> registeredAsIgnoredList;

        /// <summary>
        /// True if reference lookup finished at least once
        /// </summary>
        private bool referenceUpdaterThreadCompleted = false;

        /// <summary>
        /// The source files previously ignored, but now edited and closed again
        /// </summary>
        public HashSet<string> sourceFilesThatNeedUpdate = new HashSet<string>();

        /// <summary>
        /// Initializes the GUI
        /// </summary>        
        public void Init<T>(AbstractSingleViewEditor<T> editor) where T : Control, IEditorControl, new() {
            try {
                this.Editor = editor as ResXEditor;
                this.registeredAsIgnoredList = new HashSet<string>();
                VLDocumentViewsManager.FileClosed += new Action<string>(VLDocumentViewsManager_FileClosed);
                this.Disposed += new EventHandler(ResXEditorControl_Disposed);

                // initialize the reference lookuper thread
                this.referenceLister = new ReferenceLister();

                this.referenceUpdaterThread = new System.Threading.Thread(ReferenceLookuperThread);
                this.referenceUpdaterThread.IsBackground = true;
                this.referenceUpdaterThread.Priority = System.Threading.ThreadPriority.BelowNormal;

                this.Dock = DockStyle.Fill;
                this.DoubleBuffered = true;
                this.conflictResolver = new KeyValueIdentifierConflictResolver(true, false);

                InitToolStrip();
                InitTabControl();

                this.RowCount = 2;
                this.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                this.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                this.ColumnCount = 1;
                this.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                this.Controls.Add(toolStrip, 0, 0);
                this.Controls.Add(tabs, 0, 1);

                this.Cursor = Cursors.Default;

                // revalidate the keys on RevalidationRequested event
                SettingsObject.Instance.RevalidationRequested += new Action(Instance_RevalidationRequested);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Closes all invisible windows opened by this editor instance
        /// </summary>        
        private void ResXEditorControl_Disposed(object sender, EventArgs e) {
            try {
                VLDocumentViewsManager.CloseInvisibleWindows(Editor, false);
            } catch { }
        }

        
        /// <summary>
        /// Called when a file in VS is closed
        /// </summary>        
        private void VLDocumentViewsManager_FileClosed(string path) {
            if (!sourceFilesThatNeedUpdate.Contains(path.ToLower())) {
                sourceFilesThatNeedUpdate.Add(path.ToLower());
            }
        }       

        /// <summary>
        /// Returns instance of the editor
        /// </summary>
        public ResXEditor Editor {
            get;
            private set;
        }        

        /// <summary>
        /// Initializes the toolstrip GUI
        /// </summary>
        private void InitToolStrip() {
            ToolStripManager.Renderer = new ToolStripProfessionalRenderer(new VsColorTable());

            toolStrip = new ToolStrip();
            toolStrip.Dock = DockStyle.Top;

            addButton = new ToolStripSplitButton("&Add Resource");
            addButton.Image = VisualLocalizer.Editor.Editor.add;
            addButton.TextAlign = ContentAlignment.MiddleCenter;
            addButton.ButtonClick += new EventHandler(AddExistingResources);
            addButton.DropDownItems.Add("Existing File", null, new EventHandler(AddExistingResources));
            addButton.DropDownItems.Add(new ToolStripSeparator());
            ToolStripMenuItem newItem = new ToolStripMenuItem("New");
            newItem.DropDownItems.Add("String",null,new EventHandler(AddNewString));
            newItem.DropDownItems.Add("Icon", null, new EventHandler((o, e) => { AddNewImage(typeof(Icon), iconsListView, "Icons"); }));
            newItem.DropDownItems.Add("Image", null, new EventHandler((o, e) => { AddNewImage(typeof(Bitmap), imagesListView, "Images"); }));
            addButton.DropDownItems.Add(newItem);
            toolStrip.Items.Add(addButton);

            mergeButton = new ToolStripDropDownButton("&Merge with ResX File");
            mergeButton.Image = VisualLocalizer.Editor.Editor.merge;
            mergeButton.TextAlign = ContentAlignment.MiddleCenter;
            mergeButton.DropDownItems.Add("Merge && &Preserve Both", null, new EventHandler(MergeButton_PreserveClick));
            mergeButton.DropDownItems.Add("Merge && &Delete Source", null, new EventHandler(MergeButton_DeleteClick));
            toolStrip.Items.Add(mergeButton);

            toolStrip.Items.Add(new ToolStripSeparator());

            profferKeysButton = new ToolStripButton("Proffer");
            profferKeysButton.Image = VisualLocalizer.Editor.Editor.sync;
            profferKeysButton.Click += new EventHandler(ProfferKeysButton_Click);
            toolStrip.Items.Add(profferKeysButton);

            updateKeysButton = new ToolStripButton("Synchronize");
            updateKeysButton.Image = VisualLocalizer.Editor.Editor.sync;
            updateKeysButton.Click += new EventHandler(UpdateKeysButton_Click);
            toolStrip.Items.Add(updateKeysButton);

            toolStrip.Items.Add(new ToolStripSeparator());

            removeButton = new ToolStripSplitButton("&Remove");
            removeButton.Image = VisualLocalizer.Editor.Editor.remove;
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
            inlineButton.ButtonClick += new EventHandler(InlineButton_ButtonClick);
            inlineButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            inlineButton.DropDownItems.Add("Inline && &remove", null, new EventHandler(InlineAndRemoveButton_ButtonClick)); 
            toolStrip.Items.Add(inlineButton);

            translateButton = new ToolStripDropDownButton("&Translate");
            translateButton.Image = VisualLocalizer.Editor.Editor.translate;
            translateButton.DropDownOpening += new EventHandler(TranslateButton_DropDownOpening);

            foreach (TRANSLATE_PROVIDER prov in Enum.GetValues(typeof(TRANSLATE_PROVIDER))) {
                ToolStripMenuItem menuItem = new ToolStripMenuItem(prov.ToHumanForm());
                menuItem.Tag = prov;
                translateButton.DropDownItems.Add(menuItem);
            }
                    
            translateButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            toolStrip.Items.Add(translateButton);

            toolStrip.Items.Add(new ToolStripSeparator());

            viewButton = new ToolStripDropDownButton("&View");
            viewButton.Image = VisualLocalizer.Editor.Editor.view;
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
            codeGenerationBox.SelectedIndexChanged += new EventHandler(NoFocusBoxSelectedIndexChanged);
            codeGenerationBox.SelectedIndexChanged += new EventHandler(CodeGenerationBox_SelectedIndexChanged);
            codeGenerationBox.Margin = new Padding(2);
            
            toolStrip.Items.Add(codeGenerationBox);
        }          
       
        /// <summary>
        /// Initializes the tabs GUI
        /// </summary>
        private void InitTabControl() {
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
            stringGrid.LanguagePairAdded += new Action<string, string>(StringGrid_LanguagePairAdded);
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

        /// <summary>
        /// Displays given list of code references in the BatchInlineToolWindow
        /// </summary>     
        public void ShowReferences(List<CodeReferenceResultItem> selected) {
            if (selected == null) throw new ArgumentNullException("selected");

            ShowReferencesToolWindow window = VisualLocalizerPackage.Instance.menuManager.ShowToolWindow<ShowReferencesToolWindow>();
            window.SetData(selected);
        }

        /// <summary>
        /// Returns IDataTabItem for given page
        /// </summary>        
        public IDataTabItem GetContentFromTabPage(TabPage page) {
            if (page == null) throw new ArgumentNullException("page");

            Control content = page.Controls.ContainsKey("Content") ? page.Controls["Content"] : null;
            if (content is IDataTabItem && content != null)
                return content as IDataTabItem;
            else
                throw new InvalidOperationException("Cannot obtain content of " + page.Text);
        }

        /// <summary>
        /// Places given data in respective tabs
        /// </summary>        
        public void SetData(Dictionary<string, ResXDataNode> data) {
            if (data == null) throw new ArgumentNullException("data");

            conflictResolver.Clear(); // clear cached conflict items info

            // disable "Access modifier" checkbox if file is not a part of solution
            if (Editor.ProjectItem ==null || (Editor.ProjectItem.InternalProjectItem.ContainingProject != null && Editor.ProjectItem.InternalProjectItem.ContainingProject.Kind.ToUpper() == StringConstants.WebSiteProject))
                codeGenerationBox.Enabled = false;

            codeGenerationBox.Tag = SELECTION_CHANGE_INITIATOR.INITIALIZER;
            codeGenerationBox.SelectedItem = GetResXCodeGenerationMode(); // get current "Access modifier"

            List<IDataTabItem> dataTabItems = new List<IDataTabItem>();
            ReferenceCounterThreadSuspended = true; // suspend the reference lookuper thread

            // call BeginAdd() on each page
            foreach (TabPage page in this.tabs.TabPages) {
                IDataTabItem content = GetContentFromTabPage(page);
                if (content != null) {
                    IDataTabItem tabItem = content as IDataTabItem;

                    dataTabItems.Add(tabItem);
                    tabItem.BeginAdd();
                }
            }

            // determine in which page the ResX node should be placed ("Files" can contain any data)
            foreach (var pair in data) {
                foreach (var item in dataTabItems) {
                    if (item.CanContainItem(pair.Value)) {
                        item.Add(pair.Key, pair.Value);
                        break;
                    }
                }
            }

            // call EndAdd() on each page
            foreach (IDataTabItem tabItem in dataTabItems)
                tabItem.EndAdd();

            ReferenceCounterThreadSuspended = false; // resume reference lookuper thread
            if (!referenceUpdaterThread.IsAlive) referenceUpdaterThread.Start(); 
        }

        /// <summary>
        /// Collects data from the pages
        /// </summary>
        /// <param name="throwExceptions">True if exception should be thrown in case some item is not valid</param>        
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

        /// <summary>
        /// Updates reference count for given list of items
        /// </summary>        
        public void UpdateReferencesCount(IEnumerable items) {
            if (items == null) throw new ArgumentNullException("items");
            if (ReferenceCounterThreadSuspended) return;
            lock (LookuperThreadLockObject) {
                ResXProjectItem resxItem = Editor.ProjectItem;

                // if edited file is part of solution
                if (resxItem != null && resxItem.InternalProjectItem.ContainingProject != null && VisualLocalizerPackage.Instance.DTE.Solution.ContainsProjectItem(resxItem.InternalProjectItem)) {
                    // get ResX project items
                    Project containingProject = resxItem.InternalProjectItem.ContainingProject;
                    resxItem.ResolveNamespaceClass(containingProject.GetResXItemsAround(false, true));
                    
                    // create list of all projects in which references will be seeked
                    List<Project> projects = new List<Project>();
                    projects.Add(containingProject); // add this project
                    foreach (Project solutionProject in VisualLocalizerPackage.Instance.DTE.Solution.Projects) {
                        foreach (Project proj in solutionProject.GetReferencedProjects()) { // add referenced projects
                            if (proj == containingProject) {
                                projects.Add(solutionProject);
                                break;
                            }
                        }
                    }

                    if (resxItem.DesignerItem == null && !resxItem.HasImplicitDesignerFile) { // this indicates an error
                        foreach (IReferencableKeyValueSource item in items) {
                            item.CodeReferences.Clear();
                            item.UpdateReferenceCount(false);
                        }
                    } else {
                        // build trie
                        Trie<CodeReferenceTrieElement> trie = new Trie<CodeReferenceTrieElement>();
                        foreach (IReferencableKeyValueSource item in items) {
                            if (!string.IsNullOrEmpty(item.LastValidKey)) {
                                string referenceKey = item.LastValidKey.CreateIdentifier(resxItem.DesignerLanguage);

                                var element = trie.Add(resxItem.Class + "." + referenceKey);
                                element.Infos.Add(new CodeReferenceInfo() { Origin = resxItem, Value = item.Value, Key = item.LastValidKey });
                            }
                        }
                        trie.CreatePredecessorsAndShortcuts();

                        registeredAsIgnoredList.Clear();
                        referenceLister.Process(Editor, projects, trie, resxItem, !referenceUpdaterThreadCompleted); // run lookuper

                        // display results
                        foreach (IReferencableKeyValueSource item in items) {
                            item.CodeReferences.RemoveAll((i) => { return !registeredAsIgnoredList.Contains(i.SourceItem.GetFullPath().ToLower()); });
                            item.CodeReferences.AddRange(referenceLister.Results.Where((i) => {
                                return i.Key == item.LastValidKey;
                            }));
                            item.UpdateReferenceCount(true);
                        }

                        referenceUpdaterThreadCompleted = true;
                        VLDocumentViewsManager.CloseInvisibleWindows(Editor, false);
                    }
                }
            }
        }

        /// <summary>
        /// Sets this editor as readonly
        /// </summary>        
        public void SetReadOnly(bool readOnly) {
            try {
                foreach (TabPage page in tabs.TabPages) {
                    IDataTabItem item = GetContentFromTabPage(page);
                    item.DataReadOnly = readOnly;
                }
                this.readOnly = readOnly;
                UpdateToolStripButtonsEnable(null, null);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// True if this editor instance is readonly or locked
        /// </summary>
        public bool ReadOnly {
            get {
                return readOnly;
            }
        }

        /// <summary>
        /// Executes Paste with clipboard data
        /// </summary>
        /// <returns>True if operation was successful</returns>
        public bool ExecutePaste() {
            return ExecutePaste(Clipboard.GetDataObject());
        }

        /// <summary>
        /// Executes Paste with given data object
        /// </summary>
        /// <returns>True if operation was successful</returns>
        public bool ExecutePaste(System.Windows.Forms.IDataObject iData) {
            try {
                if (iData == null) throw new ArgumentNullException("iData");

                if (iData.GetDataPresent(StringConstants.FILE_LIST)) { // contains Windows Explorer-like file list
                    string[] files = (string[])iData.GetData(StringConstants.FILE_LIST);                    
                    AddExistingFiles(files, FILES_ORIGIN.CLIPBOARD_REF);
                    return true;
                } else if (iData.GetDataPresent(DataFormats.CommaSeparatedValue) && !iData.GetDataPresent(StringConstants.SOLUTION_EXPLORER_FILE_LIST)) {
                    // contains plain text
                    object o = iData.GetData(DataFormats.CommaSeparatedValue);
                    string text;

                    if (o is MemoryStream) {
                        MemoryStream ms = (MemoryStream)o;
                        byte[] buffer = new byte[ms.Length];
                        ms.Read(buffer, 0, buffer.Length);
                        text = Encoding.Default.GetString(buffer);
                    } else {
                        text = o.ToString();
                    }

                    stringGrid.AddClipboardText(text, true);
                    tabs.SelectedTab = stringTab;
                    return true;
                } else if (iData.GetDataPresent("Text") && !iData.GetDataPresent(StringConstants.SOLUTION_EXPLORER_FILE_LIST)) {
                    // contains plain text
                    stringGrid.AddClipboardText((string)iData.GetData("Text"), false);
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

                    if (iData.GetDataPresent(typeof(List<object>))) { // embedded data added by this editor
                        InternalEmbeddedPaste((List<object>)iData.GetData(typeof(List<object>)), dataTabItems);                        
                        return true;
                    } else if (iData.GetDataPresent(StringConstants.SOLUTION_EXPLORER_FILE_LIST)) { // contains Solution Explorer file list
                        InternalSolExpPaste((MemoryStream)iData.GetData(StringConstants.SOLUTION_EXPLORER_FILE_LIST), dataTabItems);                        
                        return true;
                    } else return false;
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            } 
            return false;
        }            

        /// <summary>
        /// Executes Copy operation on selected tab
        /// </summary>
        /// <returns>True if operation was successful</returns>
        public bool ExecuteCopy() {
            try {
                IDataTabItem content = GetContentFromTabPage(tabs.SelectedTab);
                if (content != null) return content.Copy();
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
            return false;
        }

        /// <summary>
        /// Executes Cut operation on selected tab
        /// </summary>
        /// <returns>True if operation was successful</returns>
        public bool ExecuteCut() {
            try {
                IDataTabItem content = GetContentFromTabPage(tabs.SelectedTab);
                if (content != null) return content.Cut();                
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);                
            }
            return false;
        }

        /// <summary>
        /// Executes Select All operation on selected tab
        /// </summary>
        /// <returns>True if operation was successful</returns>
        public bool ExecuteSelectAll() {
            try {
                IDataTabItem content = GetContentFromTabPage(tabs.SelectedTab);
                if (content != null) return content.SelectAllItems();
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
            return false;
        }

        /// <summary>
        /// Returns CanCutOrCopy of the selected tab
        /// </summary>
        public COMMAND_STATUS CanCutOrCopy {
            get {
                try {
                    IDataTabItem content = GetContentFromTabPage(tabs.SelectedTab);
                    if (content != null) return content.CanCutOrCopy;
                } catch (Exception ex) {
                    VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                    VisualLocalizer.Library.MessageBox.ShowException(ex);
                }
                return COMMAND_STATUS.UNSUPPORTED;
            }
        }

        /// <summary>
        /// Returns CanPaste of the selected tab
        /// </summary>
        public COMMAND_STATUS CanPaste {
            get {
                try {
                    IDataTabItem content = GetContentFromTabPage(tabs.SelectedTab);
                    if (content != null) return content.CanPaste;
                } catch (Exception ex) {
                    VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                    VisualLocalizer.Library.MessageBox.ShowException(ex);
                }
                return COMMAND_STATUS.UNSUPPORTED;
            }
        }

        /// <summary>
        /// Returns true if Delete command can be performed on selected tab
        /// </summary>
        public COMMAND_STATUS CanDelete {
            get {
                try {
                    IDataTabItem content = GetContentFromTabPage(tabs.SelectedTab);
                    if (content != null) return content.HasSelectedItems && !content.IsEditing && !content.DataReadOnly ? COMMAND_STATUS.ENABLED : COMMAND_STATUS.DISABLED;
                } catch (Exception ex) {
                    VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                    VisualLocalizer.Library.MessageBox.ShowException(ex);
                }
                return COMMAND_STATUS.UNSUPPORTED;
            }
        }

        /// <summary>
        /// Returns true if Select All command can be performed on selected tab
        /// </summary>
        public COMMAND_STATUS CanSelectAll {
            get {
                try {
                    IDataTabItem content = GetContentFromTabPage(tabs.SelectedTab);
                    if (content != null) return content.HasItems && !content.IsEditing ? COMMAND_STATUS.ENABLED : COMMAND_STATUS.DISABLED;
                } catch (Exception ex) {
                    VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                    VisualLocalizer.Library.MessageBox.ShowException(ex);
                }
                return COMMAND_STATUS.UNSUPPORTED;
            }
        }

        /// <summary>
        /// Issue the RemoveRequested event
        /// </summary>        
        public void NotifyRemoveRequested(REMOVEKIND kind) {
            if (RemoveRequested != null) RemoveRequested(kind);
        }

        /// <summary>
        /// Called by GlobalTranslate command, adds string resources values to translation list
        /// </summary>        
        public void AddForTranslation(List<AbstractTranslateInfoItem> list) {
            if (list == null) throw new ArgumentNullException("list");
            stringGrid.AddToTranslationList(stringGrid.Rows, list);
        }

        /// <summary>
        /// Called from lookuper thread, when project item with no code model is found - its result items are fixed 
        /// </summary>        
        public void RegisterAsStaticReferenceSource(ProjectItem pitem) {
            if (pitem == null) throw new ArgumentNullException("pitem");
            string fullPath = pitem.GetFullPath().ToLower();

            if (!registeredAsIgnoredList.Contains(fullPath)) {
                registeredAsIgnoredList.Add(fullPath);
            }
        }

        /// <summary>
        /// Updates references count for specified item
        /// </summary>
        /// <param name="src"></param>
        public void UpdateReferencesCount(IReferencableKeyValueSource src) {
            if (src == null) throw new ArgumentNullException("src");
            UpdateReferencesCount(new List<IReferencableKeyValueSource>() { src });
        }

        #endregion

        #region private - adding resources        

        /// <summary>
        /// Displays dialog letting user choose the files, copies them (if appropriate) to project's folder and references them as resources.
        /// </summary>        
        private void AddExistingResources(object sender, EventArgs e) {
            try {
                string imageFilter = "*" + string.Join(";*", StringConstants.IMAGE_FILE_EXT);
                string iconFilter = "*" + string.Join(";*", StringConstants.ICON_FILE_EXT);
                string soundFilter = "*" + string.Join(";*", StringConstants.SOUND_FILE_EXT);
                uint selectedFilter = (uint)Math.Max(0, tabs.SelectedIndex - 1);

                string[] files = VisualLocalizer.Library.MessageBox.SelectFilesViaDlg("Select files", Path.GetDirectoryName(Editor.FileName),
                    string.Format("Image files({0})\0{0}\0Icon files({1})\0{1}\0Sound files({2})\0{2}\0", imageFilter, iconFilter, soundFilter), selectedFilter, OPENFILENAME.OFN_ALLOWMULTISELECT);
                if (files == null) return;

                AddExistingFiles(files, FILES_ORIGIN.CLIPBOARD_REF);                
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Adds given list of files as resources, creating folder structure Resources/Images, Resources/Icons etc.
        /// </summary>
        /// <param name="files">List of full paths to the files</param>
        internal void AddExistingFiles(IEnumerable<string> files, FILES_ORIGIN origin) {
            if (files == null) throw new ArgumentNullException("files");

            Project project = null;
            bool userDefinedSolution=VisualLocalizerPackage.Instance.DTE.Solution.ContainsProjectItem(Editor.ProjectItem.InternalProjectItem);
            if (userDefinedSolution) {
                project = VisualLocalizerPackage.Instance.DTE.Solution.FindProjectItem(Editor.FileName).ContainingProject;
            }

            ProjectItem imagesFolder = null, iconsFolder = null, soundFolder = null, filesFolder = null;
            List<ListViewKeyItem> newItems = new List<ListViewKeyItem>();

            foreach (string file in files) {
                if (Directory.Exists(file)) {
                    AddExistingFiles(Directory.GetFiles(file), origin);
                    continue;
                }

                string extension = Path.GetExtension(file);
                if (!string.IsNullOrEmpty(extension)) extension = extension.ToLower();

                if (StringConstants.IMAGE_FILE_EXT.Contains(extension)) {
                    if (imagesFolder == null && project != null) imagesFolder = project.AddResourceDir("Images"); // create Resources/Images folder
                    var newItem = AddExistingItem(imagesListView, imagesFolder, file, typeof(Bitmap), origin);
                    if (newItem != null) newItems.Add(newItem); // add the file
                    tabs.SelectedTab = imagesTab;
                } else if (StringConstants.ICON_FILE_EXT.Contains(extension)) {
                    if (iconsFolder == null && project != null) iconsFolder = project.AddResourceDir("Icons"); // create Resources/Icons folder
                    var newItem = AddExistingItem(iconsListView, iconsFolder, file, typeof(Icon), origin);
                    if (newItem != null) newItems.Add(newItem); // add the file                    
                    tabs.SelectedTab = iconsTab;
                } else if (StringConstants.SOUND_FILE_EXT.Contains(extension)) {
                    if (soundFolder == null && project != null) soundFolder = project.AddResourceDir("Sounds"); // create Resources/Sounds folder
                    var newItem = AddExistingItem(soundsListView, soundFolder, file, typeof(MemoryStream), origin);
                    if (newItem != null) newItems.Add(newItem); // add the file
                    tabs.SelectedTab = soundsTab;
                } else {
                    if (filesFolder == null && project != null) filesFolder = project.AddResourceDir("Others"); // create Resources/Others folder
                    if (StringConstants.TEXT_FILE_EXT.Contains(extension)) { // is text file
                        var newItem = AddExistingItem(filesListView, filesFolder, file, typeof(string), origin);
                        if (newItem != null) newItems.Add(newItem); // add the file
                    } else { // is binary file
                        var newItem = AddExistingItem(filesListView, filesFolder, file, typeof(byte[]), origin);
                        if (newItem != null) newItems.Add(newItem); // add the file
                    }
                    tabs.SelectedTab = filesTab;
                }
            }

            if (newItems.Count > 0) {
                // add undo unit
                ListViewItemsAddUndoUnit unit = new ListViewItemsAddUndoUnit(this, newItems, conflictResolver);
                Editor.AddUndoUnit(unit);

                VLOutputWindow.VisualLocalizerPane.WriteLine("Added {0} existing files", newItems.Count);
            }
        }

        /// <summary>
        /// Adds given file resource to the given tab and folder
        /// </summary>        
        /// <returns>The new item</returns>
        private ListViewKeyItem AddExistingItem(AbstractListView list, ProjectItem folder, string file, Type type, FILES_ORIGIN origin) {
            if (list == null) throw new ArgumentNullException("list");
            if (type == null) throw new ArgumentNullException("type");
            if (file == null) throw new ArgumentNullException("file");

            string fileName = Path.GetFileName(file);
            string localFile = null;
            bool localFileExists,fileSameAsLocalFile;            
            ListViewKeyItem addedListItem;

            if (folder == null) { // when this file is not a part of soluton
                localFileExists = false;
                fileSameAsLocalFile = true;
            } else {                
                localFile = Path.Combine(folder.GetFullPath(), fileName);
                localFileExists = File.Exists(localFile);
                fileSameAsLocalFile = string.Compare(Path.GetFullPath(localFile), Path.GetFullPath(file), true) == 0;             
            }

            if (localFileExists) { // file with same name already exists in the project
                if (fileSameAsLocalFile) { // it's the same file that is being added
                    // get existing item                                      
                    if (origin == FILES_ORIGIN.SOLUTION_EXPLORER || !list.ExistsReference(localFile)) { // item does not exist or it was dragged from Sol. Explorer - add it
                        addedListItem = AddExistingItem(list, file, type, false);
                    } else { // item exists - add copy with different name
                        string copyFileName = GenerateCopyFileName(localFile);
                        File.Copy(file, copyFileName);

                        ProjectItem newItem = folder.ProjectItems.AddFromFile(copyFileName);
                        SetBuildAction(newItem, prjBuildAction.prjBuildActionNone);

                        addedListItem = AddExistingItem(list, copyFileName, type, false);
                    }
                } else { // local file is different from the added
                    // create conflictResolveAction - what should be done if user chooses to overwrite the files
                    Action conflictResolveAction = null;
                    if (folder.ProjectItems.ContainsItem(localFile)) {
                        ProjectItem existingItem = folder.ProjectItems.Item(localFile);
                        conflictResolveAction = new Action(() => { existingItem.Delete(); });
                    } else {
                        conflictResolveAction = new Action(() => { File.Delete(localFile); });
                    }

                    // display dialog asking user if overwrite
                    DialogResult result = VisualLocalizer.Library.MessageBox.Show(string.Format("Item \"{0}\" already exists. Do you want to overwrite the file?", fileName), null, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_QUERY);
                    string fullPath = localFile;
                    if (result == DialogResult.Yes) { // yes, overwrite
                        if (conflictResolveAction != null) {
                            conflictResolveAction(); // remove the local file
                        }

                        // add copy of the new file
                        if (folder != null) {
                            ProjectItem newItem = folder.ProjectItems.AddFromFileCopy(file);
                            SetBuildAction(newItem, prjBuildAction.prjBuildActionNone);
                            fullPath = newItem.GetFullPath();
                        } else {
                            fullPath = file;
                        }
                    } else { // don't overwrite - just make sure the file is in the project
                        if (!folder.ProjectItems.ContainsItem(Path.GetFileName(localFile))) {
                            ProjectItem newItem = folder.ProjectItems.AddFromFile(localFile);
                            SetBuildAction(newItem, prjBuildAction.prjBuildActionNone);
                        }
                    }

                    if (origin == FILES_ORIGIN.MAKE_EXTERNAL) {
                        addedListItem = AddExistingItem(list, fullPath, type, false);
                    } else {
                        if (list.ExistsReference(fullPath)) { // items referencing this files already exist
                            UpdateExistingItemsData(list, fullPath, type); // update their data                            
                            addedListItem = null;
                        } else {
                            addedListItem = AddExistingItem(list, fullPath, type, false);// add new item
                        }                         
                    }
                    
                    list.Refresh();
                    list.NotifyDataChanged();
                }
            } else { // local file does not exist - add it
                if (folder == null) {
                    addedListItem = AddExistingItem(list, file, type, true);
                } else {
                    ProjectItem newItem = folder.ProjectItems.AddFromFileCopy(file);
                    SetBuildAction(newItem, prjBuildAction.prjBuildActionNone);
                    string fullPath = newItem.GetFullPath();

                    addedListItem = AddExistingItem(list, fullPath, type, false);
                }
            }
            return addedListItem;
        }

        /// <summary>
        /// Updates data of list view item
        /// </summary>        
        private void UpdateExistingItemsData(AbstractListView list, string fullPath, Type type) {
            if (list == null) throw new ArgumentNullException("list");
            if (fullPath == null) throw new ArgumentNullException("fullPath");
            if (type == null) throw new ArgumentNullException("type");

            string name = Path.GetFileNameWithoutExtension(fullPath).CreateIdentifier(Editor.ProjectItem.DesignerLanguage);
            
            foreach (ListViewKeyItem item in list.Items) {
                if (string.Compare(Path.GetFullPath(item.ImageKey), Path.GetFullPath(fullPath), true) == 0) {
                    ResXDataNode node = new ResXDataNode(name, new ResXFileRef(fullPath, type.AssemblyQualifiedName));                    
                    node.Comment = item.SubItems[list.CommentColumnName].Text;
                    
                    item.BeforeEditKey = item.Text;
                    item.DataNode = node;

                    list.UpdateDataOf(item, true);
                }
            }            
            
            list.NotifyDataChanged();
            list.NotifyItemsStateChanged();
        }

       /// <summary>
       /// Adds specified file to given list view
       /// </summary>       
       /// <returns>Newly created list view item</returns>
        private ListViewKeyItem AddExistingItem(AbstractListView list, string fullPath, Type type, bool embedded) {
            if (list == null) throw new ArgumentNullException("list");
            if (fullPath == null) throw new ArgumentNullException("fullPath");
            if (type == null) throw new ArgumentNullException("type");
            
            string name = Path.GetFileNameWithoutExtension(fullPath).CreateIdentifier(Editor.ProjectItem.DesignerLanguage);

            ResXDataNode node = new ResXDataNode(name, new ResXFileRef(fullPath, type.AssemblyQualifiedName));
            
            ListViewKeyItem newItem;
            if (embedded) {
                object value = node.GetValue((ITypeResolutionService)null);
                node = new ResXDataNode(name, value);
            } 

            newItem = list.Add(name, node) as ListViewKeyItem;
            
            list.NotifyDataChanged();
            list.NotifyItemsStateChanged();

            return newItem;
        }

        /// <summary>
        /// Adds new string resource
        /// </summary>        
        private void AddNewString(object sender, EventArgs e) {
            try {
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
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Handles creating new image and adding it to the resources
        /// </summary>        
        private void AddNewImage(Type resourceType, AbstractListView listView, string resourceSubfolder) {
            try {
                if (resourceType == null) throw new ArgumentNullException("resourceType");
                if (listView == null) throw new ArgumentNullException("listView");
                if (resourceSubfolder == null) throw new ArgumentNullException("resourceSubfolder");

                // display dialog letting user choose the format and dimensions
                NewImageWindow win = new NewImageWindow(resourceType == typeof(Icon));
                win.Owner = (Form)Form.FromHandle(new IntPtr(VisualLocalizerPackage.Instance.DTE.MainWindow.HWnd));

                if (win.ShowDialog(this) == DialogResult.OK) {
                    Solution solution = VisualLocalizerPackage.Instance.DTE.Solution;

                    string imageName = win.ImageName.ToLower();

                    // determine whether file name has extension specified
                    bool hasExtension = false;
                    foreach (var item in win.ImageFormat.Extensions)
                        if (imageName.EndsWith(item)) hasExtension = true;
                    
                    if (!hasExtension) {
                        imageName = win.ImageName + win.ImageFormat.Extensions[0]; // add extension to a file name
                    } else {
                        imageName = win.ImageName;
                    }
                    
                    ListViewKeyItem newItem;
                    if (!solution.ContainsProjectItem(Editor.ProjectItem.InternalProjectItem)) {
                        newItem = AddNewImageNoSolution(imageName, resourceType, listView, resourceSubfolder, win);
                    } else {
                        newItem = AddNewImageWithSolution(imageName, solution, resourceType, listView, resourceSubfolder, win);
                    }

                    // create the undo unit
                    ListViewNewItemCreateUndoUnit unit = new ListViewNewItemCreateUndoUnit(this, newItem, conflictResolver);
                    Editor.AddUndoUnit(unit);

                    VLOutputWindow.VisualLocalizerPane.WriteLine("Created and added new object \"{0}\"", newItem.DataNode.FileRef.FileName);
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Adds new image file to the project resources
        /// </summary>
        /// <param name="imageName">Image name including extension</param>
        /// <param name="solution">Parent solution</param>
        /// <param name="resourceType">Type of the image (Bitmap or Icon)</param>
        /// <param name="listView">List in which the image should be added</param>
        /// <param name="resourceSubfolder">Folder in which the file should be created</param>
        /// <param name="win">Dialog containing data like format and dimensions</param>
        /// <returns>The newly created item</returns>
        private ListViewKeyItem AddNewImageWithSolution(string imageName, Solution solution, Type resourceType, AbstractListView listView, string resourceSubfolder, NewImageWindow win) {
            ProjectItem thisItem = solution.FindProjectItem(Editor.FileName);
            if (thisItem == null) {
                return AddNewImageNoSolution(imageName, resourceType, listView, resourceSubfolder, win);
            } else {
                Project project = thisItem.ContainingProject;

                ProjectItem imagesFolder = project.AddResourceDir(resourceSubfolder);
                string imagesFolderPath = imagesFolder.GetFullPath();
                string newImagePath = Path.Combine(imagesFolderPath, imageName);

                if (imagesFolder.ProjectItems.ContainsItem(imageName))
                    throw new Exception(string.Format("File \"{0}\" already exists!", imageName));

                // create new image
                Bitmap bmp = new Bitmap(win.ImageWidth, win.ImageHeight);
                bmp.Save(newImagePath, win.ImageFormat.Value); // save it in the file
                bmp.Dispose();

                // add project item corresponding to the image
                ProjectItem newImageItem = imagesFolder.ProjectItems.AddFromFile(newImagePath);
                SetBuildAction(newImageItem, prjBuildAction.prjBuildActionNone);   

                Window newImageWindow = newImageItem.Open(null); // open the image
                if (newImageWindow != null) newImageWindow.Activate();

                return AddExistingItem(listView, newImagePath, resourceType, false);
            }
        }

        /// <summary>
        /// Adds new image file to the project resources
        /// </summary>        
        private ListViewKeyItem AddNewImageNoSolution(string imageName, Type resourceType, AbstractListView listView, string resourceSubfolder, NewImageWindow win) {
            string currentDir = Path.GetDirectoryName(Editor.FileName);
            string newImagePath = Path.Combine(currentDir, imageName);

            if (File.Exists(newImagePath))
                throw new Exception(string.Format("File \"{0}\" already exists!", imageName));

            Bitmap bmp = new Bitmap(win.ImageWidth, win.ImageHeight);
            bmp.Save(newImagePath, win.ImageFormat.Value);
            bmp.Dispose();

            Window newWindow = VisualLocalizerPackage.Instance.DTE.OpenFile(null, newImagePath);
            if (newWindow != null) newWindow.Activate();

            return AddExistingItem(listView, newImagePath, resourceType, false);
        }

        /// <summary>
        /// Pastes given embedded objects
        /// </summary>        
        private void InternalEmbeddedPaste(List<object> list, List<AbstractListView> dataTabItems) {
            ListViewItemsAddUndoUnit unit = null;
            List<ListViewKeyItem> newItems = null;
            try {
                if (list == null) throw new ArgumentNullException("list");
                if (dataTabItems == null) throw new ArgumentNullException("dataTabItems");

                newItems = new List<ListViewKeyItem>();
                unit = new ListViewItemsAddUndoUnit(this, newItems, conflictResolver);

                foreach (ResXDataNode o in list) {
                    foreach (var item in dataTabItems) {
                        if (item.CanContainItem(o)) { // select proper tab for the item
                            // generate new unique name
                            string name = GetNextCopyName(o.Name); 
                            bool contains = true;
                            while (contains) {
                                contains = item.ItemFromName(name) != null;
                                if (contains) name = GetNextCopyName(name);
                            }

                            ListViewKeyItem newItem = (ListViewKeyItem)item.Add(name, o);
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

        /// <summary>
        /// Pastes data from Solution Explorer's clipboard
        /// </summary>
        /// <param name="memoryStream">Stream containing Solution Explorer-initialized data about copied files</param>        
        private void InternalSolExpPaste(MemoryStream memoryStream, List<AbstractListView> dataTabItems) {
            if (memoryStream == null) throw new ArgumentNullException("memoryStream");
            if (dataTabItems == null) throw new ArgumentNullException("dataTabItems");

            byte[] buffer = new byte[memoryStream.Length];
            memoryStream.Read(buffer, 0, buffer.Length); // read all stream

            string text = Encoding.UTF8.GetString(buffer); // create text
            List<string> paths = new List<string>();
            StringBuilder dataBuilder = new StringBuilder();

            // remove single occurences of \0 
            char prevChar = '?';
            foreach (char c in text) {
                if (c != '\0' || prevChar == '\0') dataBuilder.Append(c);
                prevChar = c;
            }

            // create file list
            string[] data = dataBuilder.ToString().Split(Path.GetInvalidPathChars(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string path in data) {
                if (File.Exists(path)) {
                    paths.Add(path);
                }
            }
            
            // add the files
            AddExistingFiles(paths, FILES_ORIGIN.SOLUTION_EXPLORER);
        }

        /// <summary>
        /// Sets "Build Action" of specified item
        /// </summary>        
        private void SetBuildAction(ProjectItem item, prjBuildAction prjBuildAction) {
            if (item == null) throw new ArgumentNullException("item");
            if (item.ContainingProject.Kind.ToUpperInvariant() == StringConstants.WebSiteProject) return;
            
            item.Properties.Item("BuildAction").Value = prjBuildAction;            
        }

        /// <summary>
        /// Generates new similar but non-existing file name
        /// </summary>        
        private string GenerateCopyFileName(string file) {
            if (file == null) throw new ArgumentNullException("file");

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

        /// <summary>
        /// Returns "next step" name - i.e. "name" -> "name1", "name1" -> "name2"
        /// </summary>
        private string GetNextCopyName(string name) {
            if (name == null) throw new ArgumentNullException("name");

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

        #endregion

        #region private - listeners

        /// <summary>
        /// Requestes revalidation of keys (after settings change)
        /// </summary>
        private void Instance_RevalidationRequested() {
            foreach (ResXStringGridRow row in stringGrid.Rows) {
                if (row.IsNewRow) continue;
                row.Cells[stringGrid.KeyColumnName].Tag = row.Cells[stringGrid.KeyColumnName].Value;
                stringGrid.ValidateRow(row);
            }
            ValidateListView(imagesListView);
            ValidateListView(soundsListView);
            ValidateListView(iconsListView);
            ValidateListView(filesListView);
        }

        /// <summary>
        /// Revalidates all keys in specified list
        /// </summary>        
        private void ValidateListView(AbstractListView view) {
            foreach (ListViewKeyItem item in view.Items) {
                item.BeforeEditKey = item.AfterEditKey = item.Key;
                view.Validate(item);
            }
        }

        /// <summary>
        /// Called when "Update" button is clicked - finds culture neutral parent file and adds all its string resources to this file, if not already present
        /// </summary>
        private void UpdateKeysButton_Click(object sender, EventArgs e) {            
            try {
                VLOutputWindow.VisualLocalizerPane.WriteLine("Attempting to find culture-neutral parent...");
                string myNeutralName = Editor.ProjectItem.GetCultureNeutralName();
                string neutralParentPath = Path.Combine(Path.GetDirectoryName(Editor.ProjectItem.InternalProjectItem.GetFullPath()), myNeutralName);

                // find parent (culture neutral) item
                ProjectItem parentItem = Editor.ProjectItem.InternalProjectItem.Collection.GetItem(myNeutralName);                
                    
                if (parentItem == null) throw new Exception("Cannot find culture-neutral resource file - file '" + neutralParentPath + "' does not exist.");

                VLOutputWindow.VisualLocalizerPane.WriteLine("Found " + neutralParentPath);
                ResXProjectItem resxParent = ResXProjectItem.ConvertToResXItem(parentItem, parentItem.ContainingProject);
                
                var result = System.Windows.Forms.MessageBox.Show("Culture-neutral parent file '" + parentItem.Name + "' was found. Do you want to import its string resources?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No) return;

                // add not existing keys
                bool loaded = resxParent.IsLoaded;
                if (!loaded) resxParent.Load();

                int rowsAdded = 0;
                foreach (var pair in resxParent.Data) {
                    if (!pair.Value.HasValue<string>()) continue;

                    string newKey = pair.Value.Name; 
                    string newValue = pair.Value.GetValue<string>();
                    string newComment = pair.Value.Comment; 
                    if (Editor.ProjectItem.GetKeyConflictType(newKey, newValue, false) == CONTAINS_KEY_RESULT.DOESNT_EXIST) {
                        ResXDataNode newNode = new ResXDataNode(newKey, newValue);
                        newNode.Comment = newComment;
                        ResXStringGridRow newRow = (ResXStringGridRow)stringGrid.Add(newKey, newNode);
                        stringGrid.StringRowAdded(newRow);
                        rowsAdded++;
                    }
                }
                if (!loaded) resxParent.Unload();

                if (rowsAdded == 0) {
                    VLOutputWindow.VisualLocalizerPane.WriteLine("Synchronize successful - no updates from \"{0}\"", parentItem.Name);
                } else {
                    VLOutputWindow.VisualLocalizerPane.WriteLine("Synchronize successful - added {0} rows from \"{1}\"", rowsAdded, parentItem.Name);
                    Editor.IsDirty = true;
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }            
        }

        /// <summary>
        /// Called when "Proffer" button is clicked - finds all culture-specific "descendants" of this file and updates their string resources
        /// </summary>        
        private void ProfferKeysButton_Click(object sender, EventArgs e) {
            try {
                VLOutputWindow.VisualLocalizerPane.WriteLine("Looking for children culture-specific files of " + Editor.ProjectItem.InternalProjectItem.Name + " ...");

                List<ProjectItem> childrenFiles = new List<ProjectItem>();
                foreach (ProjectItem projectItem in Editor.ProjectItem.InternalProjectItem.Collection)
                    // if current project item is culture-specific version of this file
                    if (projectItem.IsCultureSpecificResX() && projectItem.GetResXCultureNeutralName() == Editor.ProjectItem.InternalProjectItem.Name) {
                        childrenFiles.Add(projectItem);
                        VLOutputWindow.VisualLocalizerPane.WriteLine("Found " + projectItem.Name);
                    }
                
                if (childrenFiles.Count == 0) {
                    System.Windows.Forms.MessageBox.Show("No culture-specific versions of this files were found.", "Proffer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                } else {
                    string childrenString = string.Join(Environment.NewLine, childrenFiles.Select((projectItem) => { return Path.GetFileName(projectItem.GetFullPath()); }).ToList().ToArray());
                    var result = System.Windows.Forms.MessageBox.Show("Following culture-specific files to update were found: " + Environment.NewLine + childrenString + Environment.NewLine + "Do you want to update their string resources?", "Proffer", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No) return;
                }

                foreach (ProjectItem projectItem in childrenFiles) {
                    // if current project item is culture-specific version of this file
                    bool wasUpdated = false;
                    string fullPath = projectItem.GetFullPath();

                    if (RDTManager.IsFileOpen(fullPath)) { // file is open
                        Dictionary<string, ResXDataNode> data = null;
                        VLDocumentViewsManager.LoadDataFromBuffer(ref data, fullPath); // load current data from buffer

                        // add all non-existing data
                        foreach (var pair in stringGrid.GetData(true)) {
                            if (!data.ContainsKey(pair.Value.Name)) {
                                data.Add(pair.Key, pair.Value);
                                wasUpdated = true;
                            }
                        }

                        VLDocumentViewsManager.SaveDataToBuffer(data, fullPath); // save the buffer
                    } else { // file is closed
                        ResXProjectItem resxChild = ResXProjectItem.ConvertToResXItem(projectItem, projectItem.ContainingProject);

                        resxChild.BeginBatch();
                        foreach (var pair in stringGrid.GetData(true)) {
                            if (resxChild.GetKeyConflictType(pair.Value.Name, pair.Value.GetValue<string>(), false) == CONTAINS_KEY_RESULT.DOESNT_EXIST) {
                                resxChild.AddString(pair.Value.Name, pair.Value.GetValue<string>());
                                wasUpdated = true;
                            }
                        }
                        resxChild.EndBatch();
                    }

                    // update list of modified files
                    if (wasUpdated) {
                        VLOutputWindow.VisualLocalizerPane.WriteLine("Updated " + projectItem.Name);
                    } else {
                        VLOutputWindow.VisualLocalizerPane.WriteLine("No updates required for " + projectItem.Name);
                    }
                }

                VLOutputWindow.VisualLocalizerPane.WriteLine("Proffer finished");
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }   

        /// <summary>
        /// Called when "Translate" button is opening, displays list of existing language pairs
        /// </summary>        
        private void TranslateButton_DropDownOpening(object sender, EventArgs eargs) {
            try {
                foreach (ToolStripMenuItem menuItem in translateButton.DropDownItems) { // for each translation provider
                    menuItem.DropDownItems.Clear(); // clear current language pairs
                    TRANSLATE_PROVIDER provider = (TRANSLATE_PROVIDER)menuItem.Tag;

                    // Bing AppID is required for this provider
                    bool enabled = true;
                    if (provider == TRANSLATE_PROVIDER.BING) {
                        enabled = !string.IsNullOrEmpty(SettingsObject.Instance.BingAppId);
                    }

                    menuItem.Enabled = enabled;

                    // add saved language pairs
                    foreach (var pair in SettingsObject.Instance.LanguagePairs) {
                        ToolStripMenuItem newItem = new ToolStripMenuItem(pair.ToString());
                        newItem.Tag = pair;
                        newItem.Click += new EventHandler((o, e) => {
                            SettingsObject.LanguagePair sentPair = (o as ToolStripMenuItem).Tag as SettingsObject.LanguagePair;
                            NotifyTranslateRequested(provider, sentPair.FromLanguage, sentPair.ToLanguage);
                        });
                        newItem.Enabled = enabled;
                        menuItem.DropDownItems.Add(newItem);
                    }

                    // add option to add new language pair
                    ToolStripMenuItem addItem = new ToolStripMenuItem("New language pair...", null, new EventHandler((o, e) => {
                        NotifyNewTranslatePairAdded(provider);
                    }));
                    addItem.Enabled = enabled;
                    menuItem.DropDownItems.Add(addItem);
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Called when new language pair should be added to the settings
        /// </summary>        
        private void StringGrid_LanguagePairAdded(string sourceLanguage, string targetLanguage) {
            try {
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
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Updates "View" style of the list views
        /// </summary>        
        private void ViewCheckStateChanged(object sender, EventArgs e) {
            try {
                ToolStripMenuItem senderItem = sender as ToolStripMenuItem;
                if (senderItem.CheckState == CheckState.Unchecked) return;

                foreach (ToolStripMenuItem item in viewButton.DropDownItems)
                    if (item != senderItem) item.CheckState = CheckState.Unchecked;

                NotifyViewKindChanged((View)senderItem.Tag);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }        

        private void NoFocusBoxSelectedIndexChanged(object sender, EventArgs e) {
            toolStrip.Focus();
        }                

        /// <summary>
        /// Invokes the InlineRequested(inline) event
        /// </summary>        
        private void InlineButton_ButtonClick(object sender, EventArgs e) {
            NotifyInlineRequested(INLINEKIND.INLINE);
        }

        /// <summary>
        /// Invokes the InlineRequested(inline & remove) event
        /// </summary>        
        private void InlineAndRemoveButton_ButtonClick(object sender, EventArgs e) {
            NotifyInlineRequested(INLINEKIND.INLINE | INLINEKIND.REMOVE);
        }

        /// <summary>
        /// Merges two resource files, preserving both
        /// </summary>        
        private void MergeButton_PreserveClick(object sender, EventArgs e) {
            MergeWithFile(false);
        }

        /// <summary>
        /// Merges two resource files, deleting original
        /// </summary>        
        private void MergeButton_DeleteClick(object sender, EventArgs e) {
            MergeWithFile(true);
        }
        
        private string previousValue = null;       

        /// <summary>
        /// Called when "Access modifier" changed
        /// </summary>        
        private void CodeGenerationBox_SelectedIndexChanged(object sender, EventArgs e) {
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

        /// <summary>
        /// Updates code references to resources periodically in interval specified in settings
        /// </summary>
        private void ReferenceLookuperThread() {
            bool init = true;
            while (!IsDisposed) {
                try {
                    if (init) { // on startup
                        UpdateReferencesCount();
                        init = false;
                    }
                    // wait
                    System.Threading.Thread.Sleep(SettingsObject.Instance.ReferenceUpdateInterval);

                    // update
                    if (Visible && !IsDisposed)
                        UpdateReferencesCount();
                } catch (Exception ex) {
                    if (!IsDisposed) {
                        VLOutputWindow.VisualLocalizerPane.WriteLine("Error occured on reference lookuper thread:");
                        VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                    }
                }
            }
            VLOutputWindow.VisualLocalizerPane.WriteLine("Reference lookuper thread of \"{0}\" terminated", Path.GetFileName(Editor.FileName));
        }

        /// <summary>
        /// Updates references count for all items in every tab
        /// </summary>
        private void UpdateReferencesCount() {
            ArrayList list = new ArrayList();

            foreach (ResXStringGridRow row in stringGrid.Rows)
                if (!row.IsNewRow) list.Add(row);

            filesListView.Invoke(new Action<IList, IEnumerable>((l, s) => AddRange(l, s)), list, filesListView.Items);
            imagesListView.Invoke(new Action<IList, IEnumerable>((l, s) => AddRange(l, s)), list, imagesListView.Items);
            iconsListView.Invoke(new Action<IList, IEnumerable>((l, s) => AddRange(l, s)), list, iconsListView.Items);
            soundsListView.Invoke(new Action<IList, IEnumerable>((l, s) => AddRange(l, s)), list, soundsListView.Items);

            UpdateReferencesCount(list);
        }

        private void AddRange(IList list, IEnumerable source) {
            foreach (IReferencableKeyValueSource item in source)
                list.Add(item);
        }

        /// <summary>
        /// Returns "Access modifier" value for this document, or null if changing "Access modifier" is disabled
        /// </summary>        
        private string GetResXCodeGenerationMode() {
            // if the file is not part of the solution, the combo box should be disabled
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

        /// <summary>
        /// Creates tab page with given content
        /// </summary>
        /// <param name="header">Text in header</param>
        /// <param name="content">Content control</param>
        /// <returns>New tab page</returns>
        private TabPage CreateItemTabPage(string header, AbstractListView content) {
            if (header == null) throw new ArgumentNullException("header");
            if (content == null) throw new ArgumentNullException("content");

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

        /// <summary>
        /// Performs "Merge" operation
        /// </summary>
        /// <param name="deleteSource">True if source file should be deleted</param>
        private void MergeWithFile(bool deleteSource) {
            ResXResourceReader reader = null;
            try {
                // display dialog and let user choose the file that should be merged with this one
                string[] files = VisualLocalizer.Library.MessageBox.SelectFilesViaDlg("Select file", Path.GetDirectoryName(Editor.FileName),
                    "ResX file\0*.resx\0", 0, 0);
                if (files == null) return;
                if (files.Length != 1) throw new Exception("Exactly one file must be selected!");

                string file = files[0];
                if (string.Compare(Path.GetFullPath(file), Path.GetFullPath(Editor.FileName), true) == 0) throw new Exception("Cannot select same file as the one being edited!");
                
                // display confirmation about deleting source files
                string delSrcString = deleteSource ? " The source file will be deleted." : string.Empty; 
                DialogResult result = VisualLocalizer.Library.MessageBox.Show(string.Format("Do you really want to add all resources from \"{0}\" to this file?" + delSrcString, Path.GetFileName(file)), null, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_THIRD, OLEMSGICON.OLEMSGICON_WARNING);
                if (result == DialogResult.No) return;                

                List<IDataTabItem> dataTabItems = new List<IDataTabItem>();

                foreach (TabPage page in this.tabs.TabPages) {
                    IDataTabItem content = GetContentFromTabPage(page);
                    if (content != null) {
                        IDataTabItem tabItem = content as IDataTabItem;
                        dataTabItems.Add(tabItem);
                    }
                }

                IEnumerable enumarable;
                if (RDTManager.IsFileOpen(file)) { // source file is open - read data from its buffer
                    Dictionary<string, ResXDataNode> data = null;
                    VLDocumentViewsManager.LoadDataFromBuffer(ref data, file);

                    enumarable = (IEnumerable)data;
                } else { // source file is closed - read data from disk
                    reader = new ResXResourceReader(file);
                    reader.BasePath = Path.GetDirectoryName(file);
                    reader.UseResXDataNodes = true;

                    enumarable = (IEnumerable)reader;
                }

                Stack<IOleUndoUnit> units = new Stack<IOleUndoUnit>();

                foreach (object o in enumarable) {
                    foreach (var item in dataTabItems) {
                        ResXDataNode node = (o is DictionaryEntry) ? ((DictionaryEntry)o).Value as ResXDataNode : ((KeyValuePair<string, ResXDataNode>)o).Value;
                        string key = node.Name;

                        // select propert tab for the new node
                        if (item.CanContainItem(node)) {
                            IKeyValueSource newItem = item.Add(key, node);
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
                
                // create undo unit
                MergeUndoUnit unit = new MergeUndoUnit(this, Path.GetFileName(file), units);
                Editor.AddUndoUnit(unit);
                VLOutputWindow.VisualLocalizerPane.WriteLine("Merged files \"{0}\" and \"{1}\"", Editor.FileName, file);

                if (deleteSource) {
                    // delete source files
                    ProjectItem item = VisualLocalizerPackage.Instance.DTE.Solution.FindProjectItem(file);
                    if (item != null) item.Delete(); // remove them from project
                    File.Delete(file);
                    VLOutputWindow.VisualLocalizerPane.WriteLine("Deleted file after merge: \"{0}\"", file);
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            } finally {
                if (reader != null) reader.Close();
            }
        }

        /// <summary>
        /// Invokes the ViewKindChanged event
        /// </summary>        
        private void NotifyViewKindChanged(View newView) {
            if (ViewKindChanged != null) ViewKindChanged(newView);
        }

        /// <summary>
        /// Updates state (enabled/disabled) of the toolstrip buttons
        /// </summary>   
        private void UpdateToolStripButtonsEnable(object sender, EventArgs e) {
            try {
                bool selectedString = stringGrid.Visible;
                IDataTabItem item = GetContentFromTabPage(tabs.SelectedTab);

                bool allSelectedResourcesExternal = false;                
                if (item is AbstractListView) {
                    allSelectedResourcesExternal = true;                    
                    foreach (ListViewKeyItem listItem in (item as AbstractListView).SelectedItems) {
                        allSelectedResourcesExternal = allSelectedResourcesExternal && listItem.DataNode.FileRef != null;                        
                    }
                }                

                inlineButton.Enabled = selectedString && item.HasSelectedItems && !item.IsEditing && !readOnly && stringGrid.AreReferencesKnownOnSelected;
                removeDeleteItem.Enabled = !selectedString && item.HasSelectedItems && !item.IsEditing && !readOnly && allSelectedResourcesExternal;
                removeExcludeItem.Enabled = !selectedString && item.HasSelectedItems && !item.IsEditing && !readOnly && allSelectedResourcesExternal;
                removeButton.Enabled = item.HasSelectedItems && !item.IsEditing && !readOnly;
                viewButton.Enabled = !selectedString && !item.IsEditing;
                addButton.Enabled = !readOnly;
                translateButton.Enabled = selectedString && item.HasSelectedItems && !item.IsEditing && !readOnly;
                mergeButton.Enabled = !readOnly;                

                if (Editor.ProjectItem != null) {
                    bool specific = Editor.ProjectItem.IsCultureSpecific();

                    if (specific) {
                        bool parentExists = Editor.ProjectItem.InternalProjectItem.Collection.ContainsItem(Editor.ProjectItem.GetCultureNeutralName());
                        updateKeysButton.Enabled = selectedString && !item.IsEditing && !readOnly && parentExists;
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
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Issues the "NewTranslatePairNeeded" event
        /// </summary>        
        private void NotifyNewTranslatePairAdded(TRANSLATE_PROVIDER provider) {
            if (NewTranslatePairAdded != null) NewTranslatePairAdded(provider);
        }

        /// <summary>
        /// Issues the "TranslateRequested" event
        /// </summary>        
        private void NotifyTranslateRequested(TRANSLATE_PROVIDER provider, string fromLanguage, string toLanguage) {
            if (TranslateRequested != null) TranslateRequested(provider, fromLanguage, toLanguage);
        }

        /// <summary>
        /// Issues the "InlineRequested" event
        /// </summary>        
        private void NotifyInlineRequested(INLINEKIND inlineKind) {
            if (InlineRequested != null) InlineRequested(inlineKind);
        }

        
    }

}
    
