using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Resources;

namespace VisualLocalizer.Editor {
    internal sealed class ResXStringGridRow : DataGridViewKeyValueRow<ResXDataNode> {
        public enum STATUS { OK, KEY_NULL }
        public int IndexAtDeleteTime { get; set; }

        public ResXStringGridRow() {
            Status = STATUS.OK;
        }

        public STATUS Status { get; set; }
    }
}
