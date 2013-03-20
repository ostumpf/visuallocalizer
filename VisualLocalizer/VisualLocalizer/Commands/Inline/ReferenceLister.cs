using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using EnvDTE80;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using System.Resources;
using VisualLocalizer.Extensions;
using System.Collections;
using VisualLocalizer.Library.AspxParser;

namespace VisualLocalizer.Commands {

    /// <summary>
    /// Used by ResX editor to track references to particular resource file.
    /// </summary>
    internal sealed class ReferenceLister : BatchInlineCommand {

        private Trie<CodeReferenceTrieElement> trie;
        private ResXProjectItem prefferedResXItem;

        /// <summary>
        /// Runs this command, filling Results with references to resources in given file
        /// </summary>
        /// <param name="projects">List of referenced projects (are included in the search)</param>
        /// <param name="trie">Trie created from resource names</param>
        /// <param name="prefferedResXItem">Original ResX project item - used when culture-neutral vs. culture-specific differences are handled</param>
        public void Process(List<Project> projects, Trie<CodeReferenceTrieElement> trie, ResXProjectItem prefferedResXItem) {
            this.trie = trie;
            this.prefferedResXItem = prefferedResXItem; 
            searchedProjectItems.Clear();
            generatedProjectItems.Clear();

            Results = new List<CodeReferenceResultItem>();            

            foreach (Project project in projects) {
                Process(project, false);
            }
            
            codeUsingsCache.Clear();      
        }

        public override void Process(bool verbose) {
            throw new InvalidOperationException("This method is not supported.");
        }

        public override void ProcessSelection(bool verbose) {
            throw new InvalidOperationException("This method is not supported.");
        }

        public override void Process(Array selectedItems, bool verbose) {
            throw new InvalidOperationException("This method is not supported.");
        }        

        protected override void Process(ProjectItems items, bool verbose) {
            if (items == null) return;

            foreach (ProjectItem o in items) {
                bool ok = o.CanShowCodeContextMenu();
                if (ok) {
                    Process(o, verbose);
                    Process(o.ProjectItems, verbose);
                }
            }
        }

        protected override void Process(Project project, bool verbose) {
            Process(project.ProjectItems, false);
        }

        protected override Trie<CodeReferenceTrieElement> GetActualTrie() {
            return trie;
        }

        public override IList LookupInCSharp(string functionText, TextPoint startPoint, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct, string codeFunctionName, string codeVariableName, bool isWithinLocFalse) {
            if (codeClassOrStruct == null) throw new ArgumentNullException("codeClassOrStruct");
            if (functionText == null) throw new ArgumentNullException("functionText");
            if (startPoint == null) throw new ArgumentNullException("startPoint");

            Trie<CodeReferenceTrieElement> trie = GetActualTrie();
            NamespacesList usedNamespaces = PutCodeUsingsInCache(parentNamespace as CodeElement, codeClassOrStruct);
            var list = CSharpReferenceLookuper.Instance.LookForReferences(currentlyProcessedItem, functionText, startPoint, trie, usedNamespaces, isWithinLocFalse, currentlyProcessedItem.ContainingProject, prefferedResXItem);

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
            var list = VBCodeReferenceLookuper.Instance.LookForReferences(currentlyProcessedItem, functionText, startPoint, trie, usedNamespaces, isWithinLocFalse, currentlyProcessedItem.ContainingProject, prefferedResXItem);

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
            var list = AspNetCSharpReferenceLookuper.Instance.LookForReferences(currentlyProcessedItem, functionText, blockSpan, trie, declaredNamespaces, currentlyProcessedItem.ContainingProject, prefferedResXItem);

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
            var list = AspNetVBReferenceLookuper.Instance.LookForReferences(currentlyProcessedItem, functionText, blockSpan, trie, declaredNamespaces, currentlyProcessedItem.ContainingProject, prefferedResXItem);

            foreach (AspNetCodeReferenceResultItem item in list) {
                Results.Add(item);
            }

            return list;
        }    
    }
}
