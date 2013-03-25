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
using VisualLocalizer.Translate;
using VisualLocalizer.Components;

namespace VisualLocalizer.Settings {

    /// <summary>
    /// Options how to handle invalid key names in ResX files
    /// </summary>
    internal enum BAD_KEY_NAME_POLICY { 
        /// <summary>
        /// Ignore possible errors
        /// </summary>
        IGNORE_COMPLETELY, 

        /// <summary>
        /// Ignore the errors, if given ResX file has no custom designer file
        /// </summary>
        IGNORE_ON_NO_DESIGNER, 

        /// <summary>
        /// Report the error
        /// </summary>
        WARN_ALWAYS 
    }

    /// <summary>
    /// Represents page of Editor settings in Tools/Options and also handles saving the EDITOR category of settings 
    /// </summary>
    [Guid("82B0FBD1-ACF3-4974-B83F-2B06B4F839F0")]
    internal sealed class EditorSettingsManager : AbstractSettingsManager {

        private TableLayoutPanel tablePanel, langTable;
        private Label bingLabel, languagePairsLabel;
        private TextBox bingBox;
        private ListBox languagePairsBox;
        private Button addButton, removeButton, moveUpButton, moveDownButton;
        private bool closed = true;
        private NumericUpDown intervalBox;
        private ComboBox keyBehaviorBox;

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

        /// <summary>
        /// Loads settings from XML (on import settings)
        /// </summary>
        /// <param name="reader"></param>
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

        /// <summary>
        /// Never called (bug?)
        /// </summary>
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

        /// <summary>
        /// Saves settings to registry storage
        /// </summary>
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

