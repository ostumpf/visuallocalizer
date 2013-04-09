using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms;
using VisualLocalizer.Gui;
using System.Collections;
using EnvDTE;
using VisualLocalizer.Extensions;

namespace VisualLocalizer.Commands {

    /// <summary>
    /// Used to perform actual "Batch move to resources" command with list of result items (found string literals).
    /// </summary>
    internal sealed class BatchMover {

        /// <summary>
        /// True if references should be in the full name format, i.e. with namespace (no using blocks added)
        /// </summary>
        public bool UseFullName { get; private set; }

        /// <summary>
        /// True if those result items, that aren't moved, should be marked with special comment
        /// </summary>
        public bool MarkUncheckedStringsWithComment { get; private set; }

        /// <summary>
        /// Cache of lists of used namespaces - key can be either source file or namespace block
        /// </summary>
        private Dictionary<object, NamespacesList> usedNamespacesCache;

        /// <summary>
        /// Cache for open documents buffers, key is full path of the file
        /// </summary>
        private Dictionary<string, IVsTextLines> buffersCache;

        /// <summary>
        /// Cache for undo managers of the open documents, key is full path of the file
        /// </summary>
        private Dictionary<string, IOleUndoManager> undoManagersCache;

        /// <summary>
        /// Cache for closed documents texts
        /// </summary>
        private Dictionary<string, StringBuilder> filesCache;

        /// <summary>
        /// Cache for adding using blocks to closed files - key is full path of the file, value list of new using blocks
        /// </summary>
        private Dictionary<string, List<string>> newUsingsPlan;
        
        /// <summary>
        /// List of ResX items that were loaded to memory
        /// </summary>
        private HashSet<ResXProjectItem> loadedResxItems;
        
        public BatchMover(bool useFullName, bool markUncheckedStringsWithComment) {
            this.MarkUncheckedStringsWithComment = markUncheckedStringsWithComment;
            this.UseFullName = useFullName;
            
            usedNamespacesCache = new Dictionary<object, NamespacesList>();
            buffersCache = new Dictionary<string, IVsTextLines>();
            undoManagersCache = new Dictionary<string, IOleUndoManager>();
            filesCache = new Dictionary<string, StringBuilder>();
            newUsingsPlan = new Dictionary<string, List<string>>();            
            loadedResxItems = new HashSet<ResXProjectItem>();
        }        

