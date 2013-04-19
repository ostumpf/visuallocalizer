﻿using System;
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
        /// Index of this row at the moment it was deleted (for undo items)
        /// </summary>
        public int IndexAtDeleteTime { get; set; }        
       

        /// <summary>
        /// Determines whether current key is null
        /// </summary>
        public KEY_STATUS Status { get; set; }

        /// <summary>
        /// Last known key in with OK state
        /// </summary>
        public string LastValidKey { get; set; }

        public ResXStringGridRow() {
            Status = KEY_STATUS.OK;
            CodeReferences = new List<CodeReferenceResultItem>();
        }

        /// <summary>
        /// Updates display of references count, based on CodeReferences
        /// </summary>
        /// <param name="determinated">True if the number of references was successfuly determined</param>
        public void UpdateReferenceCount(bool determinated) {
            if (DataGridView == null) return;

            ResXStringGrid stringGrid = (ResXStringGrid)DataGridView;
            if (determinated) {
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

        /// <summary>
        /// Returns true if any of the code references comes from readonly (or locked) file
        /// </summary>
        public bool CodeReferenceContainsReadonly {
            get {
                bool readonlyExists = false;
                if (CodeReferences != null) {
                    foreach (CodeReferenceResultItem item in CodeReferences) {
                        if (RDTManager.IsFileReadonly(item.SourceItem.GetFullPath()) || VLDocumentViewsManager.IsFileLocked(item.SourceItem.GetFullPath())) {
                            readonlyExists = true;
                            break;
                        }
                    }
                }
                return readonlyExists;
            }
        }
    }
}
