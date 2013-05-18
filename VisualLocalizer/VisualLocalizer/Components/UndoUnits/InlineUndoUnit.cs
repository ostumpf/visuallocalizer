using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using VisualLocalizer.Library.Components;

namespace VisualLocalizer.Components.UndoUnits {

    /// <summary>
    /// Represents undo unit for Inline operation
    /// </summary>
    [Guid("DF803F40-545A-4e91-A692-1AEF63117BA7")]
    internal sealed class InlineUndoUnit : AbstractUndoUnit {

        /// <summary>
        /// Inlined resource key
        /// </summary>
        private string Key { get; set; }

        /// <summary>
        /// True if the operation was triggered from outside the document and therefore cannot be undone
        /// </summary>
        private bool ExternalChange { get; set; }

        public InlineUndoUnit(string key, bool externalChange) {
            if (key == null) throw new ArgumentNullException("key");

            this.Key = key;
            this.ExternalChange = externalChange;
        }
        
        /// <summary>
        /// It is not neccessary to do anything, the actual replace (the only thing to undo) is added in the Append units
        /// </summary>
        public override void Undo() {
            if (ExternalChange) throw new InvalidOperationException("Cannot undo external change.");
        }

        public override void Redo() {
            
        }

        /// <summary>
        /// Returns text that appears in the undo list
        /// </summary>        
        public override string GetUndoDescription() {
            return String.Format("{1}Inline {0}", Key, ExternalChange ? "*" : "");
        }

        /// <summary>
        /// Returns text that appears in the redo list
        /// </summary>        
        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
