using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VisualLocalizer.Commands;
using VisualLocalizer.Translate;
using System.Globalization;
using VisualLocalizer.Settings;
using VisualLocalizer.Components;

namespace VisualLocalizer.Gui {
    internal partial class GlobalTranslateForm : Form {

        public List<GlobalTranslateProjectItem> ResxTargetList { get; private set; }
        public TRANSLATE_PROVIDER Provider { get; private set; }
        public SettingsObject.LanguagePair LanguagePair { get; private set; }

        private CultureInfo[] displayedCultures;

        public GlobalTranslateForm(List<GlobalTranslateProjectItem> resxTargetList) {
            InitializeComponent();
            this.Icon = VSPackage._400;
            this.ResxTargetList = resxTargetList;

            foreach (TRANSLATE_PROVIDER prov in Enum.GetValues(typeof(TRANSLATE_PROVIDER))) {
                providerBox.Items.Add(new ProviderItem(prov));
            }
            providerBox.SelectedIndex = 0;

            displayedCultures = CultureInfo.GetCultures(CultureTypes.FrameworkCultures);
            souceLanguageBox.Items.Add("(auto)");
            foreach (var culture in displayedCultures) {
                souceLanguageBox.Items.Add(culture.DisplayName);
                targetLanguageBox.Items.Add(culture.DisplayName);
            }

            foreach (var pair in SettingsObject.Instance.LanguagePairs) {
                languagePairsBox.Items.Add(pair);
            }

            foreach (var item in resxTargetList) {
                resxListBox.Items.Add(item);                
                resxListBox.SetItemEnabled(resxListBox.Items.Count - 1, !item.Readonly);
                if (!item.Readonly) {
                    resxListBox.SetItemChecked(resxListBox.Items.Count - 1, true);
                    item.Checked = true;
                }
            }

            useSavedPairBox.Checked = false;
            useSavedPairBox.Checked = true;
            useNewPairBox.Checked = true;
            useNewPairBox.Checked = false;
        }

       
        private void resxListBox_SelectedValueChanged(object sender, EventArgs e) {
            GlobalTranslateProjectItem item = (GlobalTranslateProjectItem)resxListBox.SelectedItem;
            item.Checked = resxListBox.CheckedIndices.Contains(resxListBox.SelectedIndex);

            translateButton.Enabled = resxListBox.CheckedIndices.Count > 0;
        }

        private bool ignoreNextCheckEvent = false;
        private void useNewPairBox_CheckedChanged(object sender, EventArgs e) {
            label2.Enabled = useNewPairBox.Checked;
            label3.Enabled = useNewPairBox.Checked;
            souceLanguageBox.Enabled = useNewPairBox.Checked;
            targetLanguageBox.Enabled = useNewPairBox.Checked;
            addLanguagePairBox.Enabled = useNewPairBox.Checked;

            if (ignoreNextCheckEvent) return;
            ignoreNextCheckEvent = true;
            useSavedPairBox.Checked = !useNewPairBox.Checked;
            ignoreNextCheckEvent = false;
        }

        private void useSavedPairBox_CheckedChanged(object sender, EventArgs e) {
            label4.Enabled = useSavedPairBox.Checked;
            languagePairsBox.Enabled = useSavedPairBox.Checked;

            if (ignoreNextCheckEvent) return;
            ignoreNextCheckEvent = true;
            useNewPairBox.Checked = !useSavedPairBox.Checked;
            ignoreNextCheckEvent = false;
        }
       
        private void providerBox_SelectedIndexChanged(object sender, EventArgs e) {
            Provider = (TRANSLATE_PROVIDER)providerBox.SelectedIndex;
        }

        private void languagePairsBox_SelectedIndexChanged(object sender, EventArgs e) {
            LanguagePair = (SettingsObject.LanguagePair)languagePairsBox.SelectedItem;
        }

        private void GlobalTranslateForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (DialogResult == DialogResult.OK) {
                try {
                    if (useSavedPairBox.Checked) {
                        if (LanguagePair == null) throw new Exception("Saved language pair must be specified.");
                    }
                    if (useNewPairBox.Checked) {
                        if (souceLanguageBox.SelectedIndex == -1 || targetLanguageBox.SelectedIndex == -1)
                            throw new Exception("Both source and target language must be specified.");

                        string srcLanguage = null, targetLanguage = null;
                        if (souceLanguageBox.SelectedIndex == 0) {
                            srcLanguage = string.Empty;
                        } else {
                            srcLanguage = displayedCultures[souceLanguageBox.SelectedIndex - 1].TwoLetterISOLanguageName;
                        }
                        targetLanguage = displayedCultures[targetLanguageBox.SelectedIndex].TwoLetterISOLanguageName;

                        LanguagePair = new SettingsObject.LanguagePair() { FromLanguage = srcLanguage, ToLanguage = targetLanguage };

                        if (addLanguagePairBox.Checked && !SettingsObject.Instance.LanguagePairs.Contains(LanguagePair)) {
                            SettingsObject.Instance.LanguagePairs.Add(LanguagePair);
                            SettingsObject.Instance.NotifyPropertyChanged(CHANGE_CATEGORY.EDITOR);
                        }
                    }
                } catch (Exception ex) {
                    e.Cancel = true;
                    VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                    VisualLocalizer.Library.MessageBox.ShowException(ex);
                }
            }
        }

        private class ProviderItem {
            private TRANSLATE_PROVIDER prov;

            public ProviderItem(TRANSLATE_PROVIDER prov) {
                this.prov = prov;
            }

            public override string ToString() {
                return prov.ToHumanForm();
            }
        }

       

    }
}
