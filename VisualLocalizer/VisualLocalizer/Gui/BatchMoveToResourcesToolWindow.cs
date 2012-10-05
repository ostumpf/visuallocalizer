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
using VisualLocalizer.Settings;
using System.Collections;

namespace VisualLocalizer.Gui {

    [Guid("121B8FE4-5358-49c2-B1BC-6EC56FFB3B33")]
    internal sealed class BatchMoveToResourcesToolWindow : AbstractCodeToolWindow<BatchMoveToResourcesToolPanel> {
        
        private readonly string[] NAMESPACE_POLICY_ITEMS = { "Add using block if neccessary", "Use full class name" };
        private readonly string[] REMEMBER_OPTIONS = { "(None)", "Mark with " + StringConstants.NoLocalizationComment };
        private string currentNamespacePolicy,currentRememberOption;
        private CommandID runCommandID;
        private OleMenuCommandService menuService;        

        public BatchMoveToResourcesToolWindow() {
            this.Caption = "Batch Move to Resources - Visual Localizer";
            this.currentNamespacePolicy = NAMESPACE_POLICY_ITEMS[SettingsObject.Instance.NamespacePolicyIndex];
            this.currentRememberOption = REMEMBER_OPTIONS[SettingsObject.Instance.MarkNotLocalizableStringsIndex];
            this.ToolBar = new CommandID(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarID);
            this.ToolBarLocation = (int)VSTWT_LOCATION.VSTWT_TOP;

            menuService = (OleMenuCommandService)GetService(typeof(IMenuCommandService));
            runCommandID = new CommandID(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarRunID);

            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarShowFilterID,
                new EventHandler(showFilterClick), null, menuService);

            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarRunID,
                new EventHandler(runClick), null, menuService);                   

            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarModeID,
                new EventHandler(handleNamespacePolicyCommand), null, menuService);

            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarModesListID,
                new EventHandler(getNamespacePolicyItems), null, menuService);

            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarRememberUncheckedListID,
                new EventHandler(getRememberOptionsItems), null, menuService);

            OleMenuCommand cmd = MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarRememberUncheckedID,
                new EventHandler(handleRememberOptionCommand), null, menuService);            
          
            panel.ToolGrid.HasErrorChanged += new EventHandler(panel_HasErrorChanged);
        }     

        private void panel_HasErrorChanged(object sender, EventArgs e) {
            menuService.FindCommand(runCommandID).Supported = !panel.ToolGrid.HasError;
        }

        private void showFilterClick(object sender, EventArgs e) {
            OleMenuCommand cmd = sender as OleMenuCommand;
            panel.FilterVisible = !panel.FilterVisible;            
            cmd.Text = panel.FilterVisible ? "Hide filter" : "Show filter";            
        } 

        protected override void OnWindowHidden(object sender, EventArgs e) {
            panel.ToolGrid.Unload();
            VLDocumentViewsManager.ReleaseLocks();
        }                
        
        public void SetData(List<CodeStringResultItem> value){
            panel.ToolGrid.SetData(value);                        
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
                        SettingsObject.Instance.NamespacePolicyIndex = indexInput;
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

        private void getRememberOptionsItems(object sender, EventArgs e) {
            if ((e == null) || (e == EventArgs.Empty)) throw new ArgumentNullException("e");

            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;
            if (eventArgs != null) {
                object inParam = eventArgs.InValue;
                IntPtr vOut = eventArgs.OutValue;
                if (inParam != null) {
                    throw new ArgumentException();
                } else if (vOut != IntPtr.Zero) {
                    Marshal.GetNativeVariantForObject(REMEMBER_OPTIONS, vOut);
                } else {
                    throw new ArgumentException();
                }
            }
        }

        private void handleRememberOptionCommand(object sender, EventArgs e) {
            if (e == EventArgs.Empty) throw new ArgumentException();

            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;
            if (eventArgs != null) {
                string newChoice = eventArgs.InValue as string;
                IntPtr vOut = eventArgs.OutValue;
                if (vOut != IntPtr.Zero && newChoice != null) {
                    throw new ArgumentException();
                } else if (vOut != IntPtr.Zero) {
                    Marshal.GetNativeVariantForObject(this.currentRememberOption, vOut);
                } else if (newChoice != null) {
                    bool validInput = false;
                    int indexInput = -1;
                    for (indexInput = 0; indexInput < REMEMBER_OPTIONS.Length; indexInput++) {
                        if (REMEMBER_OPTIONS[indexInput] == newChoice) {
                            validInput = true;
                            break;
                        }
                    }
                    if (validInput) {
                        SettingsObject.Instance.MarkNotLocalizableStringsIndex = indexInput;
                        this.currentRememberOption = REMEMBER_OPTIONS[indexInput];
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

        private void runClick(object sender, EventArgs args) {
            int checkedRows = panel.ToolGrid.CheckedRowsCount;
            int rowCount = panel.ToolGrid.Rows.Count;
            int rowErrors = 0;

            try {
                bool usingFullName = currentNamespacePolicy == NAMESPACE_POLICY_ITEMS[1];
                bool markUncheckedStringsWithComment = currentRememberOption == REMEMBER_OPTIONS[1];
                Dictionary<object, Dictionary<string, string>> usedNamespacesCache = new Dictionary<object, Dictionary<string, string>>();
                Dictionary<string, IVsTextLines> buffersCache = new Dictionary<string, IVsTextLines>();
                Dictionary<string, IOleUndoManager> undoManagersCache = new Dictionary<string, IOleUndoManager>();
                Dictionary<string, StringBuilder> filesCache = new Dictionary<string, StringBuilder>();
                Dictionary<string, List<string>> externalUsingsPlan = new Dictionary<string, List<string>>();
                List<ResXProjectItem> modifiedResxItems = new List<ResXProjectItem>();
                VLDocumentViewsManager.ReleaseLocks();
                List<CodeStringResultItem> dataList=panel.ToolGrid.GetData();

                for (int i = dataList.Count - 1; i >= 0; i--) {
                    try {
                        CodeStringResultItem resultItem = dataList[i];
                        string path = resultItem.SourceItem.Properties.Item("FullPath").Value.ToString();
                        string referenceText = null;
                        bool addNamespace = false;
                        CONTAINS_KEY_RESULT keyConflict = CONTAINS_KEY_RESULT.DOESNT_EXIST;

                        if (resultItem.MoveThisItem) {
                            if (string.IsNullOrEmpty(resultItem.Key) || resultItem.Value == null)
                                throw new InvalidOperationException("Item key and value cannot be null");
                            if (resultItem.DestinationItem == null)
                                throw new InvalidOperationException("Item destination cannot be null");
                            if (!string.IsNullOrEmpty(resultItem.ErrorText))
                                throw new InvalidOperationException(string.Format("on key \"{0}\": \"{1}\"", resultItem.Key, resultItem.ErrorText));

                            keyConflict = resultItem.DestinationItem.StringKeyInConflict(resultItem.Key, resultItem.Value);
                            if (keyConflict == CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE) throw new InvalidOperationException(string.Format("Key \"{0}\" already exists with different value.", resultItem.Key));
                            resultItem.Key = resultItem.DestinationItem.GetRealKey(resultItem.Key);

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
                                if (resultItem.NamespaceElement == null) {
                                    usedNamespacesCache[resultItem.SourceItem].Add(resultItem.DestinationItem.Namespace, null);
                                } else {
                                    usedNamespacesCache[resultItem.NamespaceElement].Add(resultItem.DestinationItem.Namespace, null);
                                }
                            }
                        }
                        
                        if (RDTManager.IsFileOpen(path)) {
                            if (resultItem.MoveThisItem || (markUncheckedStringsWithComment && !resultItem.IsMarkedWithUnlocalizableComment)) {
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
                            } else if (markUncheckedStringsWithComment && !resultItem.IsMarkedWithUnlocalizableComment) {
                                MarkAsNoLoc(buffersCache[path], resultItem);

                                List<IOleUndoUnit> units = undoManagersCache[path].RemoveTopFromUndoStack(1);
                                MarkAsNotLocalizedStringUndoUnit newUnit = new MarkAsNotLocalizedStringUndoUnit(resultItem.Value);
                                newUnit.AppendUnits.AddRange(units);
                                undoManagersCache[path].Add(newUnit);
                            }
                        } else {
                            if (resultItem.MoveThisItem || (markUncheckedStringsWithComment && !resultItem.IsMarkedWithUnlocalizableComment)) {
                                if (!filesCache.ContainsKey(path)) {
                                    filesCache.Add(path, new StringBuilder(File.ReadAllText(path)));
                                }
                            }

                            if (resultItem.MoveThisItem) {
                                StringBuilder b = filesCache[path];
                                b.Remove(resultItem.AbsoluteCharOffset, resultItem.AbsoluteCharLength);
                                b.Insert(resultItem.AbsoluteCharOffset, referenceText);

                                if (addNamespace) {
                                    if (!externalUsingsPlan.ContainsKey(path))
                                        externalUsingsPlan.Add(path, new List<string>());
                                    externalUsingsPlan[path].Add(resultItem.DestinationItem.Namespace);
                                }
                            } else if (markUncheckedStringsWithComment && !resultItem.IsMarkedWithUnlocalizableComment) {
                                StringBuilder b = filesCache[path];
                                b.Insert(resultItem.AbsoluteCharOffset, StringConstants.NoLocalizationComment);
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
                        rowErrors++;
                        VLOutputWindow.VisualLocalizerPane.WriteLine(ex.Message);
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
                if (rowErrors > 0) throw new Exception("Error occured while processing some rows - see Output window for details."); 
                      
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

        private void MarkAsNoLoc(IVsTextLines textLines, CodeStringResultItem resultItem) {
            int hr=textLines.ReplaceLines(resultItem.ReplaceSpan.iStartLine, resultItem.ReplaceSpan.iStartIndex, resultItem.ReplaceSpan.iStartLine, resultItem.ReplaceSpan.iStartIndex,
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
