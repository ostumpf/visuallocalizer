﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using System.Windows.Forms;
using System.Drawing;
using VisualLocalizer.Components;
using System.IO;
using System.Text.RegularExpressions;

namespace VisualLocalizer.Settings {

    /// <summary>
    /// Represents page of Filter settings in Tools/Options and also handles saving the FILTER category of settings 
    /// </summary>
    [Guid("24E41562-4D26-4166-BCB8-B10F773A6A18")]
    internal sealed class FilterSettingsManager : AbstractSettingsManager {

        private TableLayoutPanel tablePanel, customCriteriaTable;
        private bool closed = true;
        private CheckBox contextBox, reflectionBox;
        private DataGridView commonCriteriaGrid;
        private DataGridViewComboBoxColumn actionColumn;
        private Button addCustomCriteriaButton;
        private Dictionary<string, int> commonCriteriaRowMap = new Dictionary<string, int>();
        private GroupBox customCriteriaBox;

        #region IProfileManager

        /// <summary>
        /// Loads settings from registry storage (on package load)
        /// </summary>
        public override void LoadSettingsFromStorage() {
            VisualLocalizerPackage package = VisualLocalizerPackage.Instance;
            RegistryKey rootKey = package.UserRegistryRoot;
            RegistryKey settingsKey = null;
            try {
                settingsKey = rootKey.OpenSubKey(REG_KEY, false);
                if (settingsKey != null) {
                    RegistryKey filtersKey = settingsKey.OpenSubKey(FILTER_KEY);

                    if (filtersKey != null) {
                        SettingsObject.Instance.IgnorePropertyChanges = true;

                        SettingsObject.Instance.ShowContextColumn = ReadBoolFromRegKey(filtersKey, "ShowFilterContext");
                        SettingsObject.Instance.UseReflectionInAsp = ReadBoolFromRegKey(filtersKey, "UseReflectionInAsp");
                        SettingsObject.Instance.NamespacePolicyIndex = ReadIntFromRegKey(filtersKey, "NamespacePolicyIndex");
                        SettingsObject.Instance.MarkNotLocalizableStringsIndex = ReadIntFromRegKey(filtersKey, "MarkNotLocalizableStringsIndex");
                        SettingsObject.Instance.BatchMoveSplitterDistance = ReadIntFromRegKey(filtersKey, "BatchMoveSplitterDistance", 110);
                        SettingsObject.Instance.SelectedKeyIndex = ReadIntFromRegKey(filtersKey, "SelectedKeyIndex", 0);
                        SettingsObject.Instance.SelectedUseFullName = ReadBoolFromRegKey(filtersKey, "SelectedUseFullName");
                        SettingsObject.Instance.SelectedResourceFilePath = (string)filtersKey.GetValue("SelectedResourceFilePath", null);

                        foreach (var pair in SettingsObject.Instance.CommonLocalizabilityCriteria) {
                            LocalizationCommonCriterion crit = pair.Value;
                            object val = filtersKey.GetValue(crit.Name);
                            if (val != null) {
                                crit.FromRegData(val.ToString());
                            }
                        }

                        SettingsObject.Instance.CustomLocalizabilityCriteria.Clear();                        
                        int customCriteriaCount = ReadIntFromRegKey(filtersKey, "CustomCriteriaCount", 0);
                        for (int i = 0; i < customCriteriaCount; i++) {
                            object val = filtersKey.GetValue("CustomCriteria" + i);
                            if (val != null) {
                                LocalizationCustomCriterion crit = new LocalizationCustomCriterion(LocalizationCriterionAction.VALUE, 0);
                                crit.FromRegData(val.ToString());
                                SettingsObject.Instance.CustomLocalizabilityCriteria.Add(crit);
                            }                            
                        }

                    } else ResetSettings();
                } else ResetSettings();
            } finally {
                if (settingsKey != null) settingsKey.Close();
                
                SettingsObject.Instance.IgnorePropertyChanges = false;
                SettingsObject.Instance.NotifySettingsLoaded();
            }
        }

