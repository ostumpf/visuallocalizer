using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VisualLocalizer;
using Microsoft.VisualStudio.Shell;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using VisualLocalizer.Commands;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Design;
using System.ComponentModel;
using EnvDTE;
using VisualLocalizer.Components;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE80;
using Microsoft.VisualStudio.OLE.Interop;
using System.IO;

namespace VisualLocalizer.Gui {

    [Guid("121B8FE4-5358-49c2-B1BC-6EC56FFB3B33")]
    internal sealed class BatchMoveToResourcesToolWindow : AbstractCodeToolWindow<BatchMoveToResourcesToolPanel> {
        
        private readonly string[] NAMESPACE_POLICY_ITEMS = { "Add using block if neccessary", "Use full class name" };
        private string currentNamespacePolicy;
        private CommandID runCommandID;
        private OleMenuCommandService menuService;

        public BatchMoveToResourcesToolWindow() {
            this.Caption = "Batch Move to Resources - Visual Localizer";
            this.currentNamespacePolicy = NAMESPACE_POLICY_ITEMS[0];
            this.ToolBar = new CommandID(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarID);
            this.ToolBarLocation = (int)VSTWT_LOCATION.VSTWT_TOP;

            menuService = (OleMenuCommandService)GetService(typeof(IMenuCommandService));
            runCommandID = new CommandID(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarRunID);
            
            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarRunID,
                new EventHandler(runClick), null, menuService);

            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarModeID,
                new EventHandler(handleNamespacePolicyCommand), null, menuService);

            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarModesListID,
                new EventHandler(getNamespacePolicyItems), null, menuService);

            panel.HasErrorChanged += new EventHandler(panel_HasErrorChanged);
        }

        private void panel_HasErrorChanged(object sender, EventArgs e) {
            menuService.FindCommand(runCommandID).Supported = !panel.HasError;
        }
        
        protected override void OnWindowHidden(object sender, EventArgs e) {
            panel.Unload();
            RDTManager.ReleaseLocks();
        }                
        
        public void SetData(List<CodeStringResultItem> value){        
            panel.SetData(value);                        
        }

        private void handleNamespacePolicyCommand(object sender, EventArgs e) {
            if (e == EventArgs.Empty) throw new ArgumentException();
            
            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;
            if (eventArgs != null) {
                string newChoice = eventArgs.InValue as string;
                IntPtr vOut = eventArgs.OutValue;
                if (vOut != IntPtr.Zero && newChoice != null) {
                    throw new ArgumentException();
                } else if (vOut != IntPtr.Zero) {
                    Marshal.GetNativeVariantForObject(this.currentNamespacePolicy, vOut);
                } else if (newChoice != null) {
                    bool validInput = false;
                    int indexInput = -1;
                    for (indexInput = 0; indexInput < NAMESPACE_POLICY_ITEMS.Length; indexInput++) {
                        if (NAMESPACE_POLICY_ITEMS[indexInput] == newChoice) {
                            validInput = true;
                            break;
                        }
                    }
                    if (validInput) {
                        this.currentNamespacePolicy = NAMESPACE_POLICY_ITEMS[indexInput];                        
                    } else {
                        throw new ArgumentException();
                    }
                } else {
                    throw new ArgumentException();
                }
            } else {
                throw new ArgumentException();
            }
        }

        private void getNamespacePolicyItems(object sender, EventArgs e) {
            if ((e == null) || (e == EventArgs.Empty)) throw new ArgumentNullException("e");
            
            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;            
            if (eventArgs != null) {
                object inParam = eventArgs.InValue;
                IntPtr vOut = eventArgs.OutValue;
                if (inParam != null) {
                    throw new ArgumentException();
                } else if (vOut != IntPtr.Zero) {
                    Marshal.GetNativeVariantForObject(NAMESPACE_POLICY_ITEMS, vOut);
                } else {
                    throw new ArgumentException();
                }
            }
        }
                
        private void runClick(object sender, EventArgs args) {
            int checkedRows = panel.CheckedRowsCount;
            int rowCount = panel.Rows.Count;
            int rowErrors = 0;

            try {
                bool usingFullName = currentNamespacePolicy == NAMESPACE_POLICY_ITEMS[1];
                Dictionary<object, Dictionary<string, string>> usedNamespacesCache = new Dictionary<object, Dictionary<string, string>>();
                Dictionary<ProjectItem, List<string>> addNamespacesPlan = new Dictionary<ProjectItem, List<string>>();
                Dictionary<string, IVsTextLines> buffersCache = new Dictionary<string, IVsTextLines>();
                Dictionary<string, IOleUndoManager> undoManagersCache = new Dictionary<string, IOleUndoManager>();
                Dictionary<string, StringBuilder> filesCache = new Dictionary<string, StringBuilder>();
                List<ResXProjectItem> modifiedResxItems = new List<ResXProjectItem>();
                RDTManager.ReleaseLocks();

                while (true) {
                    try {
                        CodeStringResultItem resultItem = (CodeStringResultItem)panel.GetNextResultItem();
                        if (resultItem == null) break;

                        Dictionary<string, string> usedNamespaces = null;
                        if (resultItem.NamespaceElement == null) {
                            if (!usedNamespacesCache.ContainsKey(resultItem.SourceItem)) {
                                usedNamespacesCache.Add(resultItem.SourceItem, resultItem.NamespaceElement.GetUsedNamespaces(resultItem.SourceItem));
                            }
                            usedNamespaces = usedNamespacesCache[resultItem.SourceItem];
                        } else {
                            if (!usedNamespacesCache.ContainsKey(resultItem.NamespaceElement)) {
                                usedNamespacesCache.Add(resultItem.NamespaceElement, resultItem.NamespaceElement.GetUsedNamespaces(resultItem.SourceItem));
                            }
                            usedNamespaces = usedNamespacesCache[resultItem.NamespaceElement];
                        }
                        
                       
                        string referenceText;
                        bool addNamespace;

                        if (usingFullName) {
                            referenceText = resultItem.DestinationItem.Namespace + "." + resultItem.DestinationItem.Class + "." + resultItem.Key;
                            addNamespace = false;
                        } else {
                            referenceText = resultItem.DestinationItem.Class + "." + resultItem.Key;
                            addNamespace = true;
                            if (usedNamespaces.ContainsKey(resultItem.DestinationItem.Namespace)) {
                                addNamespace = false;
                                string alias = usedNamespaces[resultItem.DestinationItem.Namespace];
                                if (!string.IsNullOrEmpty(alias)) referenceText = alias + "." + referenceText;
                            }     
                        }
                        if (addNamespace) {
                            if (!addNamespacesPlan.ContainsKey(resultItem.SourceItem)) {
                                addNamespacesPlan.Add(resultItem.SourceItem, new List<string>());
                            }
                            var list = addNamespacesPlan[resultItem.SourceItem];
                            if (!list.Contains(resultItem.DestinationItem.Namespace))
                                list.Add(resultItem.DestinationItem.Namespace);
                        }

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

                            MoveToResource(buffersCache[path], resultItem, referenceText);
                            
                            List<IOleUndoUnit> units = undoManagersCache[path].RemoveTopFromUndoStack(1);
                            MoveToResourcesUndoUnit newUnit = new MoveToResourcesUndoUnit(resultItem.Key, resultItem.Value, resultItem.DestinationItem);
                            newUnit.AppendUnits.AddRange(units);
                            undoManagersCache[path].Add(newUnit);
                        } else {
                            if (!filesCache.ContainsKey(path)) {                                
                                filesCache.Add(path, new StringBuilder(File.ReadAllText(path)));
                            }
                            StringBuilder b = filesCache[path];
                            b = b.Remove(resultItem.AbsoluteCharOffset, resultItem.AbsoluteCharLength);
                            b = b.Insert(resultItem.AbsoluteCharOffset, referenceText);
                            filesCache[path] = b;
                        }

                        if (!resultItem.DestinationItem.IsInBatchMode) {
                            resultItem.DestinationItem.BeginBatch();
                            modifiedResxItems.Add(resultItem.DestinationItem);
                        }
                        resultItem.DestinationItem.AddString(resultItem.Key, resultItem.Value);

                        panel.SetCurrentItemFinished(true, referenceText.Length);
                    } catch (Exception ex) {
                        panel.SetCurrentItemFinished(false, -1);
                        rowErrors++;
                    }
                }

                modifiedResxItems.ForEach((item) => { item.EndBatch(); });
                
                foreach (var pair in filesCache) {
                    File.WriteAllText(pair.Key, pair.Value.ToString());
                }
                foreach (var pair in addNamespacesPlan) {
                    foreach (string nmspc in pair.Value) {
                        pair.Key.Document.AddUsingBlock(nmspc);
                        undoManagersCache[pair.Key.Properties.Item("FullPath").Value.ToString()].RemoveTopFromUndoStack(1);
                    }
                }                                
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                VisualLocalizer.Library.MessageBox.ShowError(text);
            } finally {
                ((IVsWindowFrame)this.Frame).CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);

                VLOutputWindow.VisualLocalizerPane.Activate();
                VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Move to Resources command completed - selected {0} rows of {1}, {2} rows processed successfully", checkedRows, rowCount, checkedRows - rowErrors);
            }
        }

        private void MoveToResource(IVsTextLines textLines, CodeStringResultItem resultItem, string referenceText) {
            int hr = textLines.ReplaceLines(resultItem.ReplaceSpan.iStartLine, resultItem.ReplaceSpan.iStartIndex, resultItem.ReplaceSpan.iEndLine, resultItem.ReplaceSpan.iEndIndex,
                        Marshal.StringToBSTR(referenceText), referenceText.Length, new TextSpan[] { resultItem.ReplaceSpan });            
            Marshal.ThrowExceptionForHR(hr);
        }
    }

    
}
