using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom.Compiler;
using System.Globalization;

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

        public static bool IsValidIdentifier(this string text, ref string errorText) {
            if (string.IsNullOrEmpty(text)) {
                errorText = "Key cannot be empty";
                return false;
            }
            if (!csharp.IsValidIdentifier(text)) {
                errorText = "Key is not valid C# identifier";
                return false;
            }

            return true;
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
    }
}
