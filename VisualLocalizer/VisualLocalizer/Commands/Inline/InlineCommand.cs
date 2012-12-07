using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;
using VisualLocalizer.Components;
using System.Globalization;
using EnvDTE;
using EnvDTE80;
using System.Reflection;
using System.IO;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.OLE.Interop;
using VisualLocalizer.Extensions;

namespace VisualLocalizer.Commands {
   
    internal abstract class InlineCommand<T> : AbstractCommand where T:CodeReferenceResultItem {

        public abstract T GetCodeReferenceResultItem();

        public override void Process() {
            base.Process();            

            T resultItem = GetCodeReferenceResultItem();
            if (resultItem != null) {
                try {
                    int x,y;
                    TextSpan inlineSpan = resultItem.GetInlineReplaceSpan(false, out x, out y);
                    string text = resultItem.GetInlineValue();

                    int hr = textLines.ReplaceLines(inlineSpan.iStartLine, inlineSpan.iStartIndex, inlineSpan.iEndLine, inlineSpan.iEndIndex,
                        Marshal.StringToBSTR(text), text.Length, null);
                    Marshal.ThrowExceptionForHR(hr);                    

                    hr = textView.SetSelection(inlineSpan.iStartLine, inlineSpan.iStartIndex, inlineSpan.iStartLine,
                        inlineSpan.iStartIndex + text.Length);
                    Marshal.ThrowExceptionForHR(hr);

                    CreateInlineUndoUnit(resultItem.FullReferenceText);                    
                } catch (Exception) {
                    VLOutputWindow.VisualLocalizerPane.WriteLine("Exception caught, rolling back...");
                    
                    IOleUndoUnit unit = undoManager.RemoveTopFromUndoStack(1)[0];
                    int itemsToRemove = 1;
                    if (unit is AbstractUndoUnit) {
                        itemsToRemove += ((AbstractUndoUnit)unit).AppendUnits.Count;
                    }
                    unit.Do(undoManager);
                    undoManager.RemoveTopFromUndoStack(itemsToRemove);

                    throw;
                }
            } else throw new Exception("This part of code cannot be inlined");        
        }        

        private bool CreateInlineUndoUnit(string key) {
            bool unitsRemoved = false;
            List<IOleUndoUnit> units = undoManager.RemoveTopFromUndoStack(1);
            unitsRemoved = true;

            InlineUndoUnit newUnit = new InlineUndoUnit(key, false);
            newUnit.AppendUnits.AddRange(units);
            undoManager.Add(newUnit);

            return unitsRemoved;
        }              
               
    }
}
