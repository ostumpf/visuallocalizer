using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE80;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {

    internal abstract class CodeStringLookuper<T> : AbstractCodeLookuper where T:CodeStringResultItem,new() {

        protected string ClassOrStructElement { get; set; }

        public List<T> LookForStrings() {
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

                    bool unlocalizableJustSet=false;
                    if (insideComment) {
                        if (commentBuilder == null) commentBuilder = new StringBuilder();
                        commentBuilder.Append(currentChar);
                    } else if (commentBuilder != null) {
                        lastCommentUnlocalizable = "/" + commentBuilder.ToString() + "/" == StringConstants.CSharpLocalizationComment;
                        commentBuilder = null;
                        unlocalizableJustSet=true;
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
                            AddResult(list, builder.ToString(), isVerbatimString, stringMarkedUnlocalized);

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
        
        protected virtual T AddResult(List<T> list, string originalValue, bool isVerbatimString, bool isUnlocalizableCommented) {
            string value = originalValue;
            if (value.StartsWith("@")) value = value.Substring(1);
            value = value.Substring(1, value.Length - 2);            

            TextSpan span = new TextSpan();
            span.iStartLine = StringStartLine-1;
            span.iStartIndex = StringStartIndex;
            span.iEndLine = CurrentLine-1;
            span.iEndIndex = CurrentIndex + (isVerbatimString ? 0 : 1);

            var resultItem = new T();
            resultItem.Value = value.ConvertCSharpEscapeSequences(isVerbatimString);
            resultItem.SourceItem = this.SourceItem;
            resultItem.ComesFromDesignerFile = this.SourceItemGenerated;
            resultItem.ReplaceSpan = span;            
            resultItem.AbsoluteCharOffset = StringStartAbsoluteOffset - (isVerbatimString ? 1 : 0);
            resultItem.AbsoluteCharLength = originalValue.Length;
            resultItem.WasVerbatim = isVerbatimString;
            resultItem.IsWithinLocalizableFalse = IsWithinLocFalse;
            resultItem.IsMarkedWithUnlocalizableComment = isUnlocalizableCommented;
            resultItem.ClassOrStructElementName = ClassOrStructElement;

            list.Add(resultItem);

            return resultItem;
        }

        
    }
}
