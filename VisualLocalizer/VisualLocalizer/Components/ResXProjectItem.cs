using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using VSLangProj;
using System.Collections;

namespace VisualLocalizer.Editor {
    internal sealed class ResXProjectItem {

        private string _Namespace,_Class;

        public ResXProjectItem(ProjectItem projectItem, string displayName) {
            this.DisplayName = displayName;
            this.ProjectItem = projectItem;
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

        private void resolveNamespaceClass() {
            ProjectItem designerItem = null;
            foreach (ProjectItem item in ProjectItem.ProjectItems)
                if (designerItem == null) {
                    designerItem = item;
                    break;
                }

            CodeElement nmspcElemet = null;
            foreach (CodeElement element in designerItem.FileCodeModel.CodeElements) {
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

        public override string ToString() {
            return DisplayName;
        }
    }
}
