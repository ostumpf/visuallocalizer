using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using VisualLocalizer.Components;

namespace VisualLocalizer.Editor {

    /// <summary>
    /// States in which the row can be, based on the key
    /// </summary>
    public enum KEY_STATUS { OK, ERROR }

    /// <summary>
    /// Base interface for resource items that have number of references in editor displayed
    /// </summary>
    internal interface IReferencableKeyValueSource : IKeyValueSource {
        /// <summary>
        /// List of references to the resource in code (used to display number and to enable key renaming)
        /// </summary>
        List<CodeReferenceResultItem> CodeReferences { get; set; }

        /// <summary>
        /// Updates display of the references count
        /// </summary>
        /// <param name="determinated">True if number of references was successfuly calculated</param>
        void UpdateReferenceCount(bool determinated);

        /// <summary>
        /// Returns true if any of the code references comes from readonly (or locked) file
        /// </summary>
        bool CodeReferenceContainsReadonly {
            get;
        }

        /// <summary>
        /// State in which the item's key is
        /// </summary>
        KEY_STATUS Status {
            get;
            set;
        }

        /// <summary>
        /// Last known key in with OK state
        /// </summary>
        string LastValidKey {
            get;
            set;
        }
    }
}
