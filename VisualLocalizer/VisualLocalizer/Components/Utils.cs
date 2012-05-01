using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.CodeDom.Compiler;
using VSLangProj;

namespace VisualLocalizer.Editor {
    internal static class Utils {

        private static CodeDomProvider csharp = Microsoft.CSharp.CSharpCodeProvider.CreateProvider("C#");

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
                } else if (item.ProjectItems.Count > 0) {
                    list.AddRange(GetResourceFilesOf(path + "/" + item.Name, item.ProjectItems));
                }
            }

            return list;            
        }

        internal static List<ResXProjectItem> GetResourceFilesOf(Project project) {
            List<ResXProjectItem> list = new List<ResXProjectItem>();
            List<Project> referenced = GetReferencedProjects(project);

            list.AddRange(GetResourceFilesOf(project.Name,project.ProjectItems));
            foreach (Project proj in referenced) {
                if (proj.Kind == VSLangProj.PrjKind.prjKindCSharpProject && proj.UniqueName != project.UniqueName) {
                    List<ResXProjectItem> l = GetResourceFilesOf(proj.Name, proj.ProjectItems);
                    list.AddRange(l);
                }
            }
            return list;
        }

        private static List<Project> GetReferencedProjects(Project project) {
            List<Project> list = new List<Project>();
            VSProject proj = project.Object as VSProject;
            foreach (Reference r in proj.References)
                if (r.SourceProject != null)
                    list.Add(r.SourceProject);
            return list;
        }

        internal static bool IsValidIdentifier(string name, ResXProjectItem selectedItem, ref string errorText) {
            if (string.IsNullOrEmpty(name)) {
                errorText = "Key cannot be empty";
                return false;
            }
            if (!csharp.IsValidIdentifier(name)) {
                errorText = "Key is not valid C# identifier";
                return false;
            }
          
            return true;
        }
    }

    
}