        /// <summary>
        /// Loads settings from XML (on import settings)
        /// </summary>        
        public override void LoadSettingsFromXml(IVsSettingsReader reader) {
            SettingsObject.Instance.IgnorePropertyChanges = true;

            SettingsObject.Instance.ShowContextColumn = ReadBoolFromXml(reader, "ShowFilterContext");
            SettingsObject.Instance.UseReflectionInAsp = ReadBoolFromXml(reader, "UseReflectionInAsp");
            SettingsObject.Instance.MarkNotLocalizableStringsIndex = ReadIntFromXml(reader, "MarkNotLocalizableStringsIndex");
            SettingsObject.Instance.NamespacePolicyIndex = ReadIntFromXml(reader, "NamespacePolicyIndex");
            SettingsObject.Instance.BatchMoveSplitterDistance = ReadIntFromXml(reader, "BatchMoveSplitterDistance");
            SettingsObject.Instance.SelectedKeyIndex = ReadIntFromXml(reader, "SelectedKeyIndex");
            SettingsObject.Instance.SelectedUseFullName = ReadBoolFromXml(reader, "SelectedUseFullName");

            int hr;
            string tmp;
            hr = reader.ReadSettingString("SelectedResourceFilePath", out tmp);
            SettingsObject.Instance.SelectedResourceFilePath = tmp;
            Marshal.ThrowExceptionForHR(hr);
            
            foreach (var pair in SettingsObject.Instance.CommonLocalizabilityCriteria) {
                LocalizationCommonCriterion crit = pair.Value;
                
                string val;
                hr = reader.ReadSettingString(crit.Name, out val);
                if (hr != VSConstants.S_OK) throw new Exception("Error retrieving data from XML.");

                if (val != null) {
                    crit.FromRegData(val.ToString());
                }
            }

            SettingsObject.Instance.CustomLocalizabilityCriteria.Clear();
            int customCriteriaCount = 0;
            hr = reader.ReadSettingLong("CustomCriteriaCount", out customCriteriaCount);
            if (hr != VSConstants.S_OK) throw new Exception("Error retrieving data from XML.");

            for (int i = 0; i < customCriteriaCount; i++) {
                string val;
                hr = reader.ReadSettingString("CustomCriteria" + i, out val);

                if (val != null) {
                    LocalizationCustomCriterion crit = new LocalizationCustomCriterion(LocalizationCriterionAction.VALUE, 0);
                    crit.FromRegData(val.ToString());
                    SettingsObject.Instance.CustomLocalizabilityCriteria.Add(crit);
                }
            }

            SettingsObject.Instance.IgnorePropertyChanges = false;
            SettingsObject.Instance.NotifySettingsLoaded();
        }

