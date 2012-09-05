using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using VSLangProj;
using System.Reflection;
using System.IO;
using System.Diagnostics;

namespace VisualLocalizer.Library {
    public static class ProjectEx {
       
        public static List<ProjectItem> GetFiles(this Project project,Predicate<ProjectItem> test,bool includeReferenced) {
            if (project == null)
                throw new ArgumentNullException("project");
            
            List<ProjectItem> list = new List<ProjectItem>();            

            List<ProjectItem> ownFiles = GetFilesOf(project.ProjectItems, test);
            ownFiles.Reverse();
            list.AddRange(ownFiles);

            if (includeReferenced) {
                List<Project> referencedProjects = project.GetReferencedProjects();
                foreach (Project referencedProj in referencedProjects) {
                    if (referencedProj.Kind == VSLangProj.PrjKind.prjKindCSharpProject && referencedProj.UniqueName != project.UniqueName) {
                        List<ProjectItem> l = GetFilesOf(referencedProj.ProjectItems, test);
                        l.Reverse();
                        list.AddRange(l);
                    }
                }
            }
            return list;
        }

        private static List<ProjectItem> GetFilesOf(ProjectItems items,Predicate<ProjectItem> test) {
            List<ProjectItem> list = new List<ProjectItem>();

            if (items != null) {
                foreach (ProjectItem item in items) {
                    if (test==null || test(item)) {
                        list.Add(item);
                    } else {
                        if (item.ProjectItems.Count > 0)
                            list.AddRange(GetFilesOf(item.ProjectItems, test));
                    }
                }
            }

            return list;
        }        

        public static List<Project> GetReferencedProjects(this Project project) {
            if (project == null)
                throw new ArgumentNullException("project");

            List<Project> list = new List<Project>();
            VSProject proj = project.Object as VSProject;
            if (proj == null)
                throw new ArgumentException(string.Format("Project {0} is not sufficiently initialized.", project.Name));

            if (proj.References != null) {
                foreach (Reference r in proj.References)
                    if (r.SourceProject != null)
                        list.Add(r.SourceProject);
            }

            return list;
        }

    
    }
}
