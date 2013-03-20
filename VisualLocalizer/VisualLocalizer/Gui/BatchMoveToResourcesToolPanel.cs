using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using VisualLocalizer.Components;
using VisualLocalizer.Settings;
using VisualLocalizer.Extensions;
using VisualLocalizer.Library;

namespace VisualLocalizer.Gui {
    internal sealed class BatchMoveToResourcesToolPanel : Panel,IHighlightRequestSource {
        
        public event EventHandler<CodeResultItemEventArgs> HighlightRequired;
        private TableLayoutPanel filterPanel;
        private SplitContainer splitContainer;
        private bool SplitterMoving;
        private Dictionary<string, AbstractLocalizationCriterion> filterCriteriaCopy;
        private HashSet<string> filterCustomCriteriaNames;

        public BatchMoveToResourcesToolPanel() {
            this.SuspendLayout();
            this.Dock = DockStyle.Fill;

            filterCriteriaCopy = new Dictionary<string, AbstractLocalizationCriterion>();
            filterCustomCriteriaNames = new HashSet<string>();

            SettingsObject.Instance.SettingsLoaded += new Action(SettingsUpdated);

            ToolGrid = new BatchMoveToResourcesToolGrid(this);
            ToolGrid.HighlightRequired += new EventHandler<CodeResultItemEventArgs>(grid_HighlightRequired);
            
            InitializeFilterPanel();

            splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Orientation = Orientation.Horizontal;
            splitContainer.Panel1.AutoScroll = true;
            splitContainer.Panel1.VerticalScroll.Visible = true;
            splitContainer.Panel1.VerticalScroll.Enabled = true;
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
            splitContainer.SuspendLayout();

            List<AbstractLocalizationCriterion> toAdd = new List<AbstractLocalizationCriterion>();
            HashSet<string> used = new HashSet<string>();
            
            foreach (var crit in SettingsObject.Instance.CustomLocalizabilityCriteria) {
                if (filterCustomCriteriaNames.Contains(crit.Name + "box")) {
                    filterPanel.Controls[crit.Name + "label"].Text = crit.Description;
                    
                    LocalizationCustomCriterion oldCrit= (LocalizationCustomCriterion)filterCriteriaCopy[crit.Name];
                    oldCrit.Predicate = crit.Predicate;
                    oldCrit.Regex = crit.Regex;
                    oldCrit.Target = crit.Target;                    

                    used.Add(crit.Name + "box");
                    used.Add(crit.Name + "label");
                } else {
                    toAdd.Add(crit);
                }
            }

            foreach (string name in filterCustomCriteriaNames.Except(used)) {
                filterPanel.Controls.RemoveByKey(name);
                if (name.EndsWith("box")) filterCriteriaCopy.Remove(name.Substring(0, name.Length - 3));
            }

            filterCustomCriteriaNames = used;
            foreach (var crit in toAdd) {
                addCriterionOption(crit);
            }

            ToolGrid.RecalculateLocProbability(filterCriteriaCopy, false);

            splitContainer.SplitterDistance = SettingsObject.Instance.BatchMoveSplitterDistance;
            splitContainer.ResumeLayout();
        }

        private void InitializeFilterPanel() {
            filterPanel = new TableLayoutPanel();
            filterPanel.Dock = DockStyle.Top;
            filterPanel.AutoSize = true;
            filterPanel.Padding = new Padding(0, 0, SystemInformation.VerticalScrollBarWidth, 0);
            filterPanel.ColumnCount = 4;
            filterPanel.ColumnStyles.Clear();
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
         
            int rowCount = (int)Math.Ceiling(filterCriteriaCopy.Count / 2.0);

            filterPanel.RowCount = rowCount + 1;
            filterPanel.RowStyles.Clear();
            for (int i = 0; i < rowCount; i++) filterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            filterPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            foreach (var crit in SettingsObject.Instance.CommonLocalizabilityCriteria.Values.Combine<AbstractLocalizationCriterion, LocalizationCriterion, LocalizationCustomCriterion>(SettingsObject.Instance.CustomLocalizabilityCriteria)) {
                addCriterionOption(crit);
            }
      
        }

        private void addCriterionOption(AbstractLocalizationCriterion crit) {
            ComboBox box = null;
            Label label = null;

            box = new ComboBox();
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            box.Tag = crit.Name;
            box.Items.Add(LocalizationCriterionAction.FORCE_ENABLE.ToHumanForm());
            box.Items.Add(LocalizationCriterionAction.FORCE_DISABLE.ToHumanForm());
            box.Items.Add(LocalizationCriterionAction.VALUE.ToHumanForm() + " " + crit.Weight);
            box.Items.Add(LocalizationCriterionAction.IGNORE.ToHumanForm());
            box.Items.Add(LocalizationCriterionAction2.CHECK.ToHumanForm());
            box.Items.Add(LocalizationCriterionAction2.CHECK_REMOVE.ToHumanForm());
            box.Items.Add(LocalizationCriterionAction2.UNCHECK.ToHumanForm());
            box.Items.Add(LocalizationCriterionAction2.REMOVE.ToHumanForm());
            box.Width = 130;
            box.Name = crit.Name + "box";
            box.SelectedIndex = (int)crit.Action;
            box.SelectedIndexChanged += new EventHandler(box_SelectedIndexChanged);

            label = new Label();
            label.Name = crit.Name + "label";
            label.Text = crit.Description;
            label.Dock = DockStyle.Fill;
            label.AutoEllipsis = true;
            label.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            filterPanel.Controls.Add(label);
            filterPanel.Controls.Add(box);

            if (crit is LocalizationCustomCriterion) {
                filterCustomCriteriaNames.Add(label.Name);
                filterCustomCriteriaNames.Add(box.Name);
            }

            filterCriteriaCopy.Add(crit.Name, crit.DeepCopy());
        }

        private bool ignoreLocRecalculation = false;
        public void ResetFilterSettings() {
            ignoreLocRecalculation=true;
            foreach (var crit in SettingsObject.Instance.CommonLocalizabilityCriteria.Values.Combine<AbstractLocalizationCriterion, LocalizationCriterion, LocalizationCustomCriterion>(SettingsObject.Instance.CustomLocalizabilityCriteria)) {
                filterCriteriaCopy[crit.Name].Action = crit.Action;
                (filterPanel.Controls[crit.Name + "box"] as ComboBox).SelectedIndex = (int)crit.Action;
            }
            ignoreLocRecalculation = false;
            ToolGrid.RecalculateLocProbability(filterCriteriaCopy, true);
        }

        private void box_SelectedIndexChanged(object sender, EventArgs e) {
            ComboBox cBox = (sender as ComboBox);
            if (cBox.SelectedIndex == -1) return;
            if (ignoreLocRecalculation) return;

            try {
                string critName = (string)cBox.Tag;
                if (Enum.IsDefined(typeof(LocalizationCriterionAction), cBox.SelectedIndex)) {
                    LocalizationCriterionAction newAction = (LocalizationCriterionAction)cBox.SelectedIndex;

                    filterCriteriaCopy[critName].Action = newAction;
                    ToolGrid.RecalculateLocProbability(filterCriteriaCopy, false);
                } else {
                    LocalizationCriterionAction2 newAction = (LocalizationCriterionAction2)(cBox.SelectedIndex - Enum.GetValues(typeof(LocalizationCriterionAction)).Length);

                    ToolGrid.ApplyFilterAction(filterCriteriaCopy[critName], newAction);
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
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
