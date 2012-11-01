using System;
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

namespace VisualLocalizer.Settings {

    [Guid("24E41562-4D26-4166-BCB8-B10F773A6A18")]
    internal sealed class FilterSettingsManager : AbstractSettingsManager {

        private TableLayoutPanel tablePanel;
        private bool closed = true;
        private CheckBox contextBox;

        #region IProfileManager

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

                        SettingsObject.Instance.FilterOutVerbatim = ReadBoolFromRegKey(filtersKey, "FilterOutVerbatim");
                        SettingsObject.Instance.FilterOutUnlocalizable = ReadBoolFromRegKey(filtersKey, "FilterOutUnlocalizable");
                        SettingsObject.Instance.FilterOutCaps = ReadBoolFromRegKey(filtersKey, "FilterOutCaps");
                        SettingsObject.Instance.FilterOutNoLetters = ReadBoolFromRegKey(filtersKey, "FilterOutNoLetters");
                        SettingsObject.Instance.FilterOutSpecificComment = ReadBoolFromRegKey(filtersKey, "FilterOutSpecificComment");
                        SettingsObject.Instance.ShowFilterContext = ReadBoolFromRegKey(filtersKey, "ShowFilterContext");

                        SettingsObject.Instance.FilterRegexps.Clear();
                        int count = ReadIntFromRegKey(filtersKey, "RegexpCount");
                        for (int i = 0; i < count; i++) {
                            string regexp = filtersKey.GetValue("RegexpItem" + i).ToString();
                            bool mustMatch = bool.Parse(filtersKey.GetValue("RegexpMustMatch" + i).ToString());
                            SettingsObject.Instance.FilterRegexps.Add(new SettingsObject.RegexpInstance() {
                                MustMatch = mustMatch,
                                Regexp = regexp
                            });
                        }
                        SettingsObject.Instance.NamespacePolicyIndex = ReadIntFromRegKey(filtersKey, "NamespacePolicyIndex");
                        SettingsObject.Instance.MarkNotLocalizableStringsIndex = ReadIntFromRegKey(filtersKey, "MarkNotLocalizableStringsIndex");
                        SettingsObject.Instance.BatchMoveSplitterDistance = ReadIntFromRegKey(filtersKey, "BatchMoveSplitterDistance", 110);

