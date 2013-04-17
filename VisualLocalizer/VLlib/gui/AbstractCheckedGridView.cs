using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Windows.Forms;
using System.Resources;
using System.Drawing;
using System.ComponentModel;

namespace VisualLocalizer.Library {

    /// <summary>
    /// Enhances standard DataGridView with checkable first column and header.
    /// </summary>
    /// <typeparam name="ItemType">Type item stored in rows</typeparam>
    public abstract class AbstractCheckedGridView<ItemType> : DataGridView where ItemType : class {
                
        /// <summary>
        /// Fired when a row with error was (un)checked
        /// </summary>
        public event EventHandler HasErrorChanged;

        /// <summary>
        /// Number of checked rows
        /// </summary>
        public int CheckedRowsCount { get; protected set; }   

        protected ToolTip ErrorToolTip; // tooltip displayed over error icon
        protected DataGridViewCheckBoxHeaderCell CheckHeader; // header cell with checkbox            
        protected Timer ErrorTimer; // controls ErrorToolTip display
        protected bool ErrorToolTipVisible;                
        protected Color ErrorColor = Color.FromArgb(255, 213, 213); // background color of rows with error (red)
        protected Color ExistingKeySameValueColor = Color.FromArgb(213, 255, 213); // background color of rows whose key is already in the resource file with the same value (ok - green)
        protected HashSet<DataGridViewRow> errorRows = new HashSet<DataGridViewRow>();
        protected DataGridViewRow previouslySelectedRow = null;
        protected List<DataGridViewRow> removedRows;

        public AbstractCheckedGridView(bool showContextColumn) {
            this.EnableHeadersVisualStyles = true;
            this.AutoGenerateColumns = false;
            this.AllowUserToAddRows = false;
            this.AllowUserToDeleteRows = false;
            this.AutoSize = true;
            this.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            this.MultiSelect = false;
            this.Dock = DockStyle.Fill;
            this.AllowUserToResizeRows = true;
            this.AllowUserToResizeColumns = true;
            this.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.ScrollBars = ScrollBars.Both;
            this.ShowContextColumn = showContextColumn;

            this.MouseMove += new MouseEventHandler(RowHeaderMouseMove);
            this.SelectionChanged += new EventHandler(RowSelectionChanged);

            this.removedRows = new List<DataGridViewRow>();

            ErrorToolTip = new ToolTip();
            ErrorToolTip.InitialDelay = 0;
            ErrorToolTip.AutoPopDelay = 0;
            ErrorToolTip.ShowAlways = true;
            ErrorToolTip.ReshowDelay = 0;

            ErrorTimer = new Timer();
            ErrorTimer.Interval = 1000;
            ErrorTimer.Tick += new EventHandler(ErrorTimer_Tick);

            CheckHeader = new DataGridViewCheckBoxHeaderCell();
            CheckHeader.ThreeStates = true;
            CheckHeader.Checked = true;
            CheckHeader.CheckBoxClicked += new EventHandler(OnCheckHeaderClicked);
            CheckHeader.Sort += new Action<SortOrder>(CheckHeader_Sort);

            CheckedRowsCount = 0;
            
            InitializeColumns();
        }        
        
        #region public members
        
        /// <summary>
        /// Returns true if there is a checked row with error
        /// </summary>
        public bool HasError {
            get {
                return errorRows.Count > 0;
            }
        }
       
        /// <summary>
        /// Returns list of items created from rows data
        /// </summary>        
        public virtual List<ItemType> GetData() {
            List<ItemType> list = new List<ItemType>(Rows.Count);
            
            CheckedRowsCount = 0;
            foreach (DataGridViewRow row in Rows) {
                list.Add(GetResultItemFromRow(row));
                if (!string.IsNullOrEmpty(CheckBoxColumnName) && Columns.Contains(CheckBoxColumnName) && (bool)row.Cells[CheckBoxColumnName].Value) 
                    CheckedRowsCount++;
            }

            return list;
        }

