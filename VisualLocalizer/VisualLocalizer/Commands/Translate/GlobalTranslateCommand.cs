using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using VisualLocalizer.Gui;
using VisualLocalizer.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Resources;
using System.Collections;
using System.IO;
using VisualLocalizer.Extensions;

namespace VisualLocalizer.Commands {

    /// <summary>
    /// Represents "Global translate" command invokeable from Solution Explorer's context menu. It searches for ResX files and
    /// translates all resource values.
    /// </summary>
    internal sealed class GlobalTranslateCommand {

        private HashSet<ProjectItem> searchedProjectItems = new HashSet<ProjectItem>();
        private List<ResXProjectItem> loadedResxItems = new List<ResXProjectItem>();

        /// <summary>
        /// Starts the command, taking array of selected project items as a parameter
        /// </summary>        
        public void Process(Array array) {
            try {
                searchedProjectItems.Clear();
                loadedResxItems.Clear();

                // find all ResX files contained within selected project items
                List<GlobalTranslateProjectItem> resxFiles = new List<GlobalTranslateProjectItem>();
                foreach (UIHierarchyItem o in array) {
                    if (o.Object is ProjectItem) {
                        ProjectItem item = (ProjectItem)o.Object;
                        SearchForResxFiles(item, resxFiles);
                    } else if (o.Object is Project) {
                        Project proj = (Project)o.Object;
                        SearchForResxFiles(proj.ProjectItems, resxFiles);
                    } else if (o.Object is Solution) {
                        Solution s = (Solution)o.Object;
                        SearchForResxFiles(s.Projects, resxFiles);
                    } else throw new Exception("Unexpected project item type: " + o.Object.GetVisualBasicType());
                }

                // display form, allowing user to choose source and target language and select ResX files, where translation should be performed
                GlobalTranslateForm form = new GlobalTranslateForm(resxFiles);
                if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    List<AbstractTranslateInfoItem> data = new List<AbstractTranslateInfoItem>();
                    try {
                        // collect string data from checked ResX files
                        ProgressBarHandler.StartIndeterminate(Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Find);
                        foreach (GlobalTranslateProjectItem item in resxFiles)
                            if (item.Checked) {
                                AddDataForTranslation(item, data);
                            }
                        ProgressBarHandler.StopIndeterminate(Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Find);

                        // translate collected data using given language pair
                        TranslationHandler.Translate(data, form.Provider, form.LanguagePair.FromLanguage, form.LanguagePair.ToLanguage);

                        // replace original texts with the translated ones
                        ProgressBarHandler.StartIndeterminate(Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Find);
                        foreach (AbstractTranslateInfoItem i in data) {
                            i.ApplyTranslation();
                        }
                    } finally {
                        // unloads all ResX files that were originally closed
                        foreach (ResXProjectItem item in loadedResxItems) {
                            item.Flush();
                            item.Unload();
                        }
                        ProgressBarHandler.StopIndeterminate(Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Find);
                    }
                }
            } finally {
                MenuManager.OperationInProgress = false;
            }
        }

