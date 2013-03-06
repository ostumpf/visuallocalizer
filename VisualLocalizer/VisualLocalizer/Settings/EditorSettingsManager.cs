using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.VisualStudio.Shell.Interop;
using System.Windows.Forms;
using System.ComponentModel;
using Microsoft.VisualStudio;
using System.Drawing;
using VisualLocalizer.Gui;
using System.Diagnostics;

namespace VisualLocalizer.Settings {

    internal enum BAD_KEY_NAME_POLICY { IGNORE_COMPLETELY, IGNORE_ON_NO_DESIGNER, WARN_ALWAYS }

    [Guid("82B0FBD1-ACF3-4974-B83F-2B06B4F839F0")]
    internal sealed class EditorSettingsManager : AbstractSettingsManager {

        private const string GET_BING_APPID_URL = "https://datamarket.azure.com/dataset/bing/microsofttranslator";
        
        private TableLayoutPanel tablePanel, langTable;
        private Label bingLabel, languagePairsLabel;
        private TextBox bingBox;
        private ListBox languagePairsBox;
        private Button addButton, removeButton, moveUpButton, moveDownButton;
        private bool closed = true;
        private NumericUpDown intervalBox;
        private ComboBox keyBehaviorBox;

        #region IProfileManager

        public override void LoadSettingsFromStorage() {
            VisualLocalizerPackage package = VisualLocalizerPackage.Instance;
            RegistryKey rootKey = package.UserRegistryRoot;
            RegistryKey settingsKey = null;
            try {
                settingsKey = rootKey.OpenSubKey(REG_KEY, false);
                if (settingsKey != null) {
                    RegistryKey editorKey = settingsKey.OpenSubKey(EDITOR_KEY);

                    if (editorKey != null) {
                        SettingsObject.Instance.IgnorePropertyChanges = true;

                        SettingsObject.Instance.ReferenceUpdateInterval = ReadIntFromRegKey(editorKey, "ReferenceUpdateInterval", 15 * 1000);
                        SettingsObject.Instance.BadKeyNamePolicy = (BAD_KEY_NAME_POLICY)ReadIntFromRegKey(editorKey, "BadKeyNamePolicy", 1);
                        SettingsObject.Instance.LanguagePairs.Clear();
                        int count = ReadIntFromRegKey(editorKey, "LanguagesCount");

                        for (int i = 0; i < count; i++) {
                            string pair = (string)editorKey.GetValue("Language" + i);
                            string[] arr = pair.Split(':');
                            SettingsObject.Instance.LanguagePairs.Add(new SettingsObject.LanguagePair() {
                                FromLanguage = arr[0],
                                ToLanguage = arr[1]
                            });
                        }

                        SettingsObject.Instance.BingAppId = (string)editorKey.GetValue("BingAppId", null);
                        if (string.IsNullOrEmpty(SettingsObject.Instance.BingAppId)) SettingsObject.Instance.BingAppId = null;

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

            SettingsObject.Instance.ReferenceUpdateInterval = ReadIntFromXml(reader, "ReferenceUpdateInterval");
            SettingsObject.Instance.BadKeyNamePolicy = (BAD_KEY_NAME_POLICY)ReadIntFromXml(reader, "BadKeyNamePolicy");
            int count = ReadIntFromXml(reader, "LanguagesCount");
            for (int i = 0; i < count; i++) {
                string pair;
                int hr = reader.ReadSettingString("Language" + i, out pair);
                if (hr != VSConstants.S_OK) reader.ReportError("Language value cannot be read", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);

                string[] arr = pair.Split(':');

                SettingsObject.Instance.LanguagePairs.Add(new SettingsObject.LanguagePair() {
                    FromLanguage = arr[0],
                    ToLanguage = arr[1]
                });
            }

            string s;
            int r = reader.ReadSettingString("BingAppId", out s);
            if (r != VSConstants.S_OK) reader.ReportError("BingAppId value cannot be read", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);
            SettingsObject.Instance.BingAppId = s;
            if (string.IsNullOrEmpty(SettingsObject.Instance.BingAppId)) SettingsObject.Instance.BingAppId = null;


            SettingsObject.Instance.IgnorePropertyChanges = false;
            SettingsObject.Instance.NotifySettingsLoaded();
        }

        public override void ResetSettings() {
            SettingsObject.Instance.IgnorePropertyChanges = true;

            SettingsObject.Instance.ReferenceUpdateInterval = 10 * 1000;
            SettingsObject.Instance.LanguagePairs.Clear();
            SettingsObject.Instance.BingAppId = null;
            SettingsObject.Instance.BadKeyNamePolicy = BAD_KEY_NAME_POLICY.IGNORE_ON_NO_DESIGNER;

            SettingsObject.Instance.IgnorePropertyChanges = false;
            SettingsObject.Instance.NotifyPropertyChanged(CHANGE_CATEGORY.EDITOR);
            SettingsObject.Instance.NotifySettingsLoaded();
        }

        public override void SaveSettingsToStorage() {
            VisualLocalizerPackage package = VisualLocalizerPackage.Instance;
            RegistryKey rootKey = package.UserRegistryRoot;
            RegistryKey settingsKey = null;
            try {
                settingsKey = rootKey.CreateSubKey(REG_KEY);
                RegistryKey editorKey = settingsKey.CreateSubKey(EDITOR_KEY);

                WriteIntToRegKey(editorKey, "ReferenceUpdateInterval", SettingsObject.Instance.ReferenceUpdateInterval);
                WriteIntToRegKey(editorKey, "BadKeyNamePolicy",(int)SettingsObject.Instance.BadKeyNamePolicy);
                WriteIntToRegKey(editorKey, "LanguagesCount", SettingsObject.Instance.LanguagePairs.Count);
                for (int i = 0; i < SettingsObject.Instance.LanguagePairs.Count; i++) {
                    editorKey.SetValue("Language" + i, SettingsObject.Instance.LanguagePairs[i].FromLanguage + ":" + SettingsObject.Instance.LanguagePairs[i].ToLanguage);
                }

                editorKey.SetValue("BingAppId", SettingsObject.Instance.BingAppId == null ? string.Empty : SettingsObject.Instance.BingAppId);
            } finally {
                if (settingsKey != null) settingsKey.Close();
            }
        }

        public override void SaveSettingsToXml(IVsSettingsWriter writer) {
            WriteIntToXml(writer, "ReferenceUpdateInterval", SettingsObject.Instance.ReferenceUpdateInterval);
            WriteIntToXml(writer, "BadKeyNamePolicy", (int)SettingsObject.Instance.BadKeyNamePolicy);
            WriteIntToXml(writer, "LanguagesCount", SettingsObject.Instance.LanguagePairs.Count);

            for (int i = 0; i < SettingsObject.Instance.LanguagePairs.Count; i++) {
                int hr = writer.WriteSettingString("Language" + i, SettingsObject.Instance.LanguagePairs[i].FromLanguage + ":" + SettingsObject.Instance.LanguagePairs[i].ToLanguage);
                if (hr != VSConstants.S_OK) writer.ReportError("Language value cannot be written", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);
            }

            int r = writer.WriteSettingString("BingAppId", SettingsObject.Instance.BingAppId == null ? string.Empty : SettingsObject.Instance.BingAppId);
            if (r != VSConstants.S_OK) writer.ReportError("BingAppId value cannot be written", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);                
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

            tablePanel.RowCount = 3;
            tablePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tablePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tablePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            tablePanel.ColumnCount = 1;            
            tablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            Label intervalLabel = new Label();
            intervalLabel.AutoSize = true;
            intervalLabel.Margin = new Padding(0, 6, 0, 0);
            intervalLabel.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            intervalLabel.Text = "Reference update interval (ms):";

            intervalBox = new NumericUpDown();
            intervalBox.Width = 80;
            intervalBox.Minimum = 1000;
            intervalBox.Maximum = int.MaxValue;
            intervalBox.ThousandsSeparator = true;
            intervalBox.Increment = 1000;
            intervalBox.InterceptArrowKeys = true;

            Label keyBehaviorLabel = new Label();
            keyBehaviorLabel.AutoSize = true;
            keyBehaviorLabel.Margin = new Padding(0, 6, 0, 0);
            keyBehaviorLabel.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            keyBehaviorLabel.Text = "Invalid key name policy:";

            keyBehaviorBox = new ComboBox();
            keyBehaviorBox.DropDownStyle = ComboBoxStyle.DropDownList;
            keyBehaviorBox.Width = 250;
            keyBehaviorBox.Items.Add("Ignore always");
            keyBehaviorBox.Items.Add("Ignore when no designer class is generated");
            keyBehaviorBox.Items.Add("Always issue error");

            GroupBox intervalGroup = new GroupBox();
            intervalGroup.AutoSize = true;
            intervalGroup.Dock = DockStyle.Fill;
            intervalGroup.Text = "Reference Counter";

            TableLayoutPanel intervalInnerPanel = new TableLayoutPanel();
            intervalInnerPanel.AutoSize = true;
            intervalInnerPanel.Dock = DockStyle.Fill;
            intervalInnerPanel.ColumnCount = 2;
            intervalInnerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            intervalInnerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            intervalInnerPanel.RowCount = 2;
            intervalInnerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            intervalInnerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            intervalInnerPanel.Controls.Add(intervalLabel, 0, 0);
            intervalInnerPanel.Controls.Add(intervalBox, 1, 0);
            intervalInnerPanel.Controls.Add(keyBehaviorLabel, 0, 1);
            intervalInnerPanel.Controls.Add(keyBehaviorBox, 1, 1);
            
            intervalGroup.Controls.Add(intervalInnerPanel);

            bingLabel = new Label();
            bingLabel.AutoSize = true;
            bingLabel.TextAlign = ContentAlignment.MiddleRight;
            bingLabel.Anchor = AnchorStyles.Right;
            bingLabel.Text = "Bing AppId:";

            bingBox = new TextBox();
            bingBox.Width = 300;

            LinkLabel bingLink = new LinkLabel();
            bingLink.AutoSize = true;
            bingLink.Text = "Get AppID";
            bingLink.Margin = new Padding(0, 6, 0, 0);
            bingLink.Click += new EventHandler(bingLink_Click);

            languagePairsLabel = new Label();
            languagePairsLabel.AutoSize = true;
            languagePairsLabel.TextAlign = ContentAlignment.MiddleRight;
            languagePairsLabel.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            languagePairsLabel.Text = "Language pairs:";

            langTable = new TableLayoutPanel();
            langTable.AutoSize = true;
            langTable.Dock = DockStyle.Fill;
            langTable.ColumnCount = 2;
            langTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            langTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));            

            langTable.RowCount = 4;
            langTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            langTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            langTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            langTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            languagePairsBox = new ListBox();
            languagePairsBox.Size = new Size(100, 110);            
            languagePairsBox.SelectedIndexChanged += new EventHandler(languagePairsBox_SelectedIndexChanged);

            addButton = new Button();
            addButton.Width = 100;
            addButton.Click += new EventHandler(addButton_Click);
            addButton.Text = "Add pair";

            removeButton = new Button();
            removeButton.Width = 100;
            removeButton.Click += new EventHandler(removeButton_Click);
            removeButton.Text = "Remove pair";

            moveUpButton = new Button();
            moveUpButton.Width = 100;
            moveUpButton.Click += new EventHandler(moveUpButton_Click);
            moveUpButton.Text = "Move up";

            moveDownButton = new Button();
            moveDownButton.Width = 100;
            moveDownButton.Click += new EventHandler(moveDownButton_Click);
            moveDownButton.Text = "Move down";

            langTable.Controls.Add(languagePairsBox, 0, 0);
            langTable.SetRowSpan(languagePairsBox, 4);
            langTable.Controls.Add(addButton, 1, 0);
            langTable.Controls.Add(removeButton, 1, 1);
            langTable.Controls.Add(moveUpButton, 1, 2);
            langTable.Controls.Add(moveDownButton, 1, 3);

            GroupBox languageGroup = new GroupBox();
            languageGroup.AutoSize = true;
            languageGroup.Dock = DockStyle.Fill;
            languageGroup.Text = "Translation Services";

            TableLayoutPanel languageInnerPanel = new TableLayoutPanel();
            languageInnerPanel.AutoSize = true;
            languageInnerPanel.Dock = DockStyle.Fill;
            languageInnerPanel.ColumnCount = 3;
            languageInnerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            languageInnerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            languageInnerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            languageInnerPanel.RowCount = 2;
            languageInnerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            languageInnerPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            languageInnerPanel.Controls.Add(bingLabel, 0, 0);
            languageInnerPanel.Controls.Add(bingBox, 1, 0);
            languageInnerPanel.Controls.Add(bingLink, 2, 0);

            languageInnerPanel.Controls.Add(languagePairsLabel, 0, 1);
            languageInnerPanel.Controls.Add(langTable, 1, 1);
            languageInnerPanel.SetColumnSpan(langTable, 2);

            languageGroup.Controls.Add(languageInnerPanel);

            tablePanel.Controls.Add(intervalGroup, 0, 0);
            tablePanel.Controls.Add(languageGroup, 0, 1);
        }        

        private void PopulateTable() {
            intervalBox.Value = SettingsObject.Instance.ReferenceUpdateInterval;
            bingBox.Text = SettingsObject.Instance.BingAppId;

            languagePairsBox.Items.Clear();
            foreach (var pair in SettingsObject.Instance.LanguagePairs) {
                languagePairsBox.Items.Add(pair);
            }

            languagePairsBox_SelectedIndexChanged(null, null);
            keyBehaviorBox.SelectedIndex = (int)SettingsObject.Instance.BadKeyNamePolicy;
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
                
                SettingsObject.Instance.ReferenceUpdateInterval = (int)intervalBox.Value;
                SettingsObject.Instance.BingAppId = string.IsNullOrEmpty(bingBox.Text) ? null : bingBox.Text;
                SettingsObject.Instance.LanguagePairs.Clear();
                foreach (SettingsObject.LanguagePair newPair in languagePairsBox.Items) {
                    SettingsObject.Instance.LanguagePairs.Add(newPair);
                }

                bool forceRevalidation = SettingsObject.Instance.BadKeyNamePolicy != (BAD_KEY_NAME_POLICY)keyBehaviorBox.SelectedIndex;
                SettingsObject.Instance.BadKeyNamePolicy = (BAD_KEY_NAME_POLICY)keyBehaviorBox.SelectedIndex;

                SettingsObject.Instance.IgnorePropertyChanges = false;
                SettingsObject.Instance.NotifyPropertyChanged(CHANGE_CATEGORY.EDITOR);

                if (forceRevalidation) SettingsObject.Instance.NotifyRevalidationRequested();
            }
        }

