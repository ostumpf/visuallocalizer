using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using EnvDTE;
using System.Drawing;
using System.Windows.Forms.VisualStyles;
using System.Collections;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;
using VisualLocalizer.Settings;
using System.Text.RegularExpressions;
using VisualLocalizer.Extensions;
using System.ComponentModel;

namespace VisualLocalizer.Gui {

    /// <summary>
    /// Represents grid displayed in "Batch move" tool window
    /// </summary>
    internal sealed class BatchMoveToResourcesToolGrid : AbstractKeyValueGridView<CodeStringResultItem>, IHighlightRequestSource {

        /// <summary>
        /// Issued when row is double-clicked - causes corresponding block of code in the code window to be selected
        /// </summary>
        public event EventHandler<CodeResultItemEventArgs> HighlightRequired;

        /// <summary>
        /// Error displayed when no destination file is selected for a result item
        /// </summary>
        private const string NoDestinationFileError = "Destination file not set";

        /// <summary>
        /// Error displayed when two keys in the grid have same names but different values
        /// </summary>
        private const string DuplicateKeyError = "Duplicate key entry - key is already present in resource file with different value";

        /// <summary>
        /// Working set of filter criteria
        /// </summary>
        private Dictionary<string, AbstractLocalizationCriterion> lastSetCriteria;
        
        /// <summary>
        /// Cache of possible destination ResX files - are common for each project
        /// </summary>
        private Dictionary<Project, DataGridViewComboBoxCell.ObjectCollection> destinationItemsCache = new Dictionary<Project, DataGridViewComboBoxCell.ObjectCollection>();
        
        /// <summary>
        /// Cache of ResX files - key is display name of the ResX file
        /// </summary>
        private Dictionary<string, ResXProjectItem> resxItemsCache = new Dictionary<string, ResXProjectItem>();        
        
        /// <summary>
        /// ResX files loaded in this instance (unloaded in the Unload() method)
        /// </summary>
        private List<ResXProjectItem> loadedItems = new List<ResXProjectItem>();
        
        /// <summary>
        /// New key was created in "key" column combo box
        /// </summary>
        private bool valueAdded = false;

        private BatchMoveToResourcesToolPanel parentToolPanel;
        private MenuItem destinationContextMenu;

        public BatchMoveToResourcesToolGrid(BatchMoveToResourcesToolPanel panel)
            : base(SettingsObject.Instance.ShowContextColumn, new DestinationKeyValueConflictResolver(true,true)) {
            this.parentToolPanel = panel;
            this.MultiSelect = true;
            this.ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable;

            // used to modify "key" column combo box style
            this.EditingControlShowing += new DataGridViewEditingControlShowingEventHandler(BatchMoveToResourcesToolPanel_EditingControlShowing);
            
            // handles adding of new key in the "key" column combo box
            this.CellValidating += new DataGridViewCellValidatingEventHandler(BatchMoveToResourcesToolPanel_CellValidating);
            
            // triggers HighlightRequired event
            this.CellDoubleClick += new DataGridViewCellEventHandler(OnRowDoubleClick);

            // called during sorting - extracts data from cells
            this.SortCompare += new DataGridViewSortCompareEventHandler(BatchMoveToResourcesToolGrid_SortCompare);

            // displayes context menu
            this.MouseUp += new MouseEventHandler(OnContextMenuShow);

            DataGridViewKeyValueRow<CodeReferenceResultItem> template = new DataGridViewKeyValueRow<CodeReferenceResultItem>();
            template.MinimumHeight = 24;
            this.RowTemplate = template;

            ContextMenu contextMenu = new ContextMenu();
            
            MenuItem stateMenu = new MenuItem("State");
            stateMenu.MenuItems.Add("Checked", new EventHandler((o, e) => {
                try {
                    SetCheckStateOfSelected(true);
                } catch (Exception ex) {
                    VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                    VisualLocalizer.Library.MessageBox.ShowException(ex);
                }
            }));
            stateMenu.MenuItems.Add("Unchecked", new EventHandler((o, e) => {
                try {
                    SetCheckStateOfSelected(false);
                } catch (Exception ex) {
                    VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                    VisualLocalizer.Library.MessageBox.ShowException(ex);
                }
            }));
            contextMenu.MenuItems.Add(stateMenu);

            destinationContextMenu = new MenuItem("Common destination");
            contextMenu.MenuItems.Add(destinationContextMenu);
            contextMenu.Popup += new EventHandler(ContextMenu_Popup);

            this.ContextMenu = contextMenu;

            SettingsObject.Instance.RevalidationRequested += new Action(Instance_RevalidationRequested);
        }
        

