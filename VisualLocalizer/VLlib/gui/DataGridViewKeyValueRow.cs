using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace VisualLocalizer.Library {

    /// <summary>
    /// Represents a row in a AbstractCheckedGridView
    /// </summary>
    /// <typeparam name="ItemType">Type of source item</typeparam>
    public class DataGridViewCheckedRow<ItemType> : DataGridViewRow where ItemType : class {        
        
        /// <summary>
        /// Model representation of data displayed by row
        /// </summary>
        public ItemType DataSourceItem { get; set; }        
    }

    /// <summary>
    /// Represents a row in AbstractKeyValueGridView (provides key/value pair)
    /// </summary>
    /// <typeparam name="ItemType">Type of source item</typeparam>
    public class DataGridViewKeyValueRow<ItemType> : DataGridViewCheckedRow<ItemType>, IKeyValueSource where ItemType : class {
        public DataGridViewKeyValueRow() {
            _ItemsWithSameKey = new List<IKeyValueSource>();
            _ConflictRows = new HashSet<IKeyValueSource>();
            _ErrorSet = new HashSet<string>();
        }        

        /// <summary>
        /// Reloads error message
        /// </summary>
        public void UpdateErrorSetDisplay() {
            if (ErrorMessages.Count == 0) {
                ErrorText = null;
            } else {
                ErrorText = ErrorMessages.First();
            }
        }
        
        public string Key {
            get {
                var grid = this.DataGridView as AbstractKeyValueGridView<ItemType>;
                if (grid == null) {
                    return null;
                } else {
                    return (string)Cells[grid.KeyColumnName].Value;
                }
            }
        }

        public string Value {
            get {
                var grid = this.DataGridView as AbstractKeyValueGridView<ItemType>;
                if (grid == null) {
                    return null;
                } else {
                    return (string)Cells[grid.ValueColumnName].Value;
                }
            }
        }
    
        private List<IKeyValueSource> _ItemsWithSameKey;
        public List<IKeyValueSource> ItemsWithSameKey {
            get {
                return _ItemsWithSameKey;
            }
            set {
                _ItemsWithSameKey = value;
            }
        }

        private HashSet<IKeyValueSource> _ConflictRows;
        public HashSet<IKeyValueSource> ConflictItems {
            get {
                return _ConflictRows;
            }
        }

        private HashSet<string> _ErrorSet;
        public HashSet<string> ErrorMessages {
            get {
                return _ErrorSet;
            }
        }
    }
    
}