        /// <summary>
        /// Removes unchecked rows from the grid
        /// </summary>
        /// <param name="remember">True if removed rows should be added to the list of removed rows for future reference (undo)</param>
        public virtual void RemoveUncheckedRows(bool remember) {
            if (string.IsNullOrEmpty(CheckBoxColumnName) || !Columns.Contains(CheckBoxColumnName)) return; // this grid does not contain checkbox column

            List<DataGridViewRow> rowsToRemove = new List<DataGridViewRow>();
            foreach (DataGridViewRow row in Rows) {
                bool check = (bool)row.Cells[CheckBoxColumnName].Value;
                if (!check) {
                    rowsToRemove.Add(row);
                    if (remember) removedRows.Add(row);                    
                }
            }

            foreach (DataGridViewRow row in rowsToRemove) {
                row.ErrorText = null;
                Rows.Remove(row);
            }

            UpdateCheckHeader();
        }

        /// <summary>
        /// Puts removed rows back to the grid
        /// </summary>
        public virtual List<DataGridViewRow> RestoreRemovedRows() {
            if (string.IsNullOrEmpty(CheckBoxColumnName) || !Columns.Contains(CheckBoxColumnName)) return null; // this grid does not contain checkbox column

            foreach (DataGridViewRow row in removedRows) {
                Rows.Add(row);
            }

            List<DataGridViewRow> returnList = new List<DataGridViewRow>(removedRows);
            removedRows.Clear();
            UpdateCheckHeader();

            return returnList;
        }

        /// <summary>
        /// Initializes the grid with provided list of items
        /// </summary>        
        public abstract void SetData(List<ItemType> list);

        /// <summary>
        /// Returns name of the column with checkbox
        /// </summary>
        public abstract string CheckBoxColumnName { get; }

        #endregion 

        #region overridable members

        /// <summary>
        /// Initializes grid columns
        /// </summary>
        protected virtual void InitializeColumns() {
            DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn(false);
            checkColumn.MinimumWidth = 50;
            checkColumn.Width = 50;
            checkColumn.Name = CheckBoxColumnName;
            checkColumn.HeaderCell = CheckHeader;
            checkColumn.ToolTipText = null;
            checkColumn.SortMode = DataGridViewColumnSortMode.Programmatic;
            checkColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            checkColumn.DefaultCellStyle.Padding = new Padding(4, 0, 0, 0);
            checkColumn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            this.Columns.Add(checkColumn);

            DataGridViewTextBoxColumn lineColumn = new DataGridViewTextBoxColumn();
            lineColumn.MinimumWidth = 40;
            lineColumn.Width = 55;
            lineColumn.HeaderText = "Line";
            lineColumn.Name = LineColumnName;
            lineColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            lineColumn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            this.Columns.Add(lineColumn);

            DataGridViewTextBoxColumn contextColumn = new DataGridViewTextBoxColumn();
            contextColumn.MinimumWidth = 40;
            contextColumn.Width = 350;
            contextColumn.HeaderText = "Context";
            contextColumn.Name = ContextColumnName;
            contextColumn.Visible = ShowContextColumn;
            this.Columns.Add(contextColumn);

        }

        /// <summary>
        /// Handles display of context column - shrinks previously displayed row and expands context of current row
        /// </summary>        
        protected virtual void RowSelectionChanged(object sender, EventArgs e) {
            if (!Columns.Contains(ContextColumnName) || !Columns[ContextColumnName].Visible) return; // the grid does not have a context column or it is hidden

            if (previouslySelectedRow != null && previouslySelectedRow.Index != -1) {
                // shrink previous row
                DataGridViewDynamicWrapCell cell = (DataGridViewDynamicWrapCell)previouslySelectedRow.Cells[ContextColumnName];
                cell.SetWrapContents(false);
            }

            DataGridViewRow row = null;
            if (SelectedRows.Count == 1) row = SelectedRows[0];

            if (row != null) {
                // expand current row
                DataGridViewDynamicWrapCell cell = (DataGridViewDynamicWrapCell)row.Cells[ContextColumnName];
                cell.SetWrapContents(true);
            }

            previouslySelectedRow = row;
        }

