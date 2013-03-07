using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using VisualLocalizer.Library;
using VisualLocalizer.Library.AspxParser;
using Microsoft.VisualStudio.TextManager.Interop;

namespace VisualLocalizer.Components {
    internal abstract class AbstractCodeLookuper<T> where T:AbstractResultItem,new() {

        protected ProjectItem SourceItem { get; set; }
        protected bool SourceItemGenerated { get; set; }
        protected int CurrentLine { get; set; }
        protected int CurrentIndex { get; set; }
        protected int CurrentAbsoluteOffset { get; set; }
        protected int StringStartLine { get; set; }
        protected int StringStartIndex { get; set; }
        protected int StringStartAbsoluteOffset { get; set; }
        protected bool IsWithinLocFalse { get; set; }        

        protected string text;
        protected char currentChar, previousChar, previousPreviousChar,previousPreviousPreviousChar, stringStartChar;
        protected int sameCharInLine = 0;
        protected int globalIndex;
        private object syncRoot = new object();

        protected Project Project { get; set; }
        protected NamespacesList UsedNamespaces { get; set; }
        protected Trie<CodeReferenceTrieElement> Trie { get; set; }
        protected int ReferenceStartLine { get; set; }
        protected int ReferenceStartIndex { get; set; }
        protected int ReferenceStartOffset { get; set; }
        protected ResXProjectItem prefferedResXItem;

        protected abstract void PreProcessChar(ref bool insideComment, ref bool insideString, ref bool isVerbatimString, out bool skipLine);


        protected List<T> LookForStrings(ProjectItem projectItem, bool isGenerated, string text, TextPoint startPoint, bool isWithinLocFalse) {
            this.SourceItemGenerated = isGenerated;
            this.SourceItem = projectItem;
            this.text = text;
            this.CurrentIndex = startPoint.LineCharOffset - 1;
            this.CurrentLine = startPoint.Line;
            this.CurrentAbsoluteOffset = startPoint.AbsoluteCharOffset + startPoint.Line - 2;            
            this.IsWithinLocFalse = isWithinLocFalse;

            return LookForStrings();
        }

        protected List<T> LookForStrings(ProjectItem projectItem, bool isGenerated, string text, BlockSpan blockSpan) {
            this.SourceItemGenerated = isGenerated;
            this.SourceItem = projectItem;
            this.text = text;
            this.CurrentIndex = blockSpan.StartIndex - 1;
            this.CurrentLine = blockSpan.StartLine;
            this.CurrentAbsoluteOffset = blockSpan.AbsoluteCharOffset;
            this.IsWithinLocFalse = false;            
            
            return LookForStrings();
        }

        public List<T> LookForReferences(ProjectItem projectItem, string text, TextPoint startPoint, Trie<CodeReferenceTrieElement> Trie, NamespacesList usedNamespaces, bool isWithinLocFalse, Project project, ResXProjectItem prefferedResXItem) {
            return LookForReferences(projectItem, text, startPoint.LineCharOffset - 1, startPoint.Line, startPoint.AbsoluteCharOffset + startPoint.Line - 2, Trie, usedNamespaces, isWithinLocFalse, project, prefferedResXItem);
        }

        public List<T> LookForReferences(ProjectItem projectItem, string text, BlockSpan blockSpan, Trie<CodeReferenceTrieElement> Trie, NamespacesList usedNamespaces, Project project, ResXProjectItem prefferedResXItem) {
            return LookForReferences(projectItem, text, blockSpan.StartIndex - 1, blockSpan.StartLine, blockSpan.AbsoluteCharOffset, Trie, usedNamespaces, false, project, prefferedResXItem);
        }

