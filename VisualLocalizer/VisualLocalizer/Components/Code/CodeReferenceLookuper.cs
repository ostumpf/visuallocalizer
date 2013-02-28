using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE;
using VisualLocalizer.Library.AspxParser;

namespace VisualLocalizer.Components {
    internal sealed class CodeReferenceLookuper<T> : AbstractCodeLookuper where T:CodeReferenceResultItem, new() {

        private static CodeReferenceLookuper<T> instance;
        private object syncRoot = new object();

        private CodeReferenceLookuper() { }

        public static CodeReferenceLookuper<T> Instance {
            get {
                if (instance == null) instance = new CodeReferenceLookuper<T>();
                return instance;
            }
        }

        public List<T> Run(ProjectItem projectItem, string text, TextPoint startPoint, Trie<CodeReferenceTrieElement> Trie, NamespacesList usedNamespaces, bool isWithinLocFalse, Project project, ResXProjectItem prefferedResXItem) {
            return Run(projectItem, text, startPoint.LineCharOffset - 1, startPoint.Line, startPoint.AbsoluteCharOffset + startPoint.Line - 2, Trie, usedNamespaces, isWithinLocFalse, project, prefferedResXItem);
        }

        public List<T> Run(ProjectItem projectItem, string text, BlockSpan blockSpan, Trie<CodeReferenceTrieElement> Trie, NamespacesList usedNamespaces, Project project, ResXProjectItem prefferedResXItem) {
            return Run(projectItem, text, blockSpan.StartIndex - 1, blockSpan.StartLine, blockSpan.AbsoluteCharOffset, Trie, usedNamespaces, false, project, prefferedResXItem);
        }

        public List<T> Run(ProjectItem projectItem, string text, int currentIndex, int currentLine, int currentOffset,
            Trie<CodeReferenceTrieElement> Trie, NamespacesList usedNamespaces, bool isWithinLocFalse, Project project, ResXProjectItem prefferedResXItem) {
            lock (syncRoot) {
                this.SourceItem = projectItem;
                this.text = text;
                this.CurrentIndex = currentIndex;
                this.CurrentLine = currentLine;
                this.CurrentAbsoluteOffset = currentOffset;
                this.Trie = Trie;
                this.UsedNamespaces = usedNamespaces;
                this.IsWithinLocFalse = isWithinLocFalse;
                this.Project = project;
                this.prefferedResXItem = prefferedResXItem;

                return LookForReferences();
            }
        }

        private Project Project { get; set; }
        private NamespacesList UsedNamespaces { get; set; }
        private Trie<CodeReferenceTrieElement> Trie { get; set; }
        private int ReferenceStartLine { get; set; }
        private int ReferenceStartIndex { get; set; }
        private int ReferenceStartOffset { get; set; }
        private ResXProjectItem prefferedResXItem;

        private List<T> LookForReferences() {
            bool insideComment = false, insideString = false, isVerbatimString = false;
            bool skipLine = false;
            currentChar = '?';
            previousChar = '?';
            previousPreviousChar = '?';
            previousPreviousPreviousChar = '?';
            stringStartChar = '?';
            List<T> list = new List<T>();
            CodeReferenceTrieElement currentElement = Trie.Root;
            StringBuilder prefixBuilder = new StringBuilder();
            bool prefixContinue = false;
         
            for (globalIndex = 0; globalIndex < text.Length; globalIndex++) {
                previousPreviousPreviousChar = previousPreviousChar;
                previousPreviousChar = previousChar;
                previousChar = currentChar;
                currentChar = text[globalIndex];

                if (skipLine) {
                    if (currentChar == '\n') {
                        previousChar = '?';
                        previousPreviousChar = '?';
                        skipLine = false;
                    }
                } else {
                    PreProcessChar(ref insideComment, ref insideString, ref isVerbatimString, out skipLine);                   

                    if (!insideString && !insideComment) {                        
                        if (currentElement.CanBeFollowedByWhitespace && char.IsWhiteSpace(currentChar)) {
                            // do nothing
                        } else {
                            if (prefixBuilder.Length > 0) {
                                if (currentChar == '.') {
                                    prefixContinue = true;
                                } else if (!currentChar.CanBePartOfIdentifier() && !char.IsWhiteSpace(currentChar)) {
                                    prefixBuilder.Length = 0;                                    
                                }
                            }
                            if (currentChar.CanBePartOfIdentifier() && !previousChar.CanBePartOfIdentifier()) {
                                if (!prefixContinue) {
                                    ReferenceStartIndex = CurrentIndex;
                                    ReferenceStartLine = CurrentLine;
                                    ReferenceStartOffset = CurrentAbsoluteOffset;
                                    prefixBuilder.Length = 0;
                                }
                                prefixContinue = false;                                
                            }                            
                            if (currentChar.CanBePartOfIdentifier() || currentChar == '.') {
                                prefixBuilder.Append(currentChar);
                            }
                            bool wasAtRoot = currentElement == Trie.Root;
                            currentElement = Trie.Step(currentElement, currentChar);
                                                                                  
                            if (currentElement.IsTerminal) {
                                AddResult(list, prefixBuilder.ToString(), currentElement.Infos);
                            }     
                        }                        
                    } else {
                        currentElement = Trie.Root;                        
                    }
                    if (!insideString  || insideComment) {
                        isVerbatimString = false;
                    }
                }
                
                Move();
            }

            return list;
        }        

