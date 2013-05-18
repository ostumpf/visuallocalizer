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
using VisualLocalizer.Library.AspX;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Text.RegularExpressions;
using VisualLocalizer.Components.Code;
using VisualLocalizer.Library.Algorithms;
using VisualLocalizer.Library.Components;
using VisualLocalizer.Library.Extensions;

namespace VisualLocalizer.Commands.Inline {

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

        /// <summary>
        /// Called from context menu of a code file, processes current document
        /// </summary>
        /// <param name="verbose">True if processing info should be printed to the output</param>
        public override void Process(bool verbose) {
            base.Process(verbose); // initialize class variables            

            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Inline command started on active document... ");
            if (verbose) ProgressBarHandler.StartIndeterminate(Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Find);

            try {
                Results = new List<CodeReferenceResultItem>();

                Process(currentlyProcessedItem, verbose);

                // set each source file as readonly
                Results.ForEach((item) => {
                    VLDocumentViewsManager.SetFileReadonly(item.SourceItem.GetFullPath(), true);
                });

            } finally {
                // clear cached data
                trieCache.Clear();
                codeUsingsCache.Clear();

                if (verbose) ProgressBarHandler.StopIndeterminate(Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Find);
            }
            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Found {0} items to be moved", Results.Count);
        }

        /// <summary>
        /// Called from context menu of Solution Explorer, processes given list of ProjectItems
        /// </summary>
        /// <param name="selectedItems">Items selected in Solution Explorer - to be searched</param>
        /// <param name="verbose">True if processing info should be printed to the output</param>
        public override void Process(Array selectedItems, bool verbose) {            
            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Inline command started on selection");
            if (verbose) ProgressBarHandler.StartIndeterminate(Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Find);

            try {
                Results = new List<CodeReferenceResultItem>();

                base.Process(selectedItems, verbose);

                // set each source file as readonly
                Results.ForEach((item) => {
                    VLDocumentViewsManager.SetFileReadonly(item.SourceItem.GetFullPath(), true);
                });
            } finally {
                // clear cached data
                trieCache.Clear();
                codeUsingsCache.Clear();

                if (verbose) ProgressBarHandler.StopIndeterminate(Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Find);
            }

            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Inline completed - found {0} items to be moved", Results.Count);
        }

        /// <summary>
        /// Called from context menu of a code file, processes selected block of code
        /// </summary>
        /// <param name="verbose">True if processing info should be printed to the output</param>
        public override void ProcessSelection(bool verbose) {
            base.ProcessSelection(verbose);

            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Inline command started on text selection of active document ");
            if (verbose) ProgressBarHandler.StartIndeterminate(Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Find);

            try {
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

            } finally {
                // clear cached data
                trieCache.Clear();
                trieCache.Clear();
                codeUsingsCache.Clear();

                if (verbose) ProgressBarHandler.StopIndeterminate(Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Find);
            }
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

        /// <summary>
        /// Searches given C# code and returns list of result items
        /// </summary>
        /// <param name="functionText">Text to search</param>
        /// <param name="startPoint">Information about position of the text (line, column...)</param>
        /// <param name="parentNamespace">Namespace where this code belongs (can be null)</param>
        /// <param name="codeClassOrStruct">Class or struct where this code belongs (cannot be null)</param>
        /// <param name="codeFunctionName">Name of the function, where this code belongs (can be null)</param>
        /// <param name="codeVariableName">Name of the variable that is initialized by this code (can be null)</param>
        /// <param name="isWithinLocFalse">True if [Localizable(false)] was set</param>
        /// <returns>
        /// List of result items
        /// </returns>      
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

        /// <summary>
        /// Searches given Visual Basic code and returns list of result items
        /// </summary>
        /// <param name="functionText">Text to search</param>
        /// <param name="startPoint">Information about position of the text (line, column...)</param>
        /// <param name="parentNamespace">Namespace where this code belongs (can be null)</param>
        /// <param name="codeClassOrStruct">Class, struct or module where this code belongs (cannot be null)</param>
        /// <param name="codeFunctionName">Name of the function, where this code belongs (can be null)</param>
        /// <param name="codeVariableName">Name of the variable that is initialized by this code (can be null)</param>
        /// <param name="isWithinLocFalse">True if [Localizable(false)] was set</param>
        /// <returns>
        /// List of result items
        /// </returns>      
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

        /// <summary>
        /// Searches given C# code block located in an ASP .NET document
        /// </summary>
        /// <param name="functionText">Text to search</param>
        /// <param name="blockSpan">Information about position of the block (line, column...</param>
        /// <param name="declaredNamespaces">Namespaces imported in the document</param>
        /// <param name="className">Name of the ASP .NET document</param>
        /// <returns>List of result items</returns>      
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

        /// <summary>
        /// Searches given VB code block located in an ASP .NET document
        /// </summary>
        /// <param name="functionText">Text to search</param>
        /// <param name="blockSpan">Information about position of the block (line, column...</param>
        /// <param name="declaredNamespaces">Namespaces imported in the document</param>
        /// <param name="className">Name of the ASP .NET document</param>
        /// <returns>List of result items</returns>
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
            string regex = @"\s*(" + StringConstants.GlobalWebSiteResourcesNamespace + @")\s*:\s*(\w+)\s*,\s*(\w+)\s*";
            Match match = Regex.Match(text, regex);
            if (!match.Success || match.Groups.Count != 4) return list;


            string prefix = StringConstants.GlobalWebSiteResourcesNamespace;
            string className = match.Groups[2].Value;
            string key = match.Groups[3].Value;

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

            int startLineOffset, startIndex;
            GetLineOffset(text, match.Groups[1].Index, out startLineOffset, out startIndex);            
            int endLineOffset, endIndex;
            GetLineOffset(text, match.Groups[3].Index + match.Groups[3].Length, out endLineOffset, out endIndex);

            // build result item
            AspNetCodeReferenceResultItem resultItem = new AspNetCodeReferenceResultItem();
            TextSpan span = new TextSpan();
            span.iStartLine = blockSpan.StartLine + startLineOffset;
            span.iStartIndex = startLineOffset == 0 ? blockSpan.StartIndex + startIndex : startIndex;
            span.iEndLine = blockSpan.StartLine + endLineOffset;
            span.iEndIndex = endLineOffset == 0 ? blockSpan.StartIndex + endIndex : endIndex;

            resultItem.Value = info.Value;
            resultItem.SourceItem = currentlyProcessedItem;
            resultItem.ReplaceSpan = span;
            resultItem.AbsoluteCharOffset = blockSpan.AbsoluteCharOffset + match.Groups[1].Index;
            resultItem.AbsoluteCharLength = match.Groups[3].Index + match.Groups[3].Length - match.Groups[1].Index;
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
        /// Calculates line and column from the absolute position in text
        /// </summary>
        /// <param name="text">Text to search</param>
        /// <param name="index">Absolute position within the text, whose line and column we want</param>
        /// <param name="lineOffset">Number of lines from the beginning of the text</param>
        /// <param name="indexOffset">Column index (number of characters from the beginning of the last line)</param>
        private void GetLineOffset(string text, int index, out int lineOffset, out int indexOffset) {
            lineOffset = 0;
            indexOffset = 0;
            for (int i = 0; i < index; i++) {
                indexOffset++;
                if (text[i] == '\n') {
                    lineOffset++;
                    indexOffset = 0;
                }                
            }
        }


    }
}
