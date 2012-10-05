using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {
    internal sealed class MoveToResourcesReferenceUndoUnit : AbstractUndoUnit {

        private string Key { get; set; }

        public MoveToResourcesReferenceUndoUnit(string key) {
            this.Key = key;
        }
        
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
