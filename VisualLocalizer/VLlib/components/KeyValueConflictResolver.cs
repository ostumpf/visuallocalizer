using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Library {
    public class KeyValueConflictResolver : Dictionary<string,IKeyValueSource> {

        public KeyValueConflictResolver() : this(false, true) {
        }

        public KeyValueConflictResolver(bool ignoreCase, bool enableSameValues) {
            this.IgnoreCase = ignoreCase;
            this.EnableSameValues = enableSameValues;
        }

        public bool IgnoreCase {
            get;
            private set;
        }

        public bool EnableSameValues {
            get;
            private set;
        }

        public virtual void TryAdd(string oldKey, string newKey, IKeyValueSource item) {
            if (IgnoreCase) {
                oldKey = oldKey == null ? null : oldKey.ToLower();
                newKey = newKey == null ? null : newKey.ToLower();
            }

            if (string.Compare(oldKey,newKey)==0) {
                if (!string.IsNullOrEmpty(newKey)) {
                    foreach (IKeyValueSource c in this[newKey].ItemsWithSameKey) {
                        if (c != item) {
                            SetConflictedItems(c, item, string.Compare(c.Value, item.Value) != 0 || !EnableSameValues);
                        }
                    }
                    if (this[newKey] != item) {
                        SetConflictedItems(this[newKey], item, string.Compare(this[newKey].Value, item.Value) != 0 || !EnableSameValues);                        
                    }
                }
            } else {
                if (!string.IsNullOrEmpty(oldKey) && ContainsKey(oldKey)) {
                    if (this[oldKey].ItemsWithSameKey.Count > 0) {
                        foreach (IKeyValueSource c in this[oldKey].ItemsWithSameKey) {
                            SetConflictedItems(c, item, false);
                        }

                        if (this[oldKey] == item) {
                            IKeyValueSource replaceRow = this[oldKey].ItemsWithSameKey[this[oldKey].ItemsWithSameKey.Count - 1];
                            this[oldKey].ItemsWithSameKey.RemoveAt(this[oldKey].ItemsWithSameKey.Count - 1);
                            replaceRow.ItemsWithSameKey = this[oldKey].ItemsWithSameKey;
                            this[oldKey] = replaceRow;
                        } else {
                            this[oldKey].ItemsWithSameKey.Remove(item);
                            SetConflictedItems(this[oldKey], item, false);
                        }
                    } else {
                        Remove(oldKey);
                    }
                }

                if (!string.IsNullOrEmpty(newKey)) {
                    if (ContainsKey(newKey)) {
                        if (this[newKey] != item && (string.Compare(this[newKey].Value, item.Value) != 0 || !EnableSameValues)) {
                            SetConflictedItems(this[newKey], item, true);
                        }
                        foreach (IKeyValueSource c in this[newKey].ItemsWithSameKey) {
                            SetConflictedItems(c, item, string.Compare(c.Value, item.Value) != 0 || !EnableSameValues);
                        }
                        if (!this[newKey].ItemsWithSameKey.Contains(item)) this[newKey].ItemsWithSameKey.Add(item);
                    } else {
                        Add(newKey, item);
                    }
                }
            }
        }

        protected virtual void SetConflictedItems(IKeyValueSource row1, IKeyValueSource row2, bool p) {
            if (p) {
                if (!row1.ConflictRows.Contains(row2)) row1.ConflictRows.Add(row2);
                if (!row2.ConflictRows.Contains(row1)) row2.ConflictRows.Add(row1);
            } else {
                row1.ConflictRows.Remove(row2);
                row2.ConflictRows.Remove(row1);
            }

            string errorText = "Duplicate key entry";

            if (row1.ConflictRows.Count == 0) {
                row1.ErrorSet.Remove(errorText);
            } else {
                if (!row1.ErrorSet.Contains(errorText)) row1.ErrorSet.Add(errorText);
            }
            row1.ErrorSetUpdate();

            if (row2.ConflictRows.Count == 0) {
                row2.ErrorSet.Remove(errorText);
            } else {
                if (!row2.ErrorSet.Contains(errorText)) row2.ErrorSet.Add(errorText);
            }
            row2.ErrorSetUpdate();
        }

    }
}
