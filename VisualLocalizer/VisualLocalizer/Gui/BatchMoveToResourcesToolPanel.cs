using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using VisualLocalizer.Components;
using VisualLocalizer.Settings;

namespace VisualLocalizer.Gui {
    internal sealed class BatchMoveToResourcesToolPanel : Panel,IHighlightRequestSource {
        
        public event EventHandler<CodeResultItemEventArgs> HighlightRequired;
        private TableLayoutPanel filterPanel;
        private Button addRegexpButton;
        private TableLayoutPanel regexTable;
        private CheckBox verbatimBox;
        private CheckBox localizableBox;
        private CheckBox noLettersBox;
        private CheckBox capsBox;
        private CheckBox commentBox;        
        private Label regexLabel;
        private SplitContainer splitContainer;
        private bool SplitterMoving;
        private CheckBox comesFromElementBox, comesFromDesignerFileBox, comesFromAspNetBox, 
            comesFromInlineExprBox, comesFromAspClientCommentBox;

        public BatchMoveToResourcesToolPanel() {
            this.SuspendLayout();
            this.Dock = DockStyle.Fill;

            SettingsObject.Instance.SettingsLoaded += new Action(SettingsUpdated);

            ToolGrid = new BatchMoveToResourcesToolGrid(this);
            ToolGrid.HighlightRequired += new EventHandler<CodeResultItemEventArgs>(grid_HighlightRequired);

            filterPanel = new TableLayoutPanel();
            InitializeFilterPanel();

            splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Orientation = Orientation.Horizontal;            

            splitContainer.Panel1.Controls.Add(filterPanel);
            splitContainer.Panel2.Controls.Add(ToolGrid);
            this.Controls.Add(splitContainer);      

            this.ResumeLayout(true);

            FilterVisible = false;
            
            splitContainer.SplitterMoved += new SplitterEventHandler(splitContainer_SplitterMoved);
            splitContainer.SplitterMoving += new SplitterCancelEventHandler(splitContainer_SplitterMoving);
        }

        private void splitContainer_SplitterMoving(object sender, SplitterCancelEventArgs e) {
            SplitterMoving = true;
        }

        private void splitContainer_SplitterMoved(object sender, SplitterEventArgs e) {
            if (SplitterMoving)
                SettingsObject.Instance.BatchMoveSplitterDistance = splitContainer.SplitterDistance;
            SplitterMoving = false;
        }

        public void SettingsUpdated() {
            verbatimBox.Checked = SettingsObject.Instance.FilterOutVerbatim;
            ToolGrid.CheckByPredicate(verbatimBox, ToolGrid.IsRowVerbatimTest);

            capsBox.Checked = SettingsObject.Instance.FilterOutCaps;
            ToolGrid.CheckByPredicate(capsBox, ToolGrid.IsRowCapitalsTest);

            noLettersBox.Checked = SettingsObject.Instance.FilterOutNoLetters;
            ToolGrid.CheckByPredicate(noLettersBox, ToolGrid.IsRowNoLettersTest);

            localizableBox.Checked = SettingsObject.Instance.FilterOutUnlocalizable;
            ToolGrid.CheckByPredicate(localizableBox, ToolGrid.IsRowUnlocalizableTest);

            commentBox.Checked = SettingsObject.Instance.FilterOutSpecificComment;
            ToolGrid.CheckByPredicate(commentBox, ToolGrid.IsRowMarkedWithUnlocCommentTest);

            comesFromElementBox.Checked = SettingsObject.Instance.FilterOutAspElement;
            ToolGrid.CheckByPredicate(comesFromElementBox, ToolGrid.IsRowAspElementTest);

            comesFromDesignerFileBox.Checked = SettingsObject.Instance.FilterOutDesignerFile;
            ToolGrid.CheckByPredicate(comesFromDesignerFileBox, ToolGrid.IsRowDesignerFileTest);

            comesFromAspNetBox.Checked = SettingsObject.Instance.FilterOutAspNet;
            ToolGrid.CheckByPredicate(comesFromAspNetBox, ToolGrid.IsRowFromAspNetTest);

            comesFromInlineExprBox.Checked = SettingsObject.Instance.FilterOutAspInlineExpr;
            ToolGrid.CheckByPredicate(comesFromInlineExprBox, ToolGrid.IsRowAspExpressionTest);

            comesFromAspClientCommentBox.Checked = SettingsObject.Instance.FilterOutAspClientComment;
            ToolGrid.CheckByPredicate(comesFromAspClientCommentBox, ToolGrid.IsRowAspClientCommentTest);

            splitContainer.SuspendLayout();
            regexTable.Controls.Clear();            
            regexTable.RowStyles.Clear();
            regexTable.RowCount = 1;
            regexTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            regexTable.Controls.Add(regexLabel, 0, 0);
            regexTable.Controls.Add(addRegexpButton, 1, 0);

            foreach (var item in SettingsObject.Instance.FilterRegexps)
                AddRegexpRow(item);                       

                                    
            splitContainer.SplitterDistance = SettingsObject.Instance.BatchMoveSplitterDistance;
            splitContainer.ResumeLayout();
        }   

