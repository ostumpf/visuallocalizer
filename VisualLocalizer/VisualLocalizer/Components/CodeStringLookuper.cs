using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE80;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {

     internal class CodeStringResultItem {
        public CodeStringResultItem() {
            MoveThisItem = true;
        }

        public bool MoveThisItem { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public ProjectItem SourceItem { get; set; }
        public ResXProjectItem DestinationItem { get; set; }
        public TextSpan ReplaceSpan { get; set; }
        public CodeNamespace NamespaceElement { get; set; }
        public string MethodElementName { get; set; }
        public string VariableElementName { get; set; }
        public string ClassOrStructElementName { get; set; }
        public int AbsoluteCharOffset { get; set; }
        public int AbsoluteCharLength { get; set; }

        public override string ToString() {
            return string.Format("CodeStringResultItem: Key=\"{0}\", Value=\"{1}\", Source=\"{2}\", Target=\"{3}\"", Key, Value, (SourceItem == null ? "(null)" : SourceItem.Name), (DestinationItem == null ? "(null)" : DestinationItem.InternalProjectItem.Name));
        }
    }

    internal sealed class CodeStringLookuper {

        private string text;

        public CodeStringLookuper(string text, int startLine, int startIndex, int startOffset, CodeNamespace namespaceElement,
            string classOrStructElement, string methodElement, string variableElement) {

            this.text = text;
            this.CurrentIndex = startIndex - 1;
            this.CurrentLine = startLine;
            this.CurrentAbsoluteOffset = startOffset;
            this.namespaceElement = namespaceElement;
            this.classOrStructElement = classOrStructElement;
            this.methodElement = methodElement;
            this.variableElement = variableElement;
        }

        public ProjectItem SourceItem { get; set; }

        private int CurrentLine { get; set; }
        private int CurrentIndex { get; set; }
        private int CurrentAbsoluteOffset { get; set; }
        private int StringStartLine { get; set; }
        private int StringStartIndex { get; set; }
        private int StringStartAbsoluteOffset { get; set; }

        private CodeNamespace namespaceElement;
        private string classOrStructElement;
        private string methodElement;
        private string variableElement;

        private char currentChar, previousChar, previousPreviousChar, stringStartChar;
        public List<CodeStringResultItem> LookForStrings() {
            bool insideComment = false, insideString = false, isVerbatimString = false;
            bool skipLine = false;
            currentChar = '?';
            previousChar = '?';
            previousPreviousChar = '?';
            stringStartChar = '?';
            List<CodeStringResultItem> list = new List<CodeStringResultItem>();

            StringBuilder builder = null;
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
                    processChar(ref insideComment, ref insideString, ref isVerbatimString, out skipLine);

                    if (insideString && !insideComment) {
                        if (builder == null) builder = new StringBuilder();

                        builder.Append(currentChar);
                    } else if (builder != null) {
                        if (!isVerbatimString) {
                            builder.Append(currentChar);
                        } else {
                            builder.Insert(0, '@');
                        }
                        if (stringStartChar == '\"')
                            addResult(list, builder.ToString(), isVerbatimString);

                        isVerbatimString = false;
                        builder = null;
                    }
                }

                move();
            }

            return list;
        }

        private void move() {
            CurrentIndex++;
            CurrentAbsoluteOffset++;
            if ((currentChar == '\n')) {
                CurrentIndex = 0;
                CurrentLine++;
            }
        }

        private void addResult(List<CodeStringResultItem> list, string originalValue, bool isVerbatimString) {
            string value = originalValue;
            if (value.StartsWith("@")) value = value.Substring(1);
            value = value.Substring(1, value.Length - 2);            

            TextSpan span = new TextSpan();
            span.iStartLine = StringStartLine-1;
            span.iStartIndex = StringStartIndex;
            span.iEndLine = CurrentLine-1;
            span.iEndIndex = CurrentIndex + (isVerbatimString ? 0 : 1);

            var resultItem = new CodeStringResultItem();
            resultItem.Value = value.ConvertEscapeSequences(isVerbatimString);
            resultItem.SourceItem = this.SourceItem;
            resultItem.ReplaceSpan = span;
            resultItem.ClassOrStructElementName = classOrStructElement;
            resultItem.MethodElementName = methodElement;
            resultItem.NamespaceElement = namespaceElement;
            resultItem.VariableElementName = variableElement;
            resultItem.AbsoluteCharOffset = StringStartAbsoluteOffset - (isVerbatimString ? 1 : 0);
            resultItem.AbsoluteCharLength = originalValue.Length;

            list.Add(resultItem);
        }

        private void processChar(ref bool insideComment, ref bool insideString, ref bool isVerbatimString, out bool skipLine) {
            skipLine = false;

            if (currentChar == '/' && !insideString) {
                if (previousChar == '/' && !insideComment) {
                    skipLine = true;
                }
                if (previousChar == '*' && previousPreviousChar != '/') {
                    insideComment = false;
                }
            } else if (currentChar == '*' && !insideString) {
                if (previousChar == '/' && !insideComment) {
                    insideComment = true;
                }
            } else if ((currentChar == '\"' || currentChar == '\'') && !insideComment) {
                if (insideString) {
                    if (stringStartChar == currentChar && !isVerbatimString) {
                        if (previousChar != '\\' || (previousChar == '\\' && previousPreviousChar == '\\'))
                            insideString = false;
                    }
                } else {                    
                    insideString = true;
                    stringStartChar = currentChar;
                    StringStartIndex = CurrentIndex;
                    StringStartLine = CurrentLine;
                    StringStartAbsoluteOffset = CurrentAbsoluteOffset;
                    if (previousChar == '@') {
                        isVerbatimString = true;
                        StringStartIndex--;
                    }
                }
            } else if (!insideComment && isVerbatimString && insideString && previousChar == stringStartChar
                && previousPreviousChar != stringStartChar && previousPreviousChar != '@') {
                insideString = false;
            }
        }
    }
}
