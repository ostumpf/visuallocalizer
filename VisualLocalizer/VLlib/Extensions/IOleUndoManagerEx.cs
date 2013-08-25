using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.OLE.Interop;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VisualLocalizer.Library.Extensions {

    /// <summary>
    /// Container for extension methods working with UndoManager.
    /// </summary>
    public static class IOleUndoManagerEx {

        /// <summary>
        /// Removes top 'count' items from given undo manager stack, returning removed undo units in a list.
        /// </summary>        
        public static List<IOleUndoUnit> RemoveTopFromUndoStack(this IOleUndoManager undoManager, int count) {
            if (undoManager == null) throw new ArgumentNullException("undoManager");
            if (count < 0) throw new ArgumentException("Count must be greater than or equal to zero.");

            IEnumOleUndoUnits enumerator;
            undoManager.EnumUndoable(out enumerator);
            if (enumerator == null) throw new InvalidOperationException("Undo manager seems to be incorrectly implemented.");

            return RemoveTop(undoManager, enumerator, count);
        }

        /// <summary>
        /// Removes top 'count' items from given redo manager stack, returning removed undo units in a list.
        /// </summary>     
        public static List<IOleUndoUnit> RemoveTopFromRedoStack(this IOleUndoManager redoManager, int count) {
            if (redoManager == null) throw new ArgumentNullException("redoManager");
            if (count < 0) throw new ArgumentException("Count must be greater than or equal to zero.");

            IEnumOleUndoUnits enumerator;
            redoManager.EnumRedoable(out enumerator);
            if (enumerator == null) throw new InvalidOperationException("Redo manager seems to be incorrectly implemented.");

            return RemoveTop(redoManager, enumerator, count);
        }
        
        private static List<IOleUndoUnit> RemoveTop(IOleUndoManager undoManager,IEnumOleUndoUnits enumerator, int count) {            
            List<IOleUndoUnit> backupList = new List<IOleUndoUnit>();
            List<IOleUndoUnit> returnList = new List<IOleUndoUnit>();
            int hr;

            if (count > 0) {
                uint returned = 1;

                // backup all existing undo units
                while (returned > 0) {
                    IOleUndoUnit[] units = new IOleUndoUnit[10];

                    hr = enumerator.Next((uint)units.Length, units, out returned);
                    Marshal.ThrowExceptionForHR(hr);

                    if (returned == 10) {
                        backupList.AddRange(units);
                    } else if (returned > 0) {
                        for (int i = 0; i < returned; i++)
                            backupList.Add(units[i]);
                    } 
                }
         
                // put units back except those which should be removed
                if (backupList.Count > 0) {                    
                    PoisonPillUndoUnit pill = new PoisonPillUndoUnit();
                    undoManager.Add(pill);

                    // clear stack
                    undoManager.DiscardFrom(pill);
                
                    // add units back
                    for (int i = 0; i < backupList.Count - count; i++) {
                        undoManager.Add(backupList[i]);
                    }

                    // return top "count" units or less, if there's not enough
                    if (count <= backupList.Count)
                        returnList = backupList.GetRange(backupList.Count - count, count);
                    else
                        returnList = backupList;
                }

            }

            returnList.Reverse();
            return returnList;
        }


        /// <summary>
        /// Fake undo unit used to clear undo stack
        /// </summary>
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
