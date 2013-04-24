using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Library {

    /// <summary>
    /// Represents item handled by conflict resolver (row in Batch move grid or string grid of ResX editor, list item in
    /// ResX editor lists...)
    /// </summary>
    public interface IKeyValueSource {
        /// <summary>
        /// Resource key
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Resource value
        /// </summary>
        string Value { get; }

        /// <summary>
        /// Items with the same key
        /// </summary>
        List<IKeyValueSource> ItemsWithSameKey { get; set; }
        
        /// <summary>
        /// Items that are in conflict with this item (have the same key and possibly different values)
        /// </summary>
        HashSet<IKeyValueSource> ConflictItems { get; }
                
        /// <summary>
        /// Returns error messages associated with this item
        /// </summary>
        HashSet<string> ErrorMessages { get; }

        /// <summary>
        /// Updates display of errors for this item (called after change in ErrorMessages)
        /// </summary>
        void UpdateErrorSetDisplay();        
    }
}
