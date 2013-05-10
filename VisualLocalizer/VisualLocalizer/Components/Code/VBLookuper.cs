using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using VisualLocalizer.Library;
using System.Text.RegularExpressions;
using System.Collections;
using System.Reflection;
using VisualLocalizer.Library.Extensions;

namespace VisualLocalizer.Components.Code {

    /// <summary>
    /// Implements PreProcessChar() method according to VB specifications.
    /// </summary>
    /// <typeparam name="T">Type of result item</typeparam>
    internal class VBLookuper<T> : AbstractCodeLookuper<T> where T : AbstractResultItem, new() {

        /// <summary>
        /// Regular expression describing possible content between two result items that can be merged
        /// </summary>
        private static Regex VBConcateningRegexp;

        /// <summary>
        /// List of possible statements between two merged result items
        /// </summary>
        private static List<AbstractConcatenatingChar> concatenatingList;

        static VBLookuper() {
            VBConcateningRegexp = new Regex(@"^(\s*_?\s*[&\+]\s*_?\s*([^&\+]+)\s*_?\s*)*\s*_?\s*[&\+]\s*_?\s*$");                        
            concatenatingList = new List<VBLookuper<T>.AbstractConcatenatingChar>();

            string nmspc = @"Microsoft\s*_?\s*\.\s*_?\s*VisualBasic\s*_?\s*\.\s*_?\s*";
            string constants = @"Constants\s*_?\s*\.\s*_?\s*";

            concatenatingList.Add(new ExplicitConcatenatingCharWithOptionalNamespace("(" + nmspc + @")?(" + constants + ")?vbCrLf", "\r\n"));
            concatenatingList.Add(new ExplicitConcatenatingCharWithOptionalNamespace("(" + nmspc + @")?(" + constants + ")?vbCr", "\r"));
            concatenatingList.Add(new ExplicitConcatenatingCharWithOptionalNamespace("(" + nmspc + @")?(" + constants + ")?vbLf", "\n"));
            concatenatingList.Add(new ExplicitConcatenatingCharWithOptionalNamespace("(" + nmspc + @")?(" + constants + ")?vbNewLine", Environment.NewLine));
            concatenatingList.Add(new ExplicitConcatenatingCharWithOptionalNamespace("(" + nmspc + @")?(" + constants + ")?vbNullString", string.Empty));
            concatenatingList.Add(new ExplicitConcatenatingCharWithOptionalNamespace("(" + nmspc + @")?(" + constants + ")?vbTab", "\t"));
            concatenatingList.Add(new ExplicitConcatenatingCharWithOptionalNamespace("(" + nmspc + @")?(" + constants + ")?vbBack", "\b"));
            concatenatingList.Add(new ExplicitConcatenatingCharWithOptionalNamespace("(" + nmspc + @")?(" + constants + ")?vbFormFeed", "\f"));
            concatenatingList.Add(new ExplicitConcatenatingCharWithOptionalNamespace("(" + nmspc + @")?(" + constants + ")?vbVerticalTab", "\v"));

            foreach (var info in typeof(Microsoft.VisualBasic.ControlChars).GetFields(BindingFlags.Static | BindingFlags.Public)) {
                concatenatingList.Add(new ExplicitConcatenatingCharWithOptionalNamespace(@"(" + nmspc + @")?ControlChars\s*_?\s*\.\s*_?\s*" + info.Name, info.GetValue(null).ToString()));
            }

            concatenatingList.Add(new ChrConcatenatingChar(@"(" + nmspc + @")?Chr\s*_?\s*\(\s*_?\s*(\d+)\s*_?\s*\)")); 
            concatenatingList.Add(new ChrConcatenatingChar(@"(" + nmspc + @")?ChrW\s*_?\s*\(\s*_?\s*(\d+)\s*_?\s*\)"));
        }

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
            if (info == null && newPrefix != null) info = TryResolve(newPrefix, className, trieElementInfos);
            if (info == null && string.IsNullOrEmpty(prefix)) info = TryResolve(className, className, trieElementInfos);            
            if (info == null && newPrefix != null) info = TryResolve(newPrefix + "." + className, className, trieElementInfos);
            
            return info;
        }

