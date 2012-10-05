using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;

namespace VisualLocalizer.Components {

    [Guid("DF803F40-545A-4e91-A692-1AEF63117BA7")]
    internal sealed class InlineUndoUnit : AbstractUndoUnit {

        private string key;

        public InlineUndoUnit(string key) {
            this.key = key;
        }
        
        public override void Undo() {
            
        }

        public override void Redo() {
            
        }

        public override string GetUndoDescription() {
            return String.Format("Inline {0}", key);
        }

        public override string GetRedoDescription() {
            return String.Format("Inline {0}", key);
        }
    }
}