        /// <summary>
        /// Never called by VS (bug?)
        /// </summary>
        public override void ResetSettings() {
            SettingsObject.Instance.IgnorePropertyChanges = true;

            SettingsObject.Instance.MarkNotLocalizableStringsIndex = 0;
            SettingsObject.Instance.NamespacePolicyIndex = 0;
            SettingsObject.Instance.BatchMoveSplitterDistance = 130;
            SettingsObject.Instance.ShowContextColumn = false;
            SettingsObject.Instance.UseReflectionInAsp = true;
            SettingsObject.Instance.SelectedKeyIndex = 0;
            SettingsObject.Instance.SelectedResourceFilePath = null;
            SettingsObject.Instance.SelectedUseFullName = false;
    
            SettingsObject.Instance.ResetCriteria();

            // create list of default criteria
            var valueIsFormat = new LocalizationCustomCriterion(LocalizationCriterionAction.VALUE, 20);
            valueIsFormat.Predicate = LocalizationCriterionPredicate.MATCHES;
            valueIsFormat.Target = LocalizationCriterionTarget.VALUE;
            valueIsFormat.Regex = @"{\d+(:.+)?}(?# formatting placeholders)";
            valueIsFormat.Name = Path.GetRandomFileName();
            SettingsObject.Instance.CustomLocalizabilityCriteria.Add(valueIsFormat);

            var valueIsMultiline = new LocalizationCustomCriterion(LocalizationCriterionAction.FORCE_ENABLE, 0);
            valueIsMultiline.Predicate = LocalizationCriterionPredicate.MATCHES;
            valueIsMultiline.Target = LocalizationCriterionTarget.VALUE;
            valueIsMultiline.Regex = "\\w+.*\r\n\\w+.*(?# end of line characters)";
            valueIsMultiline.Name = Path.GetRandomFileName();
            SettingsObject.Instance.CustomLocalizabilityCriteria.Add(valueIsMultiline);

            var valueNoLetters = new LocalizationCustomCriterion(LocalizationCriterionAction.FORCE_DISABLE, 0);
            valueNoLetters.Predicate = LocalizationCriterionPredicate.NO_LETTERS;
            valueNoLetters.Target = LocalizationCriterionTarget.VALUE;
            valueNoLetters.Regex = string.Empty;
            valueNoLetters.Name = Path.GetRandomFileName();
            SettingsObject.Instance.CustomLocalizabilityCriteria.Add(valueNoLetters);

            var valueCaps = new LocalizationCustomCriterion(LocalizationCriterionAction.FORCE_DISABLE, 0);
            valueCaps.Predicate = LocalizationCriterionPredicate.ONLY_CAPS;
            valueCaps.Target = LocalizationCriterionTarget.VALUE;
            valueCaps.Regex = string.Empty;
            valueCaps.Name = Path.GetRandomFileName();
            SettingsObject.Instance.CustomLocalizabilityCriteria.Add(valueCaps);

            var valueWhitespace = new LocalizationCustomCriterion(LocalizationCriterionAction.VALUE, 30);
            valueWhitespace.Predicate = LocalizationCriterionPredicate.MATCHES;
            valueWhitespace.Target = LocalizationCriterionTarget.VALUE;
            valueWhitespace.Regex = @"\w+\s+\w+(?# contains whitespace)";
            valueWhitespace.Name = Path.GetRandomFileName();
            SettingsObject.Instance.CustomLocalizabilityCriteria.Add(valueWhitespace);
            
            var smallLength = new LocalizationCustomCriterion(LocalizationCriterionAction.VALUE, -60);
            smallLength.Predicate = LocalizationCriterionPredicate.DOESNT_MATCH;
            smallLength.Target = LocalizationCriterionTarget.VALUE;
            smallLength.Name = Path.GetRandomFileName();
            smallLength.Regex = "..(?# too short)";
            SettingsObject.Instance.CustomLocalizabilityCriteria.Add(smallLength);

            var ipv4Address = new LocalizationCustomCriterion(LocalizationCriterionAction.FORCE_DISABLE, 0);
            ipv4Address.Predicate = LocalizationCriterionPredicate.MATCHES;
            ipv4Address.Target = LocalizationCriterionTarget.VALUE;
            ipv4Address.Name = Path.GetRandomFileName();
            ipv4Address.Regex = @"^\d+\.\d+\.\d+.\d+$(?# IPv4 address)";
            SettingsObject.Instance.CustomLocalizabilityCriteria.Add(ipv4Address);

            var ipv6Address = new LocalizationCustomCriterion(LocalizationCriterionAction.FORCE_DISABLE, 0);
            ipv6Address.Predicate = LocalizationCriterionPredicate.MATCHES;
            ipv6Address.Target = LocalizationCriterionTarget.VALUE;
            ipv6Address.Name = Path.GetRandomFileName();
            ipv6Address.Regex = @"^[0-9a-fA-F]+(:?([0-9a-fA-F]+)?)*:[0-9a-fA-F]+$(?# IPv6 address)";
            SettingsObject.Instance.CustomLocalizabilityCriteria.Add(ipv6Address);

            var link = new LocalizationCustomCriterion(LocalizationCriterionAction.FORCE_DISABLE, 0);
            link.Predicate = LocalizationCriterionPredicate.MATCHES;
            link.Target = LocalizationCriterionTarget.VALUE;
            link.Name = Path.GetRandomFileName();
            link.Regex = @"^(http|https|ftp)://.*$(?# URL)";
            SettingsObject.Instance.CustomLocalizabilityCriteria.Add(link);

            var camelCase = new LocalizationCustomCriterion(LocalizationCriterionAction.VALUE, -80);
            camelCase.Predicate = LocalizationCriterionPredicate.MATCHES;
            camelCase.Target = LocalizationCriterionTarget.VALUE;
            camelCase.Name = Path.GetRandomFileName();
            camelCase.Regex = @"^[0-9A-Z]?\w+([0-9A-Z](\w+)?)+$(?# camel case)";
            SettingsObject.Instance.CustomLocalizabilityCriteria.Add(camelCase);

            var reference = new LocalizationCustomCriterion(LocalizationCriterionAction.VALUE, -60);
            reference.Predicate = LocalizationCriterionPredicate.MATCHES;
            reference.Target = LocalizationCriterionTarget.VALUE;
            reference.Name = Path.GetRandomFileName();
            reference.Regex = @"^\w+(\.\w+)*\.\w+$(?# reference)";
            SettingsObject.Instance.CustomLocalizabilityCriteria.Add(reference);

            var filepath = new LocalizationCustomCriterion(LocalizationCriterionAction.VALUE, -40);
            filepath.Predicate = LocalizationCriterionPredicate.MATCHES;
            filepath.Target = LocalizationCriterionTarget.VALUE;
            filepath.Name = Path.GetRandomFileName();
            filepath.Regex = @"^.+([/\\].+)*[/\\].+$(?# file path)";
            SettingsObject.Instance.CustomLocalizabilityCriteria.Add(filepath);

            var menuitem = new LocalizationCustomCriterion(LocalizationCriterionAction.VALUE, 30);
            menuitem.Predicate = LocalizationCriterionPredicate.MATCHES;
            menuitem.Target = LocalizationCriterionTarget.VALUE;
            menuitem.Name = Path.GetRandomFileName();
            menuitem.Regex = @"^.*&\w.*$(?# menu item text)";
            SettingsObject.Instance.CustomLocalizabilityCriteria.Add(menuitem);

            var relativePath = new LocalizationCustomCriterion(LocalizationCriterionAction.VALUE, -60);
            relativePath.Predicate = LocalizationCriterionPredicate.MATCHES;
            relativePath.Target = LocalizationCriterionTarget.VALUE;
            relativePath.Name = Path.GetRandomFileName();
            relativePath.Regex = @"^~[/\\].*$(?# relative path)";
            SettingsObject.Instance.CustomLocalizabilityCriteria.Add(relativePath);

            var sqlQuery = new LocalizationCustomCriterion(LocalizationCriterionAction.VALUE, -80);
            sqlQuery.Predicate = LocalizationCriterionPredicate.MATCHES;
            sqlQuery.Target = LocalizationCriterionTarget.VALUE;
            sqlQuery.Name = Path.GetRandomFileName();
            sqlQuery.Regex = @"^\s*(SELECT|UPDATE|DELETE|CREATE|DROP|ALTER|REMOVE|INSERT|SHOW|TRUNCATE).*$(?# SQL query)";
            SettingsObject.Instance.CustomLocalizabilityCriteria.Add(sqlQuery);

            var email = new LocalizationCustomCriterion(LocalizationCriterionAction.VALUE, -60);
            email.Predicate = LocalizationCriterionPredicate.MATCHES;
            email.Target = LocalizationCriterionTarget.VALUE;
            email.Name = Path.GetRandomFileName();
            email.Regex = @"^\w+@\w+\.\w+.*$(?# email)";
            SettingsObject.Instance.CustomLocalizabilityCriteria.Add(email);

            var style = new LocalizationCustomCriterion(LocalizationCriterionAction.VALUE, -80);
            style.Predicate = LocalizationCriterionPredicate.MATCHES;
            style.Target = LocalizationCriterionTarget.ELEMENT_NAME;
            style.Name = Path.GetRandomFileName();
            style.Regex = @"^style$(?# CSS)";
            SettingsObject.Instance.CustomLocalizabilityCriteria.Add(style);

            var names = new LocalizationCustomCriterion(LocalizationCriterionAction.FORCE_DISABLE, 0);
            names.Predicate = LocalizationCriterionPredicate.MATCHES;
            names.Target = LocalizationCriterionTarget.LINE;
            names.Name = Path.GetRandomFileName();
            names.Regex = @".+\.Name =(?# something.Name = <string>)";
            SettingsObject.Instance.CustomLocalizabilityCriteria.Add(names);

            var diacritics = new LocalizationCustomCriterion(LocalizationCriterionAction.FORCE_ENABLE, 0);
            diacritics.Predicate = LocalizationCriterionPredicate.MATCHES;
            diacritics.Target = LocalizationCriterionTarget.VALUE;
            diacritics.Name = Path.GetRandomFileName();
            diacritics.Regex = @"[ěščřžýáíéúůďťňóĚŠČŘŽÝÁÍÉÚŮĎŤÓöüäëÖÜÄËľĽĹĺôÔ](?# common culture-specific characters)";
            SettingsObject.Instance.CustomLocalizabilityCriteria.Add(diacritics);

            SettingsObject.Instance.IgnorePropertyChanges = false;
            SettingsObject.Instance.NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);
            SettingsObject.Instance.NotifySettingsLoaded();
        }

