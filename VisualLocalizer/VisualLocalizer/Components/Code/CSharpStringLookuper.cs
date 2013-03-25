using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {

    /// <summary>
    /// Represents lookuper of string literals in C# code
    /// </summary>
    internal sealed class CSharpStringLookuper : CSharpLookuper<CSharpStringResultItem> {

        /// <summary>
        /// Namespace where current code block belongs
        /// </summary>
        private CodeNamespace namespaceElement { get; set; }

        /// <summary>
        /// Name of the method where current code block belongs
        /// </summary>
        private string methodElement { get; set; }

        /// <summary>
        /// Name of the variable current code block initializes
        /// </summary>
        private string variableElement { get; set; }

        /// <summary>
        /// Name of the class/struct/module where current code block belongs
        /// </summary>
        private string ClassOrStructElement { get; set; }

        private static CSharpStringLookuper instance;

        private CSharpStringLookuper() { }

        public static CSharpStringLookuper Instance {
            get {
                if (instance == null) instance = new CSharpStringLookuper();
                return instance;
            }
        }

        /// <summary>
        /// Returns list of string literals result items in given block of code
        /// </summary>
        /// <param name="projectItem">Project item where code belongs</param>
        /// <param name="isGenerated">Whether project item is designer file</param>
        /// <param name="text">Text to search</param>
        /// <param name="startPoint">Position of the text in its source file</param>
        /// <param name="namespaceElement">Namespace where current code block belongs</param>
        /// <param name="classOrStructElement">Name of the class/struct/module where current code block belongs</param>
        /// <param name="methodElement">Name of the method where current code block belongs</param>
        /// <param name="variableElement">Name of the variable current code block initializes</param>
        /// <param name="isWithinLocFalse">True if code block is decorated with [Localizable(false)]</param>   
        public List<CSharpStringResultItem> LookForStrings(ProjectItem projectItem, bool isGenerated, string text, TextPoint startPoint, CodeNamespace namespaceElement,
            string classOrStructElement, string methodElement, string variableElement, bool isWithinLocFalse) {
            this.namespaceElement = namespaceElement;
            this.ClassOrStructElement = classOrStructElement;
            this.methodElement = methodElement;
            this.variableElement = variableElement;

            return LookForStrings(projectItem, isGenerated, text, startPoint, isWithinLocFalse);
        }

        /// <summary>
        /// Adds string literal to the list of results
        /// </summary>
        /// <param name="list">List of results in which it gets added</param>
        /// <param name="originalValue">String literal, including quotes</param>
        /// <param name="isVerbatimString">True if string was verbatim</param>
        /// <param name="isUnlocalizableCommented">True if there was "no-localization" comment</param>
        /// <returns>
        /// New result item
        /// </returns>
        protected override CSharpStringResultItem AddStringResult(List<CSharpStringResultItem> list, string originalValue, bool isVerbatimString, bool isUnlocalizableCommented) {
            if (originalValue.StartsWith("@") && isVerbatimString) originalValue = originalValue.Substring(1); // trim leading @ (verbatim string)
            CSharpStringResultItem resultItem = base.AddStringResult(list, originalValue, isVerbatimString, isUnlocalizableCommented);
            
            resultItem.MethodElementName = methodElement;
            resultItem.NamespaceElement = namespaceElement;
            resultItem.VariableElementName = variableElement;
            resultItem.ClassOrStructElementName = ClassOrStructElement;
            resultItem.WasVerbatim = isVerbatimString;
            resultItem.Value=resultItem.Value.ConvertCSharpEscapeSequences(isVerbatimString);

            return resultItem;
        }
    }
}
