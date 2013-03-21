using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using VisualLocalizer.Settings;

namespace VisualLocalizer.Components {

    /// <summary>
    /// Enhances KeyValueConflictResolver functionality by validating keys as identifiers of the specified language
    /// </summary>
    internal class KeyValueIdentifierConflictResolver : KeyValueConflictResolver {

        public KeyValueIdentifierConflictResolver(bool ignoreCase, bool enableSameKeys)
            : base(ignoreCase, enableSameKeys) {            
        }

        /// <summary>
        /// Returns error message displayed when key is considered invalid
        /// </summary>
        protected string KeyIsNotValidIdentifierErrorMessage {
            get {
                return "Key is not a valid identifier";
            }
        }

        /// <summary>
        /// Validates if new key is a valid identifier of specified language, with respect to specified policy
        /// </summary>
        public void TryAdd(string oldKey, string newKey, IKeyValueSource item, ResXProjectItem resxItem, LANGUAGE? language) {
            if (item == null) throw new ArgumentNullException("item");

            if (resxItem != null) {
                if (language == null && resxItem != null) {
                    language = resxItem.DesignerLanguage;
                }
                if (!language.HasValue) throw new InvalidOperationException("Cannot determine file language.");

                bool empty = string.IsNullOrEmpty(newKey); // new key is empty
                bool validIdentifier = newKey.IsValidIdentifier(language.Value); // new key is valid identifier of the language
                bool hasOwnDesigner = resxItem.DesignerItem != null && !resxItem.IsCultureSpecific(); // ResX file has own designer file
                bool identifierError = false;

                switch (SettingsObject.Instance.BadKeyNamePolicy) {
                    case BAD_KEY_NAME_POLICY.IGNORE_COMPLETELY:
                        identifierError = empty; // only empty keys are reported as an error
                        break;
                    case BAD_KEY_NAME_POLICY.IGNORE_ON_NO_DESIGNER:
                        identifierError = empty || (!validIdentifier && hasOwnDesigner); // empty keys and invalid identifiers in own designers are reported
                        break;
                    case BAD_KEY_NAME_POLICY.WARN_ALWAYS:
                        identifierError = empty || !validIdentifier; // all kinds of invalid identifiers are reported
                        break;
                }
                
                if (identifierError) {
                    item.ErrorMessages.Add(KeyIsNotValidIdentifierErrorMessage);
                } else {
                    item.ErrorMessages.Remove(KeyIsNotValidIdentifierErrorMessage);
                }                
            }
            TryAdd(oldKey, newKey, item);
        }
    }
}
