using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using VisualLocalizer.Library;
using System.Windows.Forms;
using System.Resources;
using VisualLocalizer.Library.Components;

namespace VisualLocalizer.Library.Gui {

    /// <summary>
    /// Enhances AbstractCheckedGridView functionality with validation
    /// </summary>    
    public abstract class AbstractKeyValueGridView<ItemType>:AbstractCheckedGridView<ItemType> where ItemType:class {
        
        /// <summary>
        /// Key conflict resolver - contains all used keys and intelligently assignes duplicate keys errors
        /// </summary>
        protected KeyValueConflictResolver ConflictResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractKeyValueGridView{ItemType}"/> class.
        /// </summary>
        /// <param name="showContextColumn">True if the context column should be visible</param>
        /// <param name="resolver">Key conflict resolver to use in this instance</param>
        public AbstractKeyValueGridView(bool showContextColumn, KeyValueConflictResolver resolver) : base(showContextColumn) {            
            this.ConflictResolver = resolver;            
        }

        /// <summary>
        /// Removes unchecked rows from grid with respect to the the conflict resolver (unregister)
        /// </summary>        
        public override void RemoveUncheckedRows(bool remember) {
            if (string.IsNullOrEmpty(CheckBoxColumnName) || !Columns.Contains(CheckBoxColumnName)) return;

            foreach (DataGridViewKeyValueRow<ItemType> row in Rows) {
                bool check = (bool)row.Cells[CheckBoxColumnName].Value;
                if (!check) {
                    ConflictResolver.TryAdd(row.Key, null, row);
                    row.Cells[KeyColumnName].Tag = null;
                }
            }

            base.RemoveUncheckedRows(remember);
        }

        /// <summary>
        /// Adds removed rows back to the grid (register)
        /// </summary>
        public override List<DataGridViewRow> RestoreRemovedRows() {
            if (string.IsNullOrEmpty(CheckBoxColumnName) || !Columns.Contains(CheckBoxColumnName)) return null;
           
            var returnList = base.RestoreRemovedRows();

            foreach (DataGridViewKeyValueRow<ItemType> row in Rows) {
                if (row.Cells[KeyColumnName].Tag == null) Validate(row);
            }

            return returnList;
        }

        /// <summary>
        /// Performs validation after edit
        /// </summary>        
        protected override void OnCellEndEdit(DataGridViewCellEventArgs e) {
            base.OnCellEndEdit(e);

            if (Columns[e.ColumnIndex].Name != KeyColumnName) {
                Rows[e.RowIndex].Cells[KeyColumnName].Tag = Rows[e.RowIndex].Cells[KeyColumnName].Value;
            }
            if (Columns[e.ColumnIndex].Name != CheckBoxColumnName) {
                Validate(e.RowIndex);
            }       
        }

        /// <summary>
        /// Use conflict resolver to validate key/value of given row
        /// </summary>        
        protected virtual void Validate(DataGridViewKeyValueRow<ItemType> row) {
            string key = row.Key;
            string value = row.Value;                                 

            string originalValue = (string)row.Cells[KeyColumnName].Tag;
            ConflictResolver.TryAdd(originalValue, key, row);
            if (originalValue == null) row.Cells[KeyColumnName].Tag = key;

            row.UpdateErrorSetDisplay();
        }

        /// <summary>
        /// Use conflict resolver to validate key/value of the row with given index
        /// </summary>        
        protected void Validate(int rowIndex) {
            Validate(Rows[rowIndex] as DataGridViewKeyValueRow<ItemType>);
        }

        /// <summary>
        /// Initializes the grid with provided list of items
        /// </summary>
        public override void SetData(List<ItemType> list) {
            ConflictResolver.Clear();
            errorRows.Clear();
        }

        /// <summary>
        /// Returns name of the column used to hold key 
        /// </summary>
        public abstract string KeyColumnName { get; }

        /// <summary>
        /// Returns name of the column used to hold value
        /// </summary>
        public abstract string ValueColumnName { get; }
    }
}
