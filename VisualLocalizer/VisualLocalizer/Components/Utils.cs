using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using System.Diagnostics;

namespace VisualLocalizer.Components {
    internal static class Utils {

        internal static string TypeOf(object o) {
            return Microsoft.VisualBasic.Information.TypeName(o);
        }

        internal static string CreateKeyFromValue(string value) {
            StringBuilder builder = new StringBuilder();
            bool upper = true;

            foreach (char c in value)
                if (char.IsLetterOrDigit(c)) {
                    if (upper) {
                        builder.Append(char.ToUpperInvariant(c));
                    } else {
                        builder.Append(c);
                    }
                    upper = false;
                } else if (char.IsWhiteSpace(c)) {
                    upper = true;
                }

            return builder.ToString();
        }

        private static List<ResXProjectItem> GetResourceFilesOf(string path, ProjectItems items) {
            List<ResXProjectItem> list = new List<ResXProjectItem>();

            foreach (ProjectItem item in items) {
                string type = string.Empty;
                
                try {
                    type = item.Properties.Item("ItemType").Value.ToString();
                } catch (Exception) { }

                if (item.FileCount == 1 && item.ProjectItems.Count <= 1
                    && item.Name.ToLowerInvariant().EndsWith(".resx") && type == "EmbeddedResource") {
                    list.Add(new ResXProjectItem(item, path + "/" + item.Name));
                } else if (item.ProjectItems.Count > 0)
                    list.AddRange(GetResourceFilesOf(path+"/"+item.Name,item.ProjectItems));              
            }

            return list;            
        }

        internal static List<ResXProjectItem> GetResourceFilesOf(Projects allProjects, Project project) {
            List<ResXProjectItem> list = new List<ResXProjectItem>();
           
            list.AddRange(GetResourceFilesOf(project.Name,project.ProjectItems));
            foreach (Project proj in allProjects) {         
                if (proj.Kind == VSLangProj.PrjKind.prjKindCSharpProject && proj.UniqueName != project.UniqueName)
                    list.AddRange(GetResourceFilesOf(proj.Name, proj.ProjectItems));
            }
            return list;
        }
    }

    internal sealed class ResXProjectItem {

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

        public override string ToString() {
            return DisplayName;
        }
    }
}