        private void InitializeFilterPanel() {
            filterPanel.Dock = DockStyle.Fill;
            filterPanel.AutoSize = true;
            filterPanel.AutoScroll = true;
            filterPanel.Padding = new Padding(0, 0, SystemInformation.VerticalScrollBarWidth, 0);

            filterPanel.ColumnCount = 2;
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); 
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            filterPanel.RowCount = 6;
            filterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            filterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            filterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            filterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            filterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            filterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            localizableBox = new CheckBox();
            localizableBox.Text = "Filter out string literals within element decorated with [Localizable(false)]";
            localizableBox.AutoSize = true;
            localizableBox.AutoEllipsis = true;
            localizableBox.Click += new EventHandler(localizableBox_Click);
            filterPanel.Controls.Add(localizableBox, 0, 0);

            verbatimBox = new CheckBox();
            verbatimBox.Text = "Filter out verbatim string literals";
            verbatimBox.AutoSize = true;
            verbatimBox.AutoEllipsis = true;
            verbatimBox.Click += new EventHandler(verbatimBox_Click);
            filterPanel.Controls.Add(verbatimBox, 1, 0);

            noLettersBox = new CheckBox();
            noLettersBox.Text = "Filter out string literals not containing any letters (e.g. 127.0.0.1)";
            noLettersBox.AutoSize = true;
            noLettersBox.AutoEllipsis = true;
            noLettersBox.Click += new EventHandler(noLettersBox_Click);
            filterPanel.Controls.Add(noLettersBox, 0, 1);

            capsBox = new CheckBox();
            capsBox.Text = "Filter out string literals containing only capital letters, symbols and punctuation";
            capsBox.AutoSize = true;
            capsBox.AutoEllipsis = true;
            capsBox.Click += new EventHandler(capsBox_Click);
            filterPanel.Controls.Add(capsBox, 1, 1);

            commentBox = new CheckBox();
            commentBox.Text = "Filter out string literals preceded by " + StringConstants.LocalizationComment; 
            commentBox.AutoSize = true;
            commentBox.AutoEllipsis = true;
            commentBox.Click += new EventHandler(commentBox_Click);
            filterPanel.Controls.Add(commentBox, 0, 2);

            comesFromAspNetBox = new CheckBox();
            comesFromAspNetBox.Text = "Filter out string literals coming from ASP .NET files";
            comesFromAspNetBox.AutoSize = true;
            comesFromAspNetBox.AutoEllipsis = true;
            comesFromAspNetBox.Click += new EventHandler(comesFromAspNetBox_Click);
            filterPanel.Controls.Add(comesFromAspNetBox, 1, 2);

            comesFromDesignerFileBox = new CheckBox();
            comesFromDesignerFileBox.Text = "Filter out string literals coming from Designer files";
            comesFromDesignerFileBox.AutoSize = true;
            comesFromDesignerFileBox.AutoEllipsis = true;
            comesFromDesignerFileBox.Click += new EventHandler(comesFromDesignerFileBox_Click);
            filterPanel.Controls.Add(comesFromDesignerFileBox, 0, 3);

            comesFromAspClientCommentBox = new CheckBox();
            comesFromAspClientCommentBox.Text = "Filter out string literals occuring within client-side comments of ASP .NET files";
            comesFromAspClientCommentBox.AutoSize = true;
            comesFromAspClientCommentBox.AutoEllipsis = true;
            comesFromAspClientCommentBox.Click +=new EventHandler(comesFromAspClientCommentBox_Click);
            filterPanel.Controls.Add(comesFromAspClientCommentBox, 1, 3);

            comesFromElementBox = new CheckBox();
            comesFromElementBox.Text = "Filter out string literals coming from <asp:???> elements";
            comesFromElementBox.AutoSize = true;
            comesFromElementBox.AutoEllipsis = true;
            comesFromElementBox.Click += new EventHandler(comesFromElementBox_Click);
            filterPanel.Controls.Add(comesFromElementBox, 0, 4);

            comesFromInlineExprBox = new CheckBox();
            comesFromInlineExprBox.Text = "Filter out string literals coming from ASP .NET inline expressions (<%=, <%:, <%$)";
            comesFromInlineExprBox.AutoSize = true;
            comesFromInlineExprBox.AutoEllipsis = true;
            comesFromInlineExprBox.Click += new EventHandler(comesFromInlineExprBox_Click);
            filterPanel.Controls.Add(comesFromInlineExprBox, 1, 4);

