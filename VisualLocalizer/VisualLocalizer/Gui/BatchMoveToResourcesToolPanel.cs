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

    /// <summary>
    /// Represents tool panel displayed in the "Batch move" toolwindow
    /// </summary>
    internal sealed class BatchMoveToResourcesToolPanel : Panel,IHighlightRequestSource {

        /// <summary>
        /// Issued when row is double-clicked - causes corresponding block of code in the code window to be selected
        /// </summary>
        public event EventHandler<CodeResultItemEventArgs> HighlightRequired;
                       
        /// <summary>
        /// Local copy of filter criteria. This copy can be edited, but the changes will not be saved in settings.
        /// </summary>
        private Dictionary<string, AbstractLocalizationCriterion> filterCriteriaCopy;

        /// <summary>
        /// Names of the custom criteria currently present in the tool panel
        /// </summary>
        private HashSet<string> filterCustomCriteriaNames;

        private TableLayoutPanel filterPanel;
        private SplitContainer splitContainer;
        private bool SplitterMoving;

        /// <summary>
        /// Creates new instance, initialization of GUI
        /// </summary>
        public BatchMoveToResourcesToolPanel() {
            this.SuspendLayout();
            this.Dock = DockStyle.Fill;

            filterCriteriaCopy = new Dictionary<string, AbstractLocalizationCriterion>();
            filterCustomCriteriaNames = new HashSet<string>();

            // update filter criteria on settings load
            SettingsObject.Instance.SettingsLoaded += new Action(SettingsUpdated);

            ToolGrid = new BatchMoveToResourcesToolGrid(this);
            ToolGrid.HighlightRequired += new EventHandler<CodeResultItemEventArgs>(Grid_HighlightRequired);
            
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

            FilterVisible = false; // set filter hidden
            
            splitContainer.SplitterMoved += new SplitterEventHandler(SplitContainer_SplitterMoved);
            splitContainer.SplitterMoving += new SplitterCancelEventHandler(SplitContainer_SplitterMoving);
        }

        /// <summary>
        /// Set SplitterMoving to true
        /// </summary>        
        private void SplitContainer_SplitterMoving(object sender, SplitterCancelEventArgs e) {
            SplitterMoving = true;            
        }

        /// <summary>
        /// After splitter moving finished
        /// </summary>        
        private void SplitContainer_SplitterMoved(object sender, SplitterEventArgs e) {
            if (SplitterMoving) // if splitter was moving before
                SettingsObject.Instance.BatchMoveSplitterDistance = splitContainer.SplitterDistance; // remember splitter distance in settings
            SplitterMoving = false;
        }

        /// <summary>
        /// Called when settings are loaded from registry or are updated in Tools/Options. Refreshes display of custom criteria.
        /// </summary>
        public void SettingsUpdated() {
            splitContainer.SuspendLayout();

            try {
                List<AbstractLocalizationCriterion> toAdd = new List<AbstractLocalizationCriterion>();
                HashSet<string> used = new HashSet<string>();

                foreach (var crit in SettingsObject.Instance.CustomLocalizabilityCriteria) {
                    if (filterCustomCriteriaNames.Contains(crit.Name + "box")) { // the criterion is already present in the tool panel
                        filterPanel.Controls[crit.Name + "label"].Text = crit.Description; // update its text

                        // update its data
                        LocalizationCustomCriterion oldCrit = (LocalizationCustomCriterion)filterCriteriaCopy[crit.Name];
                        oldCrit.Predicate = crit.Predicate;
                        oldCrit.Regex = crit.Regex;
                        oldCrit.Target = crit.Target;

                        // add it to he future list of criteria in the tool panel
                        used.Add(crit.Name + "box");
                        used.Add(crit.Name + "label");
                    } else { // the criterion is not yet in the tool panel - add it
                        toAdd.Add(crit);
                    }
                }

                // remove all criteria that were not "touched" by previous operation, i.e. were deleted in the settings
                foreach (string name in filterCustomCriteriaNames.Except(used)) {
                    filterPanel.Controls.RemoveByKey(name); // remove from GUI
                    if (name.EndsWith("box")) filterCriteriaCopy.Remove(name.Substring(0, name.Length - 3)); // remove from local copy of criteria
                }

                filterCustomCriteriaNames = used; // set new list of criteria in the tool panel
                foreach (var crit in toAdd) { // add new custom criteria
                    AddCriterionOption(crit);
                }

                // recalculate localization probability with new criteria
                ToolGrid.RecalculateLocProbability(filterCriteriaCopy, false);

                splitContainer.SplitterDistance = SettingsObject.Instance.BatchMoveSplitterDistance;
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }

            splitContainer.ResumeLayout();
        }

        /// <summary>
        /// Creates filter panel GUI
        /// </summary>
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

            // add criteria boxes
            foreach (var crit in SettingsObject.Instance.CommonLocalizabilityCriteria.Values.Combine<AbstractLocalizationCriterion, LocalizationCommonCriterion, LocalizationCustomCriterion>(SettingsObject.Instance.CustomLocalizabilityCriteria)) {
                AddCriterionOption(crit);
            }      
        }

        /// <summary>
        /// Adds given criterion to the GUI and local copy of criteria
        /// </summary>       
        private void AddCriterionOption(AbstractLocalizationCriterion crit) {
            if (crit == null) throw new ArgumentNullException("crit");

            try {
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
                box.DropDownWidth = 200;
                box.Name = crit.Name + "box";
                box.SelectedIndex = (int)crit.Action;
                box.SelectedIndexChanged += new EventHandler(Box_SelectedIndexChanged);

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

                // add deep copy of the criterion to the local copy of criteria
                filterCriteriaCopy.Add(crit.Name, crit.DeepCopy());
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        private bool ignoreLocRecalculation = false;

        /// <summary>
        /// Sets actions of all criteria to values from settings and performs recalculation of localization probability.
        /// </summary>
        public void ResetFilterSettings() {
            try {
                ignoreLocRecalculation = true; // prevent changes in checkboxes to trigger localization probability recalculation

                // update the actions
                foreach (var crit in SettingsObject.Instance.CommonLocalizabilityCriteria.Values.Combine<AbstractLocalizationCriterion, LocalizationCommonCriterion, LocalizationCustomCriterion>(SettingsObject.Instance.CustomLocalizabilityCriteria)) {
                    filterCriteriaCopy[crit.Name].Action = crit.Action;
                    (filterPanel.Controls[crit.Name + "box"] as ComboBox).SelectedIndex = (int)crit.Action;
                }
                ignoreLocRecalculation = false;

                // recalculate loc. probability
                ToolGrid.RecalculateLocProbability(filterCriteriaCopy, true);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            } finally {
                ignoreLocRecalculation = false;
            }
        }

        /// <summary>
        /// Criterion action combobox value changed
        /// </summary>        
        private void Box_SelectedIndexChanged(object sender, EventArgs e) {
            ComboBox cBox = (sender as ComboBox);
            if (cBox.SelectedIndex == -1) return;
            if (ignoreLocRecalculation) return; // ignore the change (set in ResetFilterSettings())

            try {
                string critName = (string)cBox.Tag;
                if (Enum.IsDefined(typeof(LocalizationCriterionAction), cBox.SelectedIndex)) { // it is a standard criterion action (Force localization, Ignore...)
                    LocalizationCriterionAction newAction = (LocalizationCriterionAction)cBox.SelectedIndex;

                    filterCriteriaCopy[critName].Action = newAction; // set the value in local copy of criteria
                    ToolGrid.RecalculateLocProbability(filterCriteriaCopy, false); // recalculate localization probability
                } else { // it is a special action (Check, Uncheck, ...)
                    LocalizationCriterionAction2 newAction = (LocalizationCriterionAction2)(cBox.SelectedIndex - Enum.GetValues(typeof(LocalizationCriterionAction)).Length);

                    ToolGrid.ApplyFilterAction(filterCriteriaCopy[critName], newAction); // apply the action
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }       
       
        /// <summary>
        /// Row double-clicked
        /// </summary>        
        private void Grid_HighlightRequired(object sender, CodeResultItemEventArgs e) {
            if (HighlightRequired != null) HighlightRequired(sender, e);
        }

        /// <summary>
        /// Inner grid containing the result items
        /// </summary>
        public BatchMoveToResourcesToolGrid ToolGrid { get; private set; }
        
        private bool _FilterVisible;

        /// <summary>
        /// Gets / sets visiblity of the filter tool panel
        /// </summary>
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
