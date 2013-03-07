using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {
    
    internal class VBLookuper<T> : AbstractCodeLookuper<T> where T : AbstractResultItem, new() {

        protected override void PreProcessChar(ref bool insideComment, ref bool insideString, ref bool isVerbatimString, out bool skipLine) {
            skipLine = false;
            char next = globalIndex + 1 < text.Length ? text[globalIndex + 1] : '?';

            if (currentChar == '\'' && !insideString) {
                skipLine = true;
            } else if (!insideString && char.ToLower(currentChar) == 'm' && char.ToLower(previousChar) == 'e' 
                && char.ToLower(previousPreviousChar) == 'r' && !next.CanBePartOfIdentifier() && !previousPreviousPreviousChar.CanBePartOfIdentifier()) {
                skipLine = true;
            } else {
                if (currentChar == '"') {
                    if (!insideString) {
                        insideString = true;
                        stringStartChar = currentChar;
                        StringStartIndex = CurrentIndex;
                        StringStartLine = CurrentLine;
                        StringStartAbsoluteOffset = CurrentAbsoluteOffset;
                    } else {                        
                        if (next != '"' && previousChar != '"') {
                            insideString = false;
                        }
                    }
                } 
            }

           
        }
        
    }
}
