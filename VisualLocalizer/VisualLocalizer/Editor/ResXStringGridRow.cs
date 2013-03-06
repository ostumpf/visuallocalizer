using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Resources;
using VisualLocalizer.Components;

namespace VisualLocalizer.Editor {
    internal sealed class ResXStringGridRow : DataGridViewKeyValueRow<ResXDataNode>, IReferencableKeyValueSource {
        public enum STATUS { OK, KEY_NULL }
        public int IndexAtDeleteTime { get; set; }        
        public string LastValidKey { get; set; }


        public ResXStringGridRow() {
            Status = STATUS.OK;
            CodeReferences = new List<CodeReferenceResultItem>();
        }

        public void UpdateReferenceCount(bool determinated) {
            if (DataGridView == null) return;
            ResXStringGrid stringGrid=(ResXStringGrid)DataGridView;
            if (ErrorSet.Count == 0 && determinated) {
                Cells[stringGrid.ReferencesColumnName].Value = CodeReferences.Count;
            } else {
                Cells[stringGrid.ReferencesColumnName].Value = "?";
            }
        }

        public STATUS Status { get; set; }

        public List<CodeReferenceResultItem> CodeReferences {
            get;
            set;
        }
    }
}
