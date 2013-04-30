using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using VisualLocalizer.Library.Components;

namespace VisualLocalizer.Components {

    /// <summary>
    /// Represents undo unit for the operation where string literal is marked with "no-localization" comment
    /// </summary>
    [Guid("87B3941B-6715-48de-AF97-A0B227BF7D15")]
    internal sealed class MarkAsNotLocalizedStringUndoUnit : AbstractUndoUnit {

        /// <summary>
        /// Literal that was marked
        /// </summary>
        private string Literal { get; set; }

        public MarkAsNotLocalizedStringUndoUnit(string literal) {
            if (literal == null) throw new ArgumentNullException("literal");

            this.Literal = literal;
        }
        
        /// <summary>
        /// Nothing to do - everything in the Append units
        /// </summary>
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
