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

namespace VisualLocalizer.Components {
    
    public class ResXProjectItem {

        private string _Namespace,_Class;
        private Dictionary<string, object> data;

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

        public void RunCustomTool() {
            (InternalProjectItem.Object as VSProjectItem).RunCustomTool();
        }

        public void AddString(string key, string value) {
            VLOutputWindow.VisualLocalizerPane.WriteLine("Adding \"{0}\":\"{1}\" to \"{2}\"", key, value, DisplayName);
            /*   foreach (Property prop in item.ProjectItem.Properties)
                   VLOutputWindow.VisualLocalizerPane.WriteLine(prop.Name+":"+prop.Value);*/


            RDTManager.SilentlyModifyFile(InternalProjectItem.Properties.Item("FullPath").Value.ToString(), (string p) => {
                ResXResourceReader reader = new ResXResourceReader(p);
                reader.BasePath = Path.GetDirectoryName(p);

                Hashtable content = new Hashtable();
                foreach (DictionaryEntry entry in reader) {
                    content.Add(entry.Key, entry.Value);
                }
                reader.Close();

                bool overwritten = false;
                if (content.ContainsKey(key)) {
                    content[key] = value;

                    if (IsLoaded) {
                        data[key] = value;
                    }

                    overwritten = true;
                } else {
                    if (IsLoaded) {
                        data.Add(key, value);
                    }
                }

                ResXResourceWriter writer = new ResXResourceWriter(p);
                foreach (DictionaryEntry entry in content) {
                    writer.AddResource(entry.Key.ToString(), entry.Value);
                }
                if (!overwritten) writer.AddResource(key, value);
                writer.Close();
            });

            RDTManager.SilentlyModifyFile(DesignerItem.Properties.Item("FullPath").Value.ToString(), (string p) => {
                RunCustomTool();
            });
        }

        public void RemoveKey(string key) {
            VLOutputWindow.VisualLocalizerPane.WriteLine("Removing \"{0}\" from \"{1}\"", key, DisplayName);
          
            RDTManager.SilentlyModifyFile(InternalProjectItem.Properties.Item("FullPath").Value.ToString(), (string p) => {

                ResXResourceReader reader = new ResXResourceReader(p);
                reader.BasePath = Path.GetDirectoryName(p);

                Hashtable content = new Hashtable();
                foreach (DictionaryEntry entry in reader) {
                    content.Add(entry.Key, entry.Value);
                }
                reader.Close();

                ResXResourceWriter writer = new ResXResourceWriter(p);
                foreach (DictionaryEntry entry in content) {
                    if (entry.Key.ToString() != key)
                        writer.AddResource(entry.Key.ToString(), entry.Value);
                }
                writer.Close();

                if (IsLoaded) {
                    data.Remove(key);
                }
            });

            RDTManager.SilentlyModifyFile(DesignerItem.Properties.Item("FullPath").Value.ToString(), (string p) => {
                RunCustomTool();
            });

        }
       
        public string GetString(string key) {
            string value = null;

            if (IsLoaded) {
                value = data[key].ToString();
            } else {
                string path = InternalProjectItem.Properties.Item("FullPath").Value.ToString();

                ResXResourceReader reader = new ResXResourceReader(path);
                reader.BasePath = Path.GetDirectoryName(path);
                
                foreach (DictionaryEntry entry in reader) {
                    if (entry.Key.ToString() == key) {
                        value = entry.Value.ToString();
                        break;
                    }
                }
                reader.Close();
            }
            return value;
        }

        public bool ContainsKey(string key) {
            if (IsLoaded) {
                return data.ContainsKey(key);
            } else {
                Load();
                bool result = data.ContainsKey(key);
                Unload();

                return result;
            }
        }

        public void Load() {
            data = new Dictionary<string, object>();
            string path = InternalProjectItem.Properties.Item("FullPath").Value.ToString();

            ResXResourceReader reader = new ResXResourceReader(path);
            reader.BasePath = Path.GetDirectoryName(path);

            foreach (DictionaryEntry entry in reader) {
                data.Add(entry.Key.ToString(), entry.Value);
            }
            reader.Close();
            IsLoaded = true;
        }

        public void Unload() {
            if (data != null) {
                data.Clear();
                data = null;
            }
            IsLoaded = false;
        }

        public string GetKeyForPropertyName(string propertyName) {
            CodeNamespace nmspcElement = null;
            foreach (CodeElement e in DesignerItem.FileCodeModel.CodeElements)
                if (e.Kind == vsCMElement.vsCMElementNamespace && e.FullName == Namespace) {
                    nmspcElement = (CodeNamespace)e;
                    break;
                }
            if (nmspcElement == null) throw new InvalidOperationException("Unexpected structure of ResX designer file.");

            CodeClass classElement = null;
            foreach (CodeElement e in nmspcElement.Children)
                if (e.Kind == vsCMElement.vsCMElementClass && e.Name == Class) {
                    classElement = (CodeClass)e;
                    break;
                }
            if (classElement == null) throw new InvalidOperationException("Unexpected structure of ResX designer file.");

            CodeProperty propertyElement = null;
            foreach (CodeElement e in classElement.Children)
                if (e.Kind == vsCMElement.vsCMElementProperty && e.Name == propertyName) {
                    propertyElement = (CodeProperty)e;
                    break;
                }
            if (propertyElement == null) throw new InvalidOperationException(string.Format("Cannot find property {0}.", propertyName));

            TextPoint startPoint = propertyElement.Getter.StartPoint;
            TextPoint endPoint = propertyElement.Getter.EndPoint;
            string getterText = startPoint.CreateEditPoint().GetText(endPoint);                       
            if (getterText == null) throw new InvalidOperationException(string.Format("Cannot read getter of property {0}.", propertyName));

            Match matchResult = Regex.Match(getterText, @"^\s*get\s*\{\s*return\s*\w+\.GetString\((.*),\s*\w+\);\s*\}\s*$");
            if (matchResult.Groups.Count != 2) throw new InvalidOperationException(string.Format("Cannot match getter of property {0}.",propertyName));

            string groupValue = matchResult.Groups[1].Value;
            string key = groupValue.Substring(1, groupValue.Length - 2);

            return key;
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

        public override string ToString() {
            return (MarkedInternalInReferencedProject ? "(internal) ":"")+DisplayName;
        }

       
    }
}
