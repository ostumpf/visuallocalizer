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

        /// <summary>
        /// Language-specific implementation, handles beginnings and ends of strings, comments etc.
        /// </summary>
        /// <param name="insideComment">IN/OUT - true if lookuper's position is within comment</param>
        /// <param name="insideString">IN/OUT - true if lookuper's position is within string literal</param>
        /// <param name="isVerbatimString">IN/OUT - true string literal is verbatim (C# only)</param>
        /// <param name="skipLine">OUT - true if lookuper should skip current line entirely</param>
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
                            int quotesCount = 0;
                            for (int i = globalIndex - 1; i + OriginalAbsoluteOffset >= StringStartAbsoluteOffset; i--) {
                                if (text[i] == '"') quotesCount++;
                            }
                            quotesCount--; // starting quotes
                            if (GetCharBack(-1) != '"' && quotesCount % 2 == 0) {
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

        /// <summary>
        /// Attempts to determine which resource key the reference points to
        /// </summary>        
        protected override CodeReferenceInfo ResolveReference(string prefix, string className, List<CodeReferenceInfo> trieElementInfos) {
            return TryResolve(prefix, className, trieElementInfos);
        }
       
    }
}
