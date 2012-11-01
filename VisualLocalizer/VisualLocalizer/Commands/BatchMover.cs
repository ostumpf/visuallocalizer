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

namespace VisualLocalizer.Commands {
    internal sealed class BatchMover {

        public bool UseFullName { get; private set; }
        public bool MarkUncheckedStringsWithComment { get; private set; }

        private Dictionary<object, NamespacesList> usedNamespacesCache;
        private Dictionary<string, IVsTextLines> buffersCache;
        private Dictionary<string, IOleUndoManager> undoManagersCache;
        private Dictionary<string, StringBuilder> filesCache;
        private Dictionary<string, List<string>> externalUsingsPlan;
        private List<ResXProjectItem> modifiedResxItems;
        private IList rows;

        public BatchMover(IList rows, bool useFullName, bool markUncheckedStringsWithComment) {
            this.MarkUncheckedStringsWithComment = markUncheckedStringsWithComment;
            this.UseFullName = useFullName;
            this.rows = rows;

            usedNamespacesCache = new Dictionary<object, NamespacesList>();
            buffersCache = new Dictionary<string, IVsTextLines>();
            undoManagersCache = new Dictionary<string, IOleUndoManager>();
            filesCache = new Dictionary<string, StringBuilder>();
            externalUsingsPlan = new Dictionary<string, List<string>>();
            modifiedResxItems = new List<ResXProjectItem>();
        }        

        public void Move(List<CodeStringResultItem> dataList, ref int errorRows) {
            Func<IList, int, CodeStringResultItem> getter = new Func<IList, int, CodeStringResultItem>((list, index) => {
                return (list[index] as DataGridViewCheckedRow<CodeStringResultItem>).DataSourceItem;
            });

            for (int i = dataList.Count - 1; i >= 0; i--) {
                int newItemLength = -1;
                try {
                    CodeStringResultItem resultItem = dataList[i];
                    string path = resultItem.SourceItem.Properties.Item("FullPath").Value.ToString();
                    string referenceText = null;
                    bool addNamespace = false;
                    CONTAINS_KEY_RESULT keyConflict = CONTAINS_KEY_RESULT.DOESNT_EXIST;

                    if (resultItem.MoveThisItem) {
                        Validate(resultItem);

                        keyConflict = resultItem.DestinationItem.StringKeyInConflict(resultItem.Key, resultItem.Value);
                        if (keyConflict == CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE)
                            throw new InvalidOperationException(string.Format("Key \"{0}\" already exists with different value.", resultItem.Key));
                        resultItem.Key = resultItem.DestinationItem.GetRealKey(resultItem.Key);

                        NamespacesList usedNamespaces = GetUsedNamespacesFor(resultItem);

                        if (UseFullName) {
                            referenceText = resultItem.DestinationItem.Namespace + "." + resultItem.DestinationItem.Class + "." + resultItem.Key;
                            addNamespace = false;
                        } else {
                            addNamespace = usedNamespaces.ResolveNewElement(resultItem.DestinationItem.Namespace, resultItem.DestinationItem.Class, resultItem.Key,
                                resultItem.SourceItem.ContainingProject, out referenceText);
                        }
                        if (addNamespace) {
                            if (resultItem.NamespaceElement == null) {
                                usedNamespacesCache[resultItem.SourceItem].Add(resultItem.DestinationItem.Namespace, null);
                            } else {
                                usedNamespacesCache[resultItem.NamespaceElement].Add(resultItem.DestinationItem.Namespace, null);
                            }
                        }
                    }

                    newItemLength = -1;
                    if (RDTManager.IsFileOpen(path)) {
                        if (resultItem.MoveThisItem || (MarkUncheckedStringsWithComment && !resultItem.IsMarkedWithUnlocalizableComment)) {
                            if (!buffersCache.ContainsKey(path)) {
                                IVsTextLines textLines = DocumentViewsManager.GetTextLinesForFile(path, false);
                                buffersCache.Add(path, textLines);

                                IOleUndoManager m;
                                int hr = textLines.GetUndoManager(out m);
                                Marshal.ThrowExceptionForHR(hr);
                                undoManagersCache.Add(path, m);
                            }
                        }

                        if (resultItem.MoveThisItem) {
                            MoveToResource(buffersCache[path], resultItem, referenceText);
                            newItemLength = referenceText.Length;

                            if (addNamespace) {
                                resultItem.SourceItem.Document.AddUsingBlock(resultItem.DestinationItem.Namespace);
                                for (int j = i; j >= 0; j--) {
                                    var item = dataList[j];
                                    TextSpan ts = new TextSpan();
                                    ts.iEndIndex = item.ReplaceSpan.iEndIndex;
                                    ts.iEndLine = item.ReplaceSpan.iEndLine + 1;
                                    ts.iStartIndex = item.ReplaceSpan.iStartIndex;
                                    ts.iStartLine = item.ReplaceSpan.iStartLine + 1;
                                    item.ReplaceSpan = ts;
                                }
                            }

                            List<IOleUndoUnit> units = undoManagersCache[path].RemoveTopFromUndoStack(addNamespace ? 2 : 1);
                            AbstractUndoUnit newUnit = null;
                            if (keyConflict == CONTAINS_KEY_RESULT.DOESNT_EXIST) {
                                newUnit = new MoveToResourcesUndoUnit(resultItem.Key, resultItem.Value, resultItem.DestinationItem);
                            } else if (keyConflict == CONTAINS_KEY_RESULT.EXISTS_WITH_SAME_VALUE) {
                                newUnit = new MoveToResourcesReferenceUndoUnit(resultItem.Key);
                            }

                            newUnit.AppendUnits.AddRange(units);
                            undoManagersCache[path].Add(newUnit);
                        } else if (MarkUncheckedStringsWithComment && !resultItem.IsMarkedWithUnlocalizableComment) {
                            MarkAsNoLoc(buffersCache[path], resultItem);
                            newItemLength = resultItem.AbsoluteCharLength + StringConstants.NoLocalizationComment.Length;

                            List<IOleUndoUnit> units = undoManagersCache[path].RemoveTopFromUndoStack(1);
                            MarkAsNotLocalizedStringUndoUnit newUnit = new MarkAsNotLocalizedStringUndoUnit(resultItem.Value);
                            newUnit.AppendUnits.AddRange(units);
                            undoManagersCache[path].Add(newUnit);
                        }
                    } else {
                        if (resultItem.MoveThisItem || (MarkUncheckedStringsWithComment && !resultItem.IsMarkedWithUnlocalizableComment)) {
                            if (!filesCache.ContainsKey(path)) {
                                filesCache.Add(path, new StringBuilder(File.ReadAllText(path)));
                            }
                        }

                        if (resultItem.MoveThisItem) {
                            StringBuilder b = filesCache[path];
                            b.Remove(resultItem.AbsoluteCharOffset, resultItem.AbsoluteCharLength);
                            b.Insert(resultItem.AbsoluteCharOffset, referenceText);
                            newItemLength = referenceText.Length;

                            if (addNamespace) {
                                if (!externalUsingsPlan.ContainsKey(path))
                                    externalUsingsPlan.Add(path, new List<string>());
                                externalUsingsPlan[path].Add(resultItem.DestinationItem.Namespace);
                            }
                        } else if (MarkUncheckedStringsWithComment && !resultItem.IsMarkedWithUnlocalizableComment) {
                            StringBuilder b = filesCache[path];
                            b.Insert(resultItem.AbsoluteCharOffset, StringConstants.NoLocalizationComment);
                            newItemLength = resultItem.AbsoluteCharLength + StringConstants.NoLocalizationComment.Length;
                        }
                    }

                    if (resultItem.MoveThisItem && keyConflict == CONTAINS_KEY_RESULT.DOESNT_EXIST) {
                        if (!resultItem.DestinationItem.IsInBatchMode) {
                            resultItem.DestinationItem.BeginBatch();
                            modifiedResxItems.Add(resultItem.DestinationItem);
                        }
                        resultItem.DestinationItem.AddString(resultItem.Key, resultItem.Value);
                    }

                } catch (Exception ex) {
                    errorRows++;
                    VLOutputWindow.VisualLocalizerPane.WriteLine(ex.Message);
                } finally {
                    if (newItemLength != -1)
                        AbstractCheckedGridViewEx.SetItemFinished(rows, getter, i, newItemLength);
                }
            }

            modifiedResxItems.ForEach((item) => { item.EndBatch(); });
            foreach (var pair in externalUsingsPlan)
                foreach (string nmspc in pair.Value) {
                    filesCache[pair.Key].Insert(0, string.Format("using {0};{1}", nmspc, Environment.NewLine));
                }
            foreach (var pair in filesCache) {
                File.WriteAllText(pair.Key, pair.Value.ToString());
            }
            if (errorRows > 0) throw new Exception("Error occured while processing some rows - see Output window for details."); 
        }

