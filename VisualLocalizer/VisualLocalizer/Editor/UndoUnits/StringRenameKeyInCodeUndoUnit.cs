using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;

namespace VisualLocalizer.Editor.UndoUnits {

    [Guid("AA762234-2BD9-4dc6-A19B-356A3D4357E0")]
    internal sealed class StringRenameKeyInCodeUndoUnit : AbstractUndoUnit {

        private string OldKey { get; set; }
        private string NewKey { get; set; }

        public StringRenameKeyInCodeUndoUnit(string oldKey, string newKey) {
            this.OldKey = oldKey;
            this.NewKey = newKey;
        }
        
        public override void Undo() {
            MessageBox.ShowError("This operation is part of global operation in ResX editor, only there it can be undone.");
            Marshal.ThrowExceptionForHR(VSConstants.E_ABORT);
        }

        public override void Redo() {
            
        }

        public override string GetUndoDescription() {
            return string.Format("*Renamed resource key from \"{0}\" to \"{1}\"", OldKey, NewKey);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
