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

    /// <summary>
    /// Represents "move to resources" command on a ASP .NET source code.
    /// </summary>
    internal sealed class AspNetMoveToResourcesCommand : MoveToResourcesCommand<AspNetStringResultItem> {

        /// <summary>
        /// Private instance of the BatchMoveCommand, which is used as callback when the AspNetCodeExplorer hits a code block
        /// </summary>
        private BatchMoveCommand batchMoveInstance;

        public AspNetMoveToResourcesCommand() {
            batchMoveInstance = new BatchMoveCommand();
            batchMoveInstance.Results = new List<CodeStringResultItem>();
        }

        /// <summary>
        /// Evaluates current selection and returns instance of AspNetStringResultItem, representing the string literal.        
        /// </summary>        
        protected override AspNetStringResultItem GetReplaceStringItem() {
            // gets current selection
            TextSpan[] spans = new TextSpan[1];
            int hr = textView.GetSelectionSpan(spans);
            Marshal.ThrowExceptionForHR(hr);

            AspNetStringResultItem result = null;
            TextSpan selectionSpan = spans[0];
                        
            batchMoveInstance.ReinitializeWith(currentDocument.ProjectItem);
            batchMoveInstance.Results.Clear();            

            // run ASP .NET parser on a file and find out all string literals
            // search is limited to the file position that matches end of current selection
            AspNetCodeExplorer.Instance.Explore(batchMoveInstance, currentDocument.ProjectItem,
                selectionSpan.iEndLine, selectionSpan.iEndIndex);

            // looks up found items and selects the one that is located within current selection
            foreach (AspNetStringResultItem resultItem in batchMoveInstance.Results) {
                if (resultItem.ReplaceSpan.Contains(selectionSpan)) {
                    result = resultItem;
                    break;
                }
            }
            
            return result;
        }
             
    }
}
