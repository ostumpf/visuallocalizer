using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE80;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {

    internal class CodeStringLookuper : AbstractCodeLookuper {        

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
       
        protected CodeNamespace namespaceElement;
        protected string classOrStructElement;
        protected string methodElement;
        protected string variableElement;        

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
                    PreProcessChar(ref insideComment, ref insideString, ref isVerbatimString, out skipLine);

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
                            AddResult(list, builder.ToString(), isVerbatimString);

                        isVerbatimString = false;
                        builder = null;
                    }
                }

                Move();
            }

            return list;
        }
        
        protected void AddResult(List<CodeStringResultItem> list, string originalValue, bool isVerbatimString) {
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

        
    }
}
