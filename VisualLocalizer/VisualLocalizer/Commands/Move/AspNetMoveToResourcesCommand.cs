using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;
using VisualLocalizer.Library.AspxParser;

namespace VisualLocalizer.Commands {
    internal sealed class AspNetMoveToResourcesCommand : MoveToResourcesCommand<AspNetStringResultItem> {
        
        protected override AspNetStringResultItem GetReplaceStringItem() {
            TextSpan[] spans = new TextSpan[1];
            int hr = textView.GetSelectionSpan(spans);
            Marshal.ThrowExceptionForHR(hr);

            AspNetStringResultItem result = null;
            TextSpan selectionSpan = spans[0];

            BatchMoveCommand batchMoveInstance = new BatchMoveCommand();
            batchMoveInstance.Results = new List<CodeStringResultItem>();

            AspNetCodeExplorer.Instance.Explore(batchMoveInstance, currentDocument.ProjectItem,
                selectionSpan.iEndLine, selectionSpan.iEndIndex);
            foreach (AspNetStringResultItem resultItem in batchMoveInstance.Results) {
                if (resultItem.ReplaceSpan.Contains(selectionSpan)) {
                    result = resultItem;
                    break;
                }
            }

            batchMoveInstance.Results.Clear();
            batchMoveInstance.Results = null;

            return result;
        }
             
    }
}
