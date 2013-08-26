using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using VisualLocalizer.Library;
using VisualLocalizer.Library.Components;

namespace VisualLocalizer.Components.UndoUnits {

    /// <summary>
    /// Represents undo unit for "move to resources" operation, in which existing resource key is overwriten with a new value
    /// </summary>
    [Guid("CA1176D3-BAAB-40c4-9390-E909EAF53715")]
    internal sealed class MoveToResourcesOverwriteUndoUnit : AbstractUndoUnit {
        
        /// <summary>
        /// Overwritten key
        /// </summary>
        private string Key { get; set; }

        /// <summary>
        /// Value of the key before this operation
        /// </summary>
        private string OldValue { get; set; }

        /// <summary>
        /// Value of the key after this operation
        /// </summary>
        private string NewValue { get; set; }

        /// <summary>
        /// ResX file in which the key belongs
        /// </summary>
        private ResXProjectItem Item { get; set; }

        public MoveToResourcesOverwriteUndoUnit(string key, string oldValue,string newValue, ResXProjectItem resxItem) {
            if (key == null) throw new ArgumentNullException("key");
            if (resxItem == null) throw new ArgumentNullException("resxItem");

            this.Key = key;
            this.Item = resxItem;
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }

        /// <summary>
        /// Return old value back to the ResX file
        /// </summary>
        public override void Undo() {
            Item.AddString(Key, OldValue);
        }

        public override void Redo() {
            Item.AddString(Key, NewValue);
        }

        public override string GetUndoDescription() {
            return String.Format("Move \"{0}\" to resources, overwriting \"{1}\"", NewValue, OldValue);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
