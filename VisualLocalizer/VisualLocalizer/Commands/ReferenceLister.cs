using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using EnvDTE80;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using System.Resources;

namespace VisualLocalizer.Commands {
    internal sealed class ReferenceLister : BatchInlineCommand {

        private Trie<CodeReferenceTrieElement> trie;

        public void Process(List<Project> projects, Trie<CodeReferenceTrieElement> trie) {
            this.trie = trie;
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
                bool ok = true;
                for (short i = 0; i < o.FileCount; i++) {
                    ok = ok && o.get_FileNames(i).ToLowerInvariant().EndsWith(StringConstants.CsExtension);
                    ok = ok && o.ContainingProject.Kind == VSLangProj.PrjKind.prjKindCSharpProject;
                }
                if (ok) {
                    Process(o, verbose);
                    Process(o.ProjectItems, verbose);
                }
            }
        }

        protected override void Process(Project project, bool verbose) {
            Process(project.ProjectItems, false);
        }

        protected override void Lookup(string functionText, TextPoint startPoint, CodeNamespace parentNamespace, 
            CodeElement2 codeClassOrStruct, string codeFunctionName, string codeVariableName, bool isWithinLocFalse) {
            NamespacesList usedNamespaces = PutCodeUsingsInCache(parentNamespace as CodeElement, codeClassOrStruct);

            CodeReferenceLookuper lookuper = new CodeReferenceLookuper(functionText, startPoint,
                trie, usedNamespaces, parentNamespace, isWithinLocFalse, currentlyProcessedItem.ContainingProject);
            lookuper.SourceItem = currentlyProcessedItem;

            var list = lookuper.LookForReferences();            
            Results.AddRange(list);
        }
        
    }
}
