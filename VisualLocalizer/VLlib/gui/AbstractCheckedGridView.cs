using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Windows.Forms;
using System.Resources;
using System.Drawing;

namespace VisualLocalizer.Library {

    public sealed class CodeDataGridViewRow<ItemType> : DataGridViewRow {
        public CodeDataGridViewRow() {
            this.RowsWithSameKey = new List<CodeDataGridViewRow<ItemType>>();
            this.ConflictRows = new HashSet<CodeDataGridViewRow<ItemType>>();
            this.ErrorSet = new HashSet<string>();
        }
        public ItemType DataSourceItem { get; set; }
        public List<CodeDataGridViewRow<ItemType>> RowsWithSameKey { get; set; }
        public HashSet<CodeDataGridViewRow<ItemType>> ConflictRows { get; private set; }
        public HashSet<string> ErrorSet { get; private set; }

        public void ErrorSetUpdate() {
            if (ErrorSet.Count == 0) {
                ErrorText = null;
            } else {
                ErrorText = ErrorSet.First();
            }
        }
    }

    public abstract class AbstractCheckedGridView<ItemType> : DataGridView where ItemType : class {
                
        public event EventHandler HasErrorChanged;
        public int CheckedRowsCount { get; protected set; }   

        protected ToolTip ErrorToolTip;
        protected DataGridViewCheckBoxHeaderCell CheckHeader;             
        protected Timer ErrorTimer;
        protected bool ErrorToolTipVisible;        
        protected int? CurrentItemIndex = null;        
        protected Color ErrorColor = Color.FromArgb(255, 213, 213);
        protected Color ExistingKeySameValueColor = Color.FromArgb(213, 255, 213);
        private int _ErrorRowsCount;
        
        public AbstractCheckedGridView() {
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
            
            this.MouseMove += new MouseEventHandler(RowHeaderMouseMove);
            
            ErrorToolTip = new ToolTip();
            ErrorToolTip.InitialDelay = 0;
            ErrorToolTip.AutoPopDelay = 0;
            ErrorToolTip.ShowAlways = true;
            ErrorToolTip.ReshowDelay = 0;

            ErrorTimer = new Timer();
            ErrorTimer.Interval = 1000;
            ErrorTimer.Tick += new EventHandler(errorTimer_Tick);

            CheckHeader = new DataGridViewCheckBoxHeaderCell();
            CheckHeader.ThreeStates = true;
            CheckHeader.Checked = true;
            CheckHeader.CheckBoxClicked += new EventHandler(OnCheckHeaderClicked);

            CheckedRowsCount = 0;

            CodeDataGridViewRow<ItemType> rowTemplate = new CodeDataGridViewRow<ItemType>();
            this.RowTemplate = rowTemplate;

            InitializeColumns();
        }

        #region public members
        public int ErrorRowsCount {
            get { return _ErrorRowsCount; }
            protected set {
                _ErrorRowsCount = value;
                if (HasErrorChanged != null) HasErrorChanged(this, null);
            }
        }

        public bool HasError {
            get {
                return ErrorRowsCount > 0;
            }
        }
       
        public virtual ItemType GetNextResultItem() {
            if (CurrentItemIndex == null)
                CurrentItemIndex = Rows.Count;

            CurrentItemIndex--;

            if (CurrentItemIndex < 0) {
                CurrentItemIndex = null;
                this.ReadOnly = false;
                Rows.Clear();
                return null;
            } else {
                this.ReadOnly = true;
                CodeDataGridViewRow<ItemType> row = Rows[CurrentItemIndex.Value] as CodeDataGridViewRow<ItemType>;
                return GetResultItemFromRow(row);
            }
        }

        public abstract void SetData(List<ItemType> list);
        public abstract string CheckBoxColumnName { get; }

        #endregion 

        #region overridable members
        protected virtual void InitializeColumns() {
            DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn(false);
            checkColumn.MinimumWidth = 30;
            checkColumn.Width = 30;
            checkColumn.HeaderText = "";
            checkColumn.Name = CheckBoxColumnName;            
            checkColumn.HeaderCell = CheckHeader;
            this.Columns.Add(checkColumn);
        }

        protected virtual void OnCheckHeaderClicked(object sender, EventArgs e) {
            int errors = 0;
            foreach (DataGridViewRow row in Rows) {
                row.Cells[CheckBoxColumnName].Value = CheckHeader.Checked == true;
                row.Cells[CheckBoxColumnName].Tag = CheckHeader.Checked == true;
                if (!string.IsNullOrEmpty(row.ErrorText)) errors++;
            }
            CheckedRowsCount = CheckHeader.Checked == true ? Rows.Count : 0;
            ErrorRowsCount = CheckHeader.Checked == true ? errors : 0;
        }
        
        protected override void OnCellBeginEdit(DataGridViewCellCancelEventArgs e) {
            base.OnCellBeginEdit(e);

            DataGridViewCell cell = (Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewCell);
            cell.Tag = cell.Value;
        }

        protected override void OnCellEndEdit(DataGridViewCellEventArgs e) {
            base.OnCellEndEdit(e);

            if (this.Columns[e.ColumnIndex].Name == CheckBoxColumnName && CheckBoxColumnName!=null) {
                DataGridViewCheckBoxCell cell = (DataGridViewCheckBoxCell)Rows[e.RowIndex].Cells[CheckBoxColumnName];
                DataGridViewRow row = Rows[e.RowIndex];
                if (cell.Value == null || cell.Tag == null) return;

                bool value=(bool)cell.Value;

                if (value != (bool)cell.Tag) {
                    CheckedRowsCount += value ? 1 : -1;
                    if (!string.IsNullOrEmpty(row.ErrorText)) ErrorRowsCount += value ? 1 : -1;                    
                }

                UpdateCheckHeader();
            }           
        }

        protected override void OnRowErrorTextChanged(DataGridViewRowEventArgs e) {
            base.OnRowErrorTextChanged(e);
            ErrorRowsCount += string.IsNullOrEmpty(e.Row.ErrorText) ? -1 : 1;

            if (!string.IsNullOrEmpty(e.Row.ErrorText)) {
                e.Row.DefaultCellStyle.BackColor = ErrorColor;
            } else {
                e.Row.DefaultCellStyle.BackColor = Color.White;
            }
            this.UpdateRowErrorText(e.Row.Index);            
        }

        #endregion                

        protected void UpdateCheckHeader() {
            if (CheckedRowsCount == Rows.Count) {
                CheckHeader.Checked = true;
            } else if (CheckedRowsCount == 0) {
                CheckHeader.Checked = false;
            } else {
                CheckHeader.Checked = null;
            }
        }

        protected abstract ItemType GetResultItemFromRow(CodeDataGridViewRow<ItemType> row);


        private void errorTimer_Tick(object sender, EventArgs e) {
            ErrorToolTipVisible = false;
            ErrorTimer.Stop();
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
    }
}
