using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.OLE.Interop;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
                    IOleUndoUnit[] units = new IOleUndoUnit[10];
                    enumerator.Next((uint)units.Length, units, out returned);

                    if (returned == 10) {
                        list.AddRange(units);
                    } else if (returned > 0) {
                        for (int i = 0; i < returned; i++)
                            list.Add(units[i]);
                    } 

                }
         
                if (list.Count > 0) {
                    // clear undo/redo stack
                    PoisonPillUndoUnit pill = new PoisonPillUndoUnit();
                    undoManager.Add(pill);
                    undoManager.DiscardFrom(pill);

                //    undoManager.DiscardFrom(list[list.Count-1]);
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



        private class PoisonPillUndoUnit : IOleUndoUnit {
            public void Do(IOleUndoManager pUndoManager) {
            }

            public void GetDescription(out string pBstr) {
                pBstr = "poison pill";
            }

            public void GetUnitType(out Guid pClsid, out int plID) {
                pClsid = new Guid("{B1E4A38E-1D75-41e2-9CFA-38F398A67C3B}");
                plID = 5;
            }

            public void OnNextAdd() {
            }
        }

    }

    
}
