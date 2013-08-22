using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Components;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.TextManager.Interop;
using VisualLocalizer.Library;
using VisualLocalizer.Components.Code;
using VisualLocalizer.Library.Extensions;

namespace VisualLocalizer.Commands.Move {

    /// <summary>
    /// Represents "move to resources" command on a C# source code.
    /// </summary>
    internal sealed class CSharpMoveToResourcesCommand : MoveToResourcesCommand<CSharpStringResultItem> {


        /// <summary>
        /// Gets result item from current selection. Returns null in any case of errors and exceptions.
        /// </summary>        
        protected override CSharpStringResultItem GetReplaceStringItem() {
            if (currentCodeModel == null)
                throw new Exception("Current document has no CodeModel.");

            string text;
            TextPoint startPoint;
            string codeFunctionName;
            string codeVariableName;
            CodeElement2 codeClass;
            TextSpan selectionSpan;
            bool isConst;
            object codeModelSource;

            // get current code block
            bool ok = GetCodeBlockFromSelection(out text, out startPoint, out codeFunctionName, out codeVariableName, out codeClass, out selectionSpan, out isConst, out codeModelSource);
            CSharpStringResultItem result = null;            
            if (ok) {
                // parses the code block text and returns list of all found result items                
                List<CSharpStringResultItem> items = CSharpStringLookuper.Instance.LookForStrings(currentDocument.ProjectItem, currentDocument.ProjectItem.IsGenerated(), text, startPoint,
                    codeClass.GetNamespace(), codeClass.Name, codeFunctionName, codeVariableName, false);                

                // look for the result item that is contained in the current selection
                foreach (CSharpStringResultItem item in items) {
                    if (item.ReplaceSpan.Contains(selectionSpan)) {
                        result = item;
                        result.SourceItem = currentDocument.ProjectItem;
                        result.IsConst = isConst;
                        result.CodeModelSource = codeModelSource;;
                        break;
                    }
                }
            }

            return result;

        }
    }
}
