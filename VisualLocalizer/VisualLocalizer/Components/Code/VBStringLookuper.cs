using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {
    internal class VBStringLookuper : VBLookuper<VBStringResultItem> {
        private CodeNamespace namespaceElement;
        private string methodElement;
        private string variableElement;
        private string ClassOrStructElement { get; set; }

        private static VBStringLookuper instance;

        private VBStringLookuper() { }

        public static VBStringLookuper Instance {
            get {
                if (instance == null) instance = new VBStringLookuper();
                return instance;
            }
        }

        public List<VBStringResultItem> LookForStrings(ProjectItem projectItem, bool isGenerated, string text, TextPoint startPoint, CodeNamespace namespaceElement,
            string classOrStructElement, string methodElement, string variableElement, bool isWithinLocFalse) {
            this.namespaceElement = namespaceElement;
            this.ClassOrStructElement = classOrStructElement;
            this.methodElement = methodElement;
            this.variableElement = variableElement;

            return LookForStrings(projectItem, isGenerated, text, startPoint, isWithinLocFalse);
        }

        protected override VBStringResultItem AddStringResult(List<VBStringResultItem> list, string originalValue, bool isVerbatimString, bool isUnlocalizableCommented) {
            char next = globalIndex + 1 < text.Length ? text[globalIndex + 1] : '?';

            if (next != 'c') {
                VBStringResultItem resultItem = base.AddStringResult(list, originalValue, isVerbatimString, isUnlocalizableCommented);

                resultItem.MethodElementName = methodElement;
                resultItem.NamespaceElement = namespaceElement;
                resultItem.VariableElementName = variableElement;
                resultItem.ClassOrStructElementName = ClassOrStructElement;
                resultItem.Value = resultItem.Value.ConvertVBEscapeSequences();
                return resultItem;
            } else return null;            
        }

    }
}
