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

namespace VisualLocalizer.Commands {
    internal sealed class BatchInliner {

        private Dictionary<string, IVsTextLines> buffersCache;
        private Dictionary<string, IOleUndoManager> undoManagersCache;
        private Dictionary<string, StringBuilder> filesCache;
        private DataGridViewRowCollection rows;

        public BatchInliner(DataGridViewRowCollection rows) {
            buffersCache = new Dictionary<string, IVsTextLines>();
            undoManagersCache = new Dictionary<string, IOleUndoManager>();
            filesCache = new Dictionary<string, StringBuilder>();
            this.rows = rows;
        }

        public void Inline(List<CodeReferenceResultItem> dataList, ref int errorRows) {
            for (int i = dataList.Count - 1; i >= 0; i--) {
                int newItemLength = -1;
                try {
                    CodeReferenceResultItem resultItem = dataList[i];

                    if (resultItem.MoveThisItem) {
                        TextSpan inlineSpan = resultItem.ReplaceSpan;
                        string text = "\"" + resultItem.Value.ConvertUnescapeSequences() + "\"";
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
                            InlineUndoUnit newUnit = new InlineUndoUnit(resultItem.ReferenceText);
                            newUnit.AppendUnits.AddRange(units);
                            undoManagersCache[path].Add(newUnit);
                        } else {
                            if (!filesCache.ContainsKey(path)) {
                                filesCache.Add(path, new StringBuilder(File.ReadAllText(path)));
                            }
                            StringBuilder b = filesCache[path];
                            b = b.Remove(resultItem.AbsoluteCharOffset, resultItem.AbsoluteCharLength);
                            b = b.Insert(resultItem.AbsoluteCharOffset, text);
                            filesCache[path] = b;
                        }                        
                    }
                    if (newItemLength != -1) rows.SetItemFinished<CodeReferenceResultItem>(i, newItemLength);
                } catch (Exception) {
                    if (newItemLength != -1) rows.SetItemFinished<CodeReferenceResultItem>(i, newItemLength);
                    errorRows++;
                }
            }

            foreach (var pair in filesCache) {
                File.WriteAllText(pair.Key, pair.Value.ToString());
            }
        }

    }
}
