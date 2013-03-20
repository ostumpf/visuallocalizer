using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using VisualLocalizer.Components;
using System.IO;
using System.Runtime.InteropServices;
using VisualLocalizer.Library;
using System.Windows.Forms;
using VisualLocalizer.Gui;
using System.Collections;

namespace VisualLocalizer.Commands {

    /// <summary>
    /// Base class for BatchReferenceReplacer and BatchInliner. Both these classes operate with references to the resource files,
    /// replacing the references either with string literals or renamed references.
    /// </summary>
    internal abstract class AbstractBatchReferenceProcessor {

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
        /// Rows containing result items
        /// </summary>
        private IList rows;
        
        public AbstractBatchReferenceProcessor(IList rows) {
            if (rows == null) throw new ArgumentNullException("rows");

            buffersCache = new Dictionary<string, IVsTextLines>();
            undoManagersCache = new Dictionary<string, IOleUndoManager>();
            filesCache = new Dictionary<string, StringBuilder>();
            this.rows = rows;            
        }

        /// <summary>
        /// Returns result item with specified index
        /// </summary>        
        public abstract CodeReferenceResultItem GetItemFromList(IList list, int index);
        
        /// <summary>
        /// Returns text that replaces current reference
        /// </summary>        
        public abstract string GetReplaceString(CodeReferenceResultItem item);
        
        /// <summary>
        /// Returns replace span of the reference (what should be replaced)
        /// </summary>        
        public abstract TextSpan GetInlineReplaceSpan(CodeReferenceResultItem item, out int absoluteStartIndex, out int absoluteLength);
        
        /// <summary>
        /// Returns new undo unit for the item
        /// </summary>        
        public abstract AbstractUndoUnit GetUndoUnit(CodeReferenceResultItem item, bool externalChange);
      
        public void Inline(List<CodeReferenceResultItem> dataList, bool externalChange, ref int errorRows) {                        
            // start with the last items - not necessary to adjust position of many items after replace
            for (int i = dataList.Count - 1; i >= 0; i--) {
                int newItemLength = -1;
                try {
                    CodeReferenceResultItem resultItem = dataList[i];

                    if (resultItem.MoveThisItem) { // the item was checked in the toolwindow grid                        
                        int absoluteStartIndex, absoluteLength;
                        
                        // get position information about block to replace
                        TextSpan inlineSpan = GetInlineReplaceSpan(resultItem, out absoluteStartIndex, out absoluteLength);
                        
                        // get text that replaces the result item
                        string text = GetReplaceString(resultItem); 
                        newItemLength = text.Length;

                        string path = resultItem.SourceItem.GetFullPath();
                        if (RDTManager.IsFileOpen(path)) { // file is open
                            if (!buffersCache.ContainsKey(path)) { // file's buffer is not yet loaded
                                // load buffer
                                IVsTextLines textLines = DocumentViewsManager.GetTextLinesForFile(path, false);
                                buffersCache.Add(path, textLines);

                                IOleUndoManager m;
                                // load undo manager
                                int hr = textLines.GetUndoManager(out m);
                                Marshal.ThrowExceptionForHR(hr);
                                undoManagersCache.Add(path, m);
                            }

                            // replace the result item with the new text
                            int h = buffersCache[path].ReplaceLines(inlineSpan.iStartLine, inlineSpan.iStartIndex, inlineSpan.iEndLine, inlineSpan.iEndIndex,
                                Marshal.StringToBSTR(text), text.Length, null);
                            Marshal.ThrowExceptionForHR(h);

                            // previous step caused undo unit to be added - remove it
                            List<IOleUndoUnit> units = undoManagersCache[path].RemoveTopFromUndoStack(1);
                            
                            // and add custom undo unit which includes whole operation
                            AbstractUndoUnit newUnit = GetUndoUnit(resultItem, externalChange);
                            newUnit.AppendUnits.AddRange(units);
                            undoManagersCache[path].Add(newUnit);                                                        
                        } else {
                            if (!filesCache.ContainsKey(path)) { // file is not yet loaded
                                // load the file and save it in cache
                                filesCache.Add(path, new StringBuilder(File.ReadAllText(path)));
                            }
                            StringBuilder b = filesCache[path];

                            // replace the text
                            b = b.Remove(absoluteStartIndex, absoluteLength);
                            b = b.Insert(absoluteStartIndex, text);
                            filesCache[path] = b;
                        }
                    }
                } catch (Exception ex) {                    
                    errorRows++;
                    VLOutputWindow.VisualLocalizerPane.WriteLine(ex.Message);
                } finally {
                    // adjust position of all following result items
                    if (newItemLength != -1)
                        AbstractCheckedGridViewEx.SetItemFinished<CodeReferenceResultItem>(rows, GetItemFromList, i, newItemLength);
                }
            }

            foreach (var pair in filesCache) {
                File.WriteAllText(pair.Key, pair.Value.ToString());
            }
        }

    }
}
