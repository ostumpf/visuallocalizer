using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Forms.VisualStyles;

namespace VisualLocalizer.Library.Gui {

    /// <summary>
    /// Header cell with checkbox
    /// </summary>
    public class DataGridViewCheckBoxHeaderCell : DataGridViewColumnHeaderCell {

        /// <summary>
        /// Checkbox clicked
        /// </summary>
        public event EventHandler CheckBoxClicked;

        /// <summary>
        /// Sort requested (click outside the checkbox)
        /// </summary>
        public event Action<SortOrder> Sort;

        /// <summary>
        /// Check state of the checkbox
        /// </summary>
        private CheckBoxState CheckBoxState { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataGridViewCheckBoxHeaderCell"/> class.
        /// </summary>
        public DataGridViewCheckBoxHeaderCell() {
            ToolTipText = null;            
        }

        /// <summary>
        /// Location of checkbox
        /// </summary>
        public Point CheckBoxPosition {
            get;
            private set;
        }

        /// <summary>
        /// Size of checkbox
        /// </summary>
        public Size CheckBoxSize {
            get;
            private set;
        }
        
        private bool? _Checked;

        /// <summary>
        /// State of checkbox
        /// </summary>
        public bool? Checked {
            get {
                return _Checked;
            }
            set {
                _Checked = value;
                ChangeValue();
            }
        }

        /// <summary>
        /// Whether three-state checkbox is enabled (true, false, indeterminate)
        /// </summary>
        public bool ThreeStates { get; set; }

        /// <summary>
        /// Paints the specified graphics.
        /// </summary>
        /// <param name="graphics">The graphics.</param>
        /// <param name="clipBounds">The clip bounds.</param>
        /// <param name="cellBounds">The cell bounds.</param>
        /// <param name="rowIndex">Index of the row.</param>
        /// <param name="dataGridViewElementState">State of the data grid view element.</param>
        /// <param name="value">The value.</param>
        /// <param name="formattedValue">The formatted value.</param>
        /// <param name="errorText">The error text.</param>
        /// <param name="cellStyle">The cell style.</param>
        /// <param name="advancedBorderStyle">The advanced border style.</param>
        /// <param name="paintParts">The paint parts.</param>
        protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
            DataGridViewElementStates dataGridViewElementState, object value, object formattedValue, string errorText,
            DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts) {            
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, dataGridViewElementState, value, null, errorText, cellStyle, advancedBorderStyle, paintParts);            
            
            CheckBoxSize = CheckBoxRenderer.GetGlyphSize(graphics, CheckBoxState);
            Point position = new Point(cellBounds.X + CheckBoxSize.Width / 2, cellBounds.Y + (cellBounds.Height - CheckBoxSize.Height) / 2);
            CheckBoxPosition = new Point(position.X - clipBounds.X, position.Y - clipBounds.Y);
            CheckBoxRenderer.DrawCheckBox(graphics, position, CheckBoxState);
        }        

        /// <summary>
        /// Determine whether checkbox or outside area was clicked
        /// </summary>
        protected override void OnMouseUp(DataGridViewCellMouseEventArgs e) {
            base.OnMouseUp(e);

            if (e.X >= CheckBoxPosition.X && e.X <= CheckBoxPosition.X + CheckBoxSize.Width
                && e.Y >= CheckBoxPosition.Y && e.Y <= CheckBoxPosition.Y + CheckBoxSize.Height) {
                if (Checked == true) {
                    Checked = false;
                } else {
                    Checked = true;
                }
                NotifyCheckBoxClicked();                
            } else if (ContentBounds.Contains(e.Location)) {
                switch (SortGlyphDirection) {
                    case SortOrder.Ascending:
                        SortGlyphDirection = SortOrder.Descending;
                        break;
                    case SortOrder.Descending:
                        SortGlyphDirection = SortOrder.Ascending;
                        break;
                    case SortOrder.None:
                        SortGlyphDirection = SortOrder.Descending;
                        break;                 
                }
                NotifySortClicked(SortGlyphDirection);
            }
        }        

        /// <summary>
        /// Fire CheckBoxClicked event
        /// </summary>
        protected void NotifyCheckBoxClicked() {
            if (CheckBoxClicked != null) {
                CheckBoxClicked(this, new EventArgs());
            }
        }

        /// <summary>
        /// Fire NotifySortClicked event
        /// </summary>
        /// <param name="order"></param>
        protected void NotifySortClicked(SortOrder order) {
            if (Sort != null) {
                Sort(order);
            }
        }

        /// <summary>
        /// Change checkbox state        
        /// </summary>
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
