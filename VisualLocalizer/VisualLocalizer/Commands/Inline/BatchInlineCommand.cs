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

    /// <summary>
    /// Represents "Batch inline" command, invokeable either from code context menu or Solution Explorer's context menu. It scans
    /// given set of files, looking for references to resource files which can be possible inlined (replaced with hard-coded string literal).
    /// </summary>
    internal class BatchInlineCommand : AbstractBatchCommand {

        /// <summary>
        /// After processing this command, returns list of found result items (references to resource files)
        /// </summary>
        public List<CodeReferenceResultItem> Results {
            get;
            set;
        }

        /// <summary>
        /// Cache of tries - tries are common for all items within a project
        /// </summary>
        private Dictionary<Project, Trie<CodeReferenceTrieElement>> trieCache = new Dictionary<Project, Trie<CodeReferenceTrieElement>>();
        
        /// <summary>
        /// Cache of information about used namespaces for each code element
        /// </summary>
        protected Dictionary<CodeElement, NamespacesList> codeUsingsCache = new Dictionary<CodeElement, NamespacesList>();

        public override void Process(bool verbose) {
            base.Process(verbose); // initialize class variables            

            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Inline command started on active document... ");
            
            Results = new List<CodeReferenceResultItem>();

            Process(currentlyProcessedItem, verbose);

            // set each source file as readonly
            Results.ForEach((item) => {
                VLDocumentViewsManager.SetFileReadonly(item.SourceItem.GetFullPath(), true); 
            });

            // clear cached data
            trieCache.Clear();
            codeUsingsCache.Clear();
            
            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Found {0} items to be moved", Results.Count);
        }

        public override void Process(Array selectedItems, bool verbose) {            
            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Inline command started on selection");
            Results = new List<CodeReferenceResultItem>();

            base.Process(selectedItems, verbose);

            // set each source file as readonly
            Results.ForEach((item) => {
                VLDocumentViewsManager.SetFileReadonly(item.SourceItem.GetFullPath(), true); 
            });

            // clear cached data
            trieCache.Clear();
            codeUsingsCache.Clear();
            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Inline completed - found {0} items to be moved", Results.Count);
        }

        public override void ProcessSelection(bool verbose) {
            base.ProcessSelection(verbose);

            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Inline command started on text selection of active document ");

            Results = new List<CodeReferenceResultItem>();

            Process(currentlyProcessedItem, IntersectsWithSelection, verbose);

            // remove items laying outside the selection
            Results.RemoveAll((item) => {
                return IsItemOutsideSelection(item);
            });

            // set each source file as readonly
            Results.ForEach((item) => {
                VLDocumentViewsManager.SetFileReadonly(item.SourceItem.GetFullPath(), true);
            });

            // clear cached data
            trieCache.Clear();
            trieCache.Clear();
            codeUsingsCache.Clear();
            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Found {0} items to be moved", Results.Count);
        }

        /// <summary>
        /// Returns trie relevant for currently processed item
        /// </summary>        
        protected virtual Trie<CodeReferenceTrieElement> GetActualTrie() {
            if (!trieCache.ContainsKey(currentlyProcessedItem.ContainingProject)) {
                var resxItems = currentlyProcessedItem.ContainingProject.GetResXItemsAround(false, true);
                trieCache.Add(currentlyProcessedItem.ContainingProject, resxItems.CreateTrie());
            }
            return trieCache[currentlyProcessedItem.ContainingProject]; 
        }

        /// <summary>
        /// Returns list of namespaces used within given parentNamespace
        /// </summary>        
        protected NamespacesList PutCodeUsingsInCache(CodeElement parentNamespace, CodeElement codeClassOrStruct) {
            if (parentNamespace == null) { // class has no parent namespace
                if (!codeUsingsCache.ContainsKey(codeClassOrStruct)) { // save the class itself in the cache
                    codeUsingsCache.Add(codeClassOrStruct, (null as CodeNamespace).GetUsedNamespaces(currentlyProcessedItem));
                }
                return codeUsingsCache[codeClassOrStruct];
            } else { // class has parent namespace
                if (!codeUsingsCache.ContainsKey(parentNamespace)) {
                    codeUsingsCache.Add(parentNamespace, (parentNamespace as CodeNamespace).GetUsedNamespaces(currentlyProcessedItem));
                }
                return codeUsingsCache[parentNamespace as CodeElement];
            }            
        }

        public override IList LookupInCSharp(string functionText, TextPoint startPoint, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct, string codeFunctionName, string codeVariableName,bool isWithinLocFalse) {
            if (codeClassOrStruct == null) throw new ArgumentNullException("codeClassOrStruct");
            if (functionText == null) throw new ArgumentNullException("functionText");
            if (startPoint == null) throw new ArgumentNullException("startPoint");

            Trie<CodeReferenceTrieElement> trie = GetActualTrie();
            NamespacesList usedNamespaces = PutCodeUsingsInCache(parentNamespace as CodeElement, codeClassOrStruct);
            
            // run C# reference lookuper, returning list of references available for inlining
            var list = CSharpReferenceLookuper.Instance.LookForReferences(currentlyProcessedItem, functionText, startPoint, trie, usedNamespaces, isWithinLocFalse, currentlyProcessedItem.ContainingProject, null);

            foreach (CSharpCodeReferenceResultItem item in list) {
                Results.Add(item);
            }

            return list;
        }

        public override IList LookupInVB(string functionText, TextPoint startPoint, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct, string codeFunctionName, string codeVariableName, bool isWithinLocFalse) {
            if (codeClassOrStruct == null) throw new ArgumentNullException("codeClassOrStruct");
            if (functionText == null) throw new ArgumentNullException("functionText");
            if (startPoint == null) throw new ArgumentNullException("startPoint");

            Trie<CodeReferenceTrieElement> trie = GetActualTrie();
            NamespacesList usedNamespaces = PutCodeUsingsInCache(parentNamespace as CodeElement, codeClassOrStruct);
            var list = VBCodeReferenceLookuper.Instance.LookForReferences(currentlyProcessedItem, functionText, startPoint, trie, usedNamespaces, isWithinLocFalse, currentlyProcessedItem.ContainingProject, null);

            foreach (VBCodeReferenceResultItem item in list) {
                Results.Add(item);
            }

            return list;
        }

        public override IList LookupInCSharpAspNet(string functionText, BlockSpan blockSpan, NamespacesList declaredNamespaces, string className) {
            if (functionText == null) throw new ArgumentNullException("functionText");
            if (blockSpan == null) throw new ArgumentNullException("blockSpan");
            if (declaredNamespaces == null) throw new ArgumentNullException("declaredNamespaces");

            Trie<CodeReferenceTrieElement> trie = GetActualTrie();
            var list = AspNetCSharpReferenceLookuper.Instance.LookForReferences(currentlyProcessedItem, functionText, blockSpan, trie, declaredNamespaces, currentlyProcessedItem.ContainingProject, null);

            foreach (AspNetCodeReferenceResultItem item in list) {
                Results.Add(item);
            }

            return list;
        }

        public override IList LookupInVBAspNet(string functionText, BlockSpan blockSpan, NamespacesList declaredNamespaces, string className) {
            if (functionText == null) throw new ArgumentNullException("functionText");
            if (blockSpan == null) throw new ArgumentNullException("blockSpan");
            if (declaredNamespaces == null) throw new ArgumentNullException("declaredNamespaces");

            Trie<CodeReferenceTrieElement> trie = GetActualTrie();
            var list = AspNetVBReferenceLookuper.Instance.LookForReferences(currentlyProcessedItem, functionText, blockSpan, trie, declaredNamespaces, currentlyProcessedItem.ContainingProject, null);

            foreach (AspNetCodeReferenceResultItem item in list) {
                Results.Add(item);
            }

            return list;
        } 

        /// <summary>
        /// Parses given ASP .NET resource expression, returning result item
        /// </summary>        
        public IList ParseResourceExpression(string text, BlockSpan blockSpan, LANGUAGE language) {
            if (text == null) throw new ArgumentNullException("text");
            if (blockSpan == null) throw new ArgumentNullException("blockSpan");            

            List<AspNetCodeReferenceResultItem> list = new List<AspNetCodeReferenceResultItem>();

            // test if is valid resource expression
            if (!Regex.IsMatch(text, @"\s*Resources\s*:\s*\w+\s*,\s*\w+\s*")) return list;
            
            int whitespaceLeft;
            int whitespaceRight;
            GetLeftRightWhitespace(text, out whitespaceLeft, out whitespaceRight);

            // get class name and key
            string expr = text.Trim();
            int colonIndex = expr.IndexOf(':');
            int commaIndex = expr.IndexOf(',');
            string prefix = expr.Substring(0, colonIndex).Trim();
            string className = expr.Substring(colonIndex + 1, commaIndex - colonIndex - 1).Trim();
            string key = expr.Substring(commaIndex + 1).Trim();

            // create standard reference from the class name and key
            string reference = string.Format("{0}.{1}", className, key);

            // run trie lookup on standard reference
            Trie<CodeReferenceTrieElement> trie = GetActualTrie();
            CodeReferenceTrieElement e = trie.Root;
            foreach (char c in reference)
                e = trie.Step(e, c);
            if (!e.IsTerminal) return list; // no result was found, return empty list

            // select culture neutral result item
            CodeReferenceInfo info = null;
            foreach (var nfo in e.Infos) {
                if (nfo.Origin.Namespace == prefix) {
                    if (info == null || info.Origin.IsCultureSpecific()) {
                        info = nfo;
                    } 
                }
            }
            if (info == null) return list;// no result was found, return empty list

            // build result item
            AspNetCodeReferenceResultItem resultItem = new AspNetCodeReferenceResultItem();
            TextSpan span = new TextSpan();
            span.iStartLine = blockSpan.StartLine;
            span.iStartIndex = blockSpan.StartIndex + whitespaceLeft;
            span.iEndLine = blockSpan.EndLine;
            span.iEndIndex = blockSpan.EndIndex - whitespaceRight + 1;

            resultItem.Value = info.Value;
            resultItem.SourceItem = currentlyProcessedItem;
            resultItem.ReplaceSpan = span;
            resultItem.AbsoluteCharOffset = blockSpan.AbsoluteCharOffset + whitespaceLeft;
            resultItem.AbsoluteCharLength = blockSpan.AbsoluteCharLength - whitespaceLeft - whitespaceRight + 1;
            resultItem.DestinationItem = info.Origin;
            resultItem.FullReferenceText = string.Format("{0}.{1}.{2}", info.Origin.Namespace, info.Origin.Class, key);
            resultItem.IsWithinLocalizableFalse = false;
            resultItem.Key = key;
            resultItem.OriginalReferenceText = string.Format("{0}:{1},{2}", info.Origin.Namespace, info.Origin.Class, key);
            resultItem.ComesFromWebSiteResourceReference = true;
            resultItem.Language = language;

            list.Add(resultItem);
            Results.Add(resultItem);

            return list;
        }

        /// <summary>
        /// Gets whitespace length on the left and right side of the text
        /// </summary>        
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
