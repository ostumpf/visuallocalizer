using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE;

namespace VisualLocalizer.Components {
    internal class CodeReferenceLookuper : AbstractCodeLookuper {

        public CodeReferenceLookuper(string text, int startLine, int startIndex, int startOffset, Trie Trie, 
            Dictionary<string, string> usedNamespaces,CodeNamespace codeNamespace) {
            this.text = text;
            this.CurrentIndex = startIndex - 1;
            this.CurrentLine = startLine;
            this.CurrentAbsoluteOffset = startOffset;
            this.Trie = Trie;
            this.UsedNamespaces = usedNamespaces;
            this.CodeNamespace = codeNamespace;
        }

        protected Dictionary<string, string> UsedNamespaces;
        protected Trie Trie { get; set; }
        protected int ReferenceStartLine { get; set; }
        protected int ReferenceStartIndex { get; set; }
        protected int ReferenceStartOffset { get; set; }
        protected int AbsoluteReferenceLength { get; set; }
        protected CodeNamespace CodeNamespace { get; set; }

        public List<CodeReferenceResultItem> LookForReferences() {
            bool insideComment = false, insideString = false, isVerbatimString = false;
            bool skipLine = false;
            currentChar = '?';
            previousChar = '?';
            previousPreviousChar = '?';
            stringStartChar = '?';
            List<CodeReferenceResultItem> list = new List<CodeReferenceResultItem>();
            TrieElement currentElement = Trie.Root; 
            StringBuilder prefixBuilder = new StringBuilder();
            string prefix = null;
            char lastNonWhitespaceChar = '?';

            for (int i = 0; i < text.Length; i++) {
                previousPreviousChar = previousChar;
                previousChar = currentChar;
                currentChar = text[i];

                if (skipLine) {
                    if (currentChar == '\n') {
                        previousChar = '?';
                        previousPreviousChar = '?';
                        skipLine = false;
                    }
                } else {
                    PreProcessChar(ref insideComment, ref insideString, ref isVerbatimString, out skipLine);

                    if (currentChar.CanBePartOfIdentifier() && !lastNonWhitespaceChar.CanBePartOfIdentifier() && lastNonWhitespaceChar != '.') {
                        ReferenceStartIndex = CurrentIndex;
                        ReferenceStartLine = CurrentLine;
                        ReferenceStartOffset = CurrentAbsoluteOffset;
                        AbsoluteReferenceLength = 1;
                        prefixBuilder.Length = 0;
                        prefixBuilder.Append(currentChar);
                        prefix = null;
                    } else {
                        AbsoluteReferenceLength++;
                        if (!char.IsWhiteSpace(currentChar)) prefixBuilder.Append(currentChar);
                    }

                    if (!insideString && !insideComment) {
                        if (!char.IsWhiteSpace(currentChar)) {
                            bool wasAtRoot = currentElement == Trie.Root;
                            currentElement = Trie.Step(currentElement, currentChar);

                            if (wasAtRoot && currentElement != Trie.Root) {
                                if (previousChar.CanBePartOfIdentifier()) {
                                    currentElement = Trie.Root;
                                } else if (prefixBuilder.Length >= 2) {
                                    if (prefixBuilder[prefixBuilder.Length - 2] == '.') {
                                        prefix = prefixBuilder.ToString(0, prefixBuilder.Length - 2);
                                    } else {
                                        prefix = null;
                                        ReferenceStartIndex = CurrentIndex;
                                        ReferenceStartLine = CurrentLine;
                                        ReferenceStartOffset = CurrentAbsoluteOffset;
                                        AbsoluteReferenceLength = 1;
                                    }
                                }
                            }
                            
                            if (currentElement.IsTerminal && (i == text.Length - 1 || !text[i + 1].CanBePartOfIdentifier())) {
                                AddResult(list, currentElement.Word, prefix, currentElement.Tag);
                            }                                                           
                        }
                    } else {
                        currentElement = Trie.Root;
                    }
                    if (!insideString  || insideComment) {
                        isVerbatimString = false;
                    }
                }

                if (!char.IsWhiteSpace(currentChar)) lastNonWhitespaceChar = currentChar;
                Move();
            }

            return list;
        }

        protected void AddResult(List<CodeReferenceResultItem> list, string referenceText,string prefix, List<object> tags) {
            CodeReferenceInfo info = null;
            if (tags.Count == 1) {
                info = (CodeReferenceInfo)tags[0];
            } else {
                if (!string.IsNullOrEmpty(prefix)) {
                    foreach (CodeReferenceInfo nfo in tags) {
                        if (nfo.Origin.Namespace == prefix) {
                            info = nfo;
                            break;
                        }
                    }
                } else {
                    CodeNamespace c = CodeNamespace;
                    while (info == null && c != null) {
                        foreach (CodeReferenceInfo nfo in tags) {
                            if (nfo.Origin.Namespace == c.FullName) {
                                info = nfo;
                                break;
                            }
                        }
                        c = (c as CodeElement).GetNamespace();
                    }
                }
            }

            if (info != null) {
                TextSpan span = new TextSpan();
                span.iStartLine = ReferenceStartLine - 1;
                span.iStartIndex = ReferenceStartIndex;
                span.iEndLine = CurrentLine - 1;
                span.iEndIndex = CurrentIndex + 1;

                var resultItem = new CodeReferenceResultItem();
                resultItem.Value = info.Value;
                resultItem.SourceItem = this.SourceItem;
                resultItem.ReplaceSpan = span;
                resultItem.AbsoluteCharOffset = ReferenceStartOffset;
                resultItem.AbsoluteCharLength = AbsoluteReferenceLength;
                resultItem.DestinationItem = info.Origin;
                resultItem.ReferenceText = string.Format("{0}.{1}.{2}", info.Origin.Namespace, info.Origin.Class, referenceText);

                list.Add(resultItem);
            }
        }

        internal class CodeReferenceInfo {
            public string Value { get; set; }
            public ResXProjectItem Origin { get; set; }
        }
    }
}
