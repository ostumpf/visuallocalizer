using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VisualLocalizer.Components;

/// Contains undo units used in ResX editor.
namespace VisualLocalizer.Editor.UndoUnits {

    internal enum SELECTION_CHANGE_INITIATOR { UNDO_MANAGER, INITIALIZER }

    /// <summary>
    /// Undo unit for "Access modifier" change
    /// </summary>
    [Guid("34402E28-02BB-4f34-A907-EC0C0AF594F3")]
    internal sealed class AccessModifierChangeUndoUnit : AbstractUndoUnit {

        private string OldValue { get; set; }
        private string NewValue { get; set; }
        private ToolStripComboBox AccessModifierBox { get; set; }

        public AccessModifierChangeUndoUnit(ToolStripComboBox accessModifierBox, string oldValue, string newValue) {
            this.OldValue = oldValue;
            this.NewValue = newValue;
            this.AccessModifierBox = accessModifierBox;
        }
        
        public override void Undo() {
            AccessModifierBox.Tag = SELECTION_CHANGE_INITIATOR.UNDO_MANAGER;
            AccessModifierBox.SelectedItem = OldValue;            
        }

        public override void Redo() {
            AccessModifierBox.Tag = SELECTION_CHANGE_INITIATOR.UNDO_MANAGER;
            AccessModifierBox.SelectedItem = NewValue;            
        }

        public override string GetUndoDescription() {
            return string.Format("Access modifier changed from {0} to {1}", OldValue, NewValue);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
