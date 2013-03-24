using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using VisualLocalizer.Components;
using EnvDTE;
using System.Text.RegularExpressions;

namespace VisualLocalizer.Extensions {

    /// <summary>
    /// Provides functionality for working with ResX files and other Visual Localizer-specific extension methods
    /// </summary>
    public static class ProjectItemEx {

        /// <summary>
        /// Returns list of ResX files in given project and all referenced projects
        /// </summary>
        /// <param name="project">Base project to search</param>
        /// <param name="includeInternal">True if ResX files with "internal" designer classes should be included in the search</param>
        /// <param name="includeReadonly">True if readonly files should be included in the search</param>        
        public static List<ResXProjectItem> GetResXItemsAround(this Project project, bool includeInternal, bool includeReadonly) {
            if (project == null) throw new ArgumentNullException("project");

            // get files from base project
            List<ProjectItem> items = project.GetFiles(ProjectEx.IsItemResX, true, includeReadonly);
            
            // convert them to ResX items
            List<ResXProjectItem> resxItems = new List<ResXProjectItem>();
            items.ForEach((i) => {
                ResXProjectItem resxItem = ResXProjectItem.ConvertToResXItem(i, project);
                if (!resxItem.MarkedInternalInReferencedProject || includeInternal) {
                    resxItems.Add(resxItem);
                }
            });
            
            // sort the ResX items
            // - culture-neutral files first
            // - ResX items from base project first (others according to names)
            // - project-default ResX file first (Propertires/Resources.resx)
            // - not-dependant ResX files first (to eliminate Form.cs/Form.resx)
            resxItems.Sort(new Comparison<ResXProjectItem>((a, b) => {
                bool isAneutral = !a.IsCultureSpecific();
                bool isBneutral = !b.IsCultureSpecific();

                bool isAProjectDefault = a.IsProjectDefault(project);
                bool isBProjectDefault = b.IsProjectDefault(project);
                bool isADepOnAny = a.InternalProjectItem.GetIsDependent();
                bool isBDepOnAny = b.InternalProjectItem.GetIsDependent();

                if (isAneutral == isBneutral) {                    
                    if (a.InternalProjectItem.ContainingProject == project && b.InternalProjectItem.ContainingProject == project) {
                        if (isAProjectDefault == isBProjectDefault) {
                            if (isADepOnAny == isBDepOnAny) {
                                return a.InternalProjectItem.Name.CompareTo(b.InternalProjectItem.Name);
                            } else {
                                return isBDepOnAny ? -1 : 1;
                            }
                        } else {
                            return isBProjectDefault ? 1 : -1;
                        }
                    } else {
                        if (a.InternalProjectItem.ContainingProject == project) {
                            return -1;
                        } else if (b.InternalProjectItem.ContainingProject == project) {
                            return 1;                                
                        } else {
                            return a.InternalProjectItem.Name.CompareTo(b.InternalProjectItem.Name);
                        }
                    }
                } else return isBneutral ? 1 : -1;
            }));

            // resolve namespace and class of the designer files
            resxItems.ForEach((item) => { item.ResolveNamespaceClass(resxItems); });
            
            return resxItems;
        }

        /// <summary>
        /// Creates trie from string resources of given list of ResX files
        /// </summary>
        public static Trie<CodeReferenceTrieElement> CreateTrie(this List<ResXProjectItem> resxItems) {
            if (resxItems == null) throw new ArgumentNullException("resxItems");

            Trie<CodeReferenceTrieElement> trie = new Trie<CodeReferenceTrieElement>();
            foreach (ResXProjectItem item in resxItems) {
                bool wasLoaded = item.IsLoaded;
                if (!wasLoaded) item.Load(); // load data from the file or buffer               

                foreach (var pair in item.GetAllStringReferences(true)) { // get string resources, adding class name
                    var element = trie.Add(pair.Key); // add to trie
                    element.Infos.Add(new CodeReferenceInfo() { Origin = item, Value = pair.Value, Key = pair.Key });
                }
                
                if (!wasLoaded) item.Unload(); // unload the data
            }

            trie.CreatePredecessorsAndShortcuts(); // finish creating the trie
            return trie;
        }