        #endregion

        #region listeners

        private void languagePairsBox_SelectedIndexChanged(object sender, EventArgs e) {
            bool selected = languagePairsBox.SelectedIndex != -1;

            removeButton.Enabled = selected;
            moveDownButton.Enabled = selected && languagePairsBox.SelectedIndex != languagePairsBox.Items.Count - 1;
            moveUpButton.Enabled = selected && languagePairsBox.SelectedIndex != 0;
        }        

        private void addButton_Click(object sender, EventArgs e) {
            NewLanguagePairWindow win = new NewLanguagePairWindow(false);
            if (win.ShowDialog() == DialogResult.OK) {
                SettingsObject.LanguagePair newPair = new SettingsObject.LanguagePair() {
                    FromLanguage = win.SourceLanguage,
                    ToLanguage = win.TargetLanguage
                };
                if (languagePairsBox.Items.Contains(newPair)) {
                    VisualLocalizer.Library.MessageBox.ShowError("This language pair is already in the list!");
                    return;
                }
                languagePairsBox.Items.Add(newPair);
            }

        }

        private void moveUpButton_Click(object sender, EventArgs e) {
            if (languagePairsBox.SelectedIndex <= 0) return;

            int originalIndex = languagePairsBox.SelectedIndex;
            int upperIndex = originalIndex - 1;

            switchItems(originalIndex, upperIndex);
        }

