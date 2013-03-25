using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using VisualLocalizer.Library.AspxParser;
using EnvDTE;

namespace VisualLocalizer.Components {
    internal sealed class AspNetVBStringLookuper : VBLookuper<AspNetStringResultItem> {

        /// <summary>
        /// Namespaces imported in the file
        /// </summary>
        private NamespacesList declaredNamespaces;
        private static AspNetVBStringLookuper instance;

        private AspNetVBStringLookuper() { }

        public static AspNetVBStringLookuper Instance {
            get {
                if (instance == null) instance = new AspNetVBStringLookuper();
                return instance;
            }
        }

        /// <summary>
        /// Returns list of string literals result items in given block of code
        /// </summary>
        /// <param name="projectItem">Project item where code belongs</param>
        /// <param name="isGenerated">Whether project item is designer file</param>
        /// <param name="text">Text to search</param>
        /// <param name="blockSpan">Position of the code block</param>
        /// <param name="className">Substitute for class name - file name</param>
        /// <param name="declaredNamespaces">Namespaces imported in the file</param>   
        public List<AspNetStringResultItem> LookForStrings(ProjectItem projectItem, bool isGenerated, string text, BlockSpan blockSpan, string className, NamespacesList declaredNamespaces) {            
            this.declaredNamespaces = declaredNamespaces;
            return base.LookForStrings(projectItem, isGenerated, text, blockSpan);
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
        protected override AspNetStringResultItem AddStringResult(List<AspNetStringResultItem> list, string originalValue, bool isVerbatimString, bool isUnlocalizableCommented) {
            AspNetStringResultItem resultItem = base.AddStringResult(list, originalValue, isVerbatimString, isUnlocalizableCommented);

            resultItem.DeclaredNamespaces = declaredNamespaces;            
            resultItem.Language = LANGUAGE.VB;
            resultItem.Value = resultItem.Value.ConvertVBEscapeSequences();

            return resultItem;
        }
    }
}
