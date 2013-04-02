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

        /// <summary>
        /// Language-specific implementation, handles beginnings and ends of strings, comments etc.
        /// </summary>
        /// <param name="insideComment">IN/OUT - true if lookuper's position is within comment</param>
        /// <param name="insideString">IN/OUT - true if lookuper's position is within string literal</param>
        /// <param name="isVerbatimString">IN/OUT - true string literal is verbatim (C# only)</param>
        /// <param name="skipLine">OUT - true if lookuper should skip current line entirely</param>
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


        /// <summary>
        /// Attempts to determine which resource key the reference points to
        /// </summary>        
        protected override CodeReferenceInfo ResolveReference(string prefix, string className, List<CodeReferenceInfo> trieElementInfos) {
            CodeReferenceInfo info = null;

            // prepend "My" with root namespace
            string newPrefix = null;
            if (prefix != null) {
                int myIndex = prefix.IndexOf("My");
                if (myIndex != -1 && (myIndex + 2 == prefix.Length || prefix[myIndex + 2] == '.')) {
                    newPrefix = prefix.Replace("My", Project.GetRootNamespace() + ".My");
                }
            }
       
            // try various combinations to lookup the reference

            info = TryResolve(prefix, className, trieElementInfos);
            if (info == null && !string.IsNullOrEmpty(prefix)) info = TryResolve(prefix + "." + className, className, trieElementInfos);
            if (info == null && string.IsNullOrEmpty(prefix)) info = TryResolve(className, className, trieElementInfos);
            if (info == null && newPrefix != null) info = TryResolve(newPrefix, className, trieElementInfos);
            if (info == null && newPrefix != null) info = TryResolve(newPrefix + "." + className, className, trieElementInfos);
            
            return info;
        }

        protected override bool UnderscoreIsLineJoiningChar {
            get {
                return true;
            }
        }

    }
}
