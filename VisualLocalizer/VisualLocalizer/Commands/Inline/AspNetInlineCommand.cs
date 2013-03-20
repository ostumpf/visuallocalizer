using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Components;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;
using VisualLocalizer.Library;

namespace VisualLocalizer.Commands {

    /// <summary>
    /// Represents "inline" command on a ASP .NET source code
    /// </summary>
    internal sealed class AspNetInlineCommand : InlineCommand<AspNetCodeReferenceResultItem> {

        public override AspNetCodeReferenceResultItem GetCodeReferenceResultItem() {
            TextSpan[] spans = new TextSpan[1];
            int hr = textView.GetSelectionSpan(spans);
            Marshal.ThrowExceptionForHR(hr);

            AspNetCodeReferenceResultItem result = null;
            TextSpan selectionSpan = spans[0];

            BatchInlineCommand batchInlineInstance = new BatchInlineCommand();
            batchInlineInstance.ReinitializeWith(currentDocument.ProjectItem);
            batchInlineInstance.Results = new List<CodeReferenceResultItem>();
            
            // run code explorer with fake batch command as callback, using selection span as a limit for the search
            AspNetCodeExplorer.Instance.Explore(batchInlineInstance, currentDocument.ProjectItem,
                selectionSpan.iEndLine, selectionSpan.iEndIndex);

            // look for result item within current selection
            foreach (AspNetCodeReferenceResultItem resultItem in batchInlineInstance.Results) {
                if (resultItem.ReplaceSpan.Contains(selectionSpan)) {
                    result = resultItem;
                    break;
                }
            }

            batchInlineInstance.Results.Clear();
            batchInlineInstance.Results = null;

            return result;
        }
    }
}
