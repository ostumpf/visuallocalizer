using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using VisualLocalizer.Components;
using Microsoft.VisualStudio.TextManager.Interop;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.Shell.Interop;
using VisualLocalizer.Commands;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.OLE.Interop;
using System.IO;

namespace VisualLocalizer.Gui {

    [Guid("E7755751-5A96-451b-9DFF-DCA1422CCA0A")]
    internal sealed class BatchInlineToolWindow : AbstractCodeToolWindow<BatchInlineToolPanel> {

        public BatchInlineToolWindow() {
            this.Caption = "Batch Inline - Visual Localizer";
            this.ToolBar = new CommandID(typeof(VisualLocalizer.Guids.VLBatchInlineToolbarCommandSet).GUID, PackageCommandIDs.BatchInlineToolbarID);
            this.ToolBarLocation = (int)VSTWT_LOCATION.VSTWT_TOP;

            OleMenuCommandService menuService = (OleMenuCommandService)GetService(typeof(IMenuCommandService));
            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchInlineToolbarCommandSet).GUID, PackageCommandIDs.BatchInlineToolbarRunID,
                new EventHandler(runClick), null, menuService);
        }

        protected override void OnWindowHidden(object sender, EventArgs e) {
            RDTManager.ReleaseLocks();
        }

        private void runClick(object sender, EventArgs e) {
            int checkedRows = panel.CheckedRowsCount;
            int rowCount = panel.Rows.Count;
            int rowErrors = 0;

            try {
                RDTManager.ReleaseLocks();

                Dictionary<string, IVsTextLines> buffersCache = new Dictionary<string, IVsTextLines>();
                Dictionary<string, IOleUndoManager> undoManagersCache = new Dictionary<string, IOleUndoManager>();
                Dictionary<string, StringBuilder> filesCache = new Dictionary<string, StringBuilder>();

                while (true) {
                    try {
                        CodeReferenceResultItem resultItem = (CodeReferenceResultItem)panel.GetNextResultItem();
                        if (resultItem == null) break;

                        TextSpan inlineSpan = resultItem.ReplaceSpan;
                        string text = "\"" + resultItem.Value.ConvertUnescapeSequences() + "\"";

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

                        panel.SetCurrentItemFinished(true, text.Length);
                    } catch (Exception ex) {
                        panel.SetCurrentItemFinished(false, -1);
                        rowErrors++;
                    }
                }
                foreach (var pair in filesCache) {
                    File.WriteAllText(pair.Key, pair.Value.ToString());
                }
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                VisualLocalizer.Library.MessageBox.ShowError(text);
            } finally {
                ((IVsWindowFrame)this.Frame).CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);

                VLOutputWindow.VisualLocalizerPane.Activate();
                VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Inline command completed - selected {0} rows of {1}, {2} rows processed successfully", checkedRows, rowCount, checkedRows - rowErrors);
            }
        }

        public void SetData(List<CodeReferenceResultItem> value) {
            panel.SetData(value);
        }
    }
}
