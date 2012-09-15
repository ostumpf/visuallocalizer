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

namespace VisualLocalizer.Gui {

    internal sealed class CodeStringResultItemEventArgs : EventArgs {
        public CodeStringResultItem Item { get; set; }
    }

    internal sealed class BatchMoveToResourcesToolPanel : DataGridView {

        private DataGridViewCheckBoxHeaderCell checkHeader;
        public int CheckedRowsCount { get; private set; }
        private Dictionary<Project, DataGridViewComboBoxCell.ObjectCollection> destinationItemsCache = new Dictionary<Project, DataGridViewComboBoxCell.ObjectCollection>();
        private Dictionary<string, ResXProjectItem> resxItemsCache = new Dictionary<string, ResXProjectItem>();
        public event EventHandler<CodeStringResultItemEventArgs> ItemHighlightRequired;
        private List<ResXProjectItem> loadedItems = new List<ResXProjectItem>();

        public BatchMoveToResourcesToolPanel() {
            this.AutoGenerateColumns = false;
            this.AllowUserToAddRows = false;
            this.AllowUserToDeleteRows = false;
            this.AutoSize = true;
            this.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            this.MultiSelect = false;
            this.Dock = DockStyle.Fill;
            this.AllowUserToResizeRows = true;
            this.AllowUserToResizeColumns = true;
            this.CellBeginEdit += new DataGridViewCellCancelEventHandler(BatchMoveToResourcesToolPanel_CellBeginEdit);
            this.CellEndEdit += new DataGridViewCellEventHandler(BatchMoveToResourcesToolPanel_CellEndEdit);
            this.EditingControlShowing += new DataGridViewEditingControlShowingEventHandler(BatchMoveToResourcesToolPanel_EditingControlShowing);
            this.CellValidating += new DataGridViewCellValidatingEventHandler(BatchMoveToResourcesToolPanel_CellValidating);
            this.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.CellDoubleClick += new DataGridViewCellEventHandler(BatchMoveToResourcesToolPanel_CellDoubleClick);

            CheckedRowsCount = 0;

            DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn(false);
            checkColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            checkColumn.Width = 30;
            checkColumn.HeaderText = "";            
            checkColumn.Resizable = DataGridViewTriState.True;
            checkColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            checkColumn.Name = "MoveThisItem";            

            checkHeader = new DataGridViewCheckBoxHeaderCell();
            checkHeader.ThreeStates = true;
            checkHeader.Checked = true;            
            checkHeader.CheckBoxClicked += new EventHandler(checkHeader_CheckBoxClicked);
            checkColumn.HeaderCell = checkHeader;
            this.Columns.Add(checkColumn);

            DataGridViewComboBoxColumn keyColumn = new DataGridViewComboBoxColumn();
            keyColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            keyColumn.MinimumWidth = 150;
            keyColumn.HeaderText = "Resource Key";
            keyColumn.FillWeight = 1;
            keyColumn.Resizable = DataGridViewTriState.True;
            keyColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            keyColumn.Name = "Key";
            this.Columns.Add(keyColumn);

            DataGridViewTextBoxColumn valueColumn = new DataGridViewTextBoxColumn();
            valueColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            valueColumn.MinimumWidth = 250;
            valueColumn.HeaderText = "Resource Value";
            valueColumn.FillWeight = 1;
            valueColumn.Resizable = DataGridViewTriState.True;
            valueColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            valueColumn.Name = "Value";
            valueColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True; 
            this.Columns.Add(valueColumn);

            DataGridViewTextBoxColumn sourceColumn = new DataGridViewTextBoxColumn();
            sourceColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            sourceColumn.MinimumWidth = 150;
            sourceColumn.HeaderText = "Source File";
            sourceColumn.ReadOnly = true;
            sourceColumn.FillWeight = 1;
            sourceColumn.Resizable = DataGridViewTriState.True;
            sourceColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            sourceColumn.Name = "SourceItem";
            this.Columns.Add(sourceColumn);

            DataGridViewComboBoxColumn destinationColumn = new DataGridViewComboBoxColumn();
            destinationColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            destinationColumn.MinimumWidth = 250;
            destinationColumn.HeaderText = "Destination File";
            destinationColumn.FillWeight = 1;
            destinationColumn.Resizable = DataGridViewTriState.True;
            destinationColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            destinationColumn.Name = "DestinationItem";
            this.Columns.Add(destinationColumn);

        }

