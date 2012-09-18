using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using VisualLocalizer.Components;
using EnvDTE;

namespace VisualLocalizer.Extensions {
    public static class ProjectItemEx {
        public static List<ResXProjectItem> GetResXItemsAround(this ProjectItem item, bool includeInternal) {
            List<ProjectItem> items = item.ContainingProject.GetFiles(ResXProjectItem.IsItemResX, true);
            List<ResXProjectItem> resxItems = new List<ResXProjectItem>();
            items.ForEach((i) => {
                ResXProjectItem resxItem = ResXProjectItem.ConvertToResXItem(i, item.ContainingProject);
                if (!resxItem.MarkedInternalInReferencedProject || includeInternal) {
                    resxItems.Add(resxItem);
                }
            });
            return resxItems;
        }

        public static Trie CreateTrie(this List<ResXProjectItem> resxItems) {
            Trie trie = new Trie();
            foreach (ResXProjectItem item in resxItems) {
                item.Load();
                item.LoadAllReferences();
                foreach (var pair in item.AllReferences) {
                    trie.Add(pair.Key, new VisualLocalizer.Components.CodeReferenceLookuper.CodeReferenceInfo() { Value = pair.Value, Origin = item });
                }
            }
            trie.CreatePredecessorsAndShortcuts();
            return trie;
        }
    }
}
