using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Resources;
using VisualLocalizer.Components;

namespace VisualLocalizer.Editor {

    /// <summary>
    /// Represents one row in the ResX editor string grid
    /// </summary>
    internal sealed class ResXStringGridRow : DataGridViewKeyValueRow<ResXDataNode>, IReferencableKeyValueSource {
        /// <summary>
        /// States in which the row can be, based on the key
        /// </summary>
        public enum STATUS { OK, KEY_NULL }

        /// <summary>
        /// Index of this row at the moment it was deleted (for undo items)
        /// </summary>
        public int IndexAtDeleteTime { get; set; }        

        /// <summary>
        /// Last valid key (non-null)
        /// </summary>
        public string LastValidKey { get; set; }

        /// <summary>
        /// Determines whether current key is null
        /// </summary>
        public STATUS Status { get; set; }

        public ResXStringGridRow() {
            Status = STATUS.OK;
            CodeReferences = new List<CodeReferenceResultItem>();
        }

        /// <summary>
        /// Updates display of references count, based on CodeReferences
        /// </summary>
        /// <param name="determinated">True if the number of references was successfuly determined</param>
        public void UpdateReferenceCount(bool determinated) {
            if (DataGridView == null) return;

            ResXStringGrid stringGrid = (ResXStringGrid)DataGridView;
            if (ErrorMessages.Count == 0 && determinated) {
                Cells[stringGrid.ReferencesColumnName].Value = CodeReferences.Count;
            } else {
                Cells[stringGrid.ReferencesColumnName].Value = "?";
            }
        }

        /// <summary>
        /// List of references to this resource in the code (to display references number and enable renaming of the keys)
        /// </summary>
        public List<CodeReferenceResultItem> CodeReferences {
            get;
            set;
        }
    }
}
