using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace VisualLocalizer.Library {

    public class DataGridViewCheckedRow<ItemType> : DataGridViewRow where ItemType : class {
        public ItemType DataSourceItem { get; set; }       
    }

    public class DataGridViewKeyValueRow<ItemType> : DataGridViewCheckedRow<ItemType>, IKeyValueSource where ItemType : class {
        public DataGridViewKeyValueRow() {
            _ItemsWithSameKey = new List<IKeyValueSource>();
            _ConflictRows = new HashSet<IKeyValueSource>();
            _ErrorSet = new HashSet<string>();
        }        

        public void ErrorSetUpdate() {
            if (ErrorSet.Count == 0) {
                ErrorText = null;
            } else {
                ErrorText = ErrorSet.First();
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
        public HashSet<IKeyValueSource> ConflictRows {
            get {
                return _ConflictRows;
            }
        }

        private HashSet<string> _ErrorSet;
        public HashSet<string> ErrorSet {
            get {
                return _ErrorSet;
            }
        }
    }
    
}
