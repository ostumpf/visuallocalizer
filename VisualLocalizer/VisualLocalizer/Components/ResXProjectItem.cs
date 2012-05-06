using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using VSLangProj;
using System.Collections;
using System.IO;

namespace VisualLocalizer.Components {
    
    public class ResXProjectItem {

        private string _Namespace,_Class;

        public ResXProjectItem(ProjectItem projectItem, string displayName) {
            this.DisplayName = displayName;
            this.ProjectItem = projectItem;
            DesignerItem = ProjectItem.ProjectItems.Item(ProjectItem.Properties.Item("CustomToolOutput").Value);
        }

        public ProjectItem ProjectItem {
            get;
            private set;
        }

        public string DisplayName {
            get;
            private set;
        }

        public void RunCustomTool() {
            (ProjectItem.Object as VSProjectItem).RunCustomTool();
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

        public bool InternalInReferenced {
            get;
            private set;
        }

        public ProjectItem DesignerItem {
            get;
            private set;
        }

        public void SetRelationTo(Project homeProject) {
            bool referenced = homeProject.UniqueName != ProjectItem.ContainingProject.UniqueName;
            bool inter = ProjectItem.Properties.Item("CustomTool").Value.ToString() != StringConstants.PublicResXTool;

            InternalInReferenced = inter && referenced;
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
                    if (child.Kind==vsCMElement.vsCMElementClass) {
                        _Class = child.Name;
                        break;
                    }
                }
            }
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
            if (!string.IsNullOrEmpty(customToolOutput) && !string.IsNullOrEmpty(customTool) && !string.IsNullOrEmpty(extension)
                && (customTool == StringConstants.InternalResXTool || customTool == StringConstants.PublicResXTool)
                && extension == StringConstants.ResXExtension) {
                return true;
            } else 
                return false;
        }

        public static ResXProjectItem ConvertFrom(ProjectItem item) {
            Uri projectUri = new Uri(item.ContainingProject.FileName);
            Uri itemUri = new Uri(item.Properties.Item("FullPath").Value.ToString());
            string path = item.ContainingProject.Name + "/" + projectUri.MakeRelativeUri(itemUri).ToString();

            ResXProjectItem resxitem = new ResXProjectItem(item, path);                       

            return resxitem;
        }

        public override string ToString() {
            return (InternalInReferenced ? "(internal) ":"")+DisplayName;
        }        
    }
}