        /// <summary>
        /// Add string data from given ResX file to the list of data for translation
        /// </summary>        
        private void AddDataForTranslation(GlobalTranslateProjectItem item, List<AbstractTranslateInfoItem> data) {
            string path = item.ProjectItem.GetFullPath();
            if (RDTManager.IsFileOpen(path)) { // file is open
                object docData = VLDocumentViewsManager.GetDocData(path); // get document buffer
                if (docData is ResXEditor) { // document is opened in ResX editor -> use custom method to get string data
                    ResXEditor editor = (ResXEditor)docData;
                    editor.UIControl.AddForTranslation(data);
                } else { // document is opened in original VS editor
                    IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(path, false);
                    string text = VLDocumentViewsManager.GetTextFrom(lines); // get plain text of ResX file
                    
                    ResXResourceReader reader = null;
                    
                    BufferTranslateInfoItem prev = null;
                    BufferTranslateInfoItem first = null;
                    
                    try {
                        reader = ResXResourceReader.FromFileContents(text);
                        reader.UseResXDataNodes = true;

                        // add all string resources to the list
                        // items are linked like a linked-list, allowing ApplyTranslation to work
                        foreach (DictionaryEntry entry in reader) {
                            ResXDataNode node = (entry.Value as ResXDataNode);
                            if (node.HasValue<string>()) {
                                BufferTranslateInfoItem translateItem = new BufferTranslateInfoItem();
                                translateItem.ResourceKey = entry.Key.ToString();
                                translateItem.Value = node.GetValue<string>();
                                translateItem.Filename = path;
                                translateItem.Applied = false;
                                translateItem.GlobalTranslateItem = item;
                                translateItem.Prev = prev;
                                translateItem.IVsTextLines = lines;

                                data.Add(translateItem);
                                prev = translateItem;
                                if (first == null) first = translateItem;
                            } else {
                                item.NonStringData.Add(node);
                            }
                        }
                        if (first != null) first.Prev = prev;
                    } finally {
                        if (reader != null) reader.Close();
                    }
                }
            } else { // file is closed
                ResXProjectItem resxItem = ResXProjectItem.ConvertToResXItem(item.ProjectItem, item.ProjectItem.ContainingProject);
                resxItem.Load();
                loadedResxItems.Add(resxItem);

                // add string data from ResX file
                resxItem.AddAllStringReferencesUnique(data);
            }
        }

        private void SearchForResxFiles(ProjectItem item, List<GlobalTranslateProjectItem> resxFiles) {
            if (searchedProjectItems.Contains(item)) return;
            SearchForResxFiles(item.ProjectItems, resxFiles);

            if (item.IsItemResX()) {
                GlobalTranslateProjectItem r = new GlobalTranslateProjectItem(item);                
                r.Checked = false;
                r.Readonly = VLDocumentViewsManager.IsFileLocked(item.GetFullPath()) || RDTManager.IsFileReadonly(item.GetFullPath());

                resxFiles.Add(r);
            }
        }

        private void SearchForResxFiles(ProjectItems items, List<GlobalTranslateProjectItem> resxFiles) {
            if (items == null) return;
            foreach (ProjectItem item in items)
                SearchForResxFiles(item, resxFiles);
        }

