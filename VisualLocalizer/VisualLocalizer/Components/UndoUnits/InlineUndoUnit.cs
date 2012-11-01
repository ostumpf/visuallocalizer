using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;

namespace VisualLocalizer.Components {

    [Guid("DF803F40-545A-4e91-A692-1AEF63117BA7")]
    internal sealed class InlineUndoUnit : AbstractUndoUnit {

        private string Key { get; set; }
        private bool ExternalChange { get; set; }

        public InlineUndoUnit(string key, bool externalChange) {
            this.Key = key;
            this.ExternalChange = externalChange;
        }
        
        public override void Undo() {
            if (ExternalChange) throw new InvalidOperationException("Cannot undo external change.");
        }

        public override void Redo() {
            
        }

        public override string GetUndoDescription() {
            return String.Format("{1}Inline {0}", Key, ExternalChange ? "*" : "");
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