        public List<T> LookForReferences(ProjectItem projectItem, string text, int currentIndex, int currentLine, int currentOffset,
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


        protected void Move() {
            CurrentIndex++;
            CurrentAbsoluteOffset++;
            if ((currentChar == '\n')) {
                CurrentIndex = 0;
                CurrentLine++;
            }
        }        
        
        protected virtual T AddStringResult(List<T> list, string originalValue, bool isVerbatimString, bool isUnlocalizableCommented) {
            string value = originalValue;            
            value = value.Substring(1, value.Length - 2);

            TextSpan span = new TextSpan();
            span.iStartLine = StringStartLine - 1;
            span.iStartIndex = StringStartIndex;
            span.iEndLine = CurrentLine - 1;
            span.iEndIndex = CurrentIndex + (isVerbatimString ? 0 : 1);

            var resultItem = new T();
            resultItem.Value = value;
            resultItem.SourceItem = this.SourceItem;
            resultItem.ComesFromDesignerFile = this.SourceItemGenerated;
            resultItem.ReplaceSpan = span;
            resultItem.AbsoluteCharOffset = StringStartAbsoluteOffset - (isVerbatimString ? 1 : 0);
            resultItem.AbsoluteCharLength = originalValue.Length;            
            resultItem.IsWithinLocalizableFalse = IsWithinLocFalse;
            resultItem.IsMarkedWithUnlocalizableComment = isUnlocalizableCommented;

            list.Add(resultItem);

            return resultItem;
        }
       
        protected T AddReferenceResult(List<T> list, string referenceText, List<CodeReferenceInfo> trieElementInfos) {
            CodeReferenceInfo info = null;
            string[] t = referenceText.Split('.');
            if (t.Length < 2) throw new Exception("Code parse error - invalid token " + referenceText);
            string referenceClass;
            string prefix;
            string key;

            if (t.Length == 2) {
                key = t[1];
                referenceClass = t[0];
                prefix = null;
            } else {
                key = t[t.Length - 1];
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
                resultItem.IsWithinLocalizableFalse = IsWithinLocFalse;
                resultItem.Key = info.Key;

                CodeReferenceResultItem refItem = (resultItem as CodeReferenceResultItem);
                refItem.FullReferenceText = string.Format("{0}.{1}.{2}", info.Origin.Namespace, info.Origin.Class, key);
                if (string.IsNullOrEmpty(prefix)) {
                    refItem.OriginalReferenceText = string.Format("{0}.{1}", referenceClass, key);
                } else {
                    refItem.OriginalReferenceText = string.Format("{0}.{1}.{2}", prefix, referenceClass, key);
                }

                list.Add(resultItem);

                return resultItem;
            } else throw new Exception("Cannot determine reference target.");
        }

        protected List<T> LookForStrings() {
            bool insideComment = false, insideString = false, isVerbatimString = false;
            bool skipLine = false;
            currentChar = '?';
            previousChar = '?';
            previousPreviousChar = '?';
            previousPreviousPreviousChar = '?';
            stringStartChar = '?';
            List<T> list = new List<T>();

            StringBuilder builder = null;
            StringBuilder commentBuilder = null;
            bool lastCommentUnlocalizable = false;
            bool stringMarkedUnlocalized = false;

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

                    bool unlocalizableJustSet = false;
                    if (insideComment) {
                        if (commentBuilder == null) commentBuilder = new StringBuilder();
                        commentBuilder.Append(currentChar);
                    } else if (commentBuilder != null) {
                        lastCommentUnlocalizable = "/" + commentBuilder.ToString() + "/" == StringConstants.CSharpLocalizationComment;
                        commentBuilder = null;
                        unlocalizableJustSet = true;
                    }

                    if (insideString && !insideComment) {
                        if (builder == null) {
                            builder = new StringBuilder();
                            stringMarkedUnlocalized = lastCommentUnlocalizable;
                        }
                        builder.Append(currentChar);
                    } else if (builder != null) {
                        if (!isVerbatimString) {
                            builder.Append(currentChar);
                        } else {
                            builder.Insert(0, '@');
                        }
                        if (stringStartChar == '\"')
                            AddStringResult(list, builder.ToString(), isVerbatimString, stringMarkedUnlocalized);

                        stringMarkedUnlocalized = false;
                        isVerbatimString = false;
                        builder = null;
                    }

                    if (!unlocalizableJustSet && !char.IsWhiteSpace(currentChar)) lastCommentUnlocalizable = false;
                }

                Move();
            }

            return list;
        }

        protected List<T> LookForReferences() {
            bool insideComment = false, insideString = false, isVerbatimString = false;
            bool skipLine = false;
            currentChar = '?';
            previousChar = '?';
            previousPreviousChar = '?';
            previousPreviousPreviousChar = '?';
            stringStartChar = '?';
            char nextChar = '?';
            List<T> list = new List<T>();
            CodeReferenceTrieElement currentElement = Trie.Root;
            StringBuilder prefixBuilder = new StringBuilder();
            bool prefixContinue = false;

            for (globalIndex = 0; globalIndex < text.Length; globalIndex++) {
                previousPreviousPreviousChar = previousPreviousChar;
                previousPreviousChar = previousChar;
                previousChar = currentChar;
                currentChar = text[globalIndex];
                nextChar = globalIndex + 1 < text.Length ? text[globalIndex + 1] : '?';

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

                            if (currentElement.IsTerminal && !nextChar.CanBePartOfIdentifier()) {
                                AddReferenceResult(list, prefixBuilder.ToString(), currentElement.Infos);
                            }
                        }
                    } else {
                        currentElement = Trie.Root;
                    }
                    if (!insideString || insideComment) {
                        isVerbatimString = false;
                    }
                }

                Move();
            }

            return list;
        }

        protected int CountBack(char c, int k) {
            k--;
            int count = 0;
            while (k >= 0 && text[k] == c) {
                count++;
                k--;
            }
            return count;
        }

        protected CodeReferenceInfo GetInfoWithNamespace(List<CodeReferenceInfo> list, string nmspc) {
            CodeReferenceInfo nfo = null;
            foreach (var item in list)
                if (item.Origin.Namespace == nmspc) {
                    if (prefferedResXItem != null) {
                        if (nfo == null || prefferedResXItem == item.Origin) {
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

    }
}
