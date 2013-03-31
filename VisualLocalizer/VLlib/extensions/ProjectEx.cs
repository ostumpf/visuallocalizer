using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using VSLangProj;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using EnvDTE80;

namespace VisualLocalizer.Library {

    /// <summary>
    /// Container for extension methods working with Project-like objects. 
    /// </summary>
    public static class ProjectEx {

        private const string GlobalWebSiteResourcesFolder = "App_GlobalResources";
        private const string WebSiteProject = "{E24C65DC-7377-472B-9ABA-BC803B73C61A}";

        /// <summary>
        /// Returns list of all files in given project, satisfying given condition
        /// </summary>
        /// <param name="project">Project to search</param>
        /// <param name="test">Add only items passing this test (can be null)</param>
        /// <param name="includeReferenced">Include referenced projects in the search</param>
        /// <param name="includeReadonly">Include readonly files in the search</param>
        /// <returns></returns>
        public static List<ProjectItem> GetFiles(this Project project,Predicate<ProjectItem> test,bool includeReferenced, bool includeReadonly) {
            if (project == null) throw new ArgumentNullException("project");
            
            List<ProjectItem> list = new List<ProjectItem>();            

            // get files from the project itself
            List<ProjectItem> ownFiles = GetFilesOf(project.ProjectItems, test, includeReadonly);
            ownFiles.Reverse();
            list.AddRange(ownFiles);

            if (includeReferenced) { // get files from referenced projects
                List<Project> referencedProjects = project.GetReferencedProjects();
                foreach (Project referencedProj in referencedProjects) {
                    if (referencedProj.UniqueName != project.UniqueName) {
                        List<ProjectItem> l = GetFilesOf(referencedProj.ProjectItems, test, includeReadonly);
                        l.Reverse();
                        list.AddRange(l);
                    }
                }
            }          

            return list;
        }

        /// <summary>
        /// Recursively searches given ProjectItems and returns list of project items satisfying given condition
        /// </summary>
        private static List<ProjectItem> GetFilesOf(ProjectItems items, Predicate<ProjectItem> test, bool includeReadonly) {
            List<ProjectItem> list = new List<ProjectItem>();

            if (items != null) {
                foreach (ProjectItem item in items) {
                    if (test==null || test(item)) {
                        if (includeReadonly || !RDTManager.IsFileReadonly(item.GetFullPath())) {
                            list.Add(item);
                        }
                    } else {
                        if (item.ProjectItems != null && item.ProjectItems.Count > 0)
                            list.AddRange(GetFilesOf(item.ProjectItems, test, includeReadonly));
                    }
                }
            }

            return list;
        }        

        /// <summary>
        /// Returns list of referenced projects
        /// </summary>        
        public static List<Project> GetReferencedProjects(this Project project) {
            if (project == null) throw new ArgumentNullException("project");

            List<Project> list = new List<Project>();
                        
            VSProject proj = project.Object as VSProject;
            if (proj == null) {
                proj = project as VSProject;
                return list;
            }
            
            if (proj.References != null) {
                foreach (Reference r in proj.References)
                    if (r.SourceProject != null)
                        list.Add(r.SourceProject);
            }

            return list;
        }

        /// <summary>
        /// Adds resource subdirectory into the project - if the project is ASP .NET website, the directory is
        /// added under the GlobalResources folder. Otherwise, project subdirectory "Resources" is created (if it doesn't exist)
        /// and its new "subdir" is created and returned as ProjectItem.
        /// </summary>        
        public static ProjectItem AddResourceDir(this Project project, string subdir) {
            if (project == null) throw new ArgumentNullException("project");
            if (string.IsNullOrEmpty(subdir)) throw new ArgumentNullException("subdir");

            string resourcesFolder;            
            if (project.Kind.ToUpperInvariant() == WebSiteProject) { // it's a website -> resources folder is GlobalResources
                resourcesFolder = GlobalWebSiteResourcesFolder;
            } else {
                resourcesFolder = "Resources";
            }

            ProjectItem resItem = null;
            // add resources folder to the project if it didn't exist
            if (project.ProjectItems.ContainsItem(resourcesFolder)) {
                resItem = project.ProjectItems.Item(resourcesFolder);
            } else {
                resItem = project.ProjectItems.AddFolder(resourcesFolder, null);
            }

            ProjectItem subItem = null;
            // add resources subfolder if it didn't exist
            if (resItem.ProjectItems.ContainsItem(subdir)) {
                subItem = resItem.ProjectItems.Item(subdir);
            } else {
                subItem = resItem.ProjectItems.AddFolder(subdir, null);
            }

            return subItem;
        }

