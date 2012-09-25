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

namespace VisualLocalizer.Components {

    public enum CONTAINS_KEY_RESULT { EXISTS_WITH_SAME_VALUE, EXISTS_WITH_DIFF_VALUE, DOESNT_EXIST }

    public class ResXProjectItem {

        private string _Namespace,_Class;
        private Dictionary<string, ResXDataNode> data;                
        private bool dataChangedInBatchMode;

        public ResXProjectItem(ProjectItem projectItem, string displayName,bool internalInReferenced) {
            this.DisplayName = displayName;
            this.InternalProjectItem = projectItem;
            this.DesignerItem = InternalProjectItem.ProjectItems.Item(InternalProjectItem.Properties.Item("CustomToolOutput").Value);
            this.MarkedInternalInReferencedProject = internalInReferenced;
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
            get {
                if (_Namespace == null) {
                    resolveNamespaceClass();                   
                }

                return _Namespace;
            }
        }

        public string Class {
            get {
                if (_Class == null) {
                    resolveNamespaceClass();
                }

                return _Class;
            }
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
                    foreach (var pair in data) {
                        writer.AddResource(pair.Value);
                    }
                    writer.Generate();
                } finally {
                    if (writer != null) writer.Close();
                }
            }

            RDTManager.SilentlyModifyFile(DesignerItem.Properties.Item("FullPath").Value.ToString(), (string p) => {
                RunCustomTool();
            });
        }

        public void AddString(string key, string value) {
            VLOutputWindow.VisualLocalizerPane.WriteLine("Adding \"{0}\":\"{1}\" to \"{2}\"", key, value, DisplayName);            

            if (!IsLoaded) Load();

            ResXDataNode node = new ResXDataNode(key, value);
            if (data.ContainsKey(key)) {
                data[key] = node;
            } else {
                data.Add(key, node);
            }

            if (IsInBatchMode) {
                dataChangedInBatchMode = true;
            } else {
                Flush();
            }
        }

        public void RemoveKey(string key) {
            VLOutputWindow.VisualLocalizerPane.WriteLine("Removing \"{0}\" from \"{1}\"", key, DisplayName);
            if (!IsLoaded) Load();
            
            data.Remove(key);             

            if (IsInBatchMode) {
                dataChangedInBatchMode = true;
            } else {
                Flush();
            }
        }
       
        public string GetString(string key) {
            if (!IsLoaded) Load();

            return data[key].GetStringValue();
        }

        public CONTAINS_KEY_RESULT StringKeyInConflict(string key, string value) {
            if (!IsLoaded) Load();
            if (data.ContainsKey(key)) {
                if (data[key].HasStringValue()) {
                    if (string.Compare(data[key].GetStringValue(), value, false, CultureInfo.CurrentCulture) == 0) {
                        return CONTAINS_KEY_RESULT.EXISTS_WITH_SAME_VALUE;
                    } else {
                        return CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE;
                    }
                } else return CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE;
            } else return CONTAINS_KEY_RESULT.DOESNT_EXIST;
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
                        data.Add(entry.Key.ToString(), entry.Value as ResXDataNode);
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
            if (!IsLoaded) Load();

            foreach (var pair in data) {
                if (pair.Value.HasStringValue())
                    AllReferences.Add(Class + "." + GetPropertyNameForKey(pair.Key), pair.Value.GetStringValue());
            }

            return AllReferences;
        }

        private CodeNamespace designerNamespaceElement = null;
        private CodeClass designerClassElement = null;
        private string GetPropertyNameForKey(string key) {
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

            CodeProperty propertyElement = null;
            foreach (CodeElement e in designerClassElement.Children)
                if (e.Kind == vsCMElement.vsCMElementProperty) {
                    propertyElement = (CodeProperty)e;
                    string getterText = propertyElement.GetText();
                    Match matchResult = Regex.Match(getterText, @"^\s*get\s*\{\s*return\s*\w+\.GetString\("""+key+@""",\s*\w+\);\s*\}\s*$");
                    if (matchResult.Success) {
                        break;
                    }                    
                }
            if (propertyElement == null) throw new InvalidOperationException(string.Format("Cannot find property for key {0}.", key));

            return propertyElement.Name;
        }

        public static bool IsItemResX(ProjectItem item) {
            string customTool = null, customToolOutput = null, extension = null;
            foreach (Property prop in item.Properties) {
                if (prop.Name == "CustomTool")
                    customTool = prop.Value.ToString();
                if (prop.Name == "CustomToolOutput")
                    customToolOutput = prop.Value.ToString();
                if (prop.Name == "Extension")
                    extension = prop.Value.ToString();
            }
            return (!string.IsNullOrEmpty(customToolOutput) && !string.IsNullOrEmpty(customTool) && !string.IsNullOrEmpty(extension)
                && (customTool == StringConstants.InternalResXTool || customTool == StringConstants.PublicResXTool)
                && extension == StringConstants.ResXExtension);
        }

        public static ResXProjectItem ConvertToResXItem(ProjectItem item,Project relationProject) {
            Uri projectUri = new Uri(item.ContainingProject.FileName);
            Uri itemUri = new Uri(item.Properties.Item("FullPath").Value.ToString());
            string path = item.ContainingProject.Name + "/" + projectUri.MakeRelativeUri(itemUri).ToString();

            bool referenced = relationProject.UniqueName != item.ContainingProject.UniqueName;
            bool inter = item.Properties.Item("CustomTool").Value.ToString() != StringConstants.PublicResXTool;

            bool internalInReferenced = inter && referenced;

            ResXProjectItem resxitem = new ResXProjectItem(item, path,internalInReferenced);                       

            return resxitem;
        }        

        private void resolveNamespaceClass() {
            if (!File.Exists(DesignerItem.Properties.Item("FullPath").Value.ToString())) {
                RunCustomTool();
            }
            CodeElement nmspcElemet = null;
            foreach (CodeElement element in DesignerItem.FileCodeModel.CodeElements) {
                _Namespace = element.FullName;
                nmspcElemet = element;
                break;
            }
            if (nmspcElemet != null) {
                foreach (CodeElement child in nmspcElemet.Children) {
                    if (child.Kind == vsCMElement.vsCMElementClass) {
                        _Class = child.Name;
                        break;
                    }
                }
            }
        }

        public string ToStringValue {
            get {
                return (MarkedInternalInReferencedProject ? "(internal) " : "") + DisplayName;
            }
        }

        public override string ToString() {
            return ToStringValue;
        }

        public override bool Equals(object obj) {
            return ReferenceEquals(this, obj);
        }
       
    }

    
}