        /// <summary>
        /// Saves settings to registry storage
        /// </summary>
        public override void SaveSettingsToStorage() {
            VisualLocalizerPackage package = VisualLocalizerPackage.Instance;
            RegistryKey rootKey = package.UserRegistryRoot;
            RegistryKey settingsKey = null;
            try {
                settingsKey = rootKey.CreateSubKey(REG_KEY);
                RegistryKey filterKey = settingsKey.CreateSubKey(FILTER_KEY);

                WriteBoolToRegKey(filterKey, "ShowFilterContext", SettingsObject.Instance.ShowContextColumn);
                WriteBoolToRegKey(filterKey, "UseReflectionInAsp", SettingsObject.Instance.UseReflectionInAsp);
                WriteIntToRegKey(filterKey, "NamespacePolicyIndex", SettingsObject.Instance.NamespacePolicyIndex);
                WriteIntToRegKey(filterKey, "MarkNotLocalizableStringsIndex", SettingsObject.Instance.MarkNotLocalizableStringsIndex);
                WriteIntToRegKey(filterKey, "BatchMoveSplitterDistance", SettingsObject.Instance.BatchMoveSplitterDistance);
                WriteIntToRegKey(filterKey, "SelectedKeyIndex", SettingsObject.Instance.SelectedKeyIndex);
                WriteBoolToRegKey(filterKey, "SelectedUseFullName", SettingsObject.Instance.SelectedUseFullName);

                if (SettingsObject.Instance.SelectedResourceFilePath != null) {
                    filterKey.SetValue("SelectedResourceFilePath", SettingsObject.Instance.SelectedResourceFilePath);
                }

                foreach (var pair in SettingsObject.Instance.CommonLocalizabilityCriteria) {
                    LocalizationCommonCriterion crit = pair.Value;
                    filterKey.SetValue(crit.Name, crit.ToRegData());
                }

                int count = ReadIntFromRegKey(filterKey, "CustomCriteriaCount");
                for (int i = 0; i < count; i++) {
                    filterKey.DeleteValue("CustomCriteria" + i, false);                    
                }
                for (int i=0;i< SettingsObject.Instance.CustomLocalizabilityCriteria.Count;i++) {
                    filterKey.SetValue("CustomCriteria" + i, SettingsObject.Instance.CustomLocalizabilityCriteria[i].ToRegData());
                }
                WriteIntToRegKey(filterKey, "CustomCriteriaCount", SettingsObject.Instance.CustomLocalizabilityCriteria.Count);
            } finally {
                if (settingsKey != null) settingsKey.Close();
            }
        }

