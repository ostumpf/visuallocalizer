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

        public void TryAdd(string oldKey, string newKey, IKeyValueSource item, ResXProjectItem resxItem, LANGUAGE? language) {            
            if (resxItem != null) {
                if (language == null && resxItem != null) {
                    language = resxItem.DesignerLanguage;
                }

                bool empty = string.IsNullOrEmpty(newKey);
                bool validIdentifier = newKey.IsValidIdentifier(language.Value);
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

                string identError = "Key is not a valid identifier";
                if (identifierError) {
                    item.ErrorMessages.Add(identError);
                } else {
                    item.ErrorMessages.Remove(identError);
                }                
            }
            TryAdd(oldKey, newKey, item);
        }
    }
}