        public void Move(List<CodeStringResultItem> dataList, ref int errorRows) {
            // sort according to position
            dataList.Sort(new ResultItemsPositionCompararer<CodeStringResultItem>());

            for (int i = dataList.Count - 1; i >= 0; i--) {
                try {
                    // initialization of data
                    CodeStringResultItem resultItem = dataList[i]; 
                    string path = resultItem.SourceItem.GetFullPath();
                    ReferenceString referenceText = null;
                    bool addUsingBlock = false;                    
                    CONTAINS_KEY_RESULT keyConflict = CONTAINS_KEY_RESULT.DOESNT_EXIST;

                    if (resultItem.MoveThisItem) { // row was checked in the toolwindow                        
                        Validate(resultItem); // check that key, value and destination item was specifed and that row has no errors
                        if (!resultItem.DestinationItem.IsLoaded) {
                            resultItem.DestinationItem.Load();                            
                        }
                        if (!loadedResxItems.Contains(resultItem.DestinationItem)) {
                            loadedResxItems.Add(resultItem.DestinationItem);
                        }

                        // check if such item already exists in destination file
                        keyConflict = resultItem.DestinationItem.GetKeyConflictType(resultItem.Key, resultItem.Value);
                        if (keyConflict == CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE)
                            throw new InvalidOperationException(string.Format("Key \"{0}\" already exists with different value.", resultItem.Key));
                        resultItem.Key = resultItem.DestinationItem.GetRealKey(resultItem.Key); // if key already exists, return its name (solves case-sensitivity problems)

                        NamespacesList usedNamespaces = GetUsedNamespacesFor(resultItem);
                        
                        if (UseFullName || resultItem.MustUseFullName) { // reference will contain namespace
                            referenceText = new ReferenceString(resultItem.DestinationItem.Namespace, resultItem.DestinationItem.Class, resultItem.Key);
                            addUsingBlock = false; // no using block will be added
                        } else {
                            // use resolver whether it is ok to add using block
                            addUsingBlock = usedNamespaces.ResolveNewElement(resultItem.DestinationItem.Namespace, resultItem.DestinationItem.Class, resultItem.Key,
                                    resultItem.SourceItem.ContainingProject, out referenceText);
                        }
                        if (addUsingBlock) { // new using block will be added                                                        
                            if (!usedNamespacesCache.ContainsKey(resultItem.SourceItem)) {
                                usedNamespacesCache.Add(resultItem.SourceItem, new NamespacesList());
                            }                            
                            foreach (var pair in usedNamespacesCache) {
                                if (!pair.Value.ContainsNamespace(resultItem.DestinationItem.Namespace)) 
                                    pair.Value.Add(resultItem.DestinationItem.Namespace, null, true);
                            }
                        }
                    }

                    if (RDTManager.IsFileOpen(path)) { // file is open
                        if (resultItem.MoveThisItem || (MarkUncheckedStringsWithComment && !resultItem.IsMarkedWithUnlocalizableComment)) { // string literal in text will be modified (referenced or marked with comment)                            
                            if (!buffersCache.ContainsKey(path)) {
                                // load file's buffer
                                IVsTextLines textLines = DocumentViewsManager.GetTextLinesForFile(path, false);
                                buffersCache.Add(path, textLines);

                                IOleUndoManager m;
                                // get file's undo manager
                                int hr = textLines.GetUndoManager(out m);
                                Marshal.ThrowExceptionForHR(hr);
                                undoManagersCache.Add(path, m);
                            }
                        }

                        if (resultItem.MoveThisItem) {
                            // perform the text replacement
                            MoveToResource(buffersCache[path], resultItem, referenceText);
                            
                            if (addUsingBlock) {
                                // add using block to the source file
                                int beforeLines, afterLines;
                                buffersCache[path].GetLineCount(out beforeLines);
                                resultItem.AddUsingBlock(null);
                                buffersCache[path].GetLineCount(out afterLines);
                                int diff = afterLines - beforeLines;

                                // because of the previous step, it is necessary to adjust position of all not-yet referenced result items 
                                for (int j = i; j >= 0; j--) {
                                    var item = dataList[j];
                                    if (item.SourceItem == resultItem.SourceItem) {
                                        TextSpan ts = new TextSpan();
                                        ts.iEndIndex = item.ReplaceSpan.iEndIndex;
                                        ts.iEndLine = item.ReplaceSpan.iEndLine + diff;
                                        ts.iStartIndex = item.ReplaceSpan.iStartIndex;
                                        ts.iStartLine = item.ReplaceSpan.iStartLine + diff;
                                        item.ReplaceSpan = ts;
                                    }
                                }
                            }

                            // previous step (replace and possibly new using block) caused undo unit to be added - remove it
                            List<IOleUndoUnit> units = undoManagersCache[path].RemoveTopFromUndoStack(addUsingBlock ? 2 : 1);
                            
                            // and add custom undo unit
                            AbstractUndoUnit newUnit = null;
                            if (keyConflict == CONTAINS_KEY_RESULT.DOESNT_EXIST) {
                                newUnit = new MoveToResourcesUndoUnit(resultItem.Key, resultItem.Value, resultItem.DestinationItem);
                            } else if (keyConflict == CONTAINS_KEY_RESULT.EXISTS_WITH_SAME_VALUE) {
                                newUnit = new MoveToResourcesReferenceUndoUnit(resultItem.Key);
                            }

                            newUnit.AppendUnits.AddRange(units);
                            undoManagersCache[path].Add(newUnit);
                        } else if (MarkUncheckedStringsWithComment && !resultItem.IsMarkedWithUnlocalizableComment) { // string literal should be marked with comment
                            AspNetStringResultItem aitem = resultItem as AspNetStringResultItem;

                            // this operation is only possible if string literal comes from C# code
                            if (resultItem is CSharpStringResultItem || (aitem != null && aitem.ComesFromCodeBlock && aitem.Language == LANGUAGE.CSHARP)) {
                                // add the comment
                                int c = MarkAsNoLoc(buffersCache[path], resultItem);
                              
                                // add undo unit
                                List<IOleUndoUnit> units = undoManagersCache[path].RemoveTopFromUndoStack(1);
                                MarkAsNotLocalizedStringUndoUnit newUnit = new MarkAsNotLocalizedStringUndoUnit(resultItem.Value);
                                newUnit.AppendUnits.AddRange(units);
                                undoManagersCache[path].Add(newUnit);
                            }
                        }
                    } else { // file is closed
                        // same as with open file, only operating with text, not buffers

                        if (resultItem.MoveThisItem || (MarkUncheckedStringsWithComment && !resultItem.IsMarkedWithUnlocalizableComment)) { // string literal will be modified
                            // load file's text into the cache
                            if (!filesCache.ContainsKey(path)) {
                                filesCache.Add(path, new StringBuilder(File.ReadAllText(path)));
                            }
                        }

                        if (resultItem.MoveThisItem) {
                            StringBuilder b = filesCache[path];

                            // perform the replacement
                            string insertText=resultItem.GetReferenceText(referenceText);
                            b.Remove(resultItem.AbsoluteCharOffset, resultItem.AbsoluteCharLength);
                            b.Insert(resultItem.AbsoluteCharOffset, insertText);
                          
                            if (addUsingBlock) {
                                // add using block
                                if (!newUsingsPlan.ContainsKey(path))
                                    newUsingsPlan.Add(path, new List<string>());
                                newUsingsPlan[path].Add(resultItem.DestinationItem.Namespace);
                            }
                        } else if (MarkUncheckedStringsWithComment && !resultItem.IsMarkedWithUnlocalizableComment) {
                             AspNetStringResultItem aitem = resultItem as AspNetStringResultItem;
                             
                             if (resultItem is CSharpStringResultItem || (aitem != null && aitem.ComesFromCodeBlock && aitem.Language == LANGUAGE.CSHARP)) {
                                 StringBuilder b = filesCache[path];
                                 b.Insert(resultItem.AbsoluteCharOffset, resultItem.NoLocalizationComment);                                 
                             }
                        }
                    }

                    if (resultItem.MoveThisItem && keyConflict == CONTAINS_KEY_RESULT.DOESNT_EXIST) {
                        if (!resultItem.DestinationItem.IsInBatchMode) {
                            resultItem.DestinationItem.BeginBatch();                            
                        }
                        // add the key to the ResX file
                        resultItem.DestinationItem.AddString(resultItem.Key, resultItem.Value);
                    }

                } catch (Exception ex) {
                    errorRows++;
                    VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                } 
            }
            
            // add using blocks to closed files texts
            foreach (var pair in newUsingsPlan)
                foreach (string nmspc in pair.Value) {
                    AddUsingBlockTo(pair.Key, nmspc);
                }

            // flush closed files texts
            foreach (var pair in filesCache) {
                File.WriteAllText(pair.Key, pair.Value.ToString());
            }
            foreach (ResXProjectItem item in loadedResxItems) {
                if (item.IsInBatchMode) {
                    item.EndBatch();                    
                }
                item.Unload();
            }
            if (errorRows > 0) throw new Exception("Error occured while processing some rows - see Output window for details."); 
        }        