        /// <summary>
        /// (Un)checks checkbox in header and sets check-state of all rows accordingly
        /// </summary>        
        protected virtual void OnCheckHeaderClicked(object sender, EventArgs e) {
            if (string.IsNullOrEmpty(CheckBoxColumnName) || !Columns.Contains(CheckBoxColumnName)) return; // this grid does not contain checkbox column          

            CheckedRowsCount = CheckHeader.Checked == true ? Rows.Count : 0; 
            errorRows.Clear();            
            foreach (DataGridViewRow row in Rows) {                 
                row.Cells[CheckBoxColumnName].Value = CheckHeader.Checked == true;
                row.Cells[CheckBoxColumnName].Tag = CheckHeader.Checked == true;                

                if (!string.IsNullOrEmpty(row.ErrorText) && CheckHeader.Checked == true) errorRows.Add(row);
            }
            
                       
            NotifyErrorRowsChanged();
        }

        /// <summary>
        /// Called after sorting of a column - removes sort glyph from checkbox column if appropriate
        /// </summary>        
        protected override void OnSorted(EventArgs e) {
            base.OnSorted(e);
            if (this.SortedColumn == null || (this.SortedColumn.Name != CheckBoxColumnName && !string.IsNullOrEmpty(CheckBoxColumnName))) {
                CheckHeader.SortGlyphDirection = SortOrder.None;
            }
        }        

        /// <summary>
        /// Called after sorting of a checkbox column
        /// </summary>        
        protected virtual void CheckHeader_Sort(SortOrder direction) {
            if (string.IsNullOrEmpty(CheckBoxColumnName) || !Columns.Contains(CheckBoxColumnName)) return; // this grid does not contain checkbox column

            this.Sort(Columns[CheckBoxColumnName], direction == SortOrder.Ascending ? ListSortDirection.Ascending : ListSortDirection.Descending);
        }

        /// <summary>
        /// Called before editting the cell - saves the value (before editting) in the Tag of the cell
        /// </summary>        
        protected override void OnCellBeginEdit(DataGridViewCellCancelEventArgs e) {
            base.OnCellBeginEdit(e);

            DataGridViewCell cell = (Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewCell);
            cell.Tag = cell.Value;
        }

        /// <summary>
        /// Called after editting the cell - if checkbox column was edited, modifies associated data (CheckedRowsCount, errorRows...)
        /// </summary>        
        protected override void OnCellEndEdit(DataGridViewCellEventArgs e) {
            base.OnCellEndEdit(e);

            if (this.Columns[e.ColumnIndex].Name == CheckBoxColumnName && !string.IsNullOrEmpty(CheckBoxColumnName)) {
                DataGridViewCheckBoxCell cell = (DataGridViewCheckBoxCell)Rows[e.RowIndex].Cells[CheckBoxColumnName];
                DataGridViewRow row = Rows[e.RowIndex];
                if (cell.Value == null || cell.Tag == null) return;

                bool newValue = (bool)cell.Value;
                bool oldValue = (bool)cell.Tag;

                ChangeRowCheckState(row, oldValue, newValue);
            }           
        }

        /// <summary>
        /// Changes check state of a row from an old value to a new value
        /// </summary>        
        protected void ChangeRowCheckState(DataGridViewRow row, bool oldValue, bool newValue) {
            if (oldValue != newValue) {
                CheckedRowsCount += newValue ? 1 : -1;
                if (!newValue && !string.IsNullOrEmpty(row.ErrorText)) {
                    errorRows.Remove(row);
                }
                if (newValue && !string.IsNullOrEmpty(row.ErrorText)) {
                    errorRows.Add(row);
                }
                NotifyErrorRowsChanged();
                UpdateCheckHeader();
            }
        }

