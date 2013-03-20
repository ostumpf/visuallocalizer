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

        public override CodeReferenceResultItem GetItemFromList(IList list, int index) {
            if (list[index] is DataGridViewCheckedRow<CodeReferenceResultItem>) {
                return (list[index] as DataGridViewCheckedRow<CodeReferenceResultItem>).DataSourceItem;
            } else {
                return (CodeReferenceResultItem)list[index];
            }
        }

        public override string GetReplaceString(CodeReferenceResultItem item) {
            return item.GetInlineValue(); // this includes escaping sequences ( " -> \" )
        }

        public override TextSpan GetInlineReplaceSpan(CodeReferenceResultItem item, out int absoluteStartIndex, out int absoluteLength) {
            return item.GetInlineReplaceSpan(false, out absoluteStartIndex, out absoluteLength);
        }

        public override AbstractUndoUnit GetUndoUnit(CodeReferenceResultItem item, bool externalChange) {
            return new InlineUndoUnit(item.FullReferenceText, externalChange);
        }
    }
}