                        SettingsObject.Instance.IgnorePropertyChanges = false;
                        SettingsObject.Instance.NotifySettingsLoaded();
                    } else ResetSettings();

                } else ResetSettings();
            } finally {
                if (settingsKey != null) settingsKey.Close();
            }
        }

        public override void LoadSettingsFromXml(IVsSettingsReader reader) {
            SettingsObject.Instance.IgnorePropertyChanges = true;
            SettingsObject.Instance.FilterOutCaps = ReadBoolFromXml(reader, "FilterOutCaps");
            SettingsObject.Instance.FilterOutNoLetters = ReadBoolFromXml(reader, "FilterOutNoLetters");
            SettingsObject.Instance.FilterOutUnlocalizable = ReadBoolFromXml(reader, "FilterOutUnlocalizable");
            SettingsObject.Instance.FilterOutVerbatim = ReadBoolFromXml(reader, "FilterOutVerbatim");
            SettingsObject.Instance.FilterOutSpecificComment = ReadBoolFromXml(reader, "FilterOutSpecificComment");
            SettingsObject.Instance.ShowFilterContext = ReadBoolFromXml(reader, "ShowFilterContext");

            SettingsObject.Instance.MarkNotLocalizableStringsIndex = ReadIntFromXml(reader, "MarkNotLocalizableStringsIndex");
            SettingsObject.Instance.NamespacePolicyIndex = ReadIntFromXml(reader, "NamespacePolicyIndex");
            SettingsObject.Instance.BatchMoveSplitterDistance = ReadIntFromXml(reader, "BatchMoveSplitterDistance");

            int count = ReadIntFromXml(reader, "RegexpCount");
            for (int i = 0; i < count; i++) {
                string regexp;
                int hr = reader.ReadSettingString("RegexpItem" + i, out regexp);
                if (hr != VSConstants.S_OK) reader.ReportError("RegexpItem value cannot be read", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);

                bool match = ReadBoolFromXml(reader, "RegexpMustMatch" + i);

                SettingsObject.Instance.FilterRegexps.Add(new SettingsObject.RegexpInstance() {
                    MustMatch = match,
                    Regexp = regexp
                });
            }
            SettingsObject.Instance.IgnorePropertyChanges = false;
            SettingsObject.Instance.NotifySettingsLoaded();
        }

        public override void ResetSettings() {
            SettingsObject.Instance.IgnorePropertyChanges = true;

            SettingsObject.Instance.MarkNotLocalizableStringsIndex = 0;
            SettingsObject.Instance.NamespacePolicyIndex = 0;
            SettingsObject.Instance.FilterOutCaps = false;
            SettingsObject.Instance.FilterOutNoLetters = false;
            SettingsObject.Instance.FilterOutUnlocalizable = false;
            SettingsObject.Instance.FilterOutVerbatim = false;
            SettingsObject.Instance.FilterOutSpecificComment = true;
            SettingsObject.Instance.BatchMoveSplitterDistance = 130;
            SettingsObject.Instance.ShowFilterContext = true;

            SettingsObject.Instance.FilterRegexps.Clear();

            SettingsObject.Instance.IgnorePropertyChanges = false;
            SettingsObject.Instance.NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);
            SettingsObject.Instance.NotifySettingsLoaded();
        }

        public override void SaveSettingsToStorage() {
            VisualLocalizerPackage package = VisualLocalizerPackage.Instance;
            RegistryKey rootKey = package.UserRegistryRoot;
            RegistryKey settingsKey = null;
            try {
                settingsKey = rootKey.CreateSubKey(REG_KEY);
                RegistryKey filterKey = settingsKey.CreateSubKey(FILTER_KEY);
              
                WriteBoolToRegKey(filterKey, "FilterOutVerbatim", SettingsObject.Instance.FilterOutVerbatim);
                WriteBoolToRegKey(filterKey, "FilterOutUnlocalizable", SettingsObject.Instance.FilterOutUnlocalizable);
                WriteBoolToRegKey(filterKey, "FilterOutNoLetters", SettingsObject.Instance.FilterOutNoLetters);
                WriteBoolToRegKey(filterKey, "FilterOutCaps", SettingsObject.Instance.FilterOutCaps);
                WriteBoolToRegKey(filterKey, "FilterOutSpecificComment", SettingsObject.Instance.FilterOutSpecificComment);
                WriteBoolToRegKey(filterKey, "ShowFilterContext", SettingsObject.Instance.ShowFilterContext);

                int count = ReadIntFromRegKey(filterKey, "RegexpCount");
                for (int i = 0; i < count; i++) {
                    filterKey.DeleteValue("RegexpItem" + i, false);
                    filterKey.DeleteValue("RegexpMustMatch" + i, false);
                }

                WriteIntToRegKey(filterKey, "RegexpCount", SettingsObject.Instance.FilterRegexps.Count);
                WriteIntToRegKey(filterKey, "NamespacePolicyIndex", SettingsObject.Instance.NamespacePolicyIndex);
                WriteIntToRegKey(filterKey, "MarkNotLocalizableStringsIndex", SettingsObject.Instance.MarkNotLocalizableStringsIndex);
                WriteIntToRegKey(filterKey, "BatchMoveSplitterDistance", SettingsObject.Instance.BatchMoveSplitterDistance);

                for (int i = 0; i < SettingsObject.Instance.FilterRegexps.Count; i++) {
                    filterKey.SetValue("RegexpItem" + i, SettingsObject.Instance.FilterRegexps[i].Regexp);
                    filterKey.SetValue("RegexpMustMatch" + i, SettingsObject.Instance.FilterRegexps[i].MustMatch);
                }              
            } finally {
                if (settingsKey != null) settingsKey.Close();
            }
        }

        public override void SaveSettingsToXml(IVsSettingsWriter writer) {
            WriteBoolToXml(writer, "FilterOutVerbatim", SettingsObject.Instance.FilterOutVerbatim);
            WriteBoolToXml(writer, "FilterOutUnlocalizable", SettingsObject.Instance.FilterOutUnlocalizable);
            WriteBoolToXml(writer, "FilterOutCaps", SettingsObject.Instance.FilterOutCaps);
            WriteBoolToXml(writer, "FilterOutNoLetters", SettingsObject.Instance.FilterOutNoLetters);
            WriteBoolToXml(writer, "FilterOutSpecificComment", SettingsObject.Instance.FilterOutSpecificComment);
            WriteBoolToXml(writer, "ShowFilterContext", SettingsObject.Instance.ShowFilterContext);

            WriteIntToXml(writer, "RegexpCount", SettingsObject.Instance.FilterRegexps.Count);
            WriteIntToXml(writer, "MarkNotLocalizableStringsIndex", SettingsObject.Instance.MarkNotLocalizableStringsIndex);
            WriteIntToXml(writer, "NamespacePolicyIndex", SettingsObject.Instance.NamespacePolicyIndex);
            WriteIntToXml(writer, "BatchMoveSplitterDistance", SettingsObject.Instance.BatchMoveSplitterDistance);

            for (int i = 0; i < SettingsObject.Instance.FilterRegexps.Count; i++) {
                int hr = writer.WriteSettingString("RegexpItem" + i, SettingsObject.Instance.FilterRegexps[i].Regexp);
                if (hr != VSConstants.S_OK) writer.ReportError("RegexpItem value cannot be written", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);

                WriteBoolToXml(writer, "RegexpMustMatch" + i, SettingsObject.Instance.FilterRegexps[i].MustMatch);
            }
        }

        #endregion

        #region DialogPage

        protected override IWin32Window Window {
            get {
                if (tablePanel == null) InitializeDialogPage();
                PopulateTable();
                return tablePanel;
            }
        }

        private void InitializeDialogPage() {
            tablePanel = new TableLayoutPanel();            
            tablePanel.Dock = DockStyle.Fill;

            tablePanel.RowCount = 2;
            tablePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tablePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

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

            generalInnerPanel.Controls.Add(contextBox);
            generalBox.Controls.Add(generalInnerPanel);

            tablePanel.Controls.Add(generalBox, 0, 0);            
        }

        private void PopulateTable() {
            contextBox.Checked = SettingsObject.Instance.ShowFilterContext;
        }

        protected override void OnActivate(CancelEventArgs e) {
            base.OnActivate(e);

            if (closed) {
                PopulateTable();
                closed = false;
            }
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);

            closed = true;
        }

        protected override void OnApply(PageApplyEventArgs e) {
            if (e.ApplyBehavior == ApplyKind.Apply) {
                SettingsObject.Instance.IgnorePropertyChanges = true;

                SettingsObject.Instance.ShowFilterContext = contextBox.Checked;

                SettingsObject.Instance.IgnorePropertyChanges = false;
                SettingsObject.Instance.NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);
            }
        }

        #endregion
    }
}

