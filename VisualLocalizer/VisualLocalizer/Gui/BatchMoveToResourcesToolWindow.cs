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

    /// <summary>
    /// Represents "Batch move" toolwindow
    /// </summary>
    [Guid("121B8FE4-5358-49c2-B1BC-6EC56FFB3B33")]
    internal sealed class BatchMoveToResourcesToolWindow : AbstractCodeToolWindow<BatchMoveToResourcesToolPanel> {
        
        /// <summary>
        /// Options displayed in the "namespace policy" combobox
        /// </summary>
        private readonly string[] NAMESPACE_POLICY_ITEMS = { "Add using block if neccessary", "Use full class name" };

        /// <summary>
        /// Options displayed in the "remember unlocalized strings" combobox
        /// </summary>
        private readonly string[] REMEMBER_OPTIONS = { "(None)", "Mark with " + StringConstants.LocalizationComment };
        
        /// <summary>
        /// Current option selected in the "namespace policy" combobox
        /// </summary>
        private string currentNamespacePolicy;

        /// <summary>
        /// Current option selected in the "remember unlocalized strings" combobox
        /// </summary>
        private string currentRememberOption;
        
        /// <summary>
        /// ID of the "Run" button
        /// </summary>
        private CommandID runCommandID;

        /// <summary>
        /// Menu service used to put buttons on the toolbar
        /// </summary>
        private OleMenuCommandService menuService;        

        public BatchMoveToResourcesToolWindow() {
            this.Caption = "Batch Move to Resources - Visual Localizer"; // title

            // set selected options according to settings
            this.currentNamespacePolicy = NAMESPACE_POLICY_ITEMS[SettingsObject.Instance.NamespacePolicyIndex];
            this.currentRememberOption = REMEMBER_OPTIONS[SettingsObject.Instance.MarkNotLocalizableStringsIndex];

            // create the toolbar
            this.ToolBar = new CommandID(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarID);
            this.ToolBarLocation = (int)VSTWT_LOCATION.VSTWT_TOP;

            menuService = (OleMenuCommandService)GetService(typeof(IMenuCommandService));
            if (menuService == null) throw new InvalidOperationException("Cannot consume OleMenuCommandService.");

            runCommandID = new CommandID(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarRunID);

            // add "Show/Hide filter" button
            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarShowFilterID,
                new EventHandler(ShowFilterClick), null, menuService);

            // add "Run" button
            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarRunID,
                new EventHandler(RunClick), null, menuService);                   

            // add "Namespace policy" combobox
            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarModeID,
                new EventHandler(HandleNamespacePolicyCommand), null, menuService);

            // necessary for the "Namespace policy" combobox to be working
            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarModesListID,
                new EventHandler(GetNamespacePolicyItems), null, menuService);

            // add "Remember unlocalized strings" combobox
            OleMenuCommand cmd = MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarRememberUncheckedID,
                new EventHandler(HandleRememberOptionCommand), null, menuService);

            // necessary for the "Remember unlocalized strings" combobox to be working
            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarRememberUncheckedListID,
                new EventHandler(GetRememberOptionsItems), null, menuService);

            // add "Restore unchecked" button
            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarRestoreUncheckedID,
                new EventHandler(RestoreUnchecked), null, menuService);

            // add "Remove unchecked" button
            MenuManager.ConfigureMenuCommand(typeof(VisualLocalizer.Guids.VLBatchMoveToolbarCommandSet).GUID, PackageCommandIDs.BatchMoveToolbarRemoveUncheckedID,
                new EventHandler(RemoveUnchecked), null, menuService);            
          
            panel.ToolGrid.HasErrorChanged += new EventHandler(Panel_HasErrorChanged);
        }     

        /// <summary>
        /// Updates "Run" buttons state according to number of error rows in the grid
        /// </summary>        
        private void Panel_HasErrorChanged(object sender, EventArgs e) {
            try {
                menuService.FindCommand(runCommandID).Supported = !panel.ToolGrid.HasError;
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Displayes/Hides the filter
        /// </summary>        
        private void ShowFilterClick(object sender, EventArgs e) {
            OleMenuCommand cmd = sender as OleMenuCommand;
            panel.FilterVisible = !panel.FilterVisible;            
            cmd.Text = panel.FilterVisible ? "Hide filter" : "Show filter";            
        }

        /// <summary>
        /// Removes unchecked rows from the grid
        /// </summary>        
        private void RemoveUnchecked(object sender, EventArgs e) {
            try {
                panel.ToolGrid.RemoveUncheckedRows(true);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Restores unchecked rows to the grid
        /// </summary>
        private void RestoreUnchecked(object sender, EventArgs e) {
            try {
                panel.ToolGrid.RestoreRemovedRows();
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// When the toolwindow is closed
        /// </summary>        
        protected override void OnWindowHidden(object sender, EventArgs e) {
            try {
                VLDocumentViewsManager.ReleaseLocks(); // unlocks all locked files
                panel.ToolGrid.UnloadResXItems();// release all ResX files loaded in the grid           
                panel.ToolGrid.ResetConflictResolver();
                MenuManager.OperationInProgress = false; // permits other operations
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }                
        
        /// <summary>
        /// Sets content of the grid
        /// </summary>        
        public void SetData(List<CodeStringResultItem> value){
            if (value == null) throw new ArgumentNullException("value");

            panel.ToolGrid.SetData(value);                        
        }

        /// <summary>
        /// Process the change in "Namespace policy" combobox
        /// </summary>        
        private void HandleNamespacePolicyCommand(object sender, EventArgs e) {
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
                        SettingsObject.Instance.NamespacePolicyIndex = indexInput; // remember the choice
                        this.currentNamespacePolicy = NAMESPACE_POLICY_ITEMS[indexInput]; // set the current option                       
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

        /// <summary>
        /// Required by VS - fills the "Namespace policy" combobox with options
        /// </summary>        
        private void GetNamespacePolicyItems(object sender, EventArgs e) {
            if ((e == null) || (e == EventArgs.Empty)) throw new ArgumentNullException("e");
            
            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;            
            if (eventArgs != null) {                
                object inParam = eventArgs.InValue;
                IntPtr vOut = eventArgs.OutValue;
                if (inParam != null) {
                    throw new ArgumentException();
                } else if (vOut != IntPtr.Zero) {
                    Marshal.GetNativeVariantForObject(NAMESPACE_POLICY_ITEMS, vOut); // marshall the options
                } else {
                    throw new ArgumentException();
                }
            }
        }

        /// <summary>
        /// Required by VS - fills the "Remember unchecked strings" combobox with options
        /// </summary>        
        private void GetRememberOptionsItems(object sender, EventArgs e) {
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

        /// <summary>
        /// Process the change in "Remember unchecked strings" combobox
        /// </summary>        
        private void HandleRememberOptionCommand(object sender, EventArgs e) {
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
                        SettingsObject.Instance.MarkNotLocalizableStringsIndex = indexInput; // remember selected value
                        this.currentRememberOption = REMEMBER_OPTIONS[indexInput]; // set the current option
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

        /// <summary>
        /// "Run" button was clicked
        /// </summary>        
        private void RunClick(object sender, EventArgs args) {
            int checkedRows = panel.ToolGrid.CheckedRowsCount;
            int rowCount = panel.ToolGrid.Rows.Count;
            int rowErrors = 0;

            try {
                VLDocumentViewsManager.ReleaseLocks(); // unlock the documents
                MenuManager.OperationInProgress = false; // permit other operations

                bool usingFullName = currentNamespacePolicy == NAMESPACE_POLICY_ITEMS[1]; // whether full references will be used
                bool markUncheckedStringsWithComment = currentRememberOption == REMEMBER_OPTIONS[1]; // whether unchecked strings will be marked with "no-localization" comment

                BatchMover mover = new BatchMover(panel.ToolGrid.Rows, usingFullName, markUncheckedStringsWithComment);

                mover.Move(panel.ToolGrid.GetData(), ref rowErrors); // run the mover
      
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            } finally {
                ((IVsWindowFrame)this.Frame).CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave); // close the toolwindow

                VLOutputWindow.VisualLocalizerPane.Activate();
                VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Move to Resources command completed - selected {0} rows of {1}, {2} rows processed successfully", checkedRows, rowCount, checkedRows - rowErrors);
            }
        }       
    }

    
}
