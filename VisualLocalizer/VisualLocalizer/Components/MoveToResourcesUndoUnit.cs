using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.OLE.Interop;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EnvDTE80;
using Microsoft.VisualStudio.TextManager.Interop;
using VisualLocalizer.Commands;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {

    [Guid("B9C8503E-80AA-4260-9954-DCAAF3EA4824")]
    internal sealed class MoveToResourcesUndoUnit : AbstractUndoUnit {

        private string key,value;
        private ResXProjectItem item;        
        
        public MoveToResourcesUndoUnit(string key, string value, ResXProjectItem resxItem) {
            this.key = key;
            this.item = resxItem;
            this.value = value;
        }

        public override void Undo() {
            ResXFileHandler.RemoveKey(key, item);                      
        }

        public override void Redo() {
            ResXFileHandler.AddString(key, value, item);        
        }

        public override string GetUndoDescription() {
            return String.Format("Move {0} to resources", key);
        }

        public override string GetRedoDescription() {
            return String.Format("Move {0} to resources", key);
        }
    }
}
