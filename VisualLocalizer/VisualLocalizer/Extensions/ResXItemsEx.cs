using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using VisualLocalizer.Components;
using EnvDTE;
using System.Text.RegularExpressions;

namespace VisualLocalizer.Extensions {
    public static class ProjectItemEx {
        public static List<ResXProjectItem> GetResXItemsAround(this Project project, ProjectItem sourceItem, bool includeInternal, bool includeReadonly) {
            List<ProjectItem> items = project.GetFiles(ProjectEx.IsItemResX, true, includeReadonly);
            
            List<ResXProjectItem> resxItems = new List<ResXProjectItem>();
            items.ForEach((i) => {
                ResXProjectItem resxItem = ResXProjectItem.ConvertToResXItem(i, project);
                if (!resxItem.MarkedInternalInReferencedProject || includeInternal) {
                    resxItems.Add(resxItem);
                }
            });
            
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

            resxItems.ForEach((item) => { item.ResolveNamespaceClass(resxItems); });
            
            return resxItems;
        }

        public static Trie<CodeReferenceTrieElement> CreateTrie(this List<ResXProjectItem> resxItems) {
            Trie<CodeReferenceTrieElement> trie = new Trie<CodeReferenceTrieElement>();
            foreach (ResXProjectItem item in resxItems) {
                item.Load();                
                foreach (var pair in item.GetAllStringReferences(true)) {
                    var element = trie.Add(pair.Key);
                    element.Infos.Add(new CodeReferenceInfo() { Origin = item, Value = pair.Value, Key = pair.Key });
                }
                item.Unload();
            }
            trie.CreatePredecessorsAndShortcuts();
            return trie;
        }

        public static bool CanShowCodeContextMenu(this ProjectItem projectItem) {
            if (projectItem == null) return false;
            if (projectItem.ContainingProject == null) return false;
            if (!projectItem.ContainingProject.IsKnownProjectType()) return false;
            
            return projectItem.IsContainer() || projectItem.GetFileType() != FILETYPE.UNKNOWN;
        }        

        public static bool IsContainer(this ProjectItem item) {
            if (item == null) return false;
            
            string kind = item.Kind.ToUpper();
            return (kind == StringConstants.PhysicalFolder || kind == StringConstants.VirtualFolder || kind == StringConstants.Subproject);
        }

        public static FILETYPE GetFileType(this ProjectItem item) {
            if (item == null) return FILETYPE.UNKNOWN;
            if (item.Kind.ToUpper() != StringConstants.PhysicalFile) return FILETYPE.UNKNOWN;

            string s = (item.GetFullPath()).ToLowerInvariant();
            return s.GetFileType();
        }

        public static FILETYPE GetFileType(this string filename) {
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

        public static bool IsKnownProjectType(this Project project) {
            if (project == null) return false;
            string pkind = project.Kind.ToUpper();
            return pkind == StringConstants.WindowsCSharpProject
                || pkind == StringConstants.WebSiteProject
                || pkind == StringConstants.WebApplicationProject
                || pkind == StringConstants.WindowsVBProject;
        }

        public static bool IsCultureSpecificResX(this ProjectItem projectItem) {
            return Regex.IsMatch(projectItem.Name, @".*\..+\.resx", RegexOptions.IgnoreCase);
        }

        public static string GetResXCultureNeutralName(this ProjectItem projectItem) {
            Match m = Regex.Match(projectItem.Name, @"(.*)\..+(\.resx)", RegexOptions.IgnoreCase);
            if (!m.Success || m.Groups.Count <= 2) return projectItem.Name;

            return m.Groups[1].Value + m.Groups[2].Value;
        }

        public static IEnumerable<Base> Combine<Base, D1, D2>(this IEnumerable<D1> l1, IEnumerable<D2> l2)
            where D1 : Base
            where D2 : Base {

            foreach (D1 x in l1) yield return x;
            foreach (D2 x in l2) yield return x;
            yield break;
        }
    }
}