        private int? currentItemIndex = null;
        public CodeStringResultItem GetNextResultItem() {
            if (currentItemIndex == null)
                currentItemIndex = Rows.Count;

            currentItemIndex--;

            if (currentItemIndex < 0) {
                currentItemIndex = null;
                Rows.Clear();
                return null;
            } else {
                DataGridViewRow row = Rows[currentItemIndex.Value];
                CodeStringResultItem item = row.Tag as CodeStringResultItem;
                item.MoveThisItem = (bool)(row.Cells["MoveThisItem"].Value);
                if (item.MoveThisItem) {
                    item.Key = row.Cells["Key"].Value.ToString();
                    item.Value = row.Cells["Value"].Value.ToString();

                    if (!string.IsNullOrEmpty(row.ErrorText))
                        throw new InvalidOperationException(string.Format("on key \"{0}\": \"{1}\"", item.Key, row.ErrorText));

                    string dest = row.Cells["DestinationItem"].Value.ToString();
                    if (resxItemsCache.ContainsKey(dest)) {
                        item.DestinationItem = resxItemsCache[dest];
                    } else throw new InvalidOperationException(string.Format("Key \"{0}\" has no specified destination item.", item.Key));

                    row.Tag = item;
                    return item;
                } else {
                    row.Tag = item;
                    return GetNextResultItem();
                }                
            }
        }

        public void SetCurrentItemFinished(string errorText,string referenceText) {
            if (currentItemIndex == null || currentItemIndex < 0) throw new ArgumentException("currentItemIndex");

            Rows[currentItemIndex.Value].ErrorText = errorText;
            if (errorText == null) {
                CodeStringResultItem resultItem=Rows[currentItemIndex.Value].Tag as CodeStringResultItem;
                TextSpan currentReplaceSpan = resultItem.ReplaceSpan;

                int diff = currentReplaceSpan.iEndLine - currentReplaceSpan.iStartLine;
                for (int i = currentItemIndex.Value + 1; i < Rows.Count;i++ ) {
                    CodeStringResultItem item = (Rows[i].Tag as CodeStringResultItem);
                    item.AbsoluteCharOffset += referenceText.Length - resultItem.AbsoluteCharLength;

                    if (item.ReplaceSpan.iStartLine > currentReplaceSpan.iEndLine) {
                        TextSpan newSpan = new TextSpan();
                        newSpan.iEndIndex = item.ReplaceSpan.iEndIndex;
                        newSpan.iStartIndex = item.ReplaceSpan.iStartIndex;
                        newSpan.iEndLine = item.ReplaceSpan.iEndLine - diff;
                        newSpan.iStartLine = item.ReplaceSpan.iStartLine - diff;
                        item.ReplaceSpan = newSpan;                        
                    } else if (item.ReplaceSpan.iStartLine == currentReplaceSpan.iEndLine) {
                        TextSpan newSpan = new TextSpan();
                        newSpan.iStartIndex = currentReplaceSpan.iStartIndex + referenceText.Length + item.ReplaceSpan.iStartIndex - currentReplaceSpan.iEndIndex;
                        if (item.ReplaceSpan.iEndLine == item.ReplaceSpan.iStartLine) {
                            newSpan.iEndIndex = newSpan.iStartIndex + item.ReplaceSpan.iEndIndex - item.ReplaceSpan.iStartIndex;
                        } else {
                            newSpan.iEndIndex = item.ReplaceSpan.iEndIndex;
                        }
                        newSpan.iEndLine = item.ReplaceSpan.iEndLine - diff;
                        newSpan.iStartLine = item.ReplaceSpan.iStartLine - diff;
                        item.ReplaceSpan = newSpan;
                    }
                }

                Rows.RemoveAt(currentItemIndex.Value);
                CheckedRowsCount--;
                updateCheckHeader();
            }
        }

        public void SetData(List<CodeStringResultItem> value) {
            this.Rows.Clear();
            destinationItemsCache.Clear();
            resxItemsCache.Clear();
            loadedItems.Clear();

            foreach (CodeStringResultItem item in value) {
                DataGridViewRow row = new DataGridViewRow();
                row.Tag = item;

                DataGridViewCheckBoxCell checkCell = new DataGridViewCheckBoxCell();
                checkCell.Value = item.MoveThisItem;
                row.Cells.Add(checkCell);

                DataGridViewComboBoxCell keyCell = new DataGridViewComboBoxCell();
                foreach (string key in item.Value.CreateKeySuggestions(item.NamespaceElement == null  ? null : (item.NamespaceElement as CodeNamespace).FullName, item.ClassOrStructElementName, item.VariableElementName == null ? item.MethodElementName : item.VariableElementName)) {
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

                Rows.Add(row);

                valueCell.ReadOnly = false;
                sourceCell.ReadOnly = true;
                validate(row);
            }

            checkHeader.Checked = true;
            CheckedRowsCount = Rows.Count;
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

        private void BatchMoveToResourcesToolPanel_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e) {
            DataGridViewCell cell=(Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewCell);
            cell.Tag = cell.Value;
        }

        private void checkHeader_CheckBoxClicked(object sender, EventArgs e) {
            foreach (DataGridViewRow row in Rows) {
                row.Cells["MoveThisItem"].Value = checkHeader.Checked == true;
                row.Cells["MoveThisItem"].Tag = checkHeader.Checked == true;
            }
            CheckedRowsCount = checkHeader.Checked == true ? Rows.Count : 0;
        }
      
        private void BatchMoveToResourcesToolPanel_CellDoubleClick(object sender, DataGridViewCellEventArgs e) {
            if (ItemHighlightRequired != null) {
                ItemHighlightRequired(this, new CodeStringResultItemEventArgs() { Item = Rows[e.RowIndex].Tag as CodeStringResultItem });
            }
        }

        private bool valueAdded = false;
        private void BatchMoveToResourcesToolPanel_CellValidating(object sender, DataGridViewCellValidatingEventArgs e) {
            valueAdded = false;
            if (e.ColumnIndex == 1) {
                var comboBoxCell = Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewComboBoxCell;
                if (!comboBoxCell.Items.Contains(e.FormattedValue)) {
                    comboBoxCell.Items.Insert(0, e.FormattedValue);
                    valueAdded = true;
                }
            }
        }

        private void BatchMoveToResourcesToolPanel_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e) {
            if (CurrentCellAddress.X == 1 && e.Control is ComboBox) {
                ComboBox box = e.Control as ComboBox;
                box.DropDownStyle = ComboBoxStyle.DropDown;
            }            
        }

