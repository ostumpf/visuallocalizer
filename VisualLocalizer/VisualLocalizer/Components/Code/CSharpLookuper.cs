using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE80;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {

    /// <summary>
    /// Implements PreProcessChar() method according to C# specifications.
    /// </summary>
    /// <typeparam name="T">Type of result item</typeparam>
    internal class CSharpLookuper<T> : AbstractCodeLookuper<T> where T:AbstractResultItem,new() {
        
        protected override void PreProcessChar(ref bool insideComment, ref bool insideString, ref bool isVerbatimString, out bool skipLine) {           
            skipLine = false;

            if (currentChar == '/' && !insideString) {
                if (GetCharBack(1) == '/' && !insideComment) {
                    skipLine = true;
                }
                if (GetCharBack(1) == '*' && GetCharBack(2) != '/') {
                    insideComment = false;
                }
            } else if (currentChar == '*' && !insideString) {
                if (GetCharBack(1) == '/' && !insideComment) {
                    insideComment = true;
                }
            } else if ((currentChar == '\"' || currentChar == '\'') && !insideComment) {
                if (insideString) {
                    if (stringStartChar == currentChar) {
                        if (!isVerbatimString) {
                            if (CountBack('\\', globalIndex) % 2 == 0)
                                insideString = false;
                        } else {
                            int q = CountBack('"', globalIndex);
                            if (GetCharBack(-1) != '"' && ((q % 2 == 0 && text[globalIndex - q - 1] != '@') || (q % 2 != 0 && text[globalIndex - q - 1] == '@'))) {
                                insideString = false;
                            }
                        }
                    }
                } else {
                    insideString = true;
                    stringStartChar = currentChar;
                    StringStartIndex = CurrentIndex;
                    StringStartLine = CurrentLine;
                    StringStartAbsoluteOffset = CurrentAbsoluteOffset;
                    if (GetCharBack(1) == '@') {
                        isVerbatimString = true;
                        StringStartIndex--;
                    }
                }
            } 
        }
        
       
    }
}
