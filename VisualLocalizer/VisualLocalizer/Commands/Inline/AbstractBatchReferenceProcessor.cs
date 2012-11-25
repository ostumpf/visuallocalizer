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
    internal abstract class AbstractBatchReferenceProcessor {

        private Dictionary<string, IVsTextLines> buffersCache;
        private Dictionary<string, IOleUndoManager> undoManagersCache;
        private Dictionary<string, StringBuilder> filesCache;
        private IList rows;
        
        public AbstractBatchReferenceProcessor(IList rows) {
            buffersCache = new Dictionary<string, IVsTextLines>();
            undoManagersCache = new Dictionary<string, IOleUndoManager>();
            filesCache = new Dictionary<string, StringBuilder>();
            this.rows = rows;            
        }

        public abstract CodeReferenceResultItem GetItemFromList(IList list, int index);
        public abstract string GetReplaceString(CodeReferenceResultItem item);
        public abstract TextSpan GetInlineReplaceSpan(CodeReferenceResultItem item, out int absoluteStartIndex, out int absoluteLength);
        public abstract AbstractUndoUnit GetUndoUnit(CodeReferenceResultItem item, bool externalChange);
      
        public void Inline(List<CodeReferenceResultItem> dataList, bool externalChange, ref int errorRows) {            
            
            for (int i = dataList.Count - 1; i >= 0; i--) {
                int newItemLength = -1;
                try {
                    CodeReferenceResultItem resultItem = dataList[i];

                    if (resultItem.MoveThisItem) {
                        int absoluteStartIndex, absoluteLength;
                        TextSpan inlineSpan = GetInlineReplaceSpan(resultItem, out absoluteStartIndex, out absoluteLength);
                        string text = GetReplaceString(resultItem); 
                        newItemLength = text.Length;

                        string path = resultItem.SourceItem.Properties.Item("FullPath").Value.ToString();
                        if (RDTManager.IsFileOpen(path)) {
                            if (!buffersCache.ContainsKey(path)) {
                                IVsTextLines textLines = DocumentViewsManager.GetTextLinesForFile(path, false);
                                buffersCache.Add(path, textLines);

                                IOleUndoManager m;
                                int hr = textLines.GetUndoManager(out m);
                                Marshal.ThrowExceptionForHR(hr);
                                undoManagersCache.Add(path, m);
                            }

                            int h = buffersCache[path].ReplaceLines(inlineSpan.iStartLine, inlineSpan.iStartIndex, inlineSpan.iEndLine, inlineSpan.iEndIndex,
                                Marshal.StringToBSTR(text), text.Length, null);
                            Marshal.ThrowExceptionForHR(h);

                            List<IOleUndoUnit> units = undoManagersCache[path].RemoveTopFromUndoStack(1);
                            
                            AbstractUndoUnit newUnit = GetUndoUnit(resultItem, externalChange);
                            newUnit.AppendUnits.AddRange(units);
                            undoManagersCache[path].Add(newUnit);
                                                        
                        } else {
                            if (!filesCache.ContainsKey(path)) {
                                filesCache.Add(path, new StringBuilder(File.ReadAllText(path)));
                            }
                            StringBuilder b = filesCache[path];
                            b = b.Remove(absoluteStartIndex, absoluteLength);
                            b = b.Insert(absoluteStartIndex, text);
                            filesCache[path] = b;
                        }
                    }
                } catch (Exception ex) {                    
                    errorRows++;
                    VLOutputWindow.VisualLocalizerPane.WriteLine(ex.Message);
                } finally {
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
