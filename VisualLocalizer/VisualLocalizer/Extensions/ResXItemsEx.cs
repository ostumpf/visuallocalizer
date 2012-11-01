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
    }
}
