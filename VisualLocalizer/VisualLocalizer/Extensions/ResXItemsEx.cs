using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using VisualLocalizer.Components;
using EnvDTE;

namespace VisualLocalizer.Extensions {
    public static class ProjectItemEx {
        public static List<ResXProjectItem> GetResXItemsAround(this Project project, bool includeInternal) {
            List<ProjectItem> items = project.GetFiles(ResXProjectItem.IsItemResX, true);
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
                if (isAneutral == isBneutral) {
                    return a.InternalProjectItem.Name.CompareTo(b.InternalProjectItem.Name);
                } else {
                    return isBneutral ? 1 : -1;
                }
            }));

            resxItems.ForEach((item) => { item.ResolveNamespaceClass(resxItems); });

            return resxItems;
        }

        public static Trie<CodeReferenceTrieElement> CreateTrie(this List<ResXProjectItem> resxItems) {
            Trie<CodeReferenceTrieElement> trie = new Trie<CodeReferenceTrieElement>();
            foreach (ResXProjectItem item in resxItems) {
                item.Load();                
                foreach (var pair in item.GetAllStringReferences()) {
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

            string s = ((string)item.Properties.Item("FullPath").Value).ToLowerInvariant();
            return s.GetFileType();
        }

        public static FILETYPE GetFileType(this string filename) {
            string s = filename.ToLower();
            if (s.EndsWithAny(StringConstants.CsExtensions)) {
                return FILETYPE.CSHARP;
            } else if (s.EndsWithAny(StringConstants.AspxExtensions)) {
                return FILETYPE.ASPX;
            } else if (s.EndsWithAny(StringConstants.RazorExtensions)) {
                return FILETYPE.RAZOR;
            } else {
                return FILETYPE.UNKNOWN;
            }
        }

        public static bool IsKnownProjectType(this Project project) {
            if (project == null) return false;
            string pkind = project.Kind.ToUpper();
            return pkind == StringConstants.WindowsCSharpProject
                || pkind == StringConstants.WebSiteProject
                || pkind == StringConstants.WebApplicationProject;
        }
    }
}