        private NamespacesList GetUsedNamespacesFor(CodeStringResultItem resultItem) {
            if (resultItem.NamespaceElement == null) {
                if (!usedNamespacesCache.ContainsKey(resultItem.SourceItem)) {
                    usedNamespacesCache.Add(resultItem.SourceItem, resultItem.NamespaceElement.GetUsedNamespaces(resultItem.SourceItem));
                }
                return usedNamespacesCache[resultItem.SourceItem];
            } else {
                if (!usedNamespacesCache.ContainsKey(resultItem.NamespaceElement)) {
                    usedNamespacesCache.Add(resultItem.NamespaceElement, resultItem.NamespaceElement.GetUsedNamespaces(resultItem.SourceItem));
                }
                return usedNamespacesCache[resultItem.NamespaceElement];
            }
        }

        private void Validate(CodeStringResultItem resultItem) {
            if (string.IsNullOrEmpty(resultItem.Key) || resultItem.Value == null)
                throw new InvalidOperationException("Item key and value cannot be null");
            if (resultItem.DestinationItem == null)
                throw new InvalidOperationException("Item destination cannot be null");
            if (!string.IsNullOrEmpty(resultItem.ErrorText))
                throw new InvalidOperationException(string.Format("on key \"{0}\": \"{1}\"", resultItem.Key, resultItem.ErrorText));
        }

        private void MarkAsNoLoc(IVsTextLines textLines, CodeStringResultItem resultItem) {
            int hr = textLines.ReplaceLines(resultItem.ReplaceSpan.iStartLine, resultItem.ReplaceSpan.iStartIndex, resultItem.ReplaceSpan.iStartLine, resultItem.ReplaceSpan.iStartIndex,
                Marshal.StringToBSTR(StringConstants.NoLocalizationComment), StringConstants.NoLocalizationComment.Length, new TextSpan[1]);
            Marshal.ThrowExceptionForHR(hr);
        }

        private void MoveToResource(IVsTextLines textLines, CodeStringResultItem resultItem, string referenceText) {
            int hr = textLines.ReplaceLines(resultItem.ReplaceSpan.iStartLine, resultItem.ReplaceSpan.iStartIndex, resultItem.ReplaceSpan.iEndLine, resultItem.ReplaceSpan.iEndIndex,
                        Marshal.StringToBSTR(referenceText), referenceText.Length, new TextSpan[] { resultItem.ReplaceSpan });
            Marshal.ThrowExceptionForHR(hr);
        }
    }
}