        private void BatchMoveToResourcesToolPanel_CellEndEdit(object sender, DataGridViewCellEventArgs e) {
            if (e.ColumnIndex == 1 && valueAdded) {
                DataGridViewComboBoxCell cell = (DataGridViewComboBoxCell)Rows[e.RowIndex].Cells["Key"];
                cell.Value = cell.Items[0];
                valueAdded = false;                
            }
            if (e.ColumnIndex == 1 || e.ColumnIndex == 4) {
                validate(e.RowIndex);
            }
            if (e.ColumnIndex == 0) {
                DataGridViewCheckBoxCell cell = (DataGridViewCheckBoxCell)Rows[e.RowIndex].Cells["MoveThisItem"];
                
                if ((bool)cell.Value!=(bool)cell.Tag)
                    CheckedRowsCount += ((bool)cell.Value) == true ? 1 : -1;

                updateCheckHeader();
            }
        }

        private void updateCheckHeader() {
            if (CheckedRowsCount == Rows.Count) {
                checkHeader.Checked = true;
            } else if (CheckedRowsCount == 0) {
                checkHeader.Checked = false;
            } else {
                checkHeader.Checked = null;
            }
        }

        private void validate(DataGridViewRow row) {
            string key = row.Cells["Key"].Value.ToString();           
            ResXProjectItem resxItem = resxItemsCache[row.Cells["DestinationItem"].Value.ToString()];
            if (!resxItem.IsLoaded) {
                resxItem.Load();
                loadedItems.Add(resxItem);
            }

            string errorText = null; 
            bool ok = !resxItem.ContainsKey(key);
            if (!ok) errorText = "Duplicate key entry";
            ok = ok && key.IsValidIdentifier(ref errorText);

            if (ok) {
                row.ErrorText = null;
            } else {
                row.ErrorText = errorText;
            }
        }

        private void validate(int row) {
            validate(Rows[row]);
        }

    }

    internal class DataGridViewCheckBoxHeaderCell : DataGridViewColumnHeaderCell {

        public event EventHandler CheckBoxClicked;

        private CheckBoxState CheckBoxState { get; set; }

        public Point CheckBoxPosition {
            get;
            private set;
        }

        public Size CheckBoxSize {
            get;
            private set;
        }

        private bool? _Checked;
        public bool? Checked {
            get {
                return _Checked;
            }
            set {
                _Checked = value;
                ChangeValue();             
            }
        }
        
        public bool ThreeStates { get; set; }

        protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, 
            DataGridViewElementStates dataGridViewElementState, object value, object formattedValue, string errorText, 
            DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts) {
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, dataGridViewElementState, value, null, errorText, cellStyle, advancedBorderStyle, paintParts);

            CheckBoxSize = CheckBoxRenderer.GetGlyphSize(graphics, CheckBoxState);
            CheckBoxPosition = new Point(cellBounds.X + (cellBounds.Width - CheckBoxSize.Width) / 2, cellBounds.Y + (cellBounds.Height - CheckBoxSize.Height) / 2);
            CheckBoxRenderer.DrawCheckBox(graphics, CheckBoxPosition, CheckBoxState); 
        }

        protected override void OnMouseClick(DataGridViewCellMouseEventArgs e) {
            base.OnMouseClick(e);

            if (Checked == true) {
                Checked = false;
            } else {
                Checked = true;
            }
            NotifyCheckBoxClicked();
        }

        protected void NotifyCheckBoxClicked() {
            if (CheckBoxClicked != null) {
                CheckBoxClicked(this, new EventArgs());
            }
        }

        protected virtual void ChangeValue() {
            if (Checked == true) {
                CheckBoxState = CheckBoxState.CheckedNormal;                
            } else if (Checked == null) {
                CheckBoxState = CheckBoxState.MixedNormal;
            } else {
                CheckBoxState = CheckBoxState.UncheckedNormal;
            }
            this.RaiseCellValueChanged(new DataGridViewCellEventArgs(this.ColumnIndex, this.RowIndex));
        }
    }
}
