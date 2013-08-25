using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Library.Components {

    /// <summary>
    /// Represents "clever" dictionary that displays error messages instead of throwing exceptions. It manages key duplicity, tracking
    /// items in conflict.
    /// </summary>
    public class KeyValueConflictResolver : Dictionary<string,IKeyValueSource> {
       
        /// <summary>
        /// Creates new instance of KeyValueConflictResolver
        /// </summary>
        /// <param name="ignoreCase">True if string comparison should be case-insensitive</param>
        /// <param name="enableSameKeys">True if entries with same keys and values should be allowed</param>
        public KeyValueConflictResolver(bool ignoreCase, bool enableSameKeys) {
            this.IgnoreCase = ignoreCase;
            this.EnableSameKeys = enableSameKeys;
        }

        /// <summary>
        /// True if string comparison should be case-insensitive
        /// </summary>
        public bool IgnoreCase {
            get;
            private set;
        }

        /// <summary>
        /// True if entries with same keys and values should be allowed
        /// </summary>
        public bool EnableSameKeys {
            get;
            private set;
        }

        /// <summary>
        /// Returns error message for duplicate key event
        /// </summary>
        protected virtual string DuplicateErrorText {
            get {
                return "Duplicate key entry";
            }
        }

        /// <summary>
        /// Adds new key/value pair to the dictionary, setting error message appropriately
        /// </summary>
        /// <param name="oldKey">Previous value of the key (possibly in the dictionary)</param>
        /// <param name="newKey">New value of the key</param>
        /// <param name="item">Value for the key</param>
        public virtual void TryAdd(string oldKey, string newKey, IKeyValueSource item) {
            if (item == null) throw new ArgumentNullException("item");

            if (IgnoreCase) {
                oldKey = oldKey == null ? null : oldKey.ToLower();
                newKey = newKey == null ? null : newKey.ToLower();
            }

            if (string.Compare(oldKey, newKey) == 0) { // new key and old key are the same                
                // update conflict state between given item and all items with the same key
                // conflict will occur either if values are different or same keys are not allowed at all
                foreach (IKeyValueSource c in item.ItemsWithSameKey) {
                    SetConflictedItems(c, item, (string.Compare(c.Value, item.Value) != 0 || !EnableSameKeys));
                }                
            } else { // new key and old key are different
                if (!string.IsNullOrEmpty(oldKey) && ContainsKey(oldKey)) { // old key is non-empty and in the dictionary
                    if (item.ItemsWithSameKey.Count > 0) { // old key was in a conflict
                        foreach (IKeyValueSource c in item.ItemsWithSameKey) {
                            SetConflictedItems(c, item, false); // break the conflict    
                            c.ItemsWithSameKey.Remove(item);
                        }

                        if (this[oldKey] == item) { // given row was present in the dictionary - find another row
                            IKeyValueSource replaceRow = item.ItemsWithSameKey.First();                                                        
                            this[oldKey] = replaceRow;
                        }
                        item.ItemsWithSameKey.Clear();
                    } else {
                        Remove(oldKey);
                    }
                }

                if (!string.IsNullOrEmpty(newKey)) { // new key is non-empty
                    if (ContainsKey(newKey)) { // new key will be in conflict
                        foreach (IKeyValueSource c in this[newKey].ItemsWithSameKey) {
                            SetConflictedItems(c, item, string.Compare(c.Value, item.Value) != 0 || !EnableSameKeys);
                            if (!item.ItemsWithSameKey.Contains(c)) item.ItemsWithSameKey.Add(c);
                            if (!c.ItemsWithSameKey.Contains(item)) c.ItemsWithSameKey.Add(item);
                        }

                        if (this[newKey] != item) {
                            SetConflictedItems(this[newKey], item, (string.Compare(this[newKey].Value, item.Value) != 0 || !EnableSameKeys));
                            if (!item.ItemsWithSameKey.Contains(this[newKey])) item.ItemsWithSameKey.Add(this[newKey]);
                            if (!this[newKey].ItemsWithSameKey.Contains(item)) this[newKey].ItemsWithSameKey.Add(item);
                        }                        
                    } else { // new key is not in conflict
                        Add(newKey, item);
                    }
                }
            }

        }

        /// <summary>
        /// Modifies conflict relation between two items
        /// </summary>        
        protected virtual void SetConflictedItems(IKeyValueSource row1, IKeyValueSource row2, bool isConflict) {
            if (row1 == null) throw new ArgumentNullException("row1");
            if (row2 == null) throw new ArgumentNullException("row2");

            if (isConflict) { // rows are in conflict
                // if they were not already in a conflict, add rows to each others ConflictItems
                if (!row1.ConflictItems.Contains(row2)) row1.ConflictItems.Add(row2);
                if (!row2.ConflictItems.Contains(row1)) row2.ConflictItems.Add(row1);
            } else { // rows are not in a conflict - remove the from ConflictItems
                row1.ConflictItems.Remove(row2);
                row2.ConflictItems.Remove(row1);
            }

            UpdateAfterConflictChangedState(row1);
            UpdateAfterConflictChangedState(row2);            
        }

        /// <summary>
        /// Updates duplicate key error message for given row
        /// </summary>        
        private void UpdateAfterConflictChangedState(IKeyValueSource row) {
            if (row.ConflictItems.Count == 0) {
                row.ErrorMessages.Remove(DuplicateErrorText); // remove duplicate error message
            } else {
                // add error message
                if (!row.ErrorMessages.Contains(DuplicateErrorText)) row.ErrorMessages.Add(DuplicateErrorText);
            }
            row.UpdateErrorSetDisplay(); // update display of error messages
        }
    }
}
