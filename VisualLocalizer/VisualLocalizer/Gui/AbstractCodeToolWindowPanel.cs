using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VisualLocalizer.Components;
using Microsoft.VisualStudio.TextManager.Interop;

namespace VisualLocalizer.Gui {

    internal sealed class CodeDataGridViewRow : DataGridViewRow {
        public CodeDataGridViewRow() {
            this.DependantRows = new List<CodeDataGridViewRow>();
            this.ConflictRows = new HashSet<CodeDataGridViewRow>();
        }
        public AbstractResultItem CodeResultItem { get; set; }
        public List<CodeDataGridViewRow> DependantRows { get; set; }
        public HashSet<CodeDataGridViewRow> ConflictRows { get; private set; }
    }

    internal abstract class AbstractCodeToolWindowPanel : DataGridView,IHighlightRequestSource {
        
        public event EventHandler<CodeResultItemEventArgs> HighlightRequired;
        protected ToolTip errorTooltip;
        protected DataGridViewCheckBoxHeaderCell checkHeader;
        public int CheckedRowsCount { get; protected set; }        
        protected Timer errorTimer;
        protected bool errorTooltipVisible;
        public event EventHandler HasErrorChanged;
        protected int? currentItemIndex = null;

        private int _ErrorRowsCount;
        public int ErrorRowsCount {
            get { return _ErrorRowsCount; }
            protected set {
                _ErrorRowsCount = value;
                if (HasErrorChanged != null) HasErrorChanged(this, null);
            }
        }

        public AbstractCodeToolWindowPanel() {
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
            this.VerticalScrollBar.Visible = true;            

            this.CellDoubleClick += new DataGridViewCellEventHandler(OnRowDoubleClick);
            this.CellBeginEdit += new DataGridViewCellCancelEventHandler(OnCellBeginEdit);
            this.CellEndEdit += new DataGridViewCellEventHandler(OnCellEndEdit);
            this.MouseMove += new MouseEventHandler(RowHeaderMouseMove);
            this.RowErrorTextChanged += new DataGridViewRowEventHandler(AbstractCodeToolWindowPanel_RowErrorTextChanged);

            errorTooltip = new ToolTip();
            errorTooltip.InitialDelay = 0;
            errorTooltip.AutoPopDelay = 0;
            errorTooltip.ShowAlways = true;
            errorTooltip.ReshowDelay = 0;

            errorTimer = new Timer();
            errorTimer.Interval = 1000;
            errorTimer.Tick += new EventHandler(errorTimer_Tick);            
            CheckedRowsCount = 0;

            DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn(false);
            checkColumn.MinimumWidth = 30;
            checkColumn.Width = 30;
            checkColumn.HeaderText = "";
            checkColumn.Name = "MoveThisItem";
            
            checkHeader = new DataGridViewCheckBoxHeaderCell();
            checkHeader.ThreeStates = true;
            checkHeader.Checked = true;
            checkHeader.CheckBoxClicked += new EventHandler(checkHeader_CheckBoxClicked);
            checkColumn.HeaderCell = checkHeader;
            this.Columns.Add(checkColumn);
        }

        protected void AbstractCodeToolWindowPanel_RowErrorTextChanged(object sender, DataGridViewRowEventArgs e) {
            ErrorRowsCount += string.IsNullOrEmpty(e.Row.ErrorText) ? -1 : 1;
        }

        private void errorTimer_Tick(object sender, EventArgs e) {
            errorTooltipVisible = false;
            errorTimer.Stop();
        }

        protected void RowHeaderMouseMove(object sender, MouseEventArgs e) {
            HitTestInfo info = this.HitTest(e.X, e.Y);
            if (info != null && info.Type == DataGridViewHitTestType.RowHeader && info.RowIndex >= 0) {
                if (!string.IsNullOrEmpty(Rows[info.RowIndex].ErrorText) && !errorTooltipVisible) {
                    errorTooltip.Show(Rows[info.RowIndex].ErrorText, this, e.X, e.Y, 1000);                    
                    errorTooltipVisible = true;
                    errorTimer.Start();
                }
            }
        }