        /// <summary>
        /// Saves settings to XML (on settings export)
        /// </summary>
        public override void SaveSettingsToXml(IVsSettingsWriter writer) {
            WriteBoolToXml(writer, "ShowFilterContext", SettingsObject.Instance.ShowContextColumn);
            WriteBoolToXml(writer, "UseReflectionInAsp", SettingsObject.Instance.UseReflectionInAsp);
            WriteIntToXml(writer, "MarkNotLocalizableStringsIndex", SettingsObject.Instance.MarkNotLocalizableStringsIndex);
            WriteIntToXml(writer, "NamespacePolicyIndex", SettingsObject.Instance.NamespacePolicyIndex);
            WriteIntToXml(writer, "BatchMoveSplitterDistance", SettingsObject.Instance.BatchMoveSplitterDistance);
            WriteIntToXml(writer, "SelectedKeyIndex", SettingsObject.Instance.SelectedKeyIndex);
            WriteBoolToXml(writer, "SelectedUseFullName", SettingsObject.Instance.SelectedUseFullName);

            if (SettingsObject.Instance.SelectedResourceFilePath != null) {
                writer.WriteSettingString("SelectedResourceFilePath", SettingsObject.Instance.SelectedResourceFilePath);
            }

            foreach (var pair in SettingsObject.Instance.CommonLocalizabilityCriteria) {
                LocalizationCommonCriterion crit = pair.Value;
                writer.WriteSettingString(crit.Name, crit.ToRegData());                
            }

            WriteIntToXml(writer, "CustomCriteriaCount", SettingsObject.Instance.CustomLocalizabilityCriteria.Count);

            for (int i = 0; i < SettingsObject.Instance.CustomLocalizabilityCriteria.Count; i++) {
                int hr = writer.WriteSettingString("CustomCriteria" + i, SettingsObject.Instance.CustomLocalizabilityCriteria[i].ToRegData());
                if (hr != VSConstants.S_OK) writer.ReportError("CustomCriteria value cannot be written", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);
            }
        }

        #endregion

        #region DialogPage

        /// <summary>
        /// Returns the content control
        /// </summary>
        protected override IWin32Window Window {
            get {
                try {
                    if (tablePanel == null) InitializeDialogPage();
                    PopulateTable();
                    return tablePanel;
                } catch (Exception ex) {
                    VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                    VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
                }
                return null;
            }
        }

