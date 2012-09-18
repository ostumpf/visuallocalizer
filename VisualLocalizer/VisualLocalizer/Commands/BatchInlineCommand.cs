using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE80;
using EnvDTE;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using VisualLocalizer.Extensions;

namespace VisualLocalizer.Commands {
    internal sealed class BatchInlineCommand : AbstractBatchCommand {

        public List<CodeReferenceResultItem> Results {
            get;
            private set;
        }
        private Dictionary<Project, Trie> trieCache = new Dictionary<Project, Trie>();
        private Dictionary<CodeElement, Dictionary<string, string>> codeUsingsCache = new Dictionary<CodeElement, Dictionary<string, string>>();

        public override void Process() {
            base.Process();            

            VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Inline command started on active document... ");

            Results = new List<CodeReferenceResultItem>();

            Process(currentlyProcessedItem);

            foreach (var item in Results)
                VLOutputWindow.VisualLocalizerPane.WriteLine(item.ToString());

            Results.ForEach((item) => { RDTManager.SetFileReadonly(item.SourceItem.Properties.Item("FullPath").Value.ToString(), true); });

            trieCache.Clear();
            codeUsingsCache.Clear();
            VLOutputWindow.VisualLocalizerPane.WriteLine("Found {0} items to be moved", Results.Count);
        }

        public override void Process(Array selectedItems) {            
            VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Inline command started on selection");
            Results = new List<CodeReferenceResultItem>();

            base.Process(selectedItems);

            Results.ForEach((item) => { RDTManager.SetFileReadonly(item.SourceItem.Properties.Item("FullPath").Value.ToString(), true); });

            trieCache.Clear();
            codeUsingsCache.Clear();
            VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Inline completed - found {0} items to be moved", Results.Count);
        }

        private Trie PutResourceFilesInCache() {
            if (!trieCache.ContainsKey(currentlyProcessedItem.ContainingProject)) {
                var resxItems = currentlyProcessedItem.GetResXItemsAround(false);
                trieCache.Add(currentlyProcessedItem.ContainingProject, resxItems.CreateTrie());
            }
            return trieCache[currentlyProcessedItem.ContainingProject]; 
        }

        private Dictionary<string, string> PutCodeUsingsInCache(CodeElement parentNamespace, CodeElement codeClassOrStruct) {
            if (parentNamespace == null) {
                if (!codeUsingsCache.ContainsKey(codeClassOrStruct)) {
                    codeUsingsCache.Add(codeClassOrStruct, (null as CodeNamespace).GetUsedNamespaces(currentlyProcessedItem));
                }
                return codeUsingsCache[codeClassOrStruct];
            } else {
                if (!codeUsingsCache.ContainsKey(parentNamespace)) {
                    codeUsingsCache.Add(parentNamespace, (parentNamespace as CodeNamespace).GetUsedNamespaces(currentlyProcessedItem));
                }
                return codeUsingsCache[parentNamespace as CodeElement];
            }            
        }

        protected override void Lookup(string functionText, TextPoint startPoint, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct, string codeFunctionName, string codeVariableName) {
            Trie trie = PutResourceFilesInCache();
            Dictionary<string, string> usedNamespaces = PutCodeUsingsInCache(parentNamespace as CodeElement, codeClassOrStruct);                       

            CodeReferenceLookuper lookuper = new CodeReferenceLookuper(functionText, startPoint.Line, startPoint.LineCharOffset,
                startPoint.AbsoluteCharOffset + startPoint.Line - 2,
                trie, usedNamespaces, parentNamespace);
            lookuper.SourceItem = currentlyProcessedItem;
            Results.AddRange(lookuper.LookForReferences());
        }
    }
}
