using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library.AspxParser;
using VisualLocalizer.Library;
using EnvDTE;

namespace VisualLocalizer.Components {
    internal sealed class AspNetCSharpStringLookuper : CSharpLookuper<AspNetStringResultItem> {

        private NamespacesList declaredNamespaces;
        private static AspNetCSharpStringLookuper instance;

        private AspNetCSharpStringLookuper() { }

        public static AspNetCSharpStringLookuper Instance {
            get {
                if (instance == null) instance = new AspNetCSharpStringLookuper();
                return instance;
            }
        }

        public List<AspNetStringResultItem> LookForStrings(ProjectItem projectItem, bool isGenerated, string text, BlockSpan blockSpan, string className, NamespacesList declaredNamespaces) {            
            this.declaredNamespaces = declaredNamespaces;
            return base.LookForStrings(projectItem, isGenerated, text, blockSpan);
        }

        protected override AspNetStringResultItem AddStringResult(List<AspNetStringResultItem> list, string originalValue, bool isVerbatimString, bool isUnlocalizableCommented) {
            if (originalValue.StartsWith("@")) originalValue = originalValue.Substring(1);
            AspNetStringResultItem resultItem = base.AddStringResult(list, originalValue, isVerbatimString, isUnlocalizableCommented);

            resultItem.DeclaredNamespaces = declaredNamespaces;            
            resultItem.Language = LANGUAGE.CSHARP;
            resultItem.Value = resultItem.Value.ConvertCSharpEscapeSequences(isVerbatimString);

            return resultItem;
        }
    }
}
