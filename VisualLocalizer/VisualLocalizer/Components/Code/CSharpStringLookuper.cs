using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {
    internal sealed class CSharpStringLookuper : CodeStringLookuper<CSharpStringResultItem> {

        private CodeNamespace namespaceElement;        
        private string methodElement;
        private string variableElement;
        private static CSharpStringLookuper instance;

        private CSharpStringLookuper() { }

        public static CSharpStringLookuper Instance {
            get {
                if (instance == null) instance = new CSharpStringLookuper();
                return instance;
            }
        }

        public List<CSharpStringResultItem> Run(ProjectItem projectItem, bool isGenerated, string text, TextPoint startPoint, CodeNamespace namespaceElement,
            string classOrStructElement, string methodElement, string variableElement, bool isWithinLocFalse) {

            this.SourceItemGenerated = isGenerated;
            this.SourceItem = projectItem;
            this.text = text;
            this.CurrentIndex = startPoint.LineCharOffset - 1;
            this.CurrentLine = startPoint.Line;
            this.CurrentAbsoluteOffset = startPoint.AbsoluteCharOffset + startPoint.Line - 2;
            this.namespaceElement = namespaceElement;
            this.ClassOrStructElement = classOrStructElement;
            this.methodElement = methodElement;
            this.variableElement = variableElement;
            this.IsWithinLocFalse = isWithinLocFalse;

            return LookForStrings();
        }

        protected override CSharpStringResultItem AddResult(List<CSharpStringResultItem> list, string originalValue, bool isVerbatimString, bool isUnlocalizableCommented) {
            CSharpStringResultItem resultItem = base.AddResult(list, originalValue, isVerbatimString, isUnlocalizableCommented);
            
            resultItem.MethodElementName = methodElement;
            resultItem.NamespaceElement = namespaceElement;
            resultItem.VariableElementName = variableElement;            

            return resultItem;
        }
    }
}
