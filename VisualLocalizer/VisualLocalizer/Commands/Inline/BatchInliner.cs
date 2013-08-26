using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Components;
using System.Collections;
using VisualLocalizer.Library;
using System.Windows.Forms;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using VisualLocalizer.Components.Code;
using VisualLocalizer.Library.Components;
using VisualLocalizer.Components.UndoUnits;

namespace VisualLocalizer.Commands.Inline {

    /// <summary>
    /// Used to perform actual Batch inline command.
    /// </summary>
    internal sealed class BatchInliner : AbstractBatchReferenceProcessor {
   
        public BatchInliner() {
        }      

        /// <summary>
        /// Returns text that replaces current reference
        /// </summary>      
        public override string GetReplaceString(CodeReferenceResultItem item) {
            return item.GetInlineValue(); // this includes escaping sequences ( " -> \" )
        }

        /// <summary>
        /// Returns replace span of the reference (what should be replaced)
        /// </summary>  
        public override TextSpan GetInlineReplaceSpan(CodeReferenceResultItem item, out int absoluteStartIndex, out int absoluteLength) {
            return item.GetInlineReplaceSpan(false, out absoluteStartIndex, out absoluteLength);
        }

        /// <summary>
        /// Returns new undo unit for the item
        /// </summary>     
        public override AbstractUndoUnit GetUndoUnit(CodeReferenceResultItem item, bool externalChange) {
            return new InlineUndoUnit(item.FullReferenceText, externalChange);
        }
    }
}
