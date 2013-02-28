using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom.Compiler;
using System.Globalization;
using EnvDTE;
using EnvDTE80;

namespace VisualLocalizer.Library {
    public static class TextEx {

        private static CodeDomProvider csharp = Microsoft.CSharp.CSharpCodeProvider.CreateProvider("C#");
        private static UnicodeCategory[] validIdentifierCategories = {
                                                     UnicodeCategory.TitlecaseLetter,
                                                     UnicodeCategory.UppercaseLetter,
                                                     UnicodeCategory.LowercaseLetter,
                                                     UnicodeCategory.ModifierLetter,
                                                     UnicodeCategory.OtherLetter,
                                                     UnicodeCategory.LetterNumber,
                                                     UnicodeCategory.NonSpacingMark,
                                                     UnicodeCategory.SpacingCombiningMark,
                                                     UnicodeCategory.DecimalDigitNumber,
                                                     UnicodeCategory.ConnectorPunctuation,
                                                     UnicodeCategory.Format
                                                    };      

        public static bool IsValidIdentifier(this string text) {
            return true;
            // TODO!!!!!
            //return !string.IsNullOrEmpty(text) && csharp.IsValidIdentifier(text);
        }

        public static string RemoveWhitespace(this string text) {
            if (text == null) return null;

            StringBuilder b = new StringBuilder();
            foreach (char c in text)
                if (!char.IsWhiteSpace(c)) b.Append(c);

            return b.ToString();
        }

        public static bool CanBePartOfIdentifier(this char p) {
            UnicodeCategory charCat = char.GetUnicodeCategory(p);
            foreach (UnicodeCategory c in validIdentifierCategories)
                if (c == charCat) return true;
            return false;
        }

        public static bool IsPrintable(this char c) {
            return !char.IsControl(c) && c != '\\' && c != '\"';
        }

        public static string ConvertAspNetUnescapeSequences(this string text) {
            return text.Replace("\"", "&quot;");
        }

        public static string ConvertCSharpUnescapeSequences(this string text) {
            StringBuilder b = new StringBuilder();

            foreach (char c in text) {
                if (c.IsPrintable()) {
                    b.Append(c);
                } else {
                    switch (c) {
                        case '\a': b.Append("\\a"); break;
                        case '\b': b.Append("\\b"); break;
                        case '\f': b.Append("\\f"); break;
                        case '\n': b.Append("\\n"); break;
                        case '\r': b.Append("\\r"); break;
                        case '\t': b.Append("\\t"); break;
                        case '\'': b.Append("'"); break;
                        case '\"': b.Append("\\\""); break;
                        case '\\': b.Append("\\\\"); break;
                        default:
                            b.Append(c.Escape());
                            break;
                    }
                }
            }

            return b.ToString();
        }

        public static string ConvertCSharpEscapeSequences(this string text,bool isVerbatim) {
            string resultText;
            if (isVerbatim) {
                resultText = text.Replace("\"\"", "\"");                
            } else {
                char? previousChar = null, previousPreviousChar = null;
                StringBuilder result = new StringBuilder();
                int escapeSeqValue = 0;
                int escapeSeqBase = -1;

                foreach (char c in text) {
                    char? currentChar = c;
                    if (escapeSeqBase == 16) {
                        char lower = char.ToLower(currentChar.Value);
                        if (lower >= '0' && lower < 'f') {
                            escapeSeqValue = escapeSeqValue * escapeSeqBase + lower.ToDecimal();                            
                        } else {
                            result.Append((char)escapeSeqValue);
                            escapeSeqBase = -1;
                            previousChar = null;
                            previousPreviousChar = null;
                        }
                    }
                    if (escapeSeqBase == 8) {
                        if (currentChar >= '0' && currentChar < '8') {
                            escapeSeqValue = escapeSeqValue * escapeSeqBase + currentChar.Value.ToDecimal();                            
                        } else {
                            result.Append((char)escapeSeqValue);
                            escapeSeqBase = -1;
                            previousChar = null;
                            previousPreviousChar = null;
                        }
                    }

                    if (escapeSeqBase == -1) {
                        if (previousChar == '\\' && previousPreviousChar!='\\') {
                            switch (currentChar) {
                                case 'a': result.Append('\a'); break;
                                case 'b': result.Append('\b'); break;
                                case 'f': result.Append('\f'); break;
                                case 'n': result.Append('\n'); break;
                                case 'r': result.Append('\r'); break;
                                case 't': result.Append('\t'); break;
                                case '\'': result.Append('\''); break;
                                case '\"': result.Append('\"'); break;
                                case '\\': result.Append('\\');  break;
                                case '?': result.Append('?'); break;
                                case 'x': escapeSeqBase = 16; escapeSeqValue = 0; break;
                                default:
                                    if (currentChar >= '0' && currentChar < '8') {
                                        escapeSeqBase = 8; 
                                        escapeSeqValue = currentChar.Value.ToDecimal();
                                    } else {
                                        throw new Exception("Error parsing string.");
                                    } break;
                            }
                            currentChar = null; 
                        } else {
                            if (previousChar.HasValue) result.Append(previousChar.Value);
                        }
                        previousPreviousChar = previousChar;
                        previousChar = currentChar;
                    } 
                    
                }
                if (previousChar.HasValue) result.Append(previousChar.Value);
                resultText = result.ToString();
            }
            return resultText;
        }

        private static int ToDecimal(this char hexDec) {
            int x = hexDec - '0';
            if (x < 10) {
                return x;
            } else {
                return (hexDec - 'a') + 10;
            }
        }

        private static string Escape(this char c) {
            return string.Format("\\x{0:x4}", (int)c);
        }

        public static string CreateIdentifier(this string original) {
            StringBuilder b = new StringBuilder();

            foreach (char c in original) {
                if (c.CanBePartOfIdentifier()) {
                    b.Append(c);
                } else {
                    b.Append('_');
                }
            }

            string ident = b.ToString();
            if (!ident.IsValidIdentifier()) ident = "_" + ident;

            return ident;
        }

        public static bool EndsWithAny(this string text, string[] extensions) {
            foreach (string ext in extensions)
                if (text.EndsWith(ext)) return true;
            return false;
        }
    }
}