        /// <summary>
        /// Creates the GUI
        /// </summary>
        private void InitializeDialogPage() {
            try {
                tablePanel = new TableLayoutPanel();
                tablePanel.Dock = DockStyle.Fill;
                tablePanel.AutoScroll = true;
                tablePanel.AutoSize = true;
                tablePanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                tablePanel.Padding = new Padding(0, 0, 10, 0);

                tablePanel.RowCount = 2;
                tablePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                tablePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                tablePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                tablePanel.ColumnCount = 1;
                tablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                GroupBox generalBox = new GroupBox();
                generalBox.Text = "General";
                generalBox.Dock = DockStyle.Fill;
                generalBox.AutoSize = true;

                FlowLayoutPanel generalInnerPanel = new FlowLayoutPanel();
                generalInnerPanel.FlowDirection = FlowDirection.TopDown;
                generalInnerPanel.Dock = DockStyle.Fill;
                generalInnerPanel.AutoSize = true;

                contextBox = new CheckBox();
                contextBox.Text = "Show context column";
                contextBox.Margin = new Padding(3, 3, 0, 0);
                contextBox.AutoSize = true;

                reflectionBox = new CheckBox();
                reflectionBox.Text = "Determine types of attributes in ASP .NET elements\n(potentially slow)";
                reflectionBox.CheckAlign = ContentAlignment.TopLeft;
                reflectionBox.Margin = new Padding(3, 3, 0, 0);
                reflectionBox.AutoSize = true;

                generalInnerPanel.Controls.Add(contextBox);
                generalInnerPanel.Controls.Add(reflectionBox);
                generalBox.Controls.Add(generalInnerPanel);

                GroupBox criteriaBox = new GroupBox();
                criteriaBox.Text = "Localizability Criteria";
                criteriaBox.Dock = DockStyle.Fill;
                criteriaBox.AutoSize = true;

                commonCriteriaGrid = new DataGridView();
                commonCriteriaGrid.Dock = DockStyle.Fill;
                commonCriteriaGrid.AutoSize = true;
                commonCriteriaGrid.AllowUserToOrderColumns = false;
                commonCriteriaGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                commonCriteriaGrid.MultiSelect = false;
                commonCriteriaGrid.RowHeadersVisible = false;
                commonCriteriaGrid.ScrollBars = ScrollBars.Vertical;
                commonCriteriaGrid.BackgroundColor = System.Drawing.SystemColors.Control;
                commonCriteriaGrid.BorderStyle = BorderStyle.None;
                commonCriteriaGrid.AllowUserToAddRows = false;
                commonCriteriaGrid.AllowUserToDeleteRows = false;
                commonCriteriaGrid.CellValueChanged += new DataGridViewCellEventHandler(CommonCriteriaGrid_CellValueChanged);

                var descrColumn = new DataGridViewTextBoxColumn();
                descrColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                descrColumn.HeaderText = "Description";
                descrColumn.ReadOnly = true;
                commonCriteriaGrid.Columns.Add(descrColumn);

                actionColumn = new DataGridViewComboBoxColumn();
                actionColumn.ReadOnly = false;
                actionColumn.Width = 100;

                foreach (var o in Enum.GetValues(typeof(LocalizationCriterionAction)))
                    actionColumn.Items.Add(((LocalizationCriterionAction)o).ToHumanForm());

                actionColumn.HeaderText = "Action";
                commonCriteriaGrid.Columns.Add(actionColumn);

                var additionalColumns = new DataGridViewTextBoxColumn();
                additionalColumns.HeaderText = "Value";
                additionalColumns.ReadOnly = true;
                additionalColumns.Width = 50;
                commonCriteriaGrid.Columns.Add(additionalColumns);

                foreach (var pair in SettingsObject.Instance.CommonLocalizabilityCriteria) {
                    LocalizationCommonCriterion crit = pair.Value;

                    int rowIndex = commonCriteriaGrid.Rows.Add();
                    commonCriteriaGrid.Rows[rowIndex].Cells[0].Value = crit.Description;
                    commonCriteriaRowMap.Add(crit.Name, rowIndex);
                }
                criteriaBox.Controls.Add(commonCriteriaGrid);

                customCriteriaBox = new GroupBox();
                customCriteriaBox.AutoSize = true;
                customCriteriaBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                customCriteriaBox.Text = "Custom Localizability Criteria";
                customCriteriaBox.Dock = DockStyle.Fill;

                customCriteriaTable = new TableLayoutPanel();
                customCriteriaTable.AutoSize = true;
                customCriteriaTable.Dock = DockStyle.Fill;
                customCriteriaTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                customCriteriaTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                customCriteriaTable.RowCount = 1;

                addCustomCriteriaButton = new Button();
                addCustomCriteriaButton.Click += new EventHandler(AddCustomCriteriaButton_Click);
                addCustomCriteriaButton.Text = "Add criterion";
                customCriteriaTable.Controls.Add(addCustomCriteriaButton, 0, 0);

                customCriteriaBox.Controls.Add(customCriteriaTable);

                tablePanel.Controls.Add(generalBox, 0, 0);
                tablePanel.Controls.Add(criteriaBox, 0, 1);
                tablePanel.Controls.Add(customCriteriaBox, 0, 2);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Adds new custom criterion controls
        /// </summary>        
        private void AddCustomCriteriaButton_Click(object sender, EventArgs e) {
            try {
                customCriteriaTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                customCriteriaTable.RowCount++;
                customCriteriaTable.SetRow(addCustomCriteriaButton, customCriteriaTable.RowCount - 1);

                TableLayoutPanel t = new TableLayoutPanel();
                t.AutoSize = true;
                t.Dock = DockStyle.Fill;
                t.AutoScroll = false;
                t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                t.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                Label targetLabel = new Label();
                targetLabel.AutoSize = true;
                targetLabel.Dock = DockStyle.Fill;
                targetLabel.TextAlign = ContentAlignment.MiddleRight;
                targetLabel.Text = "Criteria predicate:";
                Label regexLabel = new Label();
                regexLabel.AutoSize = true;
                regexLabel.Dock = DockStyle.Fill;
                regexLabel.TextAlign = ContentAlignment.MiddleRight;
                regexLabel.Text = "Regular expression:";
                Label actionLabel = new Label();
                actionLabel.AutoSize = true;
                actionLabel.Dock = DockStyle.Fill;
                actionLabel.TextAlign = ContentAlignment.MiddleRight;
                actionLabel.Text = "Action:";

                ComboBox targetBox = new ComboBox();
                targetBox.Width = 120;
                targetBox.DropDownStyle = ComboBoxStyle.DropDownList;
                foreach (var o in Enum.GetValues(typeof(LocalizationCriterionTarget)))
                    targetBox.Items.Add(((LocalizationCriterionTarget)o).ToHumanForm());

                TextBox regexBox = new TextBox();
                regexBox.Width = 210;

                ComboBox predicateBox = new ComboBox();
                predicateBox.Width = 110;
                predicateBox.DropDownWidth = 180;
                predicateBox.DropDownStyle = ComboBoxStyle.DropDownList;

                foreach (var o in Enum.GetValues(typeof(LocalizationCriterionPredicate)))
                    predicateBox.Items.Add(((LocalizationCriterionPredicate)o).ToHumanForm());

                predicateBox.SelectedIndexChanged += new EventHandler((o, a) => { regexBox.Enabled = predicateBox.SelectedIndex <= 1; });

                NumericUpDown valueUpDown = new NumericUpDown();
                valueUpDown.Minimum = -100;
                valueUpDown.Maximum = 100;
                valueUpDown.Width = 50;
                valueUpDown.AutoSize = false;
                valueUpDown.Enabled = false;

                ComboBox actionBox = new ComboBox();
                actionBox.DropDownStyle = ComboBoxStyle.DropDownList;
                actionBox.Width = 120;

                foreach (var o in Enum.GetValues(typeof(LocalizationCriterionAction)))
                    actionBox.Items.Add(((LocalizationCriterionAction)o).ToHumanForm());

                actionBox.SelectedIndexChanged += new EventHandler((o, a) => {
                    valueUpDown.Enabled = actionBox.SelectedIndex == (int)LocalizationCriterionAction.VALUE;
                });

                Button removeButton = new Button();
                removeButton.Tag = customCriteriaTable.RowCount - 2;
                removeButton.Click += new EventHandler(RemoveButton_Click);
                removeButton.Width = 60;
                removeButton.Text = "Remove";

                t.Controls.Add(targetLabel, 0, 0);
                t.Controls.Add(targetBox, 1, 0);
                t.Controls.Add(predicateBox, 2, 0);
                t.Controls.Add(regexLabel, 0, 1);
                t.Controls.Add(regexBox, 1, 1);
                t.Controls.Add(removeButton, 3, 1);
                t.Controls.Add(actionLabel, 0, 2);
                t.Controls.Add(actionBox, 1, 2);
                t.Controls.Add(valueUpDown, 2, 2);
                t.SetColumnSpan(regexBox, 2);

                customCriteriaTable.Controls.Add(t, 0, customCriteriaTable.RowCount - 2);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Removes the custom criterion
        /// </summary>        
        private void RemoveButton_Click(object sender, EventArgs e) {
            try {
                int rowIndex = (int)(sender as Button).Tag;

                customCriteriaTable.SuspendLayout();
                tablePanel.AutoScroll = false;

                customCriteriaTable.Controls.Remove(customCriteriaTable.GetControlFromPosition(0, rowIndex));

                for (int i = rowIndex; i < customCriteriaTable.RowCount - 1; i++) {
                    if (i != customCriteriaTable.RowCount - 2) {
                        TableLayoutPanel p = (TableLayoutPanel)customCriteriaTable.GetControlFromPosition(0, i + 1);
                        p.GetControlFromPosition(3, 1).Tag = i;
                        customCriteriaTable.SetRow(p, i);
                    } else {
                        customCriteriaTable.SetRow(customCriteriaTable.GetControlFromPosition(0, i + 1), i);
                    }
                }

                customCriteriaTable.RowStyles.RemoveAt(customCriteriaTable.RowCount - 1);
                customCriteriaTable.RowCount--;

                tablePanel.AutoScroll = true;
                tablePanel.PerformLayout();
                tablePanel.ScrollControlIntoView(customCriteriaBox);

                customCriteriaTable.ResumeLayout(true);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Enables/disables "values" column of the common criteria grid based on currently selected action
        /// </summary>        
        private void CommonCriteriaGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e) {
            try {
                if (e.ColumnIndex == 1) {
                    commonCriteriaGrid.Rows[e.RowIndex].Cells[2].ReadOnly = commonCriteriaGrid.Rows[e.RowIndex].Cells[1].Value.ToString() != actionColumn.Items[(int)LocalizationCriterionAction.VALUE].ToString();
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Populates the GUI with current settings data
        /// </summary>
        private void PopulateTable() {
            try {
                contextBox.Checked = SettingsObject.Instance.ShowContextColumn;
                reflectionBox.Checked = SettingsObject.Instance.UseReflectionInAsp;

                foreach (var pair in SettingsObject.Instance.CommonLocalizabilityCriteria) {
                    LocalizationCommonCriterion crit = pair.Value;
                    DataGridViewRow row = commonCriteriaGrid.Rows[commonCriteriaRowMap[crit.Name]];

                    row.Cells[1].Value = actionColumn.Items[(int)crit.Action];
                    row.Cells[2].Value = crit.Weight;
                }

                // remove existing custom criteria
                for (int i = 0; i < customCriteriaTable.RowCount - 1; i++)
                    customCriteriaTable.Controls.Remove(customCriteriaTable.GetControlFromPosition(0, i));
                customCriteriaTable.SetRow(addCustomCriteriaButton, 0);
                customCriteriaTable.RowCount = 1;

                // add new custom criteria
                foreach (LocalizationCustomCriterion crit in SettingsObject.Instance.CustomLocalizabilityCriteria) {
                    AddCustomCriteriaButton_Click(null, null); // prepares the GUI

                    // fill the GUI with data
                    TableLayoutPanel t = (TableLayoutPanel)customCriteriaTable.GetControlFromPosition(0, customCriteriaTable.RowCount - 2);
                    t.Name = crit.Name;
                    ComboBox targetBox = (ComboBox)t.GetControlFromPosition(1, 0);
                    ComboBox predicateBox = (ComboBox)t.GetControlFromPosition(2, 0);
                    TextBox regexBox = (TextBox)t.GetControlFromPosition(1, 1);
                    ComboBox actionBox = (ComboBox)t.GetControlFromPosition(1, 2);
                    NumericUpDown valueBox = (NumericUpDown)t.GetControlFromPosition(2, 2);

                    targetBox.SelectedIndex = (int)crit.Target;
                    predicateBox.SelectedIndex = (int)crit.Predicate;
                    actionBox.SelectedIndex = (int)crit.Action;
                    regexBox.Text = crit.Regex;
                    valueBox.Value = crit.Weight;
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// When the page is displayed
        /// </summary>        
        protected override void OnActivate(CancelEventArgs e) {
            try {
                base.OnActivate(e);
               
                if (closed) { // displayed for the first time
                    PopulateTable();
                    closed = false;
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);

            closed = true;
        }

        /// <summary>
        /// When "Apply" button of the settings dialog was hit; save the GUI state in the settings
        /// </summary>        
        protected override void OnApply(PageApplyEventArgs e) {            
            if (e.ApplyBehavior == ApplyKind.Apply) {
                try {
                    SettingsObject.Instance.IgnorePropertyChanges = true;

                    bool revalidate = SettingsObject.Instance.ShowContextColumn != contextBox.Checked;
                    SettingsObject.Instance.ShowContextColumn = contextBox.Checked;
                    SettingsObject.Instance.UseReflectionInAsp = reflectionBox.Checked;

                    foreach (var pair in SettingsObject.Instance.CommonLocalizabilityCriteria) {
                        LocalizationCommonCriterion crit = pair.Value;
                        DataGridViewRow row = commonCriteriaGrid.Rows[commonCriteriaRowMap[crit.Name]];

                        crit.Action = (LocalizationCriterionAction)actionColumn.Items.IndexOf(row.Cells[1].Value);

                        int w = 0;
                        object val = row.Cells[2].Value;
                        if (val == null || !int.TryParse(val.ToString(), out w) || w < -100 || w > 100)
                            throw new Exception("Error on '" + crit.Description + "' - invalid value '" + val + "'. Please enter an integer from -100 to 100.");

                        crit.Weight = w;
                    }

                    SettingsObject.Instance.CustomLocalizabilityCriteria.Clear();
                    for (int i = 0; i < customCriteriaTable.RowCount - 1; i++) {
                        TableLayoutPanel t = (TableLayoutPanel)customCriteriaTable.GetControlFromPosition(0, i);
                        ComboBox targetBox = (ComboBox)t.GetControlFromPosition(1, 0);
                        ComboBox predicateBox = (ComboBox)t.GetControlFromPosition(2, 0);
                        TextBox regexBox = (TextBox)t.GetControlFromPosition(1, 1);
                        ComboBox actionBox = (ComboBox)t.GetControlFromPosition(1, 2);
                        NumericUpDown valueBox = (NumericUpDown)t.GetControlFromPosition(2, 2);

                        if (actionBox.SelectedIndex == -1) throw new Exception("Error on custom rule no. "+(i+1)+" - must select action.");
                        LocalizationCriterionAction action = (LocalizationCriterionAction)actionBox.SelectedIndex;

                        LocalizationCustomCriterion crit = new LocalizationCustomCriterion(action, (int)valueBox.Value);
                        if (!string.IsNullOrEmpty(t.Name)) crit.Name = t.Name;

                        if (predicateBox.SelectedIndex == -1) throw new Exception("Error on custom rule no. " + (i + 1) + " - must select predicate.");
                        crit.Predicate = (LocalizationCriterionPredicate)predicateBox.SelectedIndex;

                        if (targetBox.SelectedIndex == -1) throw new Exception("Error on custom rule no. " + (i + 1) + " - must select target.");
                        crit.Target = (LocalizationCriterionTarget)targetBox.SelectedIndex;

                        new Regex(regexBox.Text); // throws exception if regex is not valid
                        crit.Regex = regexBox.Text;

                        SettingsObject.Instance.CustomLocalizabilityCriteria.Add(crit);
                    }

                    if (revalidate) {
                        SettingsObject.Instance.NotifyRevalidationRequested();
                    }                   
                } catch (Exception ex) {
                    VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                    VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
                    e.ApplyBehavior = ApplyKind.CancelNoNavigate;
                } finally {
                    SettingsObject.Instance.IgnorePropertyChanges = false;
                    SettingsObject.Instance.NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);
                }
            }
        }

        #endregion
    }
}