        /// <summary>
        /// Returns list of namespaces that affect given result item (using blocks and namespaces declarations)
        /// </summary>        
        private NamespacesList GetUsedNamespacesFor(CodeStringResultItem resultItem) {
            if (resultItem is NetStringResultItem) {
                CodeNamespace namespaceElement = (resultItem as NetStringResultItem).NamespaceElement;

                if (namespaceElement == null) { // no parent namespace - use source item as a key
                    if (!usedNamespacesCache.ContainsKey(resultItem.SourceItem)) {
                        usedNamespacesCache.Add(resultItem.SourceItem, namespaceElement.GetUsedNamespaces(resultItem.SourceItem));
                    }
                    return usedNamespacesCache[resultItem.SourceItem];
                } else { // has parent namespace - used namespaces can differ, use the parent namespace as a key
                    if (!usedNamespacesCache.ContainsKey(namespaceElement)) {
                        usedNamespacesCache.Add(namespaceElement, namespaceElement.GetUsedNamespaces(resultItem.SourceItem));
                        if (usedNamespacesCache.ContainsKey(resultItem.SourceItem)) {
                            usedNamespacesCache[namespaceElement].AddRange(usedNamespacesCache[resultItem.SourceItem]);
                        }
                    }
                    
                    return usedNamespacesCache[namespaceElement];
                }
            } else if (resultItem is AspNetStringResultItem) {
                // in case of ASP .NET result items, always use source item as a key
                if (!usedNamespacesCache.ContainsKey(resultItem.SourceItem)) {
                    usedNamespacesCache.Add(resultItem.SourceItem, (resultItem as AspNetStringResultItem).DeclaredNamespaces);
                }
                return usedNamespacesCache[resultItem.SourceItem];
            } else throw new Exception("Unkown result item type.");
        }

