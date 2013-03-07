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

    internal sealed class BatchMoveToResourcesToolGrid : AbstractKeyValueGridView<CodeStringResultItem>, IHighlightRequestSource {

        public event EventHandler<CodeResultItemEventArgs> HighlightRequired;
        private Dictionary<Project, DataGridViewComboBoxCell.ObjectCollection> destinationItemsCache = new Dictionary<Project, DataGridViewComboBoxCell.ObjectCollection>();
        private Dictionary<string, ResXProjectItem> resxItemsCache = new Dictionary<string, ResXProjectItem>();        
        private List<ResXProjectItem> loadedItems = new List<ResXProjectItem>();
        private bool valueAdded = false;
        private BatchMoveToResourcesToolPanel parentToolPanel;
        private MenuItem destinationContextMenu;

        public BatchMoveToResourcesToolGrid(BatchMoveToResourcesToolPanel panel)
            : base(SettingsObject.Instance.ShowFilterContext, new DestinationKeyValueConflictResolver()) {
            this.parentToolPanel = panel;
            this.MultiSelect = true;
            this.ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable;

            this.EditingControlShowing += new DataGridViewEditingControlShowingEventHandler(BatchMoveToResourcesToolPanel_EditingControlShowing);
            this.CellValidating += new DataGridViewCellValidatingEventHandler(BatchMoveToResourcesToolPanel_CellValidating);
            this.CellDoubleClick += new DataGridViewCellEventHandler(OnRowDoubleClick);
            this.SortCompare += new DataGridViewSortCompareEventHandler(BatchMoveToResourcesToolGrid_SortCompare);
            this.MouseUp += new MouseEventHandler(OnContextMenuShow);

            DataGridViewKeyValueRow<CodeReferenceResultItem> template = new DataGridViewKeyValueRow<CodeReferenceResultItem>();
            template.MinimumHeight = 24;
            this.RowTemplate = template;

            ContextMenu contextMenu = new ContextMenu();
            
            MenuItem stateMenu = new MenuItem("State");
            stateMenu.MenuItems.Add("Checked", new EventHandler((o, e) => { setCheckStateOfSelected(true); }));
            stateMenu.MenuItems.Add("Unchecked", new EventHandler((o, e) => { setCheckStateOfSelected(false); }));
            contextMenu.MenuItems.Add(stateMenu);

            destinationContextMenu = new MenuItem("Common destination");
            contextMenu.MenuItems.Add(destinationContextMenu);
            contextMenu.Popup += new EventHandler(ContextMenu_Popup);

            this.ContextMenu = contextMenu;
        }        

        #region public members

        public void Unload() {
            foreach (var item in loadedItems)
                item.Unload();
            loadedItems.Clear();
        }

        public override void SetData(List<CodeStringResultItem> value) {
            base.SetData(value);

            this.Rows.Clear();
            destinationItemsCache.Clear();
            resxItemsCache.Clear();
            loadedItems.Clear();            
            CheckedRowsCount = 0;            
            SuspendLayout();            

            if (Columns.Contains(ContextColumnName)) Columns[ContextColumnName].Visible = SettingsObject.Instance.ShowFilterContext;

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
                
                foreach (string key in item.GetKeyNameSuggestions()) {
                    keyCell.Items.Add(key);
                    if (keyCell.Value == null)
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
            
            parentToolPanel.ResetFilterSettings();
            UpdateCheckHeader();

            if (SortedColumn != null) {
                Sort(SortedColumn, SortOrder == SortOrder.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending);
            }
        }

        private Dictionary<string, AbstractLocalizationCriterion> lastSetCriteria;
        public void RecalculateLocProbability(Dictionary<string, AbstractLocalizationCriterion> criteria, bool changeChecks) {
            if (Rows.Count == 0) return;
            lastSetCriteria = criteria;

            foreach (DataGridViewKeyValueRow<CodeStringResultItem> row in Rows) {
                updateLocProbability(criteria, row, changeChecks);
            }
          
            UpdateCheckHeader();
        }        

        #endregion

        #region overridable members

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

        protected override CodeStringResultItem GetResultItemFromRow(DataGridViewRow originalRow) {
            DataGridViewKeyValueRow<CodeStringResultItem> row = originalRow as DataGridViewKeyValueRow<CodeStringResultItem>;

            CodeStringResultItem item = row.DataSourceItem;
            item.MoveThisItem = (bool)(row.Cells[CheckBoxColumnName].Value);
            item.Key = row.Key;
            item.Value = row.Value;

            string dest = (string)row.Cells[DestinationColumnName].Value;
            if (!string.IsNullOrEmpty(dest) && resxItemsCache.ContainsKey(dest))
                item.DestinationItem = resxItemsCache[dest];
            else
                item.DestinationItem = null;

            item.ErrorText = row.ErrorText;

            row.DataSourceItem = item;
            return item;
        }

        protected override void OnCellEndEdit(DataGridViewCellEventArgs e) {
            if (Columns[e.ColumnIndex].Name == KeyColumnName) {
                if (valueAdded) {
                    DataGridViewComboBoxCell cell = (DataGridViewComboBoxCell)Rows[e.RowIndex].Cells[KeyColumnName];
                    cell.Value = cell.Items[0];
                    valueAdded = false;
                }
            }
            updateLocProbability(lastSetCriteria, (DataGridViewKeyValueRow<CodeStringResultItem>)Rows[e.RowIndex], false);
            base.OnCellEndEdit(e);
        }

        protected override void Validate(DataGridViewKeyValueRow<CodeStringResultItem> row) {
            object dest = row.Cells[DestinationColumnName].Value;
            bool existsSameValue = false; 
            string destError = "Destination file not set";
            if (dest == null) {
                row.ErrorSet.Add(destError);
            } else {
                row.ErrorSet.Remove(destError);

                ResXProjectItem resxItem = resxItemsCache[dest.ToString()];
                if (!resxItem.IsLoaded) {
                    resxItem.Load();
                    VLDocumentViewsManager.SetFileReadonly(resxItem.InternalProjectItem.GetFullPath(), true);
                    loadedItems.Add(resxItem);
                }

                string key = row.Key;
                string value = row.Value;
                
                string errorText = "Duplicate key entry - key is already present in resource file with different value";
                CONTAINS_KEY_RESULT keyConflict = resxItem.StringKeyInConflict(key, value);
                switch (keyConflict) {
                    case CONTAINS_KEY_RESULT.EXISTS_WITH_SAME_VALUE:                        
                        row.ErrorSet.Remove(errorText);
                        existsSameValue=true;
                        break;
                    case CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE:                        
                        row.ErrorSet.Add(errorText);
                        break;
                    case CONTAINS_KEY_RESULT.DOESNT_EXIST:                        
                        row.ErrorSet.Remove(errorText);
                        break;
                }

                string originalValue = (string)row.Cells[KeyColumnName].Tag;
                ((DestinationKeyValueConflictResolver)ConflictResolver).TryAdd(originalValue, key, row, resxItem);
                if (originalValue == null) row.Cells[KeyColumnName].Tag = key;
            }            

            row.ErrorSetUpdate();

            if (row.ErrorSet.Count == 0) {
                if (existsSameValue) {
                    row.DefaultCellStyle.BackColor = ExistingKeySameValueColor;
                } else {
                    row.DefaultCellStyle.BackColor = Color.White;
                }
            }
        }
        
        #endregion      

        private void updateLocProbability(Dictionary<string, AbstractLocalizationCriterion> criteria, DataGridViewKeyValueRow<CodeStringResultItem> row, bool changeChecks) {
            int newLocProb = GetResultItemFromRow(row).GetLocalizationProbability(criteria);
            row.Cells[LocProbColumnName].Tag = newLocProb;
            row.Cells[LocProbColumnName].Value = newLocProb + "%";

            if (changeChecks) {
                bool isChecked = (bool)row.Cells[CheckBoxColumnName].Value;
                bool willBeChecked = (newLocProb >= AbstractLocalizationCriterion.TRESHOLD_LOC_PROBABILITY);
                row.Cells[CheckBoxColumnName].Tag = row.Cells[CheckBoxColumnName].Value = willBeChecked;

                if (isChecked && !willBeChecked) CheckedRowsCount--;
                if (!isChecked && willBeChecked) CheckedRowsCount++;
            }
        }    

        private void OnRowDoubleClick(object sender, DataGridViewCellEventArgs e) {
            if (HighlightRequired != null && e.RowIndex >= 0) {
                HighlightRequired(this, new CodeResultItemEventArgs() {
                    Item = (Rows[e.RowIndex] as DataGridViewKeyValueRow<CodeStringResultItem>).DataSourceItem
                });
            }
        }
        
        private DataGridViewComboBoxCell.ObjectCollection CreateDestinationOptions(DataGridViewComboBoxCell cell, ProjectItem item) {
            if (!destinationItemsCache.ContainsKey(item.ContainingProject)) {                
                DataGridViewComboBoxCell.ObjectCollection resxItems = new DataGridViewComboBoxCell.ObjectCollection(cell);
                foreach (ResXProjectItem projectItem in item.ContainingProject.GetResXItemsAround(item, true, false)) {
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

        protected override bool ProcessDataGridViewKey(KeyEventArgs e) {            
            if (this.IsCurrentCellInEditMode) {
                if (e.KeyData == Keys.Left || e.KeyData == Keys.Right || e.KeyData==Keys.Home || e.KeyData==Keys.End) {
                    return false;
                } else return base.ProcessDataGridViewKey(e);
            } else return base.ProcessDataGridViewKey(e);
        }

        private void BatchMoveToResourcesToolPanel_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e) {
            if (Columns[CurrentCell.ColumnIndex].Name == KeyColumnName && e.Control is ComboBox) {
                ComboBox box = e.Control as ComboBox;
                box.DropDownStyle = ComboBoxStyle.DropDown;                     
            }                        
        }        

        private void ContextMenu_Popup(object sender, EventArgs e) {
            destinationContextMenu.MenuItems.Clear();
            if (SelectedRows.Count == 0) return;

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

            destinationContextMenu.Enabled = options.Count > 0;
            foreach (string item in options) {
                MenuItem menuItem = new MenuItem(item);
                menuItem.Tag = resxItemsCache[item];
                menuItem.Click += new EventHandler((o, a) => { setDestinationOfSelected((ResXProjectItem)(o as MenuItem).Tag); });                
                destinationContextMenu.MenuItems.Add(menuItem);
            }
        }        

        private void setDestinationOfSelected(ResXProjectItem item) {
            foreach (DataGridViewKeyValueRow<CodeStringResultItem> row in SelectedRows) {
                row.Cells[DestinationColumnName].Value = item.ToString();
                Validate(row);
            }            
        }

        public override string CheckBoxColumnName {
            get { return "MoveThisItem"; }
        }

        public override string KeyColumnName {
            get { return "Key"; }
        }

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