        /// <summary>
        /// Saves settings to XML (on settings export)
        /// </summary>
        /// <param name="writer"></param>
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
                    VisualLocalizer.Library.MessageBox.ShowException(ex);
                }
                return null;
            }
        }

        /// <summary>
        /// Create the GUI
        /// </summary>
        private void InitializeDialogPage() {
            try {
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
                bingLink.Click += new EventHandler(BingLink_Click);

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
                languagePairsBox.SelectedIndexChanged += new EventHandler(LanguagePairsBox_SelectedIndexChanged);

                addButton = new Button();
                addButton.Width = 100;
                addButton.Click += new EventHandler(AddButton_Click);
                addButton.Text = "Add pair";

                removeButton = new Button();
                removeButton.Width = 100;
                removeButton.Click += new EventHandler(RemoveButton_Click);
                removeButton.Text = "Remove pair";

                moveUpButton = new Button();
                moveUpButton.Width = 100;
                moveUpButton.Click += new EventHandler(MoveUpButton_Click);
                moveUpButton.Text = "Move up";

                moveDownButton = new Button();
                moveDownButton.Width = 100;
                moveDownButton.Click += new EventHandler(MoveDownButton_Click);
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
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }        

        /// <summary>
        /// Populate created table with current settings data
        /// </summary>
        private void PopulateTable() {
            intervalBox.Value = SettingsObject.Instance.ReferenceUpdateInterval;
            bingBox.Text = SettingsObject.Instance.BingAppId;

            languagePairsBox.Items.Clear();
            foreach (var pair in SettingsObject.Instance.LanguagePairs) {
                languagePairsBox.Items.Add(pair);
            }

            LanguagePairsBox_SelectedIndexChanged(null, null);
            keyBehaviorBox.SelectedIndex = (int)SettingsObject.Instance.BadKeyNamePolicy;
        }

        /// <summary>
        /// Called when this settings page is displayed
        /// </summary>        
        protected override void OnActivate(CancelEventArgs e) {
            try {
                base.OnActivate(e);

                if (closed) { // displayed for the first time - populate with data
                    PopulateTable();
                    closed = false;
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// When the settings dialog is cancelled
        /// </summary>        
        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);

            closed = true;
        }

        /// <summary>
        /// When "Apply" button was hit in the settings dialog; save current GUI state into the settings
        /// </summary>        
        protected override void OnApply(PageApplyEventArgs e) {
            try {
                if (e.ApplyBehavior == ApplyKind.Apply) {
                    SettingsObject.Instance.IgnorePropertyChanges = true;

                    SettingsObject.Instance.ReferenceUpdateInterval = (int)intervalBox.Value;
                    SettingsObject.Instance.BingAppId = string.IsNullOrEmpty(bingBox.Text) ? null : bingBox.Text;
                    SettingsObject.Instance.LanguagePairs.Clear();
                    foreach (SettingsObject.LanguagePair newPair in languagePairsBox.Items) {
                        SettingsObject.Instance.LanguagePairs.Add(newPair);
                    }

                    // key policy changed - loaded keys must be re-evaluated (in open ResX files etc.)
                    bool forceRevalidation = SettingsObject.Instance.BadKeyNamePolicy != (BAD_KEY_NAME_POLICY)keyBehaviorBox.SelectedIndex;
                    SettingsObject.Instance.BadKeyNamePolicy = (BAD_KEY_NAME_POLICY)keyBehaviorBox.SelectedIndex;

                    if (forceRevalidation) SettingsObject.Instance.NotifyRevalidationRequested();
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            } finally {
                SettingsObject.Instance.IgnorePropertyChanges = false;
                SettingsObject.Instance.NotifyPropertyChanged(CHANGE_CATEGORY.EDITOR);
            }
        }

        #endregion

        #region listeners

        private void LanguagePairsBox_SelectedIndexChanged(object sender, EventArgs e) {
            try {
                bool selected = languagePairsBox.SelectedIndex != -1;

                removeButton.Enabled = selected; // can remove item when some is selected
                moveDownButton.Enabled = selected && languagePairsBox.SelectedIndex != languagePairsBox.Items.Count - 1; // cannot move down the last item
                moveUpButton.Enabled = selected && languagePairsBox.SelectedIndex != 0; // cannot move up the uppermost item
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }        

        /// <summary>
        /// Adds new language pair by displaying dialog
        /// </summary>        
        private void AddButton_Click(object sender, EventArgs e) {
            try {
                NewLanguagePairWindow win = new NewLanguagePairWindow(false);
                if (win.ShowDialog() == DialogResult.OK) {
                    SettingsObject.LanguagePair newPair = new SettingsObject.LanguagePair() {
                        FromLanguage = win.SourceLanguage,
                        ToLanguage = win.TargetLanguage
                    };
                    if (languagePairsBox.Items.Contains(newPair)) throw new Exception("This language pair is already in the list!");
                    languagePairsBox.Items.Add(newPair);
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Moves selected language pair up by one
        /// </summary>        
        private void MoveUpButton_Click(object sender, EventArgs e) {
            try {
                if (languagePairsBox.SelectedIndex <= 0) return;

                int originalIndex = languagePairsBox.SelectedIndex;
                int upperIndex = originalIndex - 1;

                SwitchItems(originalIndex, upperIndex);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Moves selected language pair down by one
        /// </summary>        
        private void MoveDownButton_Click(object sender, EventArgs e) {
            try {
                if (languagePairsBox.SelectedIndex == -1) return;
                if (languagePairsBox.SelectedIndex == languagePairsBox.Items.Count - 1) return;

                int originalIndex = languagePairsBox.SelectedIndex;
                int downIndex = originalIndex + 1;

                SwitchItems(originalIndex, downIndex);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        private void SwitchItems(int originalIndex, int newIndex) {
            object upperItem = languagePairsBox.Items[newIndex];
            languagePairsBox.Items[newIndex] = languagePairsBox.Items[originalIndex];
            languagePairsBox.Items[originalIndex] = upperItem;

            languagePairsBox.SelectedIndex = newIndex;
        }

        /// <summary>
        /// Removes selected language pair
        /// </summary>        
        private void RemoveButton_Click(object sender, EventArgs e) {
            try {
                if (languagePairsBox.SelectedIndex == -1) return;

                languagePairsBox.Items.RemoveAt(languagePairsBox.SelectedIndex);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// "Get AppID" link was clicked - display browser with Bing url
        /// </summary>        
        private void BingLink_Click(object sender, EventArgs e) {
            try {
                Process browser = new Process();
                browser.StartInfo = new ProcessStartInfo(BingTranslator.GET_BING_APPID_URL);
                browser.StartInfo.UseShellExecute = true;
                browser.Start();
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        #endregion
    }
}
