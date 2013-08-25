using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell;
using System.Diagnostics;
using EnvDTE;
using VisualLocalizer.Editor;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Text.RegularExpressions;
using VisualLocalizer.Gui;
using EnvDTE80;
using Microsoft.VisualStudio.OLE.Interop;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using VisualLocalizer.Components.Code;
using VisualLocalizer.Library.Extensions;
using VisualLocalizer.Library.Components;
using VisualLocalizer.Components.UndoUnits;

namespace VisualLocalizer.Commands.Move {

    /// <summary>
    /// Ancestor of C#, VB and ASP .NET "move to resources" commands. It provides functionality from the moment where
    /// the string literal has already been located.
    /// </summary>
    /// <typeparam name="T">Type of result items this class handles.</typeparam>
    internal abstract class MoveToResourcesCommand<T> : AbstractCommand where T:CodeStringResultItem, new() {

        /// <summary>
        /// Gets result item from current selection. Returns null in any case of errors and exceptions.
        /// </summary>        
        protected abstract T GetReplaceStringItem();


        /// <summary>
        /// Called on click - when overriden, finds object in current selection and displayes dialog offering to move it.
        /// </summary>        
        public override void Process() {
            base.Process(); // initialize basic variables
            
            T resultItem = GetReplaceStringItem(); // get result item (language specific)

            if (resultItem != null) { // result item found and ok - proceed              
                TextSpan replaceSpan = resultItem.ReplaceSpan;
                string referencedCodeText = resultItem.Value;
                resultItem.SourceItem = currentDocument.ProjectItem; // set origin project item of the result item               

                // display dialog enabling user to modify resource key, select destination resource file etc.
                // also enables user to resolve conflicts (duplicate key entries)
                SelectResourceFileForm f = new SelectResourceFileForm(currentDocument.ProjectItem, resultItem);
                System.Windows.Forms.DialogResult result = f.ShowDialog();

                resultItem.DestinationItem = f.SelectedItem; // set destination project item - ResX file

                if (result == System.Windows.Forms.DialogResult.OK) {
                    bool removeConst = false;
                    if (resultItem.IsConst) {
                        var deleteConst = VisualLocalizer.Library.Components.MessageBox.Show("This field is marked as 'const'. In order to continue with this operation, this modifier must be removed. Continue?", "Const", OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_WARNING);
                        if (deleteConst == DialogResult.Yes) {
                            removeConst = true;
                        } else {
                            return;
                        }
                    }

                    bool unitsFromStackRemoved = false;
                    bool unitMovedToResource = false;
                    ReferenceString referenceText;
                    bool addUsing = false;

                    // Now we must resolve the namespaces issue. If user selected the "use full name" in previous dialog,
                    // there's no trouble. Otherwise we must find out, if necessary namespace has already been included (using)
                    // and if not, create new using block with the namespace name.

                    try {
                        resultItem.DestinationItem = f.SelectedItem;
                        if (resultItem.DestinationItem == null) throw new InvalidOperationException("Destination item must be selected.");
                        if (resultItem.DestinationItem.InternalProjectItem == null) throw new InvalidOperationException("Destination item must be selected.");
                        if (!File.Exists(resultItem.DestinationItem.InternalProjectItem.GetFullPath())) throw new InvalidOperationException("Destination item file does not exist.");

                        if (f.UsingFullName || resultItem.MustUseFullName || (resultItem.Language==LANGUAGE.VB && resultItem.DestinationItem.IsProjectDefault(resultItem.SourceItem.ContainingProject))) {
                            referenceText = new ReferenceString(f.SelectedItem.Namespace, f.SelectedItem.Class, f.Key);
                            addUsing = false;
                        } else {
                            NamespacesList usedNamespaces = resultItem.GetUsedNamespaces();
                            addUsing = usedNamespaces.ResolveNewElement(f.SelectedItem.Namespace, f.SelectedItem.Class, f.Key,
                                currentDocument.ProjectItem.ContainingProject, out referenceText);                            
                        }
                        
                        string newText = resultItem.GetReferenceText(referenceText);

                        // perform actual replace
                        int hr = textLines.ReplaceLines(replaceSpan.iStartLine, replaceSpan.iStartIndex, replaceSpan.iEndLine, replaceSpan.iEndIndex,
                            Marshal.StringToBSTR(newText), newText.Length, new TextSpan[] { replaceSpan });                        
                        Marshal.ThrowExceptionForHR(hr);                        

                        // set selection to the new text
                        hr = textView.SetSelection(replaceSpan.iStartLine, replaceSpan.iStartIndex,
                            replaceSpan.iStartLine, replaceSpan.iStartIndex + newText.Length);
                        Marshal.ThrowExceptionForHR(hr);

                        if (removeConst) {
                            CodeVariable2 codeVar = (CodeVariable2)resultItem.CodeModelSource;
                            codeVar.ConstKind = vsCMConstKind.vsCMConstKindNone;
                        }

                        if (addUsing) {
                            resultItem.AddUsingBlock(textLines);
                        }                        

                        if (f.Result == SELECT_RESOURCE_FILE_RESULT.INLINE) {
                            // conflict -> user chooses to reference existing key
                            unitsFromStackRemoved = CreateMoveToResourcesReferenceUndoUnit(f.Key, addUsing, removeConst);
                        } else if (f.Result == SELECT_RESOURCE_FILE_RESULT.OVERWRITE) {
                            // conflict -> user chooses to overwrite existing key and reference the new one
                            f.SelectedItem.AddString(f.Key, f.Value);
                            unitMovedToResource = true;
                            unitsFromStackRemoved = CreateMoveToResourcesOverwriteUndoUnit(f.Key, f.Value, f.OverwrittenValue, f.SelectedItem, addUsing, removeConst);                            
                        } else {
                            // no conflict occured
                            f.SelectedItem.AddString(f.Key, f.Value);
                            unitMovedToResource = true;
                            unitsFromStackRemoved = CreateMoveToResourcesUndoUnit(f.Key, f.Value, f.SelectedItem, addUsing, removeConst);                            
                        }
                
                    } catch (Exception) {
                        // exception occured - rollback all already performed actions in order to restore original state

                        VLOutputWindow.VisualLocalizerPane.WriteLine("Exception caught, rolling back...");
                        if (!unitsFromStackRemoved) {
                            int unitsToRemoveCount = (addUsing && removeConst) ? 3 : (addUsing || removeConst ? 2 : 1);
                            List<IOleUndoUnit> units = undoManager.RemoveTopFromUndoStack(unitsToRemoveCount);                            
                            foreach (var unit in units) {
                                unit.Do(undoManager);                                
                            }
                            undoManager.RemoveTopFromUndoStack(units.Count);
                            if (unitMovedToResource) {
                                f.SelectedItem.RemoveKey(f.Key);
                            }
                        } else {
                            AbstractUndoUnit unit = (AbstractUndoUnit)undoManager.RemoveTopFromUndoStack(1)[0];
                            int unitsToRemove = unit.AppendUnits.Count + 1;
                            unit.Do(undoManager);
                            undoManager.RemoveTopFromUndoStack(unitsToRemove);
                        }                        
                        throw;
                    }
                }
            } else throw new Exception("This part of code cannot be referenced");
        }      

