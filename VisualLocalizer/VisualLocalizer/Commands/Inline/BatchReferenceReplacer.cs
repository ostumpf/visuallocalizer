using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Components;
using System.Collections;
using VisualLocalizer.Library;
using VisualLocalizer.Editor.UndoUnits;
using Microsoft.VisualStudio.TextManager.Interop;

namespace VisualLocalizer.Commands {

    /// <summary>
    /// Used to perform rename operation with a resource reference.
    /// </summary>
    internal sealed class BatchReferenceReplacer : AbstractBatchReferenceProcessor {

        public BatchReferenceReplacer() { }

        /// <summary>
        /// Returns text that replaces current reference
        /// </summary> 
        public override string GetReplaceString(CodeReferenceResultItem item) {
            return item.GetReferenceAfterRename(item.KeyAfterRename);
        }

        /// <summary>
        /// Returns replace span of the reference (what should be replaced)
        /// </summary>
        public override TextSpan GetInlineReplaceSpan(CodeReferenceResultItem item, out int absoluteStartIndex, out int absoluteLength) {
            return item.GetInlineReplaceSpan(true, out absoluteStartIndex, out absoluteLength);
        }

        /// <summary>
        /// Returns new undo unit for the item
        /// </summary>  
        public override AbstractUndoUnit GetUndoUnit(CodeReferenceResultItem item, bool externalChange) {
            return new StringRenameKeyInCodeUndoUnit(item.Key, item.KeyAfterRename);
        }
    }
}
