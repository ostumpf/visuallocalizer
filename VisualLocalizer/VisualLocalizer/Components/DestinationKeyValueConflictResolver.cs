using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Windows.Forms;
using VisualLocalizer.Gui;

namespace VisualLocalizer.Components {
    internal class DestinationKeyValueConflictResolver : KeyValueIdentifierConflictResolver {

        public DestinationKeyValueConflictResolver() : base(true, true) { }

        public DestinationKeyValueConflictResolver(bool ignoreCase, bool enableSameValues)
            : base(ignoreCase, enableSameValues) {            
        }

        protected override void SetConflictedItems(IKeyValueSource row1, IKeyValueSource row2, bool p) {
            BatchMoveToResourcesToolGrid grid = (row1 as DataGridViewRow).DataGridView as BatchMoveToResourcesToolGrid;

            object dest1 = (row1 as DataGridViewRow).Cells[grid.DestinationColumnName].Value;
            object dest2 = (row2 as DataGridViewRow).Cells[grid.DestinationColumnName].Value;
            p = p && (dest1 == null || dest2 == null || dest1.ToString() == dest2.ToString());

            base.SetConflictedItems(row1, row2, p);
        }        
    }


}