        /// <summary>
        /// Adds a new undo unit to the undo stack, representing the "move to resources" action. 
        /// Text replacement and adding new using block is already in the undo stack - 
        /// these items are removed and merged into one atomic action.
        /// </summary>
        /// <param name="key">Resource file key</param>
        /// <param name="value">Resource value</param>
        /// <param name="resXProjectItem">Destination ResX project item</param>
        /// <param name="addNamespace">Whether new using block has been added</param>
        /// <returns>True, if original undo units has been successfully removed from the undo stack</returns>
        private bool CreateMoveToResourcesUndoUnit(string key,string value, ResXProjectItem resXProjectItem,bool addNamespace, bool removeConst) {
            bool unitsRemoved = false;
            int unitsToRemoveCount = (addNamespace && removeConst) ? 3 : (addNamespace || removeConst ? 2 : 1);
            List<IOleUndoUnit> units = undoManager.RemoveTopFromUndoStack(unitsToRemoveCount);
            unitsRemoved = true;

            MoveToResourcesUndoUnit newUnit = new MoveToResourcesUndoUnit(key, value, resXProjectItem);
            newUnit.AppendUnits.AddRange(units);
            undoManager.Add(newUnit);

            return unitsRemoved;
        }

        /// <summary>
        /// Adds a new undo unit to the undo stack, representing the "overwrite" action. 
        /// </summary>
        /// <param name="key">Resource file key</param>
        /// <param name="newValue">New (overwriting) resource value</param>
        /// <param name="oldValue">Old (overwritten) resource value</param>
        /// <param name="resXProjectItem">Destination ResX project item</param>
        /// <param name="addNamespace">Whether new using block has been added</param>
        /// <returns>True, if original undo units has been successfully removed from the undo stack</returns>
        private bool CreateMoveToResourcesOverwriteUndoUnit(string key, string newValue, string oldValue, ResXProjectItem resXProjectItem, bool addNamespace, bool removeConst) {
            bool unitsRemoved = false;
            int unitsToRemoveCount = (addNamespace && removeConst) ? 3 : (addNamespace || removeConst ? 2 : 1);
            List<IOleUndoUnit> units = undoManager.RemoveTopFromUndoStack(unitsToRemoveCount);
            unitsRemoved = true;

            MoveToResourcesOverwriteUndoUnit newUnit = new MoveToResourcesOverwriteUndoUnit(key, oldValue, newValue, resXProjectItem);
            newUnit.AppendUnits.AddRange(units);
            undoManager.Add(newUnit);

            return unitsRemoved;
        }

        /// <summary>
        /// Adds a new undo unit to the undo stack, representing the "inline" action. 
        /// </summary>
        /// <param name="key">Key that it being referenced</param>
        /// <param name="addNamespace">Whether new using block has been added</param>
        /// <returns>True, if original undo units has been successfully removed from the undo stack</returns>
        private bool CreateMoveToResourcesReferenceUndoUnit(string key, bool addNamespace, bool removeConst) {
            bool unitsRemoved = false;
            int unitsToRemoveCount = (addNamespace && removeConst) ? 3 : (addNamespace || removeConst ? 2 : 1);
            List<IOleUndoUnit> units = undoManager.RemoveTopFromUndoStack(unitsToRemoveCount);
            unitsRemoved = true;

            MoveToResourcesReferenceUndoUnit newUnit = new MoveToResourcesReferenceUndoUnit(key);
            newUnit.AppendUnits.AddRange(units);
            undoManager.Add(newUnit);

            return unitsRemoved;
        }        

    }
}
