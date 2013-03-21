using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {

    /// <summary>
    /// Implements PreProcessChar() method according to VB specifications.
    /// </summary>
    /// <typeparam name="T">Type of result item</typeparam>
    internal class VBLookuper<T> : AbstractCodeLookuper<T> where T : AbstractResultItem, new() {

        protected override void PreProcessChar(ref bool insideComment, ref bool insideString, ref bool isVerbatimString, out bool skipLine) {
            skipLine = false;            

            if (currentChar == '\'' && !insideString) {
                skipLine = true;
            } else if (!insideString && char.ToLower(currentChar) == 'm' && char.ToLower(GetCharBack(1)) == 'e'
                && char.ToLower(GetCharBack(2)) == 'r' && !GetCharBack(-1).CanBePartOfIdentifier() && !GetCharBack(3).CanBePartOfIdentifier()) {
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
                        int q = CountBack('"', globalIndex);
                        if (GetCharBack(-1) != '"' && ((q % 2 == 0 && CurrentAbsoluteOffset - q > StringStartAbsoluteOffset) || (q % 2 != 0 && CurrentAbsoluteOffset - q == StringStartAbsoluteOffset))) {
                            insideString = false;
                        }
                    }
                } 
            }

           
        }
        
    }
}