        #region public members

        /// <summary>
        /// Unloads all loaded ResX files
        /// </summary>
        public void UnloadResXItems() {
            try {
                if (loadedItems != null) {
                    foreach (var item in loadedItems)
                        item.Unload();
                    loadedItems.Clear();
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Sets content of the grid
        /// </summary>        
        public override void SetData(List<CodeStringResultItem> value) {
            if (value == null) throw new ArgumentNullException("value");
            base.SetData(value);

            this.Rows.Clear();
            destinationItemsCache.Clear();
            resxItemsCache.Clear();
            loadedItems.Clear();            
            CheckedRowsCount = 0;            
            SuspendLayout();            

            // set "context" column visibility according to settings
            if (Columns.Contains(ContextColumnName)) Columns[ContextColumnName].Visible = SettingsObject.Instance.ShowContextColumn;

            foreach (CodeStringResultItem item in value) {
                DataGridViewKeyValueRow<CodeStringResultItem> row = new DataGridViewKeyValueRow<CodeStringResultItem>();
                row.DataSourceItem = item;                

                DataGridViewCheckBoxCell checkCell = new DataGridViewCheckBoxCell();
                checkCell.Value = false;
                row.Cells.Add(checkCell);

                DataGridViewTextBoxCell locProbCell = new DataGridViewTextBoxCell();
                row.Cells.Add(locProbCell);

                DataGridViewTextBoxCell lineCell = new DataGridViewTextBoxCell();
                lineCell.Value = item.ReplaceSpan.iStartLine + 1;
                row.Cells.Add(lineCell);                

                DataGridViewComboBoxCell keyCell = new DataGridViewComboBoxCell();
                
                // add key name suggestions
                foreach (string key in item.GetKeyNameSuggestions()) {
                    keyCell.Items.Add(key);
                    if (keyCell.Value == null) // add first suggestion as default value
                        keyCell.Value = key;
                }

                row.Cells.Add(keyCell);

                DataGridViewTextBoxCell valueCell = new DataGridViewTextBoxCell();
                valueCell.Value = item.Value;
                row.Cells.Add(valueCell);                

                DataGridViewTextBoxCell sourceCell = new DataGridViewTextBoxCell();
                sourceCell.Value = item.SourceItem.Name;
                row.Cells.Add(sourceCell);

                DataGridViewComboBoxCell destinationCell = new DataGridViewComboBoxCell();

                // add possible ResX files as destinations
                destinationCell.Items.AddRange(CreateDestinationOptions(destinationCell, item.SourceItem));
                if (destinationCell.Items.Count > 0)
                    destinationCell.Value = destinationCell.Items[0].ToString();
                row.Cells.Add(destinationCell);
                
                DataGridViewDynamicWrapCell contextCell = new DataGridViewDynamicWrapCell();
                contextCell.Value = item.Context;
                contextCell.RelativeLine = item.ContextRelativeLine;
                contextCell.FullText = item.Context;
                contextCell.SetWrapContents(false);
                row.Cells.Add(contextCell);
                                    
                DataGridViewTextBoxCell cell = new DataGridViewTextBoxCell();
                row.Cells.Add(cell);
                Rows.Add(row);

                locProbCell.ReadOnly = true;
                valueCell.ReadOnly = false;
                sourceCell.ReadOnly = true;
                lineCell.ReadOnly = true;
                contextCell.ReadOnly = true;
                
                Validate(row);
            }            
            
            this.ClearSelection();            
            this.ResumeLayout();            
            this.OnResize(null);
            
            parentToolPanel.ResetFilterSettings(); // reset filter according to settings
            UpdateCheckHeader();

            // perform sorting
            if (SortedColumn != null) {
                Sort(SortedColumn, SortOrder == SortOrder.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending);
            }
        }

        /// <summary>
        /// Recalculates localization probability of all rows with given criteria set
        /// </summary>
        /// <param name="criteria">Criteria set</param>
        /// <param name="changeChecks">True if checkboxes should be (un)checked according to localization probability value</param>
        public void RecalculateLocProbability(Dictionary<string, AbstractLocalizationCriterion> criteria, bool changeChecks) {
            if (Rows.Count == 0) return;
            lastSetCriteria = criteria;

            foreach (DataGridViewKeyValueRow<CodeStringResultItem> row in Rows) {
                UpdateLocProbability(criteria, row, changeChecks); // update loc. probability for the row
            }
          
            UpdateCheckHeader(); 
        }

        /// <summary>
        /// Applies given criterion action to rows satisfying given criterion
        /// </summary>        
        public void ApplyFilterAction(AbstractLocalizationCriterion crit, LocalizationCriterionAction2 act) {
            if (crit == null) throw new ArgumentNullException("crit");

            List<DataGridViewRow> toBeDeletedRows = new List<DataGridViewRow>(); // list of rows to be deleted

            foreach (DataGridViewKeyValueRow<CodeStringResultItem> row in Rows) {
                bool oldCheckValue = (bool)row.Cells[CheckBoxColumnName].Value;
                bool newCheckValue = oldCheckValue;     
                var evalResult = crit.Eval(row.DataSourceItem); // criterion evaluation result

                if (evalResult == true) { // row satisfies the criterion
                    if (act == LocalizationCriterionAction2.CHECK || act == LocalizationCriterionAction2.CHECK_REMOVE) { // check the row
                        row.Cells[CheckBoxColumnName].Tag = row.Cells[CheckBoxColumnName].Value = true;
                        newCheckValue = true;
                    } else if (act == LocalizationCriterionAction2.UNCHECK) {
                        row.Cells[CheckBoxColumnName].Tag = row.Cells[CheckBoxColumnName].Value = false; // uncheck the row
                        newCheckValue = false;
                    } else if (act == LocalizationCriterionAction2.REMOVE) {
                        row.Cells[CheckBoxColumnName].Tag = row.Cells[CheckBoxColumnName].Value = false;
                        toBeDeletedRows.Add(row); // add row to the list of rows to be deleted
                        newCheckValue = false;
                    }                    
                } else if (evalResult == false && act == LocalizationCriterionAction2.CHECK_REMOVE) { 
                    toBeDeletedRows.Add(row); 
                    newCheckValue = false;
                }

                ChangeRowCheckState(row, oldCheckValue, newCheckValue); // change row check state
            }


            foreach (DataGridViewKeyValueRow<CodeStringResultItem> row in toBeDeletedRows) {
                ConflictResolver.TryAdd(row.Key, null, row); // remove the row from conflict resolver
                row.Cells[KeyColumnName].Tag = null;
                row.ErrorText = null;
                Rows.Remove(row);
                
                removedRows.Add(row); // add to the list of remebered rows
            }

            UpdateCheckHeader();
        }

        #endregion

        #region overridable members

        /// <summary>
        /// Initialize grid GUI
        /// </summary>
        protected override void InitializeColumns() {
            base.InitializeColumns();

            DataGridViewComboBoxColumn locProbColumn = new DataGridViewComboBoxColumn();
            locProbColumn.MinimumWidth = 40;
            locProbColumn.HeaderText = "";
            locProbColumn.Width = 40;
            locProbColumn.Name = LocProbColumnName;
            locProbColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            locProbColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            this.Columns.Insert(1, locProbColumn);
            
            DataGridViewComboBoxColumn keyColumn = new DataGridViewComboBoxColumn();
            keyColumn.MinimumWidth = 150;
            keyColumn.HeaderText = "Resource Key";
            keyColumn.Name = KeyColumnName;
            keyColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            this.Columns.Insert(3, keyColumn);

            DataGridViewTextBoxColumn valueColumn = new DataGridViewTextBoxColumn();
            valueColumn.MinimumWidth = 250;
            valueColumn.HeaderText = "Resource Value";
            valueColumn.Name = ValueColumnName;
            valueColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            this.Columns.Insert(4, valueColumn);

            DataGridViewTextBoxColumn sourceColumn = new DataGridViewTextBoxColumn();
            sourceColumn.MinimumWidth = 150;
            sourceColumn.HeaderText = "Source File";
            sourceColumn.Name = "SourceItem";
            this.Columns.Insert(5, sourceColumn);

            DataGridViewComboBoxColumn destinationColumn = new DataGridViewComboBoxColumn();
            destinationColumn.MinimumWidth = 250;
            destinationColumn.HeaderText = "Destination File";
            destinationColumn.Name = DestinationColumnName;
            destinationColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            destinationColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            this.Columns.Insert(6, destinationColumn);

            DataGridViewColumn column = new DataGridViewColumn();
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.Columns.Add(column);
        }

        /// <summary>
        /// Returns result item associated with the given row, initialized with row values
        /// </summary>        
        protected override CodeStringResultItem GetResultItemFromRow(DataGridViewRow originalRow) {
            if (originalRow == null) throw new ArgumentNullException("originalRow");

            DataGridViewKeyValueRow<CodeStringResultItem> row = originalRow as DataGridViewKeyValueRow<CodeStringResultItem>;

            CodeStringResultItem item = row.DataSourceItem;
            if (CheckBoxColumnName != null && Columns.Contains(CheckBoxColumnName)) {
                item.MoveThisItem = (bool)(row.Cells[CheckBoxColumnName].Value);
            } else {
                item.MoveThisItem = true;
            }

            item.Key = row.Key;
            item.Value = row.Value;

            if (DestinationColumnName != null && Columns.Contains(DestinationColumnName)) {
                string dest = (string)row.Cells[DestinationColumnName].Value;
                if (!string.IsNullOrEmpty(dest) && resxItemsCache.ContainsKey(dest))
                    item.DestinationItem = resxItemsCache[dest];
                else
                    item.DestinationItem = null;
            } else {
                item.DestinationItem = null;
            }

            item.ErrorText = row.ErrorText;

            row.DataSourceItem = item;
            return item;
        }

        protected override void OnCellEndEdit(DataGridViewCellEventArgs e) {
            try {
                if (Columns[e.ColumnIndex].Name == KeyColumnName) {
                    if (valueAdded) { // new key name was created
                        DataGridViewComboBoxCell cell = (DataGridViewComboBoxCell)Rows[e.RowIndex].Cells[KeyColumnName];
                        cell.Value = cell.Items[0];
                        valueAdded = false;
                    }
                }

                // update localization probability
                UpdateLocProbability(lastSetCriteria, (DataGridViewKeyValueRow<CodeStringResultItem>)Rows[e.RowIndex], false);

                base.OnCellEndEdit(e);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Validates given row and updates its error messages
        /// </summary>        
        protected override void Validate(DataGridViewKeyValueRow<CodeStringResultItem> row) {
            object dest = row.Cells[DestinationColumnName].Value;
            bool existsSameValue = false;             

            if (dest == null) { // no destination file was selected
                row.ErrorMessages.Add(NoDestinationFileError);
            } else {
                row.ErrorMessages.Remove(NoDestinationFileError);

                ResXProjectItem resxItem = resxItemsCache[dest.ToString()];
                if (!resxItem.IsLoaded) {
                    resxItem.Load(); // load the ResX file
                    VLDocumentViewsManager.SetFileReadonly(resxItem.InternalProjectItem.GetFullPath(), true); // lock it
                    loadedItems.Add(resxItem);
                }

                string key = row.Key;
                string value = row.Value;
                                
                CONTAINS_KEY_RESULT keyConflict = resxItem.GetKeyConflictType(key, value); // get conflict type
                switch (keyConflict) {
                    case CONTAINS_KEY_RESULT.EXISTS_WITH_SAME_VALUE:                        
                        row.ErrorMessages.Remove(DuplicateKeyError);
                        existsSameValue=true;
                        break;
                    case CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE:                        
                        row.ErrorMessages.Add(DuplicateKeyError);
                        break;
                    case CONTAINS_KEY_RESULT.DOESNT_EXIST:                        
                        row.ErrorMessages.Remove(DuplicateKeyError);
                        break;
                }

                string originalValue = (string)row.Cells[KeyColumnName].Tag;
                ((DestinationKeyValueConflictResolver)ConflictResolver).TryAdd(originalValue, key, row, resxItem, row.DataSourceItem.Language);
                if (originalValue == null) row.Cells[KeyColumnName].Tag = key;
            }            

            row.UpdateErrorSetDisplay(); // update error messages

            if (row.ErrorMessages.Count == 0) {
                if (existsSameValue) { // set background color according to conflict type
                    row.DefaultCellStyle.BackColor = ExistingKeySameValueColor;
                } else {
                    row.DefaultCellStyle.BackColor = Color.White;
                }
            }
        }
        
        #endregion      

        /// <summary>
        /// Recalculate localization probability for given row, using given criteria
        /// </summary>        
        private void UpdateLocProbability(Dictionary<string, AbstractLocalizationCriterion> criteria, DataGridViewKeyValueRow<CodeStringResultItem> row, bool changeChecks) {
            int newLocProb = GetResultItemFromRow(row).GetLocalizationProbability(criteria);
            row.Cells[LocProbColumnName].Tag = newLocProb;
            row.Cells[LocProbColumnName].Value = newLocProb + "%";

            if (changeChecks) { // row should be (un)checked according to localization probability 
                bool isChecked = (bool)row.Cells[CheckBoxColumnName].Value;
                bool willBeChecked = (newLocProb >= AbstractLocalizationCriterion.TRESHOLD_LOC_PROBABILITY);
                row.Cells[CheckBoxColumnName].Tag = row.Cells[CheckBoxColumnName].Value = willBeChecked;

                ChangeRowCheckState(row, isChecked, willBeChecked);
            }
        }

        /// <summary>
        /// Called when settings "key name policy" changed - must re-evaluate keys
        /// </summary>
        private void Instance_RevalidationRequested() {
            if (!this.Visible) return;

            foreach (DataGridViewKeyValueRow<CodeStringResultItem> row in Rows) {
                if (row.IsNewRow) continue;

                Validate(row);
            }
        }        

        private void OnRowDoubleClick(object sender, DataGridViewCellEventArgs e) {
            if (HighlightRequired != null && e.RowIndex >= 0) {
                HighlightRequired(this, new CodeResultItemEventArgs() {
                    Item = (Rows[e.RowIndex] as DataGridViewKeyValueRow<CodeStringResultItem>).DataSourceItem
                });
            }
        }
        
        /// <summary>
        /// Returns possible destination files for given project item
        /// </summary>        
        private DataGridViewComboBoxCell.ObjectCollection CreateDestinationOptions(DataGridViewComboBoxCell cell, ProjectItem item) {
            if (item == null) throw new ArgumentNullException("item");

            if (!destinationItemsCache.ContainsKey(item.ContainingProject)) {                
                DataGridViewComboBoxCell.ObjectCollection resxItems = new DataGridViewComboBoxCell.ObjectCollection(cell);
                foreach (ResXProjectItem projectItem in item.ContainingProject.GetResXItemsAround(true, false)) {
                    if (!string.IsNullOrEmpty(projectItem.Class) && !string.IsNullOrEmpty(projectItem.Namespace)) {
                        string key = projectItem.ToString();
                        resxItems.Add(key);
                        if (!resxItemsCache.ContainsKey(key)) resxItemsCache.Add(key, projectItem);
                    }
                }
                destinationItemsCache.Add(item.ContainingProject, resxItems);
            }

            return destinationItemsCache[item.ContainingProject];
        }
                
        /// <summary>
        /// Used to determine whether new value was added to "key" column combo box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BatchMoveToResourcesToolPanel_CellValidating(object sender, DataGridViewCellValidatingEventArgs e) {
            valueAdded = false;
            if (Columns[e.ColumnIndex].Name == KeyColumnName) {
                var comboBoxCell = Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewComboBoxCell;
                if (!comboBoxCell.Items.Contains(e.FormattedValue)) {
                    comboBoxCell.Items.Insert(0, e.FormattedValue);
                    valueAdded = true;
                }
            }            
        }

        /// <summary>
        /// Called during sort process, extracts compare data from cells and compares them; this needs to be done only for "localization probability" column,
        /// other columns are handled automatically
        /// </summary>        
        private void BatchMoveToResourcesToolGrid_SortCompare(object sender, DataGridViewSortCompareEventArgs e) {
            if (e.Column.Name == LocProbColumnName) {
                int val1;
                if (!int.TryParse(((string)e.CellValue1).TrimEnd('%'), out val1)) val1 = 0;

                int val2;
                if (!int.TryParse(((string)e.CellValue2).TrimEnd('%'), out val2)) val2 = 0;

                e.SortResult = val1.CompareTo(val2);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Processes keys used for navigating in the <see cref="T:System.Windows.Forms.DataGridView" />.
        /// </summary>
        /// <param name="e">Contains information about the key that was pressed.</param>
        /// <returns>
        /// true if the key was processed; otherwise, false.
        /// </returns>
        protected override bool ProcessDataGridViewKey(KeyEventArgs e) {            
            if (this.IsCurrentCellInEditMode) {
                // prevents GridView default actions for these keys
                if (e.KeyData == Keys.Left || e.KeyData == Keys.Right || e.KeyData==Keys.Home || e.KeyData==Keys.End) {
                    return false;
                } else return base.ProcessDataGridViewKey(e);
            } else return base.ProcessDataGridViewKey(e);
        }

        /// <summary>
        /// Modifies display of "key" column combo box
        /// </summary>        
        private void BatchMoveToResourcesToolPanel_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e) {            
            if (Columns[CurrentCell.ColumnIndex].Name == KeyColumnName && e.Control is ComboBox) {
                ComboBox box = e.Control as ComboBox;
                box.DropDownStyle = ComboBoxStyle.DropDown;                     
            }                        
        }        

        /// <summary>
        /// Context menu is shown
        /// </summary>        
        private void ContextMenu_Popup(object sender, EventArgs e) {
            try {
                destinationContextMenu.MenuItems.Clear(); // remove all possible destinations items
                if (SelectedRows.Count == 0) return;

                // sets possible destinations to intersection of possible destinations of all selected rows
                HashSet<string> options = new HashSet<string>();
                var destCell = SelectedRows[0].Cells[DestinationColumnName] as DataGridViewComboBoxCell;
                foreach (string dest in destCell.Items) {
                    if (!string.IsNullOrEmpty(dest)) {
                        options.Add(dest);
                    }
                }

                foreach (DataGridViewKeyValueRow<CodeStringResultItem> row in SelectedRows) {
                    DataGridViewComboBoxCell cell = (DataGridViewComboBoxCell)row.Cells[DestinationColumnName];
                    options.IntersectWith(cell.Items.Cast<string>());
                }

                // add the intersection to the menu
                destinationContextMenu.Enabled = options.Count > 0;
                foreach (string item in options) {
                    MenuItem menuItem = new MenuItem(item);
                    menuItem.Tag = resxItemsCache[item];
                    menuItem.Click += new EventHandler((o, a) => { SetDestinationOfSelected((ResXProjectItem)(o as MenuItem).Tag); });
                    destinationContextMenu.MenuItems.Add(menuItem);
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }        

        /// <summary>
        /// Sets destination file of selected rows to given ResX file
        /// </summary>        
        private void SetDestinationOfSelected(ResXProjectItem item) {
            try {
                if (item == null) throw new ArgumentNullException("item");

                foreach (DataGridViewKeyValueRow<CodeStringResultItem> row in SelectedRows) {
                    row.Cells[DestinationColumnName].Value = item.ToString();
                    Validate(row);
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Returns name of the column with checkbox
        /// </summary>
        public override string CheckBoxColumnName {
            get { return "MoveThisItem"; }
        }

        /// <summary>
        /// Returns name of the column used to hold key
        /// </summary>
        public override string KeyColumnName {
            get { return "Key"; }
        }

        /// <summary>
        /// Returns name of the column used to hold value
        /// </summary>
        public override string ValueColumnName {
            get { return "Value"; }
        }
        
        public string LocProbColumnName {
            get { return "LocalizationProbability"; }
        }

        public string DestinationColumnName {
            get { return "Destination"; }
        }
        
    }

    
}
