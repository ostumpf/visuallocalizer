using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE80;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {

    internal class CSharpLookuper<T> : AbstractCodeLookuper<T> where T:AbstractResultItem,new() {
        
        protected override void PreProcessChar(ref bool insideComment, ref bool insideString, ref bool isVerbatimString, out bool skipLine) {
            if (previousPreviousChar == previousChar) {
                sameCharInLine++;
            } else {
                sameCharInLine = 1;
            }
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
                        if (CountBack('\\', globalIndex) % 2 == 0)
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
            } else if (!insideComment && isVerbatimString && insideString && previousChar == stringStartChar) {
                if (previousPreviousChar != stringStartChar && (previousPreviousChar != '@' || CurrentAbsoluteOffset - StringStartAbsoluteOffset > 3)) {
                    insideString = false;
                }
                if (previousPreviousChar != '@' && (previousPreviousChar != stringStartChar || previousPreviousPreviousChar == stringStartChar)
                    && (CurrentAbsoluteOffset - StringStartAbsoluteOffset > 4 || sameCharInLine == 4)) {
                    insideString = false;
                }
            }
        }
        
       
    }
}
