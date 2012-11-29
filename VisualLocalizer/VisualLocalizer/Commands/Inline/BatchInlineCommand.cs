using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE80;
using EnvDTE;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using VisualLocalizer.Extensions;
using System.Collections;
using VisualLocalizer.Library.AspxParser;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Text.RegularExpressions;

namespace VisualLocalizer.Commands {
    internal class BatchInlineCommand : AbstractBatchCommand {

        public List<CodeReferenceResultItem> Results {
            get;
            protected set;
        }
        private Dictionary<Project, Trie<CodeReferenceTrieElement>> trieCache = new Dictionary<Project, Trie<CodeReferenceTrieElement>>();
        protected Dictionary<CodeElement, NamespacesList> codeUsingsCache = new Dictionary<CodeElement, NamespacesList>();

        public override void Process(bool verbose) {
            base.Process(verbose);            

            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Inline command started on active document... ");
            if (currentlyProcessedItem.Document.ReadOnly)
                throw new Exception("Cannot perform this operation - active document is readonly");

            Results = new List<CodeReferenceResultItem>();

            Process(currentlyProcessedItem, verbose);

            Results.ForEach((item) => {
                VLDocumentViewsManager.SetFileReadonly(item.SourceItem.Properties.Item("FullPath").Value.ToString(), true); 
            });

            trieCache.Clear();
            codeUsingsCache.Clear();
            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Found {0} items to be moved", Results.Count);
        }

        public override void Process(Array selectedItems, bool verbose) {            
            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Inline command started on selection");
            Results = new List<CodeReferenceResultItem>();

            base.Process(selectedItems, verbose);

            Results.ForEach((item) => {
                VLDocumentViewsManager.SetFileReadonly(item.SourceItem.Properties.Item("FullPath").Value.ToString(), true); 
            });

            trieCache.Clear();
            codeUsingsCache.Clear();
            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Inline completed - found {0} items to be moved", Results.Count);
        }

        public override void ProcessSelection(bool verbose) {
            base.ProcessSelection(verbose);

            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Inline command started on text selection of active document ");

            Results = new List<CodeReferenceResultItem>();

            Process(currentlyProcessedItem, IntersectsWithSelection, verbose);

            Results.RemoveAll((item) => {
                bool empty = item.Value.Trim().Length == 0;
                return empty || IsItemOutsideSelection(item);
            });
            Results.ForEach((item) => {
                VLDocumentViewsManager.SetFileReadonly(item.SourceItem.Properties.Item("FullPath").Value.ToString(), true);
            });

            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Found {0} items to be moved", Results.Count);
        }

        protected virtual Trie<CodeReferenceTrieElement> GetActualTrie() {
            if (!trieCache.ContainsKey(currentlyProcessedItem.ContainingProject)) {
                var resxItems = currentlyProcessedItem.ContainingProject.GetResXItemsAround(false);
                trieCache.Add(currentlyProcessedItem.ContainingProject, resxItems.CreateTrie());
            }
            return trieCache[currentlyProcessedItem.ContainingProject]; 
        }

