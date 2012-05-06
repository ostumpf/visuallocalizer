using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.OLE.Interop;
using System.Diagnostics;

namespace VisualLocalizer.Library {
    public static class IOleUndoManagerEx {

        public static List<IOleUndoUnit> RemoveTopFromUndoStack(this IOleUndoManager undoManager, int count) {
            IEnumOleUndoUnits enumerator;
            undoManager.EnumUndoable(out enumerator);
            
            return RemoveTop(undoManager, enumerator, count);
        }

        public static List<IOleUndoUnit> RemoveTopFromRedoStack(this IOleUndoManager undoManager, int count) {
            IEnumOleUndoUnits enumerator;
            undoManager.EnumRedoable(out enumerator);

            return RemoveTop(undoManager, enumerator, count);
        }

        private static List<IOleUndoUnit> RemoveTop(IOleUndoManager undoManager,IEnumOleUndoUnits enumerator, int count) {            
            List<IOleUndoUnit> list = new List<IOleUndoUnit>();
            List<IOleUndoUnit> returnList = new List<IOleUndoUnit>();

            if (count > 0) {
                uint returned = 1;
                while (returned > 0) {
                    IOleUndoUnit[] units = new IOleUndoUnit[1];
                    enumerator.Next(1, units, out returned);

                    if (returned > 0) {
                        list.Add(units[0]);
                    }
                }

                if (list.Count > 0) {
                    undoManager.DiscardFrom(list[list.Count - 1]);
                    for (int i = 0; i < list.Count - count; i++) {
                        undoManager.Add(list[i]);
                    }
                    if (count <= list.Count)
                        returnList = list.GetRange(list.Count - count, count);
                    else
                        returnList = list;
                }


            }
            returnList.Reverse();
            return returnList;
        }
    }
}
