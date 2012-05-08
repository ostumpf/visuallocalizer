using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using VSLangProj;

namespace VisualLocalizer.Library {
    public static class PackageEx {

        public static List<ProjectItem> GetFilesOf(this Project project,Predicate<ProjectItem> test) {
            List<ProjectItem> list = new List<ProjectItem>();
            List<Project> referencedProjects = project.GetReferencedProjects();

            List<ProjectItem> ownFiles = GetFilesOf(project.ProjectItems, test);
            ownFiles.Reverse();
            list.AddRange(ownFiles);

            foreach (Project referencedProj in referencedProjects) {
                if (referencedProj.Kind == VSLangProj.PrjKind.prjKindCSharpProject && referencedProj.UniqueName != project.UniqueName) {
                    List<ProjectItem> l = GetFilesOf(referencedProj.ProjectItems, test);
                    l.Reverse();
                    list.AddRange(l);
                }
            }
            return list;
        }

        private static List<ProjectItem> GetFilesOf(ProjectItems items,Predicate<ProjectItem> test) {
            List<ProjectItem> list = new List<ProjectItem>();
            
            foreach (ProjectItem item in items) {
                if (test(item)) {
                    list.Add(item);
                } else {
                    if (item.ProjectItems.Count>0)
                        list.AddRange(GetFilesOf(item.ProjectItems, test));
                }
            }

            return list;
        }        

        public static List<Project> GetReferencedProjects(this Project project) {
            List<Project> list = new List<Project>();
            VSProject proj = project.Object as VSProject;
            foreach (Reference r in proj.References)
                if (r.SourceProject != null)
                    list.Add(r.SourceProject);
            return list;
        }


    }
}
