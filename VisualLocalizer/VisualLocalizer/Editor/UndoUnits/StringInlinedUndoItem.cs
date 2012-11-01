using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using VisualLocalizer.Components;
using Microsoft.VisualStudio;

namespace VisualLocalizer.Editor.UndoUnits {

    [Guid("25CC582E-F07A-4ac0-A5C0-9A6DA49272E6")]
    internal sealed class StringInlinedUndoItem : AbstractUndoUnit {

        private int Count { get; set; }
        
        public StringInlinedUndoItem(int count) {
            this.Count = count;
        }

        public override void Undo() {
            MessageBox.ShowError("This operation cannot be undone.");
            Marshal.ThrowExceptionForHR(VSConstants.E_ABORT);
        }

        public override void Redo() {
            
        }

        public override string GetUndoDescription() {
            return string.Format("*Inlined {0} references", Count);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