        /// <summary>
        /// Returns true if list of project items contains item with given name
        /// </summary>        
        public static bool ContainsItem(this ProjectItems items, string name) {
            if (name == null) throw new ArgumentNullException("item");
            if (items == null) throw new ArgumentNullException("items");

            foreach (ProjectItem i in items)
                if (i.Name == name) return true;
            return false;
        }

        /// <summary>
        /// Returns true if given solution contains given project item
        /// </summary>        
        public static bool ContainsProjectItem(this Solution solution, ProjectItem item) {
            if (solution == null) return false;
            if (!solution.IsOpen) return false;
            if (item == null) return false;
            if (item.Object == null) return false;

            try {
                ProjectItem found = solution.FindProjectItem(item.GetFullPath());
                return found != null;
            } catch (Exception) {
                return false;
            }
        }

        /// <summary>
        /// Returns true if given project item is output of some custom tool
        /// </summary>        
        public static bool IsGenerated(this ProjectItem item) {
            if (item == null) throw new ArgumentNullException("item");

            try {
                bool isCustomToolOutput = false;
                bool isDependant = false;
                bool isAspxCodeBehind = false;

                foreach (Property prop in item.Properties) {
                    if (prop.Name == "IsCustomToolOutput") {
                        isCustomToolOutput = (bool)prop.Value;
                    }
                    if (prop.Name == "IsDependentFile") {
                        isDependant = (bool)prop.Value;
                    }
                    if (prop.Name == "SubType") {
                        isAspxCodeBehind = (string)prop.Value == "ASPXCodeBehind";
                    }
                }
                return isCustomToolOutput || (isDependant && !isAspxCodeBehind);
            } catch (Exception) {
                return false;
            }
        }

        /// <summary>
        /// Returns value of "FullPath" property, if present, null otherwise
        /// </summary>
        public static string GetFullPath(this ProjectItem item) {
            if (item == null) throw new ArgumentNullException("item");

            try {
                return (string)item.Properties.Item("FullPath").Value;
            } catch (Exception) {
                return null;
            }
        }

        /// <summary>
        /// Returns value of "RelativeURL" property, if present, null otherwise
        /// </summary>
        public static string GetRelativeURL(this ProjectItem item) {
            if (item == null) throw new ArgumentNullException("item");

            try {
                return (string)item.Properties.Item("RelativeURL").Value;
            } catch (Exception) {
                return null;
            }
        }

        /// <summary>
        /// Returns value of "Extension" property, if present, null otherwise
        /// </summary>
        public static string GetExtension(this ProjectItem item) {
            if (item == null) throw new ArgumentNullException("item");

            try {
                return (string)item.Properties.Item("Extension").Value;
            } catch (Exception) {
                return null;
            }
        }

        /// <summary>
        /// Returns value of "CustomTool" property, if present, null otherwise
        /// </summary>
        public static string GetCustomTool(this ProjectItem item) {
            if (item == null) throw new ArgumentNullException("item");

            try {
                return (string)item.Properties.Item("CustomTool").Value;
            } catch (Exception) {
                return null;
            }
        }

        /// <summary>
        /// Returns value of "CustomToolOutput" property, if present, null otherwise
        /// </summary>
        public static string GetCustomToolOutput(this ProjectItem item) {
            if (item == null) throw new ArgumentNullException("item");

            try {
                return (string)item.Properties.Item("CustomToolOutput").Value;
            } catch (Exception) {
                return null;
            }
        }   

        /// <summary>
        /// Returns value of "IsDependentFile" property, if present, false otherwise
        /// </summary> 
        public static bool GetIsDependent(this ProjectItem item) {
            if (item == null) throw new ArgumentNullException("item");

            try {
                return (bool)item.Properties.Item("IsDependentFile").Value;
            } catch (Exception) {
                return false;
            }
        }

        /// <summary>
        /// Returns true if given item has ".resx" extension (either set in project item properties or in a file name)
        /// </summary>        
        public static bool IsItemResX(this ProjectItem item) {
            if (item == null) throw new ArgumentNullException("item");

            string ext = item.GetExtension();
            if (ext == null) ext = Path.GetExtension(item.GetFullPath());

            return ext.ToLower() == ".resx";
        }

        /// <summary>
        /// Returns code model for given item
        /// </summary>        
        public static FileCodeModel2 GetCodeModel(this ProjectItem item) {
            if (item == null) throw new ArgumentNullException("item");

            if (item.FileCodeModel == null) {
                item.Open(EnvDTE.Constants.vsViewKindCode);
            }

            return (FileCodeModel2)item.FileCodeModel;
        }
    }
}
