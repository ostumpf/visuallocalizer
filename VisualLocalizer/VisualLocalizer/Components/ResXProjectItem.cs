using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using VSLangProj;
using System.Collections;
using System.IO;
using System.Resources;
using VisualLocalizer.Library;
using EnvDTE80;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using System.Globalization;
using System.ComponentModel.Design;
using VisualLocalizer.Extensions;
using VisualLocalizer.Commands;

namespace VisualLocalizer.Components {

    public enum CONTAINS_KEY_RESULT { EXISTS_WITH_SAME_VALUE, EXISTS_WITH_DIFF_VALUE, DOESNT_EXIST }

    public class ResXProjectItem {

        private Dictionary<string, ResXDataNode> data;                
        private bool dataChangedInBatchMode;

        private ResXProjectItem(ProjectItem projectItem, string displayName,bool internalInReferenced) {
            this.DisplayName = displayName;
            this.InternalProjectItem = projectItem;

            string customToolOutput=InternalProjectItem.GetCustomToolOutput();
            if (string.IsNullOrEmpty(customToolOutput)) {
                this.DesignerItem = null;
            } else {
                if (InternalProjectItem.ProjectItems.ContainsItem(customToolOutput)) {
                    this.DesignerItem = InternalProjectItem.ProjectItems.Item(customToolOutput);
                } else {
                    this.DesignerItem = null;
                }
            }

            this.MarkedInternalInReferencedProject = internalInReferenced;
        }

        public bool IsCultureSpecific() {
            return InternalProjectItem.IsCultureSpecificResX();
        }

        public string GetCultureNeutralName() {
            return InternalProjectItem.GetResXCultureNeutralName();
        }

        public bool IsProjectDefault(Project project) {
            object parent = InternalProjectItem.Collection.Parent;
            if (!(parent is ProjectItem)) return false;
            if (InternalProjectItem.ContainingProject != project) return false;
            if (InternalProjectItem.Name.ToLower() != "resources.resx") return false;

            ProjectItem pitem = (ProjectItem)parent;
            return pitem.Name == "Resources" && pitem.Kind.ToUpper() == StringConstants.PhysicalFolder;
        }

        public bool IsDependantOn(ProjectItem item) {
            if (item == null) return false;
            bool dep = InternalProjectItem.GetIsDependent();
            if (!dep) return false;

            object parent = InternalProjectItem.Collection.Parent;
            if (!(parent is ProjectItem)) return false;

            ProjectItem pitem = (ProjectItem)parent;
            return pitem == item;
        }

        public void ModifyKey(string key, string newValue) {
            if (data == null) throw new Exception("Cannot modify key " + key + " - data not loaded.");
            if (!data.ContainsKey(key)) throw new Exception("Cannot modify key " + key + " - key does not exist.");

            data[key] = new ResXDataNode(key, newValue);
        }

        public ProjectItem InternalProjectItem {
            get;
            private set;
        }

        public string DisplayName {
            get;
            private set;
        }

        public string Namespace {
            get;
            private set;
        }

        public string Class {
            get;
            private set;
        }

        public bool MarkedInternalInReferencedProject {
            get;
            private set;
        }

        public ProjectItem DesignerItem {
            get;
            private set;
        }

        public bool IsLoaded {
            get;
            private set;
        }

        public bool IsInBatchMode {
            get;
            private set;
        }

        public void RunCustomTool() {
            (InternalProjectItem.Object as VSProjectItem).RunCustomTool();
        }

        public void BeginBatch() {
            IsInBatchMode = true;
            dataChangedInBatchMode = false;
        }

        public void EndBatch() {            
            Flush();
            IsInBatchMode = false;
        }

        public Dictionary<string, ResXDataNode> Data {
            get {
                return data;
            }
        }

        public void Flush() {
            if (!dataChangedInBatchMode && IsInBatchMode) return;
            string path = InternalProjectItem.GetFullPath();
            
            if (RDTManager.IsFileOpen(path)) {
                VLDocumentViewsManager.SaveDataToBuffer(data, path);
            } else {
                ResXResourceWriter writer = null;
                try {
                    writer = new ResXResourceWriter(path);
                    writer.BasePath = Path.GetDirectoryName(path);

                    foreach (var pair in data) {
                        writer.AddResource(pair.Value);
                    }
                    writer.Generate();
                } finally {
                    if (writer != null) writer.Close();
                }
            }

            if (DesignerItem != null) {
                RDTManager.SilentlyModifyFile(DesignerItem.GetFullPath(), (string p) => {
                    RunCustomTool();
                });
            }
        }