        protected NamespacesList PutCodeUsingsInCache(CodeElement parentNamespace, CodeElement codeClassOrStruct) {
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

        public override IList LookupInCSharp(string functionText, TextPoint startPoint, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct, string codeFunctionName, string codeVariableName,bool isWithinLocFalse) {
            Trie<CodeReferenceTrieElement> trie = GetActualTrie();
            NamespacesList usedNamespaces = PutCodeUsingsInCache(parentNamespace as CodeElement, codeClassOrStruct);

            CodeReferenceLookuper<CSharpCodeReferenceResultItem> lookuper = 
                new CodeReferenceLookuper<CSharpCodeReferenceResultItem>(functionText, startPoint, trie, usedNamespaces, isWithinLocFalse, currentlyProcessedItem.ContainingProject);

            return onLookuperCreated(lookuper);
        }

        public override IList LookupInAspNet(string functionText, BlockSpan blockSpan, NamespacesList declaredNamespaces, string className) {
            Trie<CodeReferenceTrieElement> trie = GetActualTrie();
            CodeReferenceLookuper<AspNetCodeReferenceResultItem> lookuper =
                new CodeReferenceLookuper<AspNetCodeReferenceResultItem>(functionText, blockSpan, trie, declaredNamespaces, currentlyProcessedItem.ContainingProject);

            return onLookuperCreated(lookuper);
        }

        private IList onLookuperCreated<T>(CodeReferenceLookuper<T> lookuper) where T : CodeReferenceResultItem, new() {
            lookuper.SourceItem = currentlyProcessedItem;
            lookuper.SourceItemGenerated = currentlyProcessedItem.IsGenerated();

            var list = lookuper.LookForReferences();
            foreach (T item in list) {
                Results.Add(item);
            }

            return list;
        }

        public IList ParseResourceExpression(string text, BlockSpan blockSpan) {
            List<AspNetCodeReferenceResultItem> list = new List<AspNetCodeReferenceResultItem>();
            if (!Regex.IsMatch(text, @"\s*Resources\s*:\s*\w+\s*,\s*\w+\s*")) return list;

            int whitespaceLeft;
            int whitespaceRight;
            GetLeftRightWhitespace(text, out whitespaceLeft, out whitespaceRight);

            string expr = text.Trim();
            int colonIndex = expr.IndexOf(':');
            int commaIndex = expr.IndexOf(',');
            string prefix = expr.Substring(0, colonIndex).Trim();
            string className = expr.Substring(colonIndex + 1, commaIndex - colonIndex - 1).Trim();
            string key = expr.Substring(commaIndex + 1).Trim();
            string reference = string.Format("{0}.{1}", className, key);

            Trie<CodeReferenceTrieElement> trie = GetActualTrie();
            CodeReferenceTrieElement e = trie.Root;
            foreach (char c in reference)
                e = trie.Step(e, c);
            if (!e.IsTerminal) return list;

            CodeReferenceInfo info = null;
            foreach (var nfo in e.Infos) {
                if (nfo.Origin.Namespace == prefix)
                    info = nfo;
            }
            if (info == null) return list;

            AspNetCodeReferenceResultItem resultItem = new AspNetCodeReferenceResultItem();
            TextSpan span = new TextSpan();
            span.iStartLine = blockSpan.StartLine;
            span.iStartIndex = blockSpan.StartIndex + whitespaceLeft;
            span.iEndLine = blockSpan.EndLine;
            span.iEndIndex = blockSpan.EndIndex - whitespaceRight - 1;

            resultItem.Value = info.Value;
            resultItem.SourceItem = currentlyProcessedItem;
            resultItem.ReplaceSpan = span;
            resultItem.AbsoluteCharOffset = blockSpan.AbsoluteCharOffset + whitespaceLeft;
            resultItem.AbsoluteCharLength = blockSpan.AbsoluteCharLength - whitespaceLeft - whitespaceRight;
            resultItem.DestinationItem = info.Origin;
            resultItem.FullReferenceText = string.Format("{0}.{1}.{2}", info.Origin.Namespace, info.Origin.Class, key);
            resultItem.IsWithinLocalizableFalse = false;
            resultItem.Key = key;
            resultItem.OriginalReferenceText = string.Format("{0}:{1},{2}", info.Origin.Namespace, info.Origin.Class, key);
            resultItem.ComesFromWebSiteResourceReference = true;

            list.Add(resultItem);
            Results.Add(resultItem);

            return list;
        }

        private void GetLeftRightWhitespace(string text, out int whitespaceLeft, out int whitespaceRight) {
            whitespaceRight = 0;
            whitespaceLeft = 0;
            int index = 0;
            while (index < text.Length && char.IsWhiteSpace(text[index])) {
                index++;
                whitespaceLeft++;
            }

            index = text.Length - 1;
            while (index >= 0 && char.IsWhiteSpace(text[index])) {
                index--;
                whitespaceRight++;
            }
        }
    }
}
