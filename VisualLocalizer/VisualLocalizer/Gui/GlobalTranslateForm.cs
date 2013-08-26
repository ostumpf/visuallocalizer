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
using VisualLocalizer.Commands.Translate;

namespace VisualLocalizer.Gui {
    
    /// <summary>
    /// Dialog displayed during "Global translate" command. Enables user to select ResX files for translation and source and target languages.
    /// </summary>
    internal partial class GlobalTranslateForm : Form {

        /// <summary>
        /// List of ResX files, each having field declaring whether this file should be translated or not
        /// </summary>
        public List<GlobalTranslateProjectItem> ResxTargetList { get; private set; }

        /// <summary>
        /// Translation provider
        /// </summary>
        public TRANSLATE_PROVIDER Provider { get; private set; }

        /// <summary>
        /// Source and target language
        /// </summary>
        public SettingsObject.LanguagePair LanguagePair { get; private set; }

        /// <summary>
        /// List of cultures in the comboboxes
        /// </summary>
        private CultureInfo[] displayedCultures;

        /// <summary>
        /// Creates new instance
        /// </summary>
        /// <param name="resxTargetList">List of ResX file, all checked by default</param>
        public GlobalTranslateForm(List<GlobalTranslateProjectItem> resxTargetList) {
            if (resxTargetList == null) throw new ArgumentNullException("resxTargetList");

            InitializeComponent();
            this.Icon = VSPackage._400;
            this.ResxTargetList = resxTargetList;

            // initialize providers combobox
            foreach (TRANSLATE_PROVIDER prov in Enum.GetValues(typeof(TRANSLATE_PROVIDER))) {
                if (prov == TRANSLATE_PROVIDER.BING) {
                    if (!string.IsNullOrEmpty(SettingsObject.Instance.BingAppId)) providerBox.Items.Add(new ProviderItem(prov));
                } else {
                    providerBox.Items.Add(new ProviderItem(prov));
                }
            }
            providerBox.SelectedIndex = 0;

            // add languages
            displayedCultures = CultureInfo.GetCultures(CultureTypes.FrameworkCultures);
            souceLanguageBox.Items.Add("(auto)"); // adds auto-detection option to list of source languages
            foreach (var culture in displayedCultures) {
                souceLanguageBox.Items.Add(culture.DisplayName);
                targetLanguageBox.Items.Add(culture.DisplayName);
            }

            // add existing language pairs
            foreach (var pair in SettingsObject.Instance.LanguagePairs) {
                languagePairsBox.Items.Add(pair);
            }

            // add ResX files
            foreach (var item in resxTargetList) {
                resxListBox.Items.Add(item);                
                resxListBox.SetItemEnabled(resxListBox.Items.Count - 1, !item.Readonly); // readonly files are disabled
                if (!item.Readonly) {
                    resxListBox.SetItemChecked(resxListBox.Items.Count - 1, true); // not-readonly files are checked by default
                    item.Checked = true;
                }
            }

            useSavedPairBox.Checked = false;
            useSavedPairBox.Checked = true;
            useNewPairBox.Checked = true;
            useNewPairBox.Checked = false;
        }

    
        /// <summary>
        /// Updates check state of the resource file and updates enabled state of the translate button
        /// </summary>
        private void ResxListBox_SelectedValueChanged(object sender, EventArgs e) {
            GlobalTranslateProjectItem item = (GlobalTranslateProjectItem)resxListBox.SelectedItem;
            item.Checked = resxListBox.CheckedIndices.Contains(resxListBox.SelectedIndex);

            translateButton.Enabled = resxListBox.CheckedIndices.Count > 0;
        }

        private bool ignoreNextCheckEvent = false;
        /// <summary>
        /// Called when "use new language pair" checkbox is checked. Enables/disables respective controls.
        /// </summary>        
        private void UseNewPairBox_CheckedChanged(object sender, EventArgs e) {
            label2.Enabled = useNewPairBox.Checked;
            label3.Enabled = useNewPairBox.Checked;
            souceLanguageBox.Enabled = useNewPairBox.Checked;
            targetLanguageBox.Enabled = useNewPairBox.Checked;
            addLanguagePairBox.Enabled = useNewPairBox.Checked;

            if (ignoreNextCheckEvent) return;
            ignoreNextCheckEvent = true; // to prevent infinite cycle
            useSavedPairBox.Checked = !useNewPairBox.Checked;
            ignoreNextCheckEvent = false;
        }

        /// <summary>
        /// Called when "use existing language pair" checkbox is checked. Enables/disables respective controls.
        /// </summary>        
        private void UseSavedPairBox_CheckedChanged(object sender, EventArgs e) {
            label4.Enabled = useSavedPairBox.Checked;
            languagePairsBox.Enabled = useSavedPairBox.Checked;

            if (ignoreNextCheckEvent) return;
            ignoreNextCheckEvent = true; // to prevent infinite cycle
            useNewPairBox.Checked = !useSavedPairBox.Checked;
            ignoreNextCheckEvent = false;
        }
       
        /// <summary>
        /// Provider combobox value changed
        /// </summary>        
        private void ProviderBox_SelectedIndexChanged(object sender, EventArgs e) {
            Provider = ((ProviderItem)providerBox.SelectedItem).Provider;
        }

        /// <summary>
        /// Selected language pair changed
        /// </summary>        
        private void LanguagePairsBox_SelectedIndexChanged(object sender, EventArgs e) {
            LanguagePair = (SettingsObject.LanguagePair)languagePairsBox.SelectedItem;
        }

        /// <summary>
        /// Initializes the language pair and saves it if requested
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GlobalTranslateForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (DialogResult == DialogResult.OK) {
                try {
                    if (useSavedPairBox.Checked) { // user selected "use existing language pair", but didn't specify the language pair
                        if (LanguagePair == null) throw new Exception("Saved language pair must be specified.");
                    }
                    if (useNewPairBox.Checked) { // user selected "use new language pair"
                        if (souceLanguageBox.SelectedIndex == -1 || targetLanguageBox.SelectedIndex == -1)
                            throw new Exception("Both source and target language must be specified.");

                        string srcLanguage = null, targetLanguage = null;
                        if (souceLanguageBox.SelectedIndex == 0) { // "auto" option selected
                            srcLanguage = string.Empty;
                        } else {
                            srcLanguage = displayedCultures[souceLanguageBox.SelectedIndex - 1].TwoLetterISOLanguageName;
                        }
                        targetLanguage = displayedCultures[targetLanguageBox.SelectedIndex].TwoLetterISOLanguageName;

                        // create new language pair from specified languages
                        LanguagePair = new SettingsObject.LanguagePair() { FromLanguage = srcLanguage, ToLanguage = targetLanguage };

                        // if user specified to remember the pair and such pair does not exist, save it
                        if (addLanguagePairBox.Checked && !SettingsObject.Instance.LanguagePairs.Contains(LanguagePair)) {
                            SettingsObject.Instance.LanguagePairs.Add(LanguagePair);
                            SettingsObject.Instance.NotifyPropertyChanged(CHANGE_CATEGORY.EDITOR);
                        }
                    }
                } catch (Exception ex) {
                    e.Cancel = true; 
                    VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                    VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
                }
            }
        }

        /// <summary>
        /// Item in the list of translation providers
        /// </summary>
        private class ProviderItem {
            public TRANSLATE_PROVIDER Provider;

            public ProviderItem(TRANSLATE_PROVIDER prov) {
                this.Provider = prov;
            }

            public override string ToString() {
                return Provider.ToHumanForm();
            }
        }

    }
}
