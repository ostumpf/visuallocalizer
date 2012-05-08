using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.CodeDom.Compiler;
using VSLangProj;

namespace VisualLocalizer.Components {
    internal static class Utils {

        private static CodeDomProvider csharp = Microsoft.CSharp.CSharpCodeProvider.CreateProvider("C#");

        internal static string TypeOf(object o) {
            return Microsoft.VisualBasic.Information.TypeName(o);
        }

        internal static string RemoveWhitespace(string text) {
            StringBuilder b = new StringBuilder();

            foreach (char c in text)
                if (!char.IsWhiteSpace(c)) b.Append(c);

            return b.ToString();
        }

        internal static string CreateKeyFromValue(string value) {
            StringBuilder builder = new StringBuilder();
            bool upper = true;

            foreach (char c in value)
                if (char.IsLetterOrDigit(c)) {
                    if (upper) {
                        builder.Append(char.ToUpperInvariant(c));
                    } else {
                        builder.Append(c);
                    }
                    upper = false;
                } else if (char.IsWhiteSpace(c)) {
                    upper = true;
                }

            return builder.ToString();
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
