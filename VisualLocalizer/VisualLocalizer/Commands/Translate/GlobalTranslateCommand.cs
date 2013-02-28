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

namespace VisualLocalizer.Commands {
    internal sealed class GlobalTranslateCommand {

        private HashSet<ProjectItem> searchedProjectItems = new HashSet<ProjectItem>();
        private List<ResXProjectItem> loadedResxItems = new List<ResXProjectItem>();

        public void Process(Array array) {
            searchedProjectItems.Clear();
            loadedResxItems.Clear();

            List<GlobalTranslateResultItem> resxFiles = new List<GlobalTranslateResultItem>();
            foreach (UIHierarchyItem o in array) {
                if (o.Object is ProjectItem) {
                    ProjectItem item = (ProjectItem)o.Object;
                    searchForResxFiles(item, resxFiles);
                } else if (o.Object is Project) {
                    Project proj = (Project)o.Object;
                    searchForResxFiles(proj.ProjectItems, resxFiles);
                } else throw new Exception("Unexpected project item type: " + o.Object.GetVisualBasicType());
            }

            GlobalTranslateForm form = new GlobalTranslateForm(resxFiles);
            if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                List<AbstractTranslateInfoItem> data = new List<AbstractTranslateInfoItem>();
                try {
                    ProgressBarHandler.StartIndeterminate();
                    foreach (GlobalTranslateResultItem item in resxFiles)
                        if (item.Checked) {
                            addDataForTranslation(item, data);
                        }
                    ProgressBarHandler.StopIndeterminate();

                    TranslationHandler.Translate(data, form.Provider, form.LanguagePair.FromLanguage, form.LanguagePair.ToLanguage);

                    ProgressBarHandler.StartIndeterminate();
                    foreach (AbstractTranslateInfoItem i in data) {
                        i.ApplyTranslation();
                    }   
                } finally {
                    foreach (ResXProjectItem item in loadedResxItems) {
                        item.Flush();
                        item.Unload();
                    }
                    ProgressBarHandler.StopIndeterminate();
                }
            }
        }

        private void addDataForTranslation(GlobalTranslateResultItem item, List<AbstractTranslateInfoItem> data) {
            string path = item.ProjectItem.GetFullPath();
            if (RDTManager.IsFileOpen(path)) {
                object docData = VLDocumentViewsManager.GetDocData(path);
                if (docData is ResXEditor) {
                    ResXEditor editor = (ResXEditor)docData;
                    editor.UIControl.AddForTranslation(data);
                } else {
                    IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(path, false);
                    string text = VLDocumentViewsManager.GetTextFrom(lines);
                    
                    ResXResourceReader reader = null;
                    
                    BufferTranslateInfoItem prev = null;
                    BufferTranslateInfoItem first = null;
                    
                    try {
                        reader = ResXResourceReader.FromFileContents(text);
                        reader.UseResXDataNodes = true;
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
            } else {
                ResXProjectItem resxItem = ResXProjectItem.ConvertToResXItem(item.ProjectItem, item.ProjectItem.ContainingProject);
                resxItem.Load();
                loadedResxItems.Add(resxItem);

                resxItem.AddAllStringReferencesUnique(data);
            }
        }

        private void searchForResxFiles(ProjectItem item, List<GlobalTranslateResultItem> resxFiles) {
            if (searchedProjectItems.Contains(item)) return;
            searchForResxFiles(item.ProjectItems, resxFiles);

            if (ResXProjectItem.IsItemResX(item)) {
                GlobalTranslateResultItem r = new GlobalTranslateResultItem(item);                
                r.Checked = false;
                r.Readonly = VLDocumentViewsManager.IsFileLocked(item.GetFullPath());

                resxFiles.Add(r);
            }
        }

        private void searchForResxFiles(ProjectItems items, List<GlobalTranslateResultItem> resxFiles) {
            if (items == null) return;
            foreach (ProjectItem item in items)
                searchForResxFiles(item, resxFiles);
        }
    }

    internal sealed class GlobalTranslateResultItem {
        public GlobalTranslateResultItem(ProjectItem item) {
            this.ProjectItem = item;
            NonStringData = new List<ResXDataNode>();
            string filePath = item.GetFullPath();

            if (item.ContainingProject != null && VisualLocalizerPackage.Instance.DTE.Solution.ContainsProjectItem(item)) {
                string projPath = item.ContainingProject.FullName;
                this.path = new Uri(projPath).MakeRelativeUri(new Uri(filePath)).ToString();
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
            return path;
        }
    }

    internal abstract class AbstractTranslateInfoItem {
        public string Value { get; set; }
        public abstract void ApplyTranslation();     
    }

    internal sealed class ResXTranslateInfoItem : AbstractTranslateInfoItem {
        public string ResourceKey { get; set; }
        public string DataKey { get; set; }
        public ResXProjectItem ResXItem { get; set; }

        public override void ApplyTranslation() {
            ResXItem.Data[ResourceKey] = new System.Resources.ResXDataNode(DataKey, Value);
        }       
    }

    internal sealed class BufferTranslateInfoItem : AbstractTranslateInfoItem {
        public string ResourceKey { get; set; }
        public string Filename { get; set; }
        public IVsTextLines IVsTextLines { get; set; }
        public BufferTranslateInfoItem Prev { get; set; }
        public bool Applied { get; set; }
        public GlobalTranslateResultItem GlobalTranslateItem { get; set; }

        public override void ApplyTranslation() {
            if (!Applied) {                
                ResXResourceWriter writer = null;
                MemoryStream stream = null;
                try {
                    stream = new MemoryStream();
                    writer = new ResXResourceWriter(stream);
                    writer.BasePath = Path.GetDirectoryName(Filename);

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
