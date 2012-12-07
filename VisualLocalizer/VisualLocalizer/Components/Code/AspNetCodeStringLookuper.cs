using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library.AspxParser;
using VisualLocalizer.Library;
using EnvDTE;

namespace VisualLocalizer.Components {
    internal sealed class AspNetCodeStringLookuper : CodeStringLookuper<AspNetStringResultItem> {

        private NamespacesList declaredNamespaces;
        private static AspNetCodeStringLookuper instance;

        private AspNetCodeStringLookuper() { }

        public static AspNetCodeStringLookuper Instance {
            get {
                if (instance == null) instance = new AspNetCodeStringLookuper();
                return instance;
            }
        }

        public List<AspNetStringResultItem> Run(ProjectItem projectItem, bool isGenerated, string text, BlockSpan blockSpan, NamespacesList declaredNamespaces, string className) {
            this.SourceItemGenerated = isGenerated;
            this.SourceItem = projectItem;
            this.text = text;
            this.CurrentIndex = blockSpan.StartIndex - 1;
            this.CurrentLine = blockSpan.StartLine;
            this.CurrentAbsoluteOffset = blockSpan.AbsoluteCharOffset;
            this.IsWithinLocFalse = false;
            this.declaredNamespaces = declaredNamespaces;
            this.ClassOrStructElement = className;

            return LookForStrings();
        }

        protected override AspNetStringResultItem AddResult(List<AspNetStringResultItem> list, string originalValue, bool isVerbatimString, bool isUnlocalizableCommented) {
            AspNetStringResultItem resultItem = base.AddResult(list, originalValue, isVerbatimString, isUnlocalizableCommented);

            resultItem.DeclaredNamespaces = declaredNamespaces;

            return resultItem;
        }
    }
}
