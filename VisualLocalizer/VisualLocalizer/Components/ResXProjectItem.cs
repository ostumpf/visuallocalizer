﻿using System;
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

namespace VisualLocalizer.Components {

    public enum CONTAINS_KEY_RESULT { EXISTS_WITH_SAME_VALUE, EXISTS_WITH_DIFF_VALUE, DOESNT_EXIST }

    public class ResXProjectItem {

        private Dictionary<string, ResXDataNode> data;                
        private bool dataChangedInBatchMode;

        private ResXProjectItem(ProjectItem projectItem, string displayName,bool internalInReferenced) {
            this.DisplayName = displayName;
            this.InternalProjectItem = projectItem;

            if (string.IsNullOrEmpty((string)InternalProjectItem.Properties.Item("CustomToolOutput").Value)) {
                this.DesignerItem = null;
            } else {
                this.DesignerItem = InternalProjectItem.ProjectItems.Item(InternalProjectItem.Properties.Item("CustomToolOutput").Value);
            }

            this.MarkedInternalInReferencedProject = internalInReferenced;
        }

        public bool IsCultureSpecific() {
            return Regex.IsMatch(InternalProjectItem.Name, @".*\..+\.resx", RegexOptions.IgnoreCase);                       
        }

        public string GetCultureNeutralName() {
            Match m = Regex.Match(InternalProjectItem.Name, @"(.*)\..+(\.resx)", RegexOptions.IgnoreCase);
            if (!m.Success || m.Groups.Count<=2) throw new Exception("Project item is culture neutral!");

            return m.Groups[1].Value+m.Groups[2].Value;
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

        private void Flush() {
            if (!dataChangedInBatchMode && IsInBatchMode) return;
            string path = InternalProjectItem.Properties.Item("FullPath").Value.ToString();  
            
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
                RDTManager.SilentlyModifyFile(DesignerItem.Properties.Item("FullPath").Value.ToString(), (string p) => {
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

            string path = InternalProjectItem.Properties.Item("FullPath").Value.ToString();                        

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
            designerNamespaceElement = null;
            designerClassElement = null;
        }

        public Dictionary<string, string> GetAllStringReferences() {
            Dictionary<string, string> AllReferences = new Dictionary<string, string>();
            bool wasLoaded = IsLoaded;
            if (!IsLoaded) Load();

            foreach (var pair in data) {
                if (pair.Value.HasValue<string>()) {
                    string property = GetPropertyNameForKey(pair.Value.Name);
                    string reference = Class + "." + property;

                    if (AllReferences.ContainsKey(reference)) {
                        AllReferences.Remove(reference);
                    } else {
                        AllReferences.Add(reference, pair.Value.GetValue<string>());
                    }
                }
            }
            if (!wasLoaded) Unload();

            return AllReferences;
        }

        private CodeNamespace designerNamespaceElement = null;
        private CodeClass designerClassElement = null;
        private string GetPropertyNameForKey(string key) {
            if (DesignerItem == null) return key;

            if (designerNamespaceElement == null || designerClassElement == null) {                
                foreach (CodeElement e in DesignerItem.FileCodeModel.CodeElements)
                    if (e.Kind == vsCMElement.vsCMElementNamespace && e.FullName == Namespace) {
                        designerNamespaceElement = (CodeNamespace)e;
                        break;
                    }
                if (designerNamespaceElement == null) throw new InvalidOperationException("Unexpected structure of ResX designer file.");
                
                foreach (CodeElement e in designerNamespaceElement.Children)
                    if (e.Kind == vsCMElement.vsCMElementClass && e.Name == Class) {
                        designerClassElement = (CodeClass)e;
                        break;
                    }
                if (designerClassElement == null) throw new InvalidOperationException("Unexpected structure of ResX designer file.");
            }

            CodeProperty foundElement = null;
            foreach (CodeElement e in designerClassElement.Children)
                if (e.Kind == vsCMElement.vsCMElementProperty) {
                    CodeProperty propertyElement = (CodeProperty)e;
                    string getterText = propertyElement.GetText();
                    Match matchResult = Regex.Match(getterText, @"^\s*get\s*\{\s*return\s*\w+\.GetString\("""+key+@""",\s*\w+\);\s*\}\s*$");
                    if (matchResult.Success) {
                        foundElement = propertyElement;
                        break;
                    }                    
                }

            return foundElement == null ? key : foundElement.Name;
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

            return ext == StringConstants.ResXExtension;
        }

        public static ResXProjectItem ConvertToResXItem(ProjectItem item, Project relationProject) {
            string projectPath=item.ContainingProject.Properties.Item("FullPath").Value.ToString();
            Uri projectUri = new Uri(projectPath, UriKind.Absolute);
            Uri itemUri = new Uri(item.Properties.Item("FullPath").Value.ToString());
            
            string path;
            if (item.ContainingProject.Kind.ToUpper() == StringConstants.WebSiteProject) {
                path = projectUri.MakeRelativeUri(itemUri).ToString();
            } else {
                path = item.ContainingProject.Name + "/" + projectUri.MakeRelativeUri(itemUri).ToString();
            }

            bool referenced = relationProject.UniqueName != item.ContainingProject.UniqueName;
            bool inter = (string)item.Properties.Item("CustomTool").Value != StringConstants.PublicResXTool;

            bool internalInReferenced = inter && referenced;

            ResXProjectItem resxitem = new ResXProjectItem(item, path,internalInReferenced);            

            return resxitem;
        }

        public void ResolveNamespaceClass(List<ResXProjectItem> neutralItems) {
            Class = null;
            Namespace = null;

            if (DesignerItem == null) {
                if (InternalProjectItem != null && InternalProjectItem.ContainingProject != null &&
                    InternalProjectItem.ContainingProject.Kind.ToUpper() == StringConstants.WebSiteProject) {
                    string relative = (string)InternalProjectItem.Properties.Item("RelativeURL").Value;

                    if (!string.IsNullOrEmpty(relative) && relative.StartsWith(StringConstants.GlobalWebSiteResourcesFolder)) {
                        Class = Path.GetFileNameWithoutExtension((string)InternalProjectItem.Properties.Item("FullPath").Value);
                        Namespace = StringConstants.GlobalWebSiteResourcesNamespace;
                    } else {
                        Class = "!";
                        Namespace = "!";
                    }
                }

            } else {
                if (!File.Exists(DesignerItem.Properties.Item("FullPath").Value.ToString())) RunCustomTool();

                if (IsCultureSpecific()) {
                    string cultureNeutralName = GetCultureNeutralName();
                    ResXProjectItem neutralItem = null;
                    foreach (ResXProjectItem item in neutralItems) {
                        if (item.IsCultureSpecific()) continue;

                        string neutralDir = Path.GetFullPath(Path.GetDirectoryName((string)item.InternalProjectItem.Properties.Item("FullPath").Value));
                        string specificDir = Path.GetFullPath(Path.GetDirectoryName((string)InternalProjectItem.Properties.Item("FullPath").Value));
                        
                        if (neutralDir == specificDir && item.InternalProjectItem.Name.ToLowerInvariant() == cultureNeutralName.ToLowerInvariant()) {
                            neutralItem = item;
                            break;
                        }
                    }

                    if (neutralItem != null) {
                        DesignerItem = neutralItem.DesignerItem;
                    } else {
                        Namespace = null;
                        Class = cultureNeutralName.Substring(0, cultureNeutralName.IndexOf('.'));
                        return;
                    }
                } 

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
            }

        }  

        public override string ToString() {
            return (MarkedInternalInReferencedProject ? "(internal) " : "") + DisplayName;
        }

        public override int GetHashCode() {
            return InternalProjectItem.GetHashCode();
        }

        public override bool Equals(object obj) {
            return ReferenceEquals(this, obj);
        }
       
    }

    
}
