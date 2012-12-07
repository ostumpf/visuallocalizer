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
        private readonly string[] REMEMBER_OPTIONS = { "(None)", "Mark with " + StringConstants.LocalizationComment };
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

            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarRestoreUncheckedID,
                new EventHandler(restoreUnchecked), null, menuService);
            
            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarRemoveUncheckedID,
                new EventHandler(removeUnchecked), null, menuService);

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

        private void removeUnchecked(object sender, EventArgs e) {
            panel.ToolGrid.RemoveUncheckedRows(true);
        }

        private void restoreUnchecked(object sender, EventArgs e) {
            panel.ToolGrid.RestoreRemovedRows();
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
                VLDocumentViewsManager.ReleaseLocks();

                bool usingFullName = currentNamespacePolicy == NAMESPACE_POLICY_ITEMS[1];
                bool markUncheckedStringsWithComment = currentRememberOption == REMEMBER_OPTIONS[1];

                BatchMover mover = new BatchMover(panel.ToolGrid.Rows, usingFullName, markUncheckedStringsWithComment);

                mover.Move(panel.ToolGrid.GetData(), ref rowErrors);
      
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
    }

    
}