        /// <summary>
        /// Tests given project item, if "Visual Localizer" context submenu should displayed
        /// </summary>        
        public static bool CanShowCodeContextMenu(this ProjectItem projectItem) {
            if (projectItem == null) return false;
            if (projectItem.ContainingProject == null) return false;
            if (!projectItem.ContainingProject.IsKnownProjectType()) return false;
            
            return projectItem.IsContainer() || projectItem.GetFileType() != FILETYPE.UNKNOWN;
        }        

        /// <summary>
        /// Tests given project item, if it's container (physical folder, virtual folder or subproject)
        /// </summary>        
        public static bool IsContainer(this ProjectItem item) {
            if (item == null) return false;
            
            string kind = item.Kind.ToUpper();
            return (kind == StringConstants.PhysicalFolder || kind == StringConstants.VirtualFolder || kind == StringConstants.Subproject);
        }

        /// <summary>
        /// Returns filetype for given project item
        /// </summary>        
        public static FILETYPE GetFileType(this ProjectItem item) {
            if (item == null) return FILETYPE.UNKNOWN;
            if (item.Kind.ToUpper() != StringConstants.PhysicalFile) return FILETYPE.UNKNOWN;

            string s = (item.GetFullPath()).ToLowerInvariant();
            return s.GetFileType();
        }

        /// <summary>
        /// Returns filetype for given file name
        /// </summary>        
        public static FILETYPE GetFileType(this string filename) {
            if (filename == null) throw new ArgumentNullException("filename");

            string s = filename.ToLower();
            if (s.EndsWithAny(StringConstants.CsExtensions)) {
                return FILETYPE.CSHARP;
            } else if (s.EndsWithAny(StringConstants.AspxExtensions)) {
                return FILETYPE.ASPX;
            } else if (s.EndsWithAny(StringConstants.VBExtensions)) {
                return FILETYPE.VB;
            } else {
                return FILETYPE.UNKNOWN;
            }
        }

        /// <summary>
        /// Returns true if given project is one of those Visual Localizer can work with
        /// </summary>        
        public static bool IsKnownProjectType(this Project project) {
            if (project == null) return false;
            string pkind = project.Kind.ToUpper();
            return pkind == StringConstants.WindowsCSharpProject
                || pkind == StringConstants.WebSiteProject
                || pkind == StringConstants.WebApplicationProject
                || pkind == StringConstants.WindowsVBProject;
        }

        /// <summary>
        /// Returns true if given project item name has format of culture-specific ResX file
        /// </summary>        
        public static bool IsCultureSpecificResX(this ProjectItem projectItem) {
            if (projectItem == null) throw new ArgumentNullException("projectItem");

            return Regex.IsMatch(projectItem.Name, @".*\..+\.resx", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Takes given project item's name and if it's culture-specific, returns its culture-neutral version. Otherwise
        /// unchanged name is returned.
        /// </summary>        
        public static string GetResXCultureNeutralName(this ProjectItem projectItem) {
            if (projectItem == null) throw new ArgumentNullException("projectItem");

            Match m = Regex.Match(projectItem.Name, @"(.*)\..+(\.resx)", RegexOptions.IgnoreCase);
            if (!m.Success || m.Groups.Count <= 2) return projectItem.Name;

            return m.Groups[1].Value + m.Groups[2].Value;
        }

        /// <summary>
        /// Combines two IEnumerables into one, if the items share base class
        /// </summary>        
        public static IEnumerable<Base> Combine<Base, D1, D2>(this IEnumerable<D1> l1, IEnumerable<D2> l2)
            where D1 : Base
            where D2 : Base {

            foreach (D1 x in l1) yield return x;
            foreach (D2 x in l2) yield return x;
            yield break;
        }
    }
}