        /// <summary>
        /// Called after error text of a row has changed - updates errorRows and row's background color
        /// </summary>        
        protected override void OnRowErrorTextChanged(DataGridViewRowEventArgs e) {
            base.OnRowErrorTextChanged(e);

            // checkbox column either does not exist or row is checked
            if (string.IsNullOrEmpty(CheckBoxColumnName) || (bool)e.Row.Cells[CheckBoxColumnName].Value) {
                if (string.IsNullOrEmpty(e.Row.ErrorText)) {
                    errorRows.Remove(e.Row);
                } else {
                    errorRows.Add(e.Row);
                }
                NotifyErrorRowsChanged();
            }

            if (!string.IsNullOrEmpty(e.Row.ErrorText)) {
                // set row background color
                if (e.Row.DefaultCellStyle.BackColor != ErrorColor) e.Row.DefaultCellStyle.Tag = e.Row.DefaultCellStyle.BackColor;
                e.Row.DefaultCellStyle.BackColor = ErrorColor;
            } else {
                e.Row.DefaultCellStyle.BackColor = e.Row.DefaultCellStyle.Tag == null ? Color.White : (Color)e.Row.DefaultCellStyle.Tag;
            }
            this.UpdateRowErrorText(e.Row.Index);            
        }

        #endregion                

        /// <summary>
        /// Updates state of checkbox column header, based on the number of checked rows
        /// </summary>
        protected void UpdateCheckHeader() {
            if (CheckedRowsCount == Rows.Count) {
                CheckHeader.Checked = true;
            } else if (CheckedRowsCount == 0) {
                CheckHeader.Checked = false;
            } else {
                CheckHeader.Checked = null;
            }            
        }

        /// <summary>
        /// Sets check state of all selected rows to specified value
        /// </summary>
        protected void SetCheckStateOfSelected(bool check) {
            if (string.IsNullOrEmpty(CheckBoxColumnName) || !Columns.Contains(CheckBoxColumnName)) return; // this grid does not contain checkbox column

            foreach (DataGridViewRow row in SelectedRows) {
                row.Cells[CheckBoxColumnName].Tag = row.Cells[CheckBoxColumnName].Value;
                row.Cells[CheckBoxColumnName].Value = check;
                OnCellEndEdit(new DataGridViewCellEventArgs(Columns[CheckBoxColumnName].Index, row.Index));
            }
            UpdateCheckHeader();
        }

        /// <summary>
        /// Displayes context menu for a grid
        /// </summary>        
        protected void OnContextMenuShow(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Right) {
                HitTestInfo hitTest = this.HitTest(e.X, e.Y);
                if (hitTest != null && hitTest.Type == DataGridViewHitTestType.Cell && hitTest.RowIndex >= 0 && hitTest.ColumnIndex >= 0) {
                    // was clicked on one of selected rows?
                    bool isRowSelected = Rows[hitTest.RowIndex].Selected;
                    if (!isRowSelected) { // no - clear selection and make the clicked row selected
                        this.ClearSelection();
                        Rows[hitTest.RowIndex].Selected = true;
                    }

                    this.ContextMenu.Show(this, e.Location); // display context menu
                }
            }
        }

        /// <summary>
        /// Returns item initialized with values from provided row
        /// </summary>        
        protected abstract ItemType GetResultItemFromRow(DataGridViewRow row);

        private void ErrorTimer_Tick(object sender, EventArgs e) {
            ErrorToolTipVisible = false;
            ErrorTimer.Stop();
        }

        protected void NotifyErrorRowsChanged() {
            if (HasErrorChanged != null) HasErrorChanged(this, null);
        }

        private void RowHeaderMouseMove(object sender, MouseEventArgs e) {
            HitTestInfo info = this.HitTest(e.X, e.Y);
            if (info != null && info.Type == DataGridViewHitTestType.RowHeader && info.RowIndex >= 0) {
                if (!string.IsNullOrEmpty(Rows[info.RowIndex].ErrorText) && !ErrorToolTipVisible) {
                    ErrorToolTip.Show(Rows[info.RowIndex].ErrorText, this, e.X, e.Y, 1000);                    
                    ErrorToolTipVisible = true;
                    ErrorTimer.Start();
                }
            }
        }

        /// <summary>
        /// Returns name of the column displaying line number
        /// </summary>
        public string LineColumnName {
            get { return "Line"; }
        }

        /// <summary>
        /// Returns name of the column displaying context
        /// </summary>
        public string ContextColumnName {
            get { return "Context"; }
        }

        /// <summary>
        /// Whether context column should be visible
        /// </summary>
        public bool ShowContextColumn {
            get;
            private set;
        }
    }

   
}
