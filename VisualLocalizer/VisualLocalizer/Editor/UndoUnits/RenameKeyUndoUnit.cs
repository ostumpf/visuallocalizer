using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;

namespace VisualLocalizer.Editor.UndoUnits {

    [Guid("D9DE0AA3-A608-4f2a-945F-1A0E5E8F7A12")]
    internal abstract class RenameKeyUndoUnit : AbstractUndoUnit {

        public RenameKeyUndoUnit(string oldKey, string newKey) {
            this.OldKey = oldKey;
            this.NewKey = newKey;
        }

        public string OldKey { get; protected set; }
        public string NewKey { get; protected set; }        

        public override string GetUndoDescription() {
            return string.Format("Key name changed from \"{0}\" to \"{1}\"", OldKey, NewKey);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