            regexLabel = new Label();
            regexLabel.Text = "Filter by regular expression:";
            regexLabel.AutoSize = true;
           
            regexTable = new TableLayoutPanel();
            regexTable.Dock = DockStyle.Fill;
            regexTable.AutoSize = true;
            regexTable.ColumnCount = 6;
            regexTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            regexTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            regexTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            regexTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            regexTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            regexTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            regexTable.RowCount = 1;
            regexTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            regexTable.Controls.Add(regexLabel, 0, 0);

            addRegexpButton = new Button();
            addRegexpButton.Text = "Add regular expression";
            addRegexpButton.Click += new EventHandler(addRegexpButton_Click);
            regexTable.Controls.Add(addRegexpButton, 1, 0);

            filterPanel.Controls.Add(regexTable, 0, 5);
            filterPanel.SetColumnSpan(regexTable, 2);
        }

        private void comesFromInlineExprBox_Click(object sender, EventArgs e) {
            SettingsObject.Instance.FilterOutAspInlineExpr = (sender as CheckBox).Checked;
            ToolGrid.CheckByPredicate(comesFromInlineExprBox, ToolGrid.IsRowAspExpressionTest);
        }

        private void comesFromElementBox_Click(object sender, EventArgs e) {
            SettingsObject.Instance.FilterOutAspElement = (sender as CheckBox).Checked;
            ToolGrid.CheckByPredicate(comesFromElementBox, ToolGrid.IsRowAspElementTest);
        }

        private void comesFromAspClientCommentBox_Click(object sender, EventArgs e) {
            SettingsObject.Instance.FilterOutAspClientComment = (sender as CheckBox).Checked;
            ToolGrid.CheckByPredicate(comesFromAspClientCommentBox, ToolGrid.IsRowAspClientCommentTest);
        }

        private void comesFromDesignerFileBox_Click(object sender, EventArgs e) {
            SettingsObject.Instance.FilterOutDesignerFile = (sender as CheckBox).Checked;
            ToolGrid.CheckByPredicate(comesFromDesignerFileBox, ToolGrid.IsRowDesignerFileTest);
        }

        private void comesFromAspNetBox_Click(object sender, EventArgs e) {
            SettingsObject.Instance.FilterOutAspNet = (sender as CheckBox).Checked;
            ToolGrid.CheckByPredicate(comesFromAspNetBox, ToolGrid.IsRowFromAspNetTest);
        }

        private void commentBox_Click(object sender, EventArgs e) {
            SettingsObject.Instance.FilterOutSpecificComment = (sender as CheckBox).Checked;
            ToolGrid.CheckByPredicate(commentBox, ToolGrid.IsRowMarkedWithUnlocCommentTest);
        }

        private void capsBox_Click(object sender, EventArgs e) {
            SettingsObject.Instance.FilterOutCaps = (sender as CheckBox).Checked;
            ToolGrid.CheckByPredicate(capsBox, ToolGrid.IsRowCapitalsTest);
        }

        private void noLettersBox_Click(object sender, EventArgs e) {
            SettingsObject.Instance.FilterOutNoLetters = (sender as CheckBox).Checked;
            ToolGrid.CheckByPredicate(noLettersBox, ToolGrid.IsRowNoLettersTest);
        }

        private void localizableBox_Click(object sender, EventArgs e) {
            SettingsObject.Instance.FilterOutUnlocalizable = (sender as CheckBox).Checked;
            ToolGrid.CheckByPredicate(localizableBox, ToolGrid.IsRowUnlocalizableTest);
        }

        private void verbatimBox_Click(object sender, EventArgs e) {
            SettingsObject.Instance.FilterOutVerbatim = (sender as CheckBox).Checked;
            ToolGrid.CheckByPredicate(verbatimBox, ToolGrid.IsRowVerbatimTest);
        }

        private void addRegexpButton_Click(object sender, EventArgs e) {
            filterPanel.SuspendLayout();

            AddRegexpRow("(new regexp)", true);            

            filterPanel.ResumeLayout();
        }

        private void AddRegexpRow(string regexp, bool mustMatch) {
            var newInstance = new SettingsObject.RegexpInstance() {
                MustMatch = mustMatch,
                Regexp = regexp
            };
            
            SettingsObject.Instance.FilterRegexps.Add(newInstance);
            SettingsObject.Instance.NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);

