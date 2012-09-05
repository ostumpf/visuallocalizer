using System;
using System.Runtime.InteropServices;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {

    [Guid("B9C8503E-80AA-4260-9954-DCAAF3EA4824")]
    internal sealed class MoveToResourcesUndoUnit : AbstractUndoUnit {

        private string key,value;
        private ResXProjectItem item;        
        
        public MoveToResourcesUndoUnit(string key, string value, ResXProjectItem resxItem) {
            this.key = key;
            this.item = resxItem;
            this.value = value;
        }

        public override void Undo() {
            item.RemoveKey(key);
        }

        public override void Redo() {
            item.AddString(key, value);
        }

        public override string GetUndoDescription() {
            return String.Format("Move \"{0}\" to resources", value);
        }

        public override string GetRedoDescription() {
            return String.Format("Move \"{0}\" to resources", value);
        }
    }
}
