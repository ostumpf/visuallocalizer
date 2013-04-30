using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Components;
using EnvDTE80;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE;
using VisualLocalizer.Library;
using VisualLocalizer.Extensions;
using VisualLocalizer.Components.Code;
using VisualLocalizer.Library.Extensions;

namespace VisualLocalizer.Commands {

    /// <summary>
    /// Represents "inline" command on a VB source code
    /// </summary>
    internal sealed class VBInlineCommand : InlineCommand<VBCodeReferenceResultItem> {

        /// <summary>
        /// Should return result item located in current selection point. Returns null in any case of errors and exceptions.
        /// </summary> 
        public override VBCodeReferenceResultItem GetCodeReferenceResultItem() {
            if (currentCodeModel == null) throw new Exception("Current document has no CodeModel.");

            string text;
            TextPoint startPoint;
            string codeFunctionName;
            string codeVariableName;
            CodeElement2 codeClass;
            TextSpan selectionSpan;
            // get code block in which current selection point is located
            bool ok = GetCodeBlockFromSelection(out text, out startPoint, out codeFunctionName, out codeVariableName, out codeClass, out selectionSpan);
            VBCodeReferenceResultItem result = null;

            if (ok) {
                CodeNamespace codeNamespace = codeClass.GetNamespace();

                // get list of all references in given code block
                List<VBCodeReferenceResultItem> items = VBCodeReferenceLookuper.Instance.LookForReferences(currentDocument.ProjectItem, text, startPoint, currentDocument.ProjectItem.ContainingProject.GetResXItemsAround(false, true).CreateTrie(),
                    codeNamespace.GetUsedNamespaces(currentDocument.ProjectItem), false, currentDocument.ProjectItem.ContainingProject, null);

                // select the reference located in current selection (if any)
                foreach (VBCodeReferenceResultItem item in items) {
                    if (item.ReplaceSpan.Contains(selectionSpan)) {
                        result = item;
                        result.SourceItem = currentDocument.ProjectItem;
                        break;
                    }
                }
            }

            return result;
        }
    }
}
