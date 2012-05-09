using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.CodeDom.Compiler;
using VSLangProj;
using System.Globalization;

namespace VisualLocalizer.Components {
    internal static class Utils {

        private static CodeDomProvider csharp = Microsoft.CSharp.CSharpCodeProvider.CreateProvider("C#");
        private static UnicodeCategory[] validIdentifierCategories = {UnicodeCategory.TitlecaseLetter,
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

        internal static string TypeOf(object o) {
            return Microsoft.VisualBasic.Information.TypeName(o);
        }

        internal static bool isIdentifierChar(char p) {
            UnicodeCategory charCat = char.GetUnicodeCategory(p);
            foreach (UnicodeCategory c in validIdentifierCategories)
                if (c == charCat) return true;
            return false;
        }

        internal static string RemoveWhitespace(string text) {
            StringBuilder b = new StringBuilder();

            foreach (char c in text)
                if (!char.IsWhiteSpace(c)) b.Append(c);

            return b.ToString();
        }        
        
        internal static bool IsValidIdentifier(string name, ref string errorText) {
            if (string.IsNullOrEmpty(name)) {
                errorText = "Key cannot be empty";
                return false;
            }
            if (!csharp.IsValidIdentifier(name)) {
                errorText = "Key is not valid C# identifier";
                return false;
            }
          
            return true;
        }
    }

    
}
