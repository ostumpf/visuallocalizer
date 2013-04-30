using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using VisualLocalizer.Library.Components;

namespace VisualLocalizer.Editor.UndoUnits {

    /// <summary>
    /// Base class for undo units handling renaming keys in string grid and list views
    /// </summary>
    [Guid("D9DE0AA3-A608-4f2a-945F-1A0E5E8F7A12")]
    internal abstract class RenameKeyUndoUnit : AbstractUndoUnit {

        public RenameKeyUndoUnit(string oldKey, string newKey) {
            this.OldKey = oldKey;
            this.NewKey = newKey;
        }

        public string OldKey { get; set; }
        public string NewKey { get; set; }        

        public override string GetUndoDescription() {
            return string.Format("Key name changed from \"{0}\" to \"{1}\"", OldKey, NewKey);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