            AddRegexpRow(newInstance);
            ToolGrid.CheckByPredicate(ToolGrid.IsRowMatchingRegexpInstance, newInstance);
        }

        private void AddRegexpRow(SettingsObject.RegexpInstance instance) {
            TextBox regexpBox = new TextBox();
            regexpBox.Text = instance.Regexp;
            regexpBox.Dock = DockStyle.Top;
            regexpBox.Tag = instance;
            regexpBox.LostFocus += new EventHandler(regexpBox_LostFocus);

            RadioButton matchingButton = new RadioButton();
            matchingButton.Text = "Matching";
            matchingButton.Checked = instance.MustMatch;
            matchingButton.Tag = instance;
            matchingButton.Click += new EventHandler(matchingButton_Click);

            RadioButton notMatchingButton = new RadioButton();
            notMatchingButton.Text = "Not matching";
            notMatchingButton.Checked = !instance.MustMatch;
            notMatchingButton.Tag = instance;
            notMatchingButton.Click += new EventHandler(notMatchingButton_Click);

            Button removeButton = new Button();
            removeButton.Click += new EventHandler(removeButton_Click);
            removeButton.Text = "Remove";
            removeButton.Tag = instance;

            Button applyButton = new Button();
            applyButton.Click += new EventHandler(applyButton_Click);
            applyButton.Tag = instance;
            applyButton.Text = "Apply"; 

            int currentRow = regexTable.GetRow(addRegexpButton);
            regexTable.RowCount = currentRow + 2;
            regexTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            regexTable.SetRow(addRegexpButton, currentRow + 1);

            regexTable.Controls.Add(regexpBox, 1, currentRow);
            regexTable.Controls.Add(matchingButton, 2, currentRow);
            regexTable.Controls.Add(notMatchingButton, 3, currentRow);
            regexTable.Controls.Add(applyButton, 4, currentRow);
            regexTable.Controls.Add(removeButton, 5, currentRow);
        }

        private void applyButton_Click(object sender, EventArgs e) {
            Control senderBox = sender as Control;
            SettingsObject.RegexpInstance inst = senderBox.Tag as SettingsObject.RegexpInstance;

            ToolGrid.CheckByPredicate(ToolGrid.IsRowMatchingRegexpInstance, inst);
        }

        private void notMatchingButton_Click(object sender, EventArgs e) {
            Control senderBox = sender as Control;
            SettingsObject.RegexpInstance inst = senderBox.Tag as SettingsObject.RegexpInstance;

            inst.MustMatch = false;
            SettingsObject.Instance.NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);            
        }

        private void matchingButton_Click(object sender, EventArgs e) {
            Control senderBox = sender as Control;
            SettingsObject.RegexpInstance inst = senderBox.Tag as SettingsObject.RegexpInstance;

            inst.MustMatch = true;
            SettingsObject.Instance.NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);            
        }

        private void regexpBox_LostFocus(object sender, EventArgs e) {
            Control senderBox = sender as Control;
            SettingsObject.RegexpInstance inst = senderBox.Tag as SettingsObject.RegexpInstance;

            inst.Regexp = senderBox.Text;
            SettingsObject.Instance.NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);            
        }

        private void removeButton_Click(object sender, EventArgs e) {
            filterPanel.SuspendLayout();
            regexTable.SuspendLayout();

            Button senderButton = sender as Button;
            int row = regexTable.GetRow(senderButton);

            for (int i = 1; i < regexTable.ColumnCount; i++)
                regexTable.Controls.Remove(regexTable.GetControlFromPosition(i, row));

            for (int i = row + 1; i < regexTable.RowCount; i++)
                for (int j = 1; j < regexTable.ColumnCount; j++) {
                    Control c=regexTable.GetControlFromPosition(j, i);
                    if (c != null) regexTable.SetRow(c, i - 1);
                }

            regexTable.RowStyles.RemoveAt(regexTable.RowStyles.Count - 1);
            regexTable.RowCount--;            

            regexTable.ResumeLayout(true);
            filterPanel.ResumeLayout();
            filterPanel.PerformLayout();

            SettingsObject.Instance.FilterRegexps.Remove(senderButton.Tag as SettingsObject.RegexpInstance);
            SettingsObject.Instance.NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);            
        }

        private void grid_HighlightRequired(object sender, CodeResultItemEventArgs e) {
            if (HighlightRequired != null) HighlightRequired(sender, e);
        }

        public BatchMoveToResourcesToolGrid ToolGrid { get; private set; }
        
        private bool _FilterVisible;
        public bool FilterVisible {
            get {
                return _FilterVisible;
            }
            set {
                _FilterVisible = value;                
                splitContainer.Panel1Collapsed = !value;
                if (value) splitContainer.SplitterDistance = SettingsObject.Instance.BatchMoveSplitterDistance;
            }
        }
    }
}
