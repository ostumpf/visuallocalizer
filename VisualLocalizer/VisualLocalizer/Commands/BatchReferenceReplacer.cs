using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Components;
using System.Collections;
using VisualLocalizer.Library;
using VisualLocalizer.Editor.UndoUnits;

namespace VisualLocalizer.Commands {
    internal sealed class BatchReferenceReplacer : AbstractBatchReferenceProcessor {

        public List<CodeReferenceResultItem> ReferenceList { get; private set; }

        public BatchReferenceReplacer(List<CodeReferenceResultItem> list)
            : base(list) {
            this.ReferenceList = list;
        }

        public override CodeReferenceResultItem GetItemFromList(IList list, int index) {
            return (CodeReferenceResultItem)list[index];
        }

        public override string GetReplaceString(CodeReferenceResultItem item) {
            string prefix = item.OriginalReferenceText.Substring(0, item.OriginalReferenceText.LastIndexOf('.'));
            return prefix + "." + item.KeyAfterRename;
        }

        public override AbstractUndoUnit GetUndoUnit(CodeReferenceResultItem item, bool externalChange) {
            return new StringRenameKeyInCodeUndoUnit(item.Key, item.KeyAfterRename);
        }
    }
}
