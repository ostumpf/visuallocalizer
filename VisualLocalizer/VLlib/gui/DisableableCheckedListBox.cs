using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace VisualLocalizer.Library {

    /// <summary>
    /// Enhances standard CheckedListBox with option to disable/enable list items
    /// </summary>
    public sealed class DisableableCheckedListBox : CheckedListBox {

        private HashSet<object> disabledItems;

        public DisableableCheckedListBox() {
            disabledItems = new HashSet<object>();
        }

        protected override void OnDrawItem(DrawItemEventArgs e) {
            DrawItemEventArgs ne = e;
            if (disabledItems.Contains(Items[e.Index])) {
                ne = new DrawItemEventArgs(e.Graphics, e.Font, e.Bounds, e.Index, e.State, System.Drawing.SystemColors.InactiveCaptionText, e.BackColor);
            }
            base.OnDrawItem(ne);
        }

        protected override void OnItemCheck(ItemCheckEventArgs ice) {
            if (disabledItems.Contains(Items[ice.Index])) {
                ice.NewValue = ice.CurrentValue;
            }
            base.OnItemCheck(ice);
        }

        /// <summary>
        /// Sets state of item with given index
        /// </summary>        
        public void SetItemEnabled(int index, bool enabled) {
            if (index < 0 || index >= Items.Count) throw new ArgumentOutOfRangeException("index");
            
            if (enabled) {
                disabledItems.Remove(Items[index]);
            } else {
                disabledItems.Add(Items[index]);
            }
        }
    }
}
