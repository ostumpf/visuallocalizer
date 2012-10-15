using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Windows.Forms;

namespace VisualLocalizer.Components {
    internal sealed class DestinationKeyValueConflictResolver : KeyValueConflictResolver {

        public DestinationKeyValueConflictResolver() : base(true, true) { }

        protected override void SetConflictedItems(IKeyValueSource row1, IKeyValueSource row2, bool p) {
            object dest1 = (row1 as DataGridViewRow).Cells["DestinationItem"].Value;
            object dest2 = (row2 as DataGridViewRow).Cells["DestinationItem"].Value;
            p = p && (dest1 == null || dest2 == null || dest1.ToString() == dest2.ToString());

            base.SetConflictedItems(row1, row2, p);
        }
    }


}
