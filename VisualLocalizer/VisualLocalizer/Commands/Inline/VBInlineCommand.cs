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

namespace VisualLocalizer.Commands {
    internal sealed class VBInlineCommand : InlineCommand<VBCodeReferenceResultItem> {
        
        public override VBCodeReferenceResultItem GetCodeReferenceResultItem() {
            if (currentCodeModel == null)
                throw new Exception("Current document has no CodeModel.");

            string text;
            TextPoint startPoint;
            string codeFunctionName;
            string codeVariableName;
            CodeElement2 codeClass;
            TextSpan selectionSpan;
            bool ok = GetCodeBlockFromSelection(out text, out startPoint, out codeFunctionName, out codeVariableName, out codeClass, out selectionSpan);
            VBCodeReferenceResultItem result = null;

            if (ok) {
                CodeNamespace codeNamespace = codeClass.GetNamespace();

                List<VBCodeReferenceResultItem> items = VBCodeReferenceLookuper.Instance.LookForReferences(currentDocument.ProjectItem, text, startPoint, currentDocument.ProjectItem.ContainingProject.GetResXItemsAround(null, false, true).CreateTrie(),
                    codeNamespace.GetUsedNamespaces(currentDocument.ProjectItem), false, currentDocument.ProjectItem.ContainingProject, null);


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