        public void AddString(string key, string value) {
            VLOutputWindow.VisualLocalizerPane.WriteLine("Adding \"{0}\":\"{1}\" to \"{2}\"", key, value, DisplayName);
            bool wasLoaded = IsLoaded;

            if (!IsLoaded) Load();
            string lowerKey = key.ToLower();

            ResXDataNode node = new ResXDataNode(key, value);
            if (data.ContainsKey(lowerKey)) {
                data[lowerKey] = node;
            } else {
                data.Add(lowerKey, node);
            }

            if (IsInBatchMode) {
                dataChangedInBatchMode = true;
            } else {
                Flush();
                if (!wasLoaded) Unload();
            }
        }

        public void RemoveKey(string key) {
            VLOutputWindow.VisualLocalizerPane.WriteLine("Removing \"{0}\" from \"{1}\"", key, DisplayName);
            bool wasLoaded = IsLoaded;
            if (!IsLoaded) Load();
            
            data.Remove(key.ToLower());             

            if (IsInBatchMode) {
                dataChangedInBatchMode = true;
            } else {
                Flush();
                if (!wasLoaded) Unload();
            }
        }
       
        public string GetString(string key) {
            bool wasLoaded = IsLoaded;            
            if (!IsLoaded) Load();

            string value = data[key.ToLower()].GetValue<string>();
            if (!wasLoaded) Unload();
            
            return value;
        }

        public CONTAINS_KEY_RESULT StringKeyInConflict(string key, string value) {
            bool wasLoaded = IsLoaded;
            if (!IsLoaded) Load();
            if (string.IsNullOrEmpty(key)) return CONTAINS_KEY_RESULT.DOESNT_EXIST;
            string lowerKey = key.ToLower();

            CONTAINS_KEY_RESULT status;
            if (data.ContainsKey(lowerKey)) {
                if (data[lowerKey].HasValue<string>()) {
                    if (string.Compare(data[lowerKey].GetValue<string>(), value, false, CultureInfo.CurrentCulture) == 0) {
                        status = CONTAINS_KEY_RESULT.EXISTS_WITH_SAME_VALUE; 
                    } else {
                        status = CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE;
                    }
                } else status = CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE;
            } else status = CONTAINS_KEY_RESULT.DOESNT_EXIST;

            if (!wasLoaded) Unload();

            return status;
        }

        public string GetRealKey(string key) {
            bool wasLoaded = IsLoaded;
            if (!IsLoaded) Load();
            
            string lowerKey=key.ToLower();
            string realKey;

            if (data.ContainsKey(lowerKey)) {
                realKey = data[lowerKey].Name;
            } else {
                realKey = key;
            }

            if (!wasLoaded) Unload();
            return realKey;
        }

        public void Load() {
            if (IsLoaded) return;

            string path = InternalProjectItem.GetFullPath();

            if (RDTManager.IsFileOpen(path)) {
                VLDocumentViewsManager.LoadDataFromBuffer(ref data, path);
            } else {
                ResXResourceReader reader = null;
                try {
                    data = new Dictionary<string, ResXDataNode>();
                    reader = new ResXResourceReader(path);
                    reader.BasePath = Path.GetDirectoryName(path);
                    reader.UseResXDataNodes = true;

                    foreach (DictionaryEntry entry in reader) {
                        data.Add(entry.Key.ToString().ToLower(), entry.Value as ResXDataNode);
                    }
                } finally {
                    if (reader != null) reader.Close();
                }
            }

            IsLoaded = true;
        }

        public void Unload() {
            if (data != null) {
                data.Clear();
                data = null;
            }
            IsLoaded = false;           
        }

        public Dictionary<string, string> GetAllStringReferences(bool addClass) {
            Dictionary<string, string> AllReferences = new Dictionary<string, string>();
            bool wasLoaded = IsLoaded;
            if (!IsLoaded) Load();

            foreach (var pair in data) {
                if (pair.Value.HasValue<string>()) {
                    string reference = (addClass ? Class + ".":"") + pair.Value.Name;

                    if (!AllReferences.ContainsKey(reference)) {                       
                        AllReferences.Add(reference, pair.Value.GetValue<string>());
                    }
                }
            }
            if (!wasLoaded) Unload();

            return AllReferences;
        }     

        public static bool IsItemResX(ProjectItem item) {
            if (item == null) return false;
            if (item.Properties == null) return false;
            
            string ext = null;
            foreach (Property prop in item.Properties)
                if (prop.Name == "Extension") {
                    ext = (string)item.Properties.Item("Extension").Value;
                    break;
                }
            if (ext == null) {
                ext = Path.GetExtension(item.GetFullPath());
            }

            return ext == StringConstants.ResXExtension;
        }

