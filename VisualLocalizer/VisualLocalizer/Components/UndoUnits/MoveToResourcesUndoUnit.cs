using System;
using System.Runtime.InteropServices;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {

    /// <summary>
    /// Represents undo unit for "move to resources" operation, in which new resource key was created
    /// </summary>
    [Guid("B9C8503E-80AA-4260-9954-DCAAF3EA4824")]
    internal sealed class MoveToResourcesUndoUnit : AbstractUndoUnit {

        /// <summary>
        /// New resource key
        /// </summary>
        private string Key {get;set;}

        /// <summary>
        /// New resource value
        /// </summary>
        private string Value { get; set; }

        /// <summary>
        /// ResX file in which the key was added
        /// </summary>
        private ResXProjectItem Item { get; set; }       
        
        public MoveToResourcesUndoUnit(string key, string value, ResXProjectItem resxItem) {
            if (key == null) throw new ArgumentNullException("key");
            if (resxItem == null) throw new ArgumentNullException("resxItem");

            this.Key = key;
            this.Item = resxItem;
            this.Value = value;
        }

        /// <summary>
        /// Removes the key from resource file
        /// </summary>
        public override void Undo() {
            Item.RemoveKey(Key);
        }

        /// <summary>
        /// Adds the key back to resource file
        /// </summary>
        public override void Redo() {
            Item.AddString(Key, Value);
        }

        public override string GetUndoDescription() {
            return String.Format("Move \"{0}\" to resources", Value);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
