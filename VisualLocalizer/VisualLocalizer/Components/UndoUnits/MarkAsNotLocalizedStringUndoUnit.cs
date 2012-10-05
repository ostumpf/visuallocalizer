using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {
    internal sealed class MarkAsNotLocalizedStringUndoUnit : AbstractUndoUnit {

        private string Literal { get; set; }

        public MarkAsNotLocalizedStringUndoUnit(string literal) {
            this.Literal = literal;
        }
        
        public override void Undo() {            
        }

        public override void Redo() {            
        }

        public override string GetUndoDescription() {
            return string.Format("Mark \"{0}\" as not localizable", Literal);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