        private CodeReferenceInfo GetInfoWithNamespace(List<CodeReferenceInfo> list, string nmspc) {
            CodeReferenceInfo nfo = null;
            foreach (var item in list)
                if (item.Origin.Namespace == nmspc) {
                    if (prefferedResXItem != null) {
                        if (nfo == null || prefferedResXItem==item.Origin) {
                            nfo = item;
                        }
                    } else {
                        if (nfo == null || nfo.Origin.IsCultureSpecific()) {
                            nfo = item;
                        }
                    }
                     
                }
            return nfo;
        }

        private T AddResult(List<T> list, string referenceText, List<CodeReferenceInfo> trieElementInfos) {            
            CodeReferenceInfo info = null;
            string[] t = referenceText.Split('.');
            if (t.Length < 2) throw new Exception("Code parse error - invalid token " + referenceText);
            string referenceClass;
            string prefix;
            
            if (t.Length == 2) {
                referenceClass = t[0];
                prefix = null;
            } else {
                referenceClass = t[t.Length - 2];
                prefix = string.Join(".", t, 0, t.Length - 2);
            }

            if (string.IsNullOrEmpty(prefix)) {                
                UsedNamespaceItem item = UsedNamespaces.ResolveNewReference(referenceClass, Project);

                if (item != null) {
                    info = GetInfoWithNamespace(trieElementInfos, item.Namespace);
                    if (info == null) return null;
                }
            } else {
                string aliasNamespace = UsedNamespaces.GetNamespace(prefix);
                if (!string.IsNullOrEmpty(aliasNamespace)) {
                    info = GetInfoWithNamespace(trieElementInfos, aliasNamespace);
                    if (info == null) return null;                    
                } else {
                    info = GetInfoWithNamespace(trieElementInfos, prefix);

                    if (info == null) {
                        UsedNamespaceItem item = UsedNamespaces.ResolveNewReference(prefix + "." + referenceClass, Project);
                        if (item == null) return null;

                        info = GetInfoWithNamespace(trieElementInfos, item.Namespace + "." + prefix);
                        if (info == null) return null;                                 
                    }
                }                
            }
            
            if (info != null) {
                TextSpan span = new TextSpan();
                span.iStartLine = ReferenceStartLine - 1;
                span.iStartIndex = ReferenceStartIndex;
                span.iEndLine = CurrentLine - 1;
                span.iEndIndex = CurrentIndex + 1;

                var resultItem = new T();
                resultItem.Value = info.Value;
                resultItem.SourceItem = this.SourceItem;
                resultItem.ReplaceSpan = span;
                resultItem.AbsoluteCharOffset = ReferenceStartOffset;
                resultItem.AbsoluteCharLength = CurrentAbsoluteOffset - ReferenceStartOffset;
                resultItem.DestinationItem = info.Origin;                
                resultItem.FullReferenceText = string.Format("{0}.{1}.{2}", info.Origin.Namespace, info.Origin.Class, referenceText.Substring(referenceText.LastIndexOf('.') + 1));
                resultItem.IsWithinLocalizableFalse = IsWithinLocFalse;
                resultItem.Key = info.Key;

                if (string.IsNullOrEmpty(prefix)) {
                    resultItem.OriginalReferenceText = referenceText;
                } else {
                    resultItem.OriginalReferenceText = string.Format("{0}.{1}", prefix, referenceText);
                }

                list.Add(resultItem);

                return resultItem;
            } else throw new Exception("Cannot determine reference target.");
        }
        
    }
}
