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

namespace VisualLocalizer.Gui {

    internal sealed class BatchMoveToResourcesToolGrid : AbstractKeyValueGridView<CodeStringResultItem>, IHighlightRequestSource {

        public event EventHandler<CodeResultItemEventArgs> HighlightRequired;
        private Dictionary<Project, DataGridViewComboBoxCell.ObjectCollection> destinationItemsCache = new Dictionary<Project, DataGridViewComboBoxCell.ObjectCollection>();
        private Dictionary<string, ResXProjectItem> resxItemsCache = new Dictionary<string, ResXProjectItem>();        
        private List<ResXProjectItem> loadedItems = new List<ResXProjectItem>();
        private bool valueAdded = false;

        public BatchMoveToResourcesToolGrid() : base(new DestinationKeyValueConflictResolver()) {                        
            this.EditingControlShowing += new DataGridViewEditingControlShowingEventHandler(BatchMoveToResourcesToolPanel_EditingControlShowing);
            this.CellValidating += new DataGridViewCellValidatingEventHandler(BatchMoveToResourcesToolPanel_CellValidating);
            this.CellDoubleClick += new DataGridViewCellEventHandler(OnRowDoubleClick);

            DataGridViewKeyValueRow<CodeReferenceResultItem> template = new DataGridViewKeyValueRow<CodeReferenceResultItem>();
            template.MinimumHeight = 24;
            this.RowTemplate = template;
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

            foreach (CodeStringResultItem item in value) {
                DataGridViewKeyValueRow<CodeStringResultItem> row = new DataGridViewKeyValueRow<CodeStringResultItem>();
                row.DataSourceItem = item;

                DataGridViewCheckBoxCell checkCell = new DataGridViewCheckBoxCell();
                checkCell.Value = item.MoveThisItem;
                row.Cells.Add(checkCell);

                DataGridViewTextBoxCell lineCell = new DataGridViewTextBoxCell();
                lineCell.Value = item.ReplaceSpan.iStartLine + 1;
                row.Cells.Add(lineCell);

                DataGridViewComboBoxCell keyCell = new DataGridViewComboBoxCell();
                foreach (string key in item.Value.CreateKeySuggestions(item.NamespaceElement == null ? null : (item.NamespaceElement as CodeNamespace).FullName, item.ClassOrStructElementName, item.VariableElementName == null ? item.MethodElementName : item.VariableElementName)) {
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
                destinationCell.Items.AddRange(CreateDestinationOptions(destinationCell, item.SourceItem.ContainingProject));
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

                valueCell.ReadOnly = false;
                sourceCell.ReadOnly = true;
                lineCell.ReadOnly = true;
                contextCell.ReadOnly = true;

                bool checkRow = TestFilterRow(row);
                row.Cells[CheckBoxColumnName].Value = checkRow;
                if (checkRow) CheckedRowsCount++;

                Validate(row);
            }

            UpdateCheckHeader();

            this.ClearSelection();            
            this.ResumeLayout();            
            this.OnResize(null);
        }

        private bool TestFilterRow(DataGridViewKeyValueRow<CodeStringResultItem> row) {
            bool check = true;

            if (check && IsRowCapitalsTest(row)) check = check && !SettingsObject.Instance.FilterOutCaps;
            if (check && IsRowNoLettersTest(row)) check = check && !SettingsObject.Instance.FilterOutNoLetters;
            if (check && IsRowUnlocalizableTest(row)) check = check && !SettingsObject.Instance.FilterOutUnlocalizable;
            if (check && IsRowVerbatimTest(row)) check = check && !SettingsObject.Instance.FilterOutVerbatim;
            if (check && IsRowMarkedWithUnlocCommentTest(row)) check = check && !SettingsObject.Instance.FilterOutSpecificComment;

            if (check) {
                foreach (var inst in SettingsObject.Instance.FilterRegexps) {
                    if (IsRowMatchingRegexpInstance(row, inst)) {
                        check = false;
                        break;
                    }
                }
            }

            return check;
        }

        public void CheckByPredicate(Predicate<DataGridViewKeyValueRow<CodeStringResultItem>> test, bool checkResult) {
            foreach (DataGridViewKeyValueRow<CodeStringResultItem> row in Rows) {                
                if (test(row)) {
                    bool oldValue = (bool)row.Cells[CheckBoxColumnName].Value;
                    row.Cells[CheckBoxColumnName].Value = checkResult;
                    if (oldValue && !checkResult) CheckedRowsCount--;
                    if (!oldValue && checkResult) CheckedRowsCount++;
                }
            }
            UpdateCheckHeader();
        }

        public void CheckByPredicate(Func<DataGridViewKeyValueRow<CodeStringResultItem>, SettingsObject.RegexpInstance, bool> test, SettingsObject.RegexpInstance regexInstance) {
            foreach (DataGridViewKeyValueRow<CodeStringResultItem> row in Rows) {
                bool oldValue = (bool)row.Cells[CheckBoxColumnName].Value;
                if (test(row, regexInstance)) {
                    row.Cells[CheckBoxColumnName].Value = false;
                    if (oldValue) CheckedRowsCount--;
                } 
            }
            UpdateCheckHeader();
        }

        public bool IsRowCapitalsTest(DataGridViewKeyValueRow<CodeStringResultItem> row) {
            string value = (string)row.Cells[ValueColumnName].Value;
            if (value == null) return false;

            bool onlyCaps = true;
            foreach (char c in value)
                if (!char.IsUpper(c) && !char.IsSymbol(c) && !char.IsPunctuation(c)) {
                    onlyCaps = false;
                    break;
                }

            return onlyCaps;
        }

        public bool IsRowNoLettersTest(DataGridViewKeyValueRow<CodeStringResultItem> row) {
            string value = (string)row.Cells[ValueColumnName].Value;
            if (value == null) return true;

            bool containsLetter = false;
            foreach (char c in value)
                if (char.IsLetter(c)) {
                    containsLetter = true;
                    break;
                }
            return !containsLetter;
        }

        public bool IsRowUnlocalizableTest(DataGridViewKeyValueRow<CodeStringResultItem> row) {
            return row.DataSourceItem.IsWithinLocalizableFalse;
        }

        public bool IsRowVerbatimTest(DataGridViewKeyValueRow<CodeStringResultItem> row) {
            return row.DataSourceItem.WasVerbatim;
        }

        public bool IsRowMarkedWithUnlocCommentTest(DataGridViewKeyValueRow<CodeStringResultItem> row) {
            return row.DataSourceItem.IsMarkedWithUnlocalizableComment;
        }

        public bool IsRowMatchingRegexpInstance(DataGridViewKeyValueRow<CodeStringResultItem> row, SettingsObject.RegexpInstance regexpInstance) {
            string value = (string)row.Cells[ValueColumnName].Value;
            return Regex.IsMatch(value, regexpInstance.Regexp) == regexpInstance.MustMatch;
        }

        #endregion

        #region overridable members

        protected override void InitializeColumns() {
            base.InitializeColumns();

            DataGridViewComboBoxColumn keyColumn = new DataGridViewComboBoxColumn();
            keyColumn.MinimumWidth = 150;
            keyColumn.HeaderText = "Resource Key";
            keyColumn.Name = KeyColumnName;
            keyColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            this.Columns.Insert(2, keyColumn);

            DataGridViewTextBoxColumn valueColumn = new DataGridViewTextBoxColumn();
            valueColumn.MinimumWidth = 250;
            valueColumn.HeaderText = "Resource Value";
            valueColumn.Name = ValueColumnName;
            valueColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            this.Columns.Insert(3, valueColumn);

            DataGridViewTextBoxColumn sourceColumn = new DataGridViewTextBoxColumn();
            sourceColumn.MinimumWidth = 150;
            sourceColumn.HeaderText = "Source File";
            sourceColumn.Name = "SourceItem";
            this.Columns.Insert(4, sourceColumn);

            DataGridViewComboBoxColumn destinationColumn = new DataGridViewComboBoxColumn();
            destinationColumn.MinimumWidth = 250;
            destinationColumn.HeaderText = "Destination File";
            destinationColumn.Name = "DestinationItem";
            destinationColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            destinationColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            this.Columns.Insert(5, destinationColumn);

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

            string dest = (string)row.Cells["DestinationItem"].Value;
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
            base.OnCellEndEdit(e);
        }

        protected override void Validate(DataGridViewKeyValueRow<CodeStringResultItem> row) {
            object dest = row.Cells["DestinationItem"].Value;
            bool existsSameValue = false; 
            string destError = "Destination file not set";
            if (dest == null) {
                row.ErrorSet.Add(destError);
            } else {
                row.ErrorSet.Remove(destError);

                ResXProjectItem resxItem = resxItemsCache[dest.ToString()];
                if (!resxItem.IsLoaded) {
                    resxItem.Load();
                    VLDocumentViewsManager.SetFileReadonly(resxItem.InternalProjectItem.Properties.Item("FullPath").Value.ToString(), true);
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
            }

            base.Validate(row);

            if (row.ErrorSet.Count == 0) {
                if (existsSameValue) {
                    row.DefaultCellStyle.BackColor = ExistingKeySameValueColor;
                } else {
                    row.DefaultCellStyle.BackColor = Color.White;
                }
            }
        }
        
        #endregion

        private void OnRowDoubleClick(object sender, DataGridViewCellEventArgs e) {
            if (HighlightRequired != null && e.RowIndex >= 0) {
                HighlightRequired(this, new CodeResultItemEventArgs() {
                    Item = (Rows[e.RowIndex] as DataGridViewKeyValueRow<CodeStringResultItem>).DataSourceItem
                });
            }
        }
        
        private DataGridViewComboBoxCell.ObjectCollection CreateDestinationOptions(DataGridViewComboBoxCell cell, Project project) {
            if (!destinationItemsCache.ContainsKey(project)) {
                List<ProjectItem> items = project.GetFiles(ResXProjectItem.IsItemResX, true);
                DataGridViewComboBoxCell.ObjectCollection resxItems = new DataGridViewComboBoxCell.ObjectCollection(cell);
                foreach (ProjectItem projectItem in items) {
                    var resxItem = ResXProjectItem.ConvertToResXItem(projectItem, project);
                    resxItems.Add(resxItem.ToString());
                    resxItemsCache.Add(resxItem.ToStringValue, resxItem);
                }
                destinationItemsCache.Add(project, resxItems);
            }

            return destinationItemsCache[project];
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
        
        public override string CheckBoxColumnName {
            get { return "MoveThisItem"; }
        }

        public override string KeyColumnName {
            get { return "Key"; }
        }

        public override string ValueColumnName {
            get { return "Value"; }
        }        
    }

    
}
