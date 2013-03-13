using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom.Compiler;
using System.Globalization;
using EnvDTE;
using EnvDTE80;
using System.Web;

namespace VisualLocalizer.Library {

    public enum LANGUAGE { CSHARP, VB }

    /// <summary>
    /// Container for extension methods working with text objects. 
    /// </summary>
    public static class TextEx {        

        private static CodeDomProvider csharp = Microsoft.CSharp.CSharpCodeProvider.CreateProvider("C#");
        private static CodeDomProvider vb = Microsoft.VisualBasic.VBCodeProvider.CreateProvider("VisualBasic");
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

        /// <summary>
        /// Returns true if given text is valid identifier in specified language.
        /// </summary>        
        public static bool IsValidIdentifier(this string text, LANGUAGE lang) {
            if (string.IsNullOrEmpty(text)) return false;

            bool ok = true; 
            if (lang == LANGUAGE.CSHARP) {
                ok = csharp.IsValidIdentifier(text);
            }
            if (lang == LANGUAGE.VB) {
                ok = vb.IsValidIdentifier(text);
            }
            return ok;
        }

        /// <summary>
        /// Removes all whitespace characters from given text and returns result.
        /// </summary>        
        public static string RemoveWhitespace(this string text) {
            if (text == null) return null;

            StringBuilder b = new StringBuilder();
            foreach (char c in text)
                if (!char.IsWhiteSpace(c)) b.Append(c);

            return b.ToString();
        }

        /// <summary>
        /// Returns true if given character can be part of identifier (that is, belongs to valid unicode category)
        /// </summary>        
        public static bool CanBePartOfIdentifier(this char p) {
            UnicodeCategory charCat = char.GetUnicodeCategory(p);
            foreach (UnicodeCategory c in validIdentifierCategories)
                if (c == charCat) return true;
            return false;
        }

        /// <summary>
        /// Returns true if given char can be part of string literal (unescaped)
        /// </summary>        
        public static bool IsPrintable(this char c) {
            return !char.IsControl(c) && c != '\\' && c != '\"';
        }

        /// <summary>
        /// Returns text modified that way, so it can be displayed as atrribute's value in ASP .NET element
        /// </summary>        
        public static string ConvertAspNetUnescapeSequences(this string text) {
            if (text == null) throw new ArgumentNullException("text");

            return HttpUtility.HtmlEncode(text);
        }

        /// <summary>
        /// Removes escape sequences from atrribute's value in ASP .NET element
        /// </summary>        
        public static string ConvertAspNetEscapeSequences(this string text) {
            if (text == null) throw new ArgumentNullException("text");
            return HttpUtility.HtmlDecode(text);
        }

        /// <summary>
        /// Returns text modified that way, so it can be displayed as string literal in C# code
        /// </summary>        
        public static string ConvertCSharpUnescapeSequences(this string text) {
            if (text == null) throw new ArgumentNullException("text");

            StringBuilder b = new StringBuilder();

            foreach (char c in text) {
                if (c.IsPrintable()) {
                    b.Append(c);
                } else {
                    // unescape well-known characters
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
                            b.Append(c.Escape()); // hexadecimal unescape
                            break;
                    }
                }
            }

            return b.ToString();
        }

        /// <summary>
        /// Removes escape sequences from C# string literal
        /// </summary>  
        public static string ConvertCSharpEscapeSequences(this string text,bool isVerbatim) {
            if (text == null) throw new ArgumentNullException("text");

            string resultText;
            if (isVerbatim) {
                resultText = text.Replace("\"\"", "\"");                
            } else {
                StringBuilder result = new StringBuilder();

                for (int i=0;i<text.Length;i++) {
                    char c = text[i];
                    
                    if (c == '\\') { // escape sequence start 
                        i++;
                        char next = text[i];
                    
                        switch (next) {
                            case '"': result.Append('"'); break;
                            case '\\': result.Append('\\'); break;
                            case 'r': result.Append('\r'); break;
                            case 'f': result.Append('\f'); break; 
                            case 't': result.Append('\t'); break;
                            case 'b': result.Append('\b'); break;
                            case 'n': result.Append('\n'); break;
                            case 'a': result.Append('\a'); break;
                            case 'x': result.Append(ReadEscapeSeq(text, i + 1, 4, 16)); i += 4; break;
                            default:
                                if (next >= '0' && next <= '8') {
                                    result.Append(ReadEscapeSeq(text, i + 1, 3, 8));
                                    i += 3;
                                } else {
                                    result.Append(next); 
                                }
                                break;
                        }
                    } else {
                        result.Append(c);  
                    }                                                  
                }
                
                resultText = result.ToString();
            }
            return resultText;
        }

        private static char ReadEscapeSeq(string text, int startIndex, int charCount, int radix) {
            int end = startIndex + charCount;
            if (end > text.Length) throw new Exception("Invalid string escape sequence.");

            int sum = 0;
            for (int i = startIndex; i < end; i++) {
                sum = sum * radix + ToDecimal(text[i]);
            }

            return (char)sum;
        }

        /// <summary>
        /// Returns text modified that way, so it can be displayed as string literal in VB code
        /// </summary>  
        public static string ConvertVBUnescapeSequences(this string text) {
            if (text == null) throw new ArgumentNullException("text");
            return text.Replace("\"", "\"\""); 
        }

        /// <summary>
        /// Removes escape sequences from VB string literal
        /// </summary>  
        public static string ConvertVBEscapeSequences(this string text) {
            if (text == null) throw new ArgumentNullException("text");
            return text.Replace("\"\"", "\""); 
        }

        /// <summary>
        /// Returns numeric value for hexadecimal character (1 for '1', 11 for 'b' ... )
        /// </summary>        
        private static int ToDecimal(this char hexDec) {
            int x = hexDec - '0';
            if (x < 10) {
                return x;
            } else {
                if (char.IsLower(hexDec)) {
                    return (hexDec - 'a') + 10;
                } else if (char.IsUpper(hexDec)) {
                    return (hexDec - 'A') + 10;
                } else throw new ArgumentException("Invalid hexdec character " + hexDec);
            }
        }

        /// <summary>
        /// Returns character in escaped hexadecimal format: \x1234
        /// </summary>        
        private static string Escape(this char c) {
            return string.Format("\\x{0:x4}", (int)c);
        }

        /// <summary>
        /// Replaces all invalid characters (those which cannot be part of identifiers) with underscores and returns result.
        /// </summary>        
        public static string CreateIdentifier(this string original, LANGUAGE lang) {            
            if (original == null) throw new ArgumentNullException("original");
            
            StringBuilder b = new StringBuilder();

            foreach (char c in original) {
                if (c.CanBePartOfIdentifier()) {
                    b.Append(c);
                } else {
                    b.Append('_');
                }
            }

            string ident = b.ToString();
            if (!ident.IsValidIdentifier(lang)) ident = "_" + ident;

            return ident;
        }

        /// <summary>
        /// Returns true if given text ends with any of specified endings.
        /// </summary>        
        public static bool EndsWithAny(this string text, string[] extensions) {
            if (extensions == null) throw new ArgumentNullException("extensions");
            if (text == null) throw new ArgumentNullException("text");

            foreach (string ext in extensions)
                if (text.EndsWith(ext)) return true;
            return false;
        }
    }
}
