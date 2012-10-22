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
   
    internal sealed class InlineCommand : AbstractCommand {          

        public override void Process() {
            base.Process();

            CodeReferenceResultItem resultItem = GetCodeReferenceResultItem();
            if (resultItem != null) {
                try {
                    TextSpan inlineSpan = resultItem.ReplaceSpan;
                    string text = "\"" + resultItem.Value.ConvertUnescapeSequences() + "\"";

                    int hr = textLines.ReplaceLines(inlineSpan.iStartLine, inlineSpan.iStartIndex, inlineSpan.iEndLine, inlineSpan.iEndIndex,
                        Marshal.StringToBSTR(text), text.Length, null);
                    Marshal.ThrowExceptionForHR(hr);                    

                    hr = textView.SetSelection(inlineSpan.iStartLine, inlineSpan.iStartIndex, inlineSpan.iStartLine,
                        inlineSpan.iStartIndex + text.Length);
                    Marshal.ThrowExceptionForHR(hr);

                    CreateInlineUndoUnit(resultItem.ReferenceText);                    
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

        private CodeReferenceResultItem GetCodeReferenceResultItem() {
            string text;
            TextPoint startPoint;
            string codeFunctionName;
            string codeVariableName;
            CodeElement2 codeClass;
            TextSpan selectionSpan;
            bool ok = GetCodeBlockFromSelection(out text, out startPoint, out codeFunctionName, out codeVariableName, out codeClass, out selectionSpan);
            CodeReferenceResultItem result = null;

            if (ok) {
                CodeNamespace codeNamespace = codeClass.GetNamespace();
                CodeReferenceLookuper lookuper = new CodeReferenceLookuper(text, startPoint,
                    currentDocument.ProjectItem.GetResXItemsAround(false).CreateTrie(),
                    codeNamespace.GetUsedNamespaces(currentDocument.ProjectItem), codeNamespace, false, currentDocument.ProjectItem.ContainingProject);
                List<CodeReferenceResultItem> items = lookuper.LookForReferences();

                foreach (CodeReferenceResultItem item in items) {
                    if (item.ReplaceSpan.Contains(selectionSpan)) {
                        result = item;
                        result.SourceItem = currentDocument.ProjectItem;
                        break;
                    }
                }
            }

            return result;
        }

        private bool CreateInlineUndoUnit(string key) {
            bool unitsRemoved = false;
            List<IOleUndoUnit> units = undoManager.RemoveTopFromUndoStack(1);
            unitsRemoved = true;

            InlineUndoUnit newUnit = new InlineUndoUnit(key);
            newUnit.AppendUnits.AddRange(units);
            undoManager.Add(newUnit);

            return unitsRemoved;
        }              
               
    }
}