        public static ResXProjectItem ConvertToResXItem(ProjectItem item, Project relationProject) {
            if (item == null) return null;
            
            string projectPath = item.ContainingProject.FullName;
           
            Uri projectUri = new Uri(projectPath, UriKind.Absolute);
            Uri itemUri = new Uri(item.GetFullPath());
          
            string path;
            if (item.ContainingProject.Kind.ToUpper() == StringConstants.WebSiteProject) {                
                path = projectUri.MakeRelativeUri(itemUri).ToString();
            } else {
                path = item.ContainingProject.Name + "/" + projectUri.MakeRelativeUri(itemUri).ToString();          
            }
           
            bool referenced = relationProject.UniqueName != item.ContainingProject.UniqueName;          
            bool inter = item.GetCustomTool() != StringConstants.PublicResXTool;
          
            bool internalInReferenced = inter && referenced;

            ResXProjectItem resxitem = new ResXProjectItem(item, path,internalInReferenced);
                     
            return resxitem;
        }

        public void ResolveNamespaceClass(List<ResXProjectItem> neutralItems) {
            Class = null;
            Namespace = null;

            if (IsCultureSpecific()) DesignerItem = getNeutralDesignerItem(neutralItems);

            if (DesignerItem != null) {                
                if (!File.Exists(DesignerItem.GetFullPath())) RunCustomTool();

                CodeElement nmspcElemet = null;
                foreach (CodeElement element in DesignerItem.FileCodeModel.CodeElements) {
                    Namespace = element.FullName;
                    nmspcElemet = element;
                    break;
                }
                if (nmspcElemet != null) {
                    foreach (CodeElement child in nmspcElemet.Children) {
                        if (child.Kind == vsCMElement.vsCMElementClass) {
                            Class = child.Name;
                            break;
                        }
                    }
                }
            } else {
                if (InternalProjectItem.ContainingProject != null &&
                    InternalProjectItem.ContainingProject.Kind.ToUpper() == StringConstants.WebSiteProject) {
                    string relative = InternalProjectItem.GetRelativeURL();

                    if (!string.IsNullOrEmpty(relative) && relative.StartsWith(StringConstants.GlobalWebSiteResourcesFolder)) {
                        Class = Path.GetFileNameWithoutExtension(InternalProjectItem.GetFullPath());
                        if (IsCultureSpecific()) {
                            Class = GetCultureNeutralName();
                            Class = Class.Substring(0, Class.IndexOf('.'));
                        }
                        Namespace = StringConstants.GlobalWebSiteResourcesNamespace;
                    } else {
                        Class = null;
                        Namespace = null;
                    }
                }
            }         
        }

        private ProjectItem getNeutralDesignerItem(List<ResXProjectItem> neutralItems) {
            string cultureNeutralName = GetCultureNeutralName();
            ResXProjectItem neutralItem = null;
            foreach (ResXProjectItem item in neutralItems) {
                if (item.IsCultureSpecific()) continue;

                string neutralDir = Path.GetFullPath(Path.GetDirectoryName(item.InternalProjectItem.GetFullPath()));
                string specificDir = Path.GetFullPath(Path.GetDirectoryName(InternalProjectItem.GetFullPath()));

                if (neutralDir == specificDir && item.InternalProjectItem.Name.ToLowerInvariant() == cultureNeutralName.ToLowerInvariant()) {
                    neutralItem = item;
                    break;
                }
            }

            if (neutralItem != null) {
                return neutralItem.DesignerItem;
            } else return null;
        }  

        public override string ToString() {
            return (MarkedInternalInReferencedProject ? "(internal) " : "") + DisplayName;
        }

        public override int GetHashCode() {
            return InternalProjectItem.GetHashCode();
        }

        public override bool Equals(object obj) {
            if (obj == null) return false;
            ResXProjectItem copy = obj as ResXProjectItem;

            if (copy == null) return false;
            return InternalProjectItem.Equals(copy.InternalProjectItem);
        }


        internal void AddAllStringReferencesUnique(List<AbstractTranslateInfoItem> outputData) {
            bool wasLoaded = IsLoaded;
            if (!IsLoaded) Load();

            foreach (var pair in data) {
                if (pair.Value.HasValue<string>()) {
                    ResXTranslateInfoItem key = new ResXTranslateInfoItem();
                    key.ResXItem = this;
                    key.ResourceKey = pair.Key;
                    key.DataKey = pair.Value.Name;
                    key.Value = pair.Value.GetValue<string>();
                    outputData.Add(key);
                }
            }
            if (!wasLoaded) Unload();
        }
    }

    
}
