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

namespace VisualLocalizer.Commands {

    /// <summary>
    /// Used to perform actual Batch inline command.
    /// </summary>
    internal sealed class BatchInliner : AbstractBatchReferenceProcessor {

        /// <summary>
        /// Initialize with rows from toolwindow's grid
        /// </summary>        
        public BatchInliner(DataGridViewRowCollection rows)
            : base(rows) {
        }

        /// <summary>
        /// Initialize directly with list of result items
        /// </summary>        
        public BatchInliner(List<CodeReferenceResultItem> list)
            : base(list) {
        }

        /// <summary>
        /// Returns result item with specified index
        /// </summary>
        public override CodeReferenceResultItem GetItemFromList(IList list, int index) {
            if (list[index] is DataGridViewCheckedRow<CodeReferenceResultItem>) {
                return (list[index] as DataGridViewCheckedRow<CodeReferenceResultItem>).DataSourceItem;
            } else {
                return (CodeReferenceResultItem)list[index];
            }
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
