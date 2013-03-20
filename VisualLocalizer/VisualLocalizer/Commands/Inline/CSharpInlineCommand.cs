using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Components;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.TextManager.Interop;
using VisualLocalizer.Library;
using VisualLocalizer.Extensions;

namespace VisualLocalizer.Commands {

    /// <summary>
    /// Represents "inline" command on a C# source code
    /// </summary>
    internal sealed class CSharpInlineCommand : InlineCommand<CSharpCodeReferenceResultItem> {
        public override CSharpCodeReferenceResultItem GetCodeReferenceResultItem() {
            if (currentCodeModel == null) throw new Exception("Current document has no CodeModel.");

            string text;
            TextPoint startPoint;
            string codeFunctionName;
            string codeVariableName;
            CodeElement2 codeClass;
            TextSpan selectionSpan;
            // get code block in which current selection point is located
            bool ok = GetCodeBlockFromSelection(out text, out startPoint, out codeFunctionName, out codeVariableName, out codeClass, out selectionSpan);
            CSharpCodeReferenceResultItem result = null;

            if (ok) {
                CodeNamespace codeNamespace = codeClass.GetNamespace();

                // get list of all references in given code block
                List<CSharpCodeReferenceResultItem> items = CSharpReferenceLookuper.Instance.LookForReferences(currentDocument.ProjectItem, text, startPoint, currentDocument.ProjectItem.ContainingProject.GetResXItemsAround(null, false, true).CreateTrie(),
                    codeNamespace.GetUsedNamespaces(currentDocument.ProjectItem), false, currentDocument.ProjectItem.ContainingProject, null);
                
                // select the reference located in current selection (if any)
                foreach (CSharpCodeReferenceResultItem item in items) {
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