        private void moveDownButton_Click(object sender, EventArgs e) {
            if (languagePairsBox.SelectedIndex == -1) return;
            if (languagePairsBox.SelectedIndex == languagePairsBox.Items.Count - 1) return;

            int originalIndex = languagePairsBox.SelectedIndex;
            int downIndex = originalIndex + 1;

            switchItems(originalIndex, downIndex);
        }

        private void switchItems(int originalIndex, int newIndex) {
            object upperItem = languagePairsBox.Items[newIndex];
            languagePairsBox.Items[newIndex] = languagePairsBox.Items[originalIndex];
            languagePairsBox.Items[originalIndex] = upperItem;

            languagePairsBox.SelectedIndex = newIndex;
        }

        private void removeButton_Click(object sender, EventArgs e) {
            if (languagePairsBox.SelectedIndex == -1) return;

            languagePairsBox.Items.RemoveAt(languagePairsBox.SelectedIndex);
        }

        private void bingLink_Click(object sender, EventArgs e) {
            try {
                Process browser = new Process();
                browser.StartInfo = new ProcessStartInfo(GET_BING_APPID_URL);
                browser.StartInfo.UseShellExecute = true;
                browser.Start();
            } catch (Exception ex) {
                VisualLocalizer.Library.MessageBox.ShowError(ex.Message);
            }
        }

        #endregion
    }
}