        /// <summary>
        /// Selects that code reference from given list of options, that best matches given namespace.
        /// </summary>        
        protected override CodeReferenceInfo GetInfoWithNamespace(List<CodeReferenceInfo> list, string nmspc) {
            CodeReferenceInfo nfo = null;
            foreach (var item in list)
                if (item.Origin.Namespace == nmspc) {
                    if (prefferedResXItem != null) {
                        if (nfo == null || prefferedResXItem == item.Origin) {
                            nfo = item;
                        }
                    } else {
                        if (nfo == null || nfo.Origin.IsCultureSpecific()) {
                            nfo = item;
                        }
                    }

                }
            return nfo;
        }

        /// <summary>
        /// Returns true if underscore (_) has a role of line-joining character
        /// </summary>
        protected override bool UnderscoreIsLineJoiningChar {
            get {
                return true;
            }
        }

        /// <summary>
        /// Concatenates two result items based on the content between them. If any control characters are added, they are interpreted and added to the strings.
        /// </summary>
        protected override void ConcatenateWithPreviousResult(IList results, CodeStringResultItem previouslyAddedItem, CodeStringResultItem resultItem) {
            string textBetween = text.Substring(previouslyAddedItem.AbsoluteCharOffset + previouslyAddedItem.AbsoluteCharLength - OriginalAbsoluteOffset, resultItem.AbsoluteCharOffset - previouslyAddedItem.AbsoluteCharOffset - previouslyAddedItem.AbsoluteCharLength);            
            Match match = VBConcateningRegexp.Match(textBetween); // test if the content between is known
            
            if (match.Success) {
                bool ok = match.Groups.Count == 3;
                if (!ok) return;

                // interpret control characters
                StringBuilder addedValues = new StringBuilder();
                for (int i = 0; i < match.Groups[2].Captures.Count; i++) {
                    bool matched = false;
                    foreach (AbstractConcatenatingChar m in concatenatingList) {
                        if (m.Matches(match.Groups[2].Captures[i].Value.Trim())) {
                            addedValues.Append(m.ReplaceChar);
                            matched = true;
                            break;
                        }
                    }
                    ok = ok && matched;
                }

                // if all control characters were known, add them to the result item and merge it with the previous one
                if (ok) {
                    previouslyAddedItem.Value += addedValues.ToString();
                    results.RemoveAt(results.Count - 1);

                    base.ConcatenateWithPreviousResult(results, previouslyAddedItem, resultItem);
                }
            }
        }

        /// <summary>
        /// Base class for statement between two VB string result items
        /// </summary>
        private abstract class AbstractConcatenatingChar {

            /// <summary>
            /// Returns true if given text corresponds to this statement
            /// </summary>            
            public abstract bool Matches(string input);

            /// <summary>
            /// The control character that this statement represents
            /// </summary>
            public string ReplaceChar { get; protected set; }
        }   

        /// <summary>
        /// Represents statements between two VB string result items like vbCrLf, vbNewLine, vbTab etc.
        /// </summary>
        private class ExplicitConcatenatingCharWithOptionalNamespace : AbstractConcatenatingChar {
            private Regex regex;

            /// <summary>
            /// Creates new instance
            /// </summary>
            /// <param name="regex">The text of the statement</param>
            /// <param name="c">Control character it represents</param>
            public ExplicitConcatenatingCharWithOptionalNamespace(string regex, string c) {
                this.regex = new Regex(regex);
                this.ReplaceChar = c;
            }

            /// <summary>
            /// Returns true if given text corresponds to this statement
            /// </summary>
            public override bool Matches(string input) {
                return regex.IsMatch(input);
            }
        }

        /// <summary>
        /// Represents Chr() and ChrW() functions used between two VB string result items
        /// </summary>
        private class ChrConcatenatingChar : AbstractConcatenatingChar {
            private Regex regex;

            /// <summary>
            /// Creates new instance
            /// </summary>
            /// <param name="regex">The regular expression for Chr and ChrW</param>
            public ChrConcatenatingChar(string regex) {
                this.regex = new Regex(regex);                
            }


            /// <summary>
            /// Returns true if given text corresponds to this statement. Parses the input and initializes ReplaceChar properly.
            /// </summary>
            public override bool Matches(string input) {
                Match m = regex.Match(input);
                if (!m.Success) return false;
                if (m.Groups.Count != 2 && m.Groups.Count != 3) return false;

                int val = int.Parse(m.Groups[m.Groups.Count - 1].Value);
                ReplaceChar = Microsoft.VisualBasic.Strings.Chr(val).ToString();

                return true;
            }
        }
    }
}
