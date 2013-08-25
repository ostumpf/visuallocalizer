using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Windows.Forms;
using VisualLocalizer.Gui;
using VisualLocalizer.Library.Components;

namespace VisualLocalizer.Components {

    /// <summary>
    /// Enhances KeyValueIdentifierConflictResolver functionality with comparing with respect to destination ResX files (used in Batch move toolgrid)
    /// </summary>
    internal class DestinationKeyValueConflictResolver : KeyValueIdentifierConflictResolver {

        public DestinationKeyValueConflictResolver(bool ignoreCase, bool enableSameKeys)
            : base(ignoreCase, enableSameKeys) {            
        }


        /// <summary>
        /// Modifies conflict relation between two items
        /// </summary>   
        protected override void SetConflictedItems(IKeyValueSource row1, IKeyValueSource row2, bool p) {
            BatchMoveToResourcesToolGrid grid = (row1 as DataGridViewRow).DataGridView as BatchMoveToResourcesToolGrid;

            object dest1 = (row1 as DataGridViewRow).Cells[grid.DestinationColumnName].Value;
            object dest2 = (row2 as DataGridViewRow).Cells[grid.DestinationColumnName].Value;
            
            // items are in conflict only if their destination files are the same
            p = p && (dest1 == null || dest2 == null || dest1.ToString() == dest2.ToString());

            base.SetConflictedItems(row1, row2, p);
        }        
    }


}
