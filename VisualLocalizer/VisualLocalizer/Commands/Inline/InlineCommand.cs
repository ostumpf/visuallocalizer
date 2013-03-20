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
   
    /// <summary>
    /// Handles the process of ad-hoc inlining from the moment the result item has already been located. Ancestor of C#, VB and ASP .NET
    /// inline commands.
    /// </summary>
    internal abstract class InlineCommand<T> : AbstractCommand where T:CodeReferenceResultItem {

        /// <summary>
        /// Should return result item located in current selection point. Returns null in any case of errors and exceptions.
        /// </summary>        
        public abstract T GetCodeReferenceResultItem();

        public override void Process() {
            base.Process(); // initialize basic variables            

            T resultItem = GetCodeReferenceResultItem();// get result item (language specific)
            if (resultItem != null) {
                try {
                    int x,y;
                    // get span of reference (may be complicated in case of references like <%= Resources.key %> )
                    TextSpan inlineSpan = resultItem.GetInlineReplaceSpan(false, out x, out y);
                    
                    // get string literal that can be inserted into the code (unescape...)
                    string text = resultItem.GetInlineValue();

                    // perform the replacement
                    int hr = textLines.ReplaceLines(inlineSpan.iStartLine, inlineSpan.iStartIndex, inlineSpan.iEndLine, inlineSpan.iEndIndex,
                        Marshal.StringToBSTR(text), text.Length, null);
                    Marshal.ThrowExceptionForHR(hr);                    

                    hr = textView.SetSelection(inlineSpan.iStartLine, inlineSpan.iStartIndex, inlineSpan.iStartLine,
                        inlineSpan.iStartIndex + text.Length);
                    Marshal.ThrowExceptionForHR(hr);

                    // create undo unit and put it in the undo stack
                    CreateInlineUndoUnit(resultItem.FullReferenceText);                    
                } catch (Exception) {
                    // rollback
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

        /// <summary>
        /// Creates new inline undo unit
        /// </summary>        
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
