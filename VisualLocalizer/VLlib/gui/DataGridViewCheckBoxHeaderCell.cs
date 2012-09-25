using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Forms.VisualStyles;

namespace VisualLocalizer.Library {
    public class DataGridViewCheckBoxHeaderCell : DataGridViewColumnHeaderCell {

        public event EventHandler CheckBoxClicked;

        private CheckBoxState CheckBoxState { get; set; }

        public Point CheckBoxPosition {
            get;
            private set;
        }

        public Size CheckBoxSize {
            get;
            private set;
        }

        private bool? _Checked;
        public bool? Checked {
            get {
                return _Checked;
            }
            set {
                _Checked = value;
                ChangeValue();
            }
        }

        public bool ThreeStates { get; set; }

        protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
            DataGridViewElementStates dataGridViewElementState, object value, object formattedValue, string errorText,
            DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts) {
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, dataGridViewElementState, value, null, errorText, cellStyle, advancedBorderStyle, paintParts);

            CheckBoxSize = CheckBoxRenderer.GetGlyphSize(graphics, CheckBoxState);
            CheckBoxPosition = new Point(cellBounds.X + (cellBounds.Width - CheckBoxSize.Width) / 2, cellBounds.Y + (cellBounds.Height - CheckBoxSize.Height) / 2);
            CheckBoxRenderer.DrawCheckBox(graphics, CheckBoxPosition, CheckBoxState);
        }

        protected override void OnMouseClick(DataGridViewCellMouseEventArgs e) {
            base.OnMouseClick(e);

            if (Checked == true) {
                Checked = false;
            } else {
                Checked = true;
            }
            NotifyCheckBoxClicked();
        }

        protected void NotifyCheckBoxClicked() {
            if (CheckBoxClicked != null) {
                CheckBoxClicked(this, new EventArgs());
            }
        }

        protected virtual void ChangeValue() {
            if (Checked == true) {
                CheckBoxState = CheckBoxState.CheckedNormal;
            } else if (Checked == null) {
                CheckBoxState = CheckBoxState.MixedNormal;
            } else {
                CheckBoxState = CheckBoxState.UncheckedNormal;
            }
            this.RaiseCellValueChanged(new DataGridViewCellEventArgs(this.ColumnIndex, this.RowIndex));
        }
    }
}
