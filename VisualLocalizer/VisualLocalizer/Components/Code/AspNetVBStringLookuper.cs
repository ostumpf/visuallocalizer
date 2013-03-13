using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using VisualLocalizer.Library.AspxParser;
using EnvDTE;

namespace VisualLocalizer.Components {
    internal sealed class AspNetVBStringLookuper : VBLookuper<AspNetStringResultItem> {
        private NamespacesList declaredNamespaces;
        private static AspNetVBStringLookuper instance;

        private AspNetVBStringLookuper() { }

        public static AspNetVBStringLookuper Instance {
            get {
                if (instance == null) instance = new AspNetVBStringLookuper();
                return instance;
            }
        }

        public List<AspNetStringResultItem> LookForStrings(ProjectItem projectItem, bool isGenerated, string text, BlockSpan blockSpan, string className, NamespacesList declaredNamespaces) {            
            this.declaredNamespaces = declaredNamespaces;
            return base.LookForStrings(projectItem, isGenerated, text, blockSpan);
        }

        protected override AspNetStringResultItem AddStringResult(List<AspNetStringResultItem> list, string originalValue, bool isVerbatimString, bool isUnlocalizableCommented) {
            AspNetStringResultItem resultItem = base.AddStringResult(list, originalValue, isVerbatimString, isUnlocalizableCommented);

            resultItem.DeclaredNamespaces = declaredNamespaces;            
            resultItem.Language = LANGUAGE.VB;
            resultItem.Value = resultItem.Value.ConvertVBEscapeSequences();

            return resultItem;
        }
    }
}
