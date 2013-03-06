using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using VisualLocalizer.Settings;

namespace VisualLocalizer.Components {
    internal class KeyValueIdentifierConflictResolver : KeyValueConflictResolver {

        public KeyValueIdentifierConflictResolver() : this(false, true) {
        }

        public KeyValueIdentifierConflictResolver(bool ignoreCase, bool enableSameValues) : base(ignoreCase, enableSameValues) {            
        }

        public void TryAdd(string oldKey, string newKey, IKeyValueSource item, ResXProjectItem resxItem) {            
            if (resxItem != null) {            
                bool empty = string.IsNullOrEmpty(newKey);
                bool validIdentifier = newKey.IsValidIdentifier();
                bool hasOwnDesigner = resxItem.DesignerItem != null && !resxItem.IsCultureSpecific();
                bool identifierError = false;

                switch (SettingsObject.Instance.BadKeyNamePolicy) {
                    case BAD_KEY_NAME_POLICY.IGNORE_COMPLETELY:
                        identifierError = empty;
                        break;
                    case BAD_KEY_NAME_POLICY.IGNORE_ON_NO_DESIGNER:
                        identifierError = empty || (!validIdentifier && hasOwnDesigner);
                        break;
                    case BAD_KEY_NAME_POLICY.WARN_ALWAYS:
                        identifierError = empty || !validIdentifier;
                        break;
                }

                string identError = "Key is not valid C# identifier";
                if (identifierError) {
                    item.ErrorSet.Add(identError);
                } else {
                    item.ErrorSet.Remove(identError);
                }                
            }
            TryAdd(oldKey, newKey, item);
        }
    }
}
