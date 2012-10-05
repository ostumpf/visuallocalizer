using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {

    [Guid("CA1176D3-BAAB-40c4-9390-E909EAF53715")]
    internal sealed class MoveToResourcesOverwriteUndoUnit : AbstractUndoUnit {
        private string key,oldValue,newValue;
        private ResXProjectItem item;

        public MoveToResourcesOverwriteUndoUnit(string key, string oldValue,string newValue, ResXProjectItem resxItem) {
            this.key = key;
            this.item = resxItem;
            this.oldValue = oldValue;
            this.newValue = newValue;
        }

        public override void Undo() {
            item.AddString(key, oldValue);
        }

        public override void Redo() {
            item.AddString(key, newValue);
        }

        public override string GetUndoDescription() {
            return String.Format("Move \"{0}\" to resources, overwriting \"{1}\"", newValue, oldValue);
        }

        public override string GetRedoDescription() {
            return String.Format("Move \"{0}\" to resources, overwriting \"{1}\"", newValue, oldValue);
        }
    }
}