        /// <summary>
        /// Checks that given result item has key, value and destination item defined and that no errors occured in toolwindow's grid
        /// </summary>
        private void Validate(CodeStringResultItem resultItem) {
            if (string.IsNullOrEmpty(resultItem.Key) || resultItem.Value == null)
                throw new InvalidOperationException("Item key and value cannot be null");
            if (resultItem.DestinationItem == null)
                throw new InvalidOperationException("Item destination cannot be null");
            if (!string.IsNullOrEmpty(resultItem.ErrorText))
                throw new InvalidOperationException(string.Format("on key \"{0}\": \"{1}\"", resultItem.Key, resultItem.ErrorText));
        }

        /// <summary>
        /// Marks given result item with no-localization comment
        /// </summary>        
        /// <returns>Length of no-localization comment (language specific)</returns>
        private int MarkAsNoLoc(IVsTextLines textLines, CodeStringResultItem resultItem) {
            int hr = textLines.ReplaceLines(resultItem.ReplaceSpan.iStartLine, resultItem.ReplaceSpan.iStartIndex, resultItem.ReplaceSpan.iStartLine, resultItem.ReplaceSpan.iStartIndex,
                Marshal.StringToBSTR(resultItem.NoLocalizationComment), resultItem.NoLocalizationComment.Length, new TextSpan[1]);
            Marshal.ThrowExceptionForHR(hr);

            return resultItem.NoLocalizationComment.Length;
        }

        /// <summary>
        /// Adds using block to the closed file's buffer
        /// </summary>
        /// <param name="filename">File path</param>
        /// <param name="nmspc">Imported namespace</param>
        private void AddUsingBlockTo(string filename, string nmspc) {
            FILETYPE type = filename.GetFileType();
            switch (type) {
                case FILETYPE.CSHARP:
                    filesCache[filename].Insert(0, string.Format(StringConstants.CSharpUsingBlockFormat, nmspc));
                    break;
                case FILETYPE.ASPX:
                    filesCache[filename].Insert(0, string.Format(StringConstants.AspImportDirectiveFormat, nmspc));
                    break;
                case FILETYPE.VB:
                    filesCache[filename].Insert(0, string.Format(StringConstants.VBUsingBlockFormat, nmspc));
                    break;             
            }
        }                        

        /// <summary>
        /// Perform actual replacement of the string literal
        /// </summary>
        /// <returns>Length of the reference</returns>
        private int MoveToResource(IVsTextLines textLines, CodeStringResultItem resultItem, ReferenceString referenceText) {
            string newText = resultItem.GetReferenceText(referenceText);
            int hr = textLines.ReplaceLines(resultItem.ReplaceSpan.iStartLine, resultItem.ReplaceSpan.iStartIndex, resultItem.ReplaceSpan.iEndLine, resultItem.ReplaceSpan.iEndIndex,
                            Marshal.StringToBSTR(newText), newText.Length, new TextSpan[] { resultItem.ReplaceSpan });
            Marshal.ThrowExceptionForHR(hr);

            return newText.Length;
        }
    }
}