        private void SearchForResxFiles(Projects projects, List<GlobalTranslateProjectItem> resxFiles) {
            if (projects == null) return;
            foreach (Project item in projects) {
                if (!item.IsKnownProjectType()) continue;
                SearchForResxFiles(item.ProjectItems, resxFiles);
            }
        }
    }

    /// <summary>
    /// Represents ResX file in the global translate process
    /// </summary>
    internal sealed class GlobalTranslateProjectItem {
        public GlobalTranslateProjectItem(ProjectItem item) {
            if (item == null) throw new ArgumentNullException("item");

            this.ProjectItem = item;
            NonStringData = new List<ResXDataNode>();
            string filePath = item.GetFullPath();

            if (item.ContainingProject != null && VisualLocalizerPackage.Instance.DTE.Solution.ContainsProjectItem(item)) {                
                this.path = Uri.UnescapeDataString(new Uri(VisualLocalizerPackage.Instance.DTE.Solution.FullName).MakeRelativeUri(new Uri(filePath)).ToString());
            } else {
                this.path = filePath;
            }            
        }

        public List<ResXDataNode> NonStringData { get; private set; }
        public ProjectItem ProjectItem { get; private set; }
        public bool Readonly { get; set; }
        public bool Checked { get; set; }
        private string path;

        public override string ToString() {
            return path + (Readonly ? " (readonly)":"");
        }
    }

    /// <summary>
    /// Represents one string to be translated
    /// </summary>
    internal abstract class AbstractTranslateInfoItem {
        /// <summary>
        /// Text to be translated
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Change the text in the source file
        /// </summary>
        public abstract void ApplyTranslation();     
    }

    /// <summary>
    /// String item loaded from ResX file opened in ResXEditor
    /// </summary>
    internal sealed class StringGridTranslationInfoItem : AbstractTranslateInfoItem {
        /// <summary>
        /// Corresponding row in the strings grid
        /// </summary>
        public ResXStringGridRow Row { get; set; }

        /// <summary>
        /// Name of the column, where Value comes from
        /// </summary>
        public string ValueColumnName { get; set; }

        /// <summary>
        /// Change the text in the source file
        /// </summary>
        public override void ApplyTranslation() {
            ResXStringGrid grid = (ResXStringGrid)Row.DataGridView; // get the grid
            string oldValue = (string)Row.Cells[ValueColumnName].Value; // get current value from the Value column

            Row.Cells[ValueColumnName].Tag = oldValue;
            Row.Cells[ValueColumnName].Value = Value; // set the new value

            // create new ResX node with modified value
            string comment = Row.DataSourceItem.Comment;
            Row.DataSourceItem = new ResXDataNode(Row.Key, Value);
            Row.DataSourceItem.Comment = comment;

            // adds undo unit
            grid.StringValueChanged(Row, oldValue, (string)Row.Cells[ValueColumnName].Value);
            
            // marks the document dirty
            grid.NotifyDataChanged();
        }
    }

    /// <summary>
    /// Represents string item coming from closed ResX file
    /// </summary>
    internal sealed class ResXTranslateInfoItem : AbstractTranslateInfoItem {
        /// <summary>
        /// Lower-case resource key
        /// </summary>
        public string ResourceKey { get; set; }

        /// <summary>
        /// Original resouce key
        /// </summary>
        public string DataKey { get; set; }

        /// <summary>
        /// ResX project item this string comes from
        /// </summary>
        public ResXProjectItem ResXItem { get; set; }

        /// <summary>
        /// Change the text in the source file
        /// </summary>
        public override void ApplyTranslation() {
            ResXItem.Data[ResourceKey] = new System.Resources.ResXDataNode(DataKey, Value);
        }       
    }

    /// <summary>
    /// Represents string item coming from ResX file opened in default VS editor. For performance reasons, all these items are
    /// linked and running ApplyTranslation on any of them will cause ApplyTranslation on all others, thus preventing the buffer 
    /// to be reloaded many times.
    /// </summary>
    internal sealed class BufferTranslateInfoItem : AbstractTranslateInfoItem {
        /// <summary>
        /// Name of the resouce
        /// </summary>
        public string ResourceKey { get; set; }

        /// <summary>
        /// Name of the file
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// File buffer
        /// </summary>
        public IVsTextLines IVsTextLines { get; set; }

        /// <summary>
        /// Link to the previous BufferTranslateInfoItem
        /// </summary>
        public BufferTranslateInfoItem Prev { get; set; }

        /// <summary>
        /// Whether translated string has already been applied
        /// </summary>
        public bool Applied { get; set; }

        /// <summary>
        /// Original ResX file
        /// </summary>
        public GlobalTranslateProjectItem GlobalTranslateItem { get; set; }

        /// <summary>
        /// Change the text in the source file
        /// </summary>
        public override void ApplyTranslation() {
            if (!Applied) {                
                ResXResourceWriter writer = null;
                MemoryStream stream = null;
                try {
                    stream = new MemoryStream();
                    writer = new ResXResourceWriter(stream);
                    writer.BasePath = Path.GetDirectoryName(Filename);

                    // change resource values in all BufferTranslateInfoItems
                    BufferTranslateInfoItem i = this;
                    while (true) {
                        if (i == null || i.Applied) break;
                        writer.AddResource(i.ResourceKey, i.Value);

                        i.Applied = true;
                        i = i.Prev;
                    }
                    foreach (ResXDataNode node in GlobalTranslateItem.NonStringData) {
                        writer.AddResource(node);
                    }

                    writer.Generate();
                    writer.Close();

                    VLDocumentViewsManager.SaveStreamToBuffer(stream, IVsTextLines, false);
                } finally {
                    if (stream != null) stream.Close();
                }
            }
        }
    }
}
