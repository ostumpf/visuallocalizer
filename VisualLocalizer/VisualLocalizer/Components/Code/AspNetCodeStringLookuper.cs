using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library.AspxParser;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {
    internal sealed class AspNetCodeStringLookuper : CodeStringLookuper<AspNetStringResultItem> {

        private NamespacesList declaredNamespaces;

        public AspNetCodeStringLookuper(string text, BlockSpan blockSpan, NamespacesList declaredNamespaces, string className) {
            this.text = text;
            this.CurrentIndex = blockSpan.StartIndex - 1;
            this.CurrentLine = blockSpan.StartLine;
            this.CurrentAbsoluteOffset = blockSpan.AbsoluteCharOffset;
            this.IsWithinLocFalse = false;            
            this.declaredNamespaces = declaredNamespaces;
            this.ClassOrStructElement = className;
        }

        protected override AspNetStringResultItem AddResult(List<AspNetStringResultItem> list, string originalValue, bool isVerbatimString, bool isUnlocalizableCommented) {
            AspNetStringResultItem resultItem = base.AddResult(list, originalValue, isVerbatimString, isUnlocalizableCommented);

            resultItem.DeclaredNamespaces = declaredNamespaces;

            return resultItem;
        }
    }
}