        protected virtual void checkHeader_CheckBoxClicked(object sender, EventArgs e) {
            int errors = 0;
            foreach (CodeDataGridViewRow row in Rows) {
                row.Cells["MoveThisItem"].Value = checkHeader.Checked == true;
                row.Cells["MoveThisItem"].Tag = checkHeader.Checked == true;
                if (!string.IsNullOrEmpty(row.ErrorText)) errors++;
            }
            CheckedRowsCount = checkHeader.Checked == true ? Rows.Count : 0;
            ErrorRowsCount = checkHeader.Checked == true ? errors : 0;
        }

        protected virtual void OnRowDoubleClick(object sender, DataGridViewCellEventArgs e) {
            if (HighlightRequired != null && e.RowIndex >= 0) {
                HighlightRequired(this, new CodeResultItemEventArgs() { Item = (Rows[e.RowIndex] as CodeDataGridViewRow).CodeResultItem });
            }
        }

        protected virtual void OnCellBeginEdit(object sender, DataGridViewCellCancelEventArgs e) {
            DataGridViewCell cell = (Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewCell);
            cell.Tag = cell.Value;
        }

        protected virtual void OnCellEndEdit(object sender, DataGridViewCellEventArgs e) {
            if (e.ColumnIndex == 0) {
                DataGridViewCheckBoxCell cell = (DataGridViewCheckBoxCell)Rows[e.RowIndex].Cells["MoveThisItem"];
                CodeDataGridViewRow row = (CodeDataGridViewRow)Rows[e.RowIndex];

                if ((bool)cell.Value != (bool)cell.Tag) {
                    CheckedRowsCount += ((bool)cell.Value) == true ? 1 : -1;

                    if (!string.IsNullOrEmpty(row.ErrorText)) {
                        ErrorRowsCount += ((bool)cell.Value) == true ? 1 : -1;
                    }
                }
                updateCheckHeader();
            }
        }

        protected void updateCheckHeader() {
            if (CheckedRowsCount == Rows.Count) {
                checkHeader.Checked = true;
            } else if (CheckedRowsCount == 0) {
                checkHeader.Checked = false;
            } else {
                checkHeader.Checked = null;
            }
        }

        public bool HasError {
            get {
                return ErrorRowsCount > 0; 
            }
        }

        public void SetCurrentItemFinished(bool ok, int newLength) {
            if (currentItemIndex == null || currentItemIndex < 0) throw new ArgumentException("currentItemIndex");

            if (ok) {
                AbstractResultItem resultItem = (Rows[currentItemIndex.Value] as CodeDataGridViewRow).CodeResultItem;
                TextSpan currentReplaceSpan = resultItem.ReplaceSpan;

                int diff = currentReplaceSpan.iEndLine - currentReplaceSpan.iStartLine;
                for (int i = currentItemIndex.Value + 1; i < Rows.Count; i++) {
                    AbstractResultItem item = (Rows[i] as CodeDataGridViewRow).CodeResultItem;
                    item.AbsoluteCharOffset += newLength - resultItem.AbsoluteCharLength;

                    if (item.ReplaceSpan.iStartLine > currentReplaceSpan.iEndLine) {
                        TextSpan newSpan = new TextSpan();
                        newSpan.iEndIndex = item.ReplaceSpan.iEndIndex;
                        newSpan.iStartIndex = item.ReplaceSpan.iStartIndex;
                        newSpan.iEndLine = item.ReplaceSpan.iEndLine - diff;
                        newSpan.iStartLine = item.ReplaceSpan.iStartLine - diff;
                        item.ReplaceSpan = newSpan;
                    } else if (item.ReplaceSpan.iStartLine == currentReplaceSpan.iEndLine) {
                        TextSpan newSpan = new TextSpan();
                        newSpan.iStartIndex = currentReplaceSpan.iStartIndex + newLength + item.ReplaceSpan.iStartIndex - currentReplaceSpan.iEndIndex;
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

        public AbstractResultItem GetNextResultItem() {
            if (currentItemIndex == null)
                currentItemIndex = Rows.Count;

            currentItemIndex--;

            if (currentItemIndex < 0) {
                currentItemIndex = null;
                this.ReadOnly = false;
                Rows.Clear();
                return null;
            } else {
                this.ReadOnly = true;
                CodeDataGridViewRow row = Rows[currentItemIndex.Value] as CodeDataGridViewRow;
                return GetResultItemFromRow(row);
            }
        }

        protected abstract AbstractResultItem GetResultItemFromRow(CodeDataGridViewRow row);
    }
}
