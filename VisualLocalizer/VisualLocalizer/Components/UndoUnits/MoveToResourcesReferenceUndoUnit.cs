using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using VisualLocalizer.Library.Components;

namespace VisualLocalizer.Components.UndoUnits {

    /// <summary>
    /// Represents undo unit for "move to resources" operation, in which no resource key is created, only existing one is referenced
    /// </summary>
    [Guid("B9B1D95C-A40A-4a7a-93B2-5480FAC347F2")]
    internal sealed class MoveToResourcesReferenceUndoUnit : AbstractUndoUnit {

        /// <summary>
        /// Referenced key
        /// </summary>
        private string Key { get; set; }

        public MoveToResourcesReferenceUndoUnit(string key) {
            if (key == null) throw new ArgumentNullException("key");

            this.Key = key;
        }
        
        /// <summary>
        /// Nothing to do - everything in Append units
        /// </summary>
        public override void Undo() {            
        }

        public override void Redo() {
        }

        public override string GetUndoDescription() {
            return string.Format("Reference key \"{0}\"", Key);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
