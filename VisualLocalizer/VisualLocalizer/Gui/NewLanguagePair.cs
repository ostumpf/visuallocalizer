using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

namespace VisualLocalizer.Gui {

    /// <summary>
    /// Dialog enabling user to create new translation language pair
    /// </summary>
    public partial class NewLanguagePairWindow : Form {

        private CultureInfo[] displayedCultures;

        /// <summary>
        /// Creates new instance
        /// </summary>
        /// <param name="displayOptionalAddToList">True if checkbox "add to the list" should be displayed</param>
        public NewLanguagePairWindow(bool displayOptionalAddToList) {
            InitializeComponent();

            // add translation languages
            displayedCultures = CultureInfo.GetCultures(CultureTypes.FrameworkCultures);
            sourceBox.Items.Add("(auto)"); // add option "auto" to source languages (translation service auto-detects source language)
            foreach (var culture in displayedCultures) {                
                sourceBox.Items.Add(culture.DisplayName);
                targetBox.Items.Add(culture.DisplayName);
            }
            
            sourceBox.SelectedIndex = 0;
            targetBox.SelectedIndex = 0;

            addToListBox.Visible = displayOptionalAddToList;
            rememberLabel.Visible = displayOptionalAddToList;

            if (displayOptionalAddToList) {
                translateButton.Text = "Translate";
            } else {
                translateButton.Text = "Add pair";
            }
        }

        /// <summary>
        /// Language from which the text will be translated
        /// </summary>
        public string SourceLanguage {
            get;
            private set;
        }

        /// <summary>
        /// Language to which the text will be translated
        /// </summary>
        public string TargetLanguage {
            get;
            private set;
        }

        /// <summary>
        /// True if this language pair should be remembered
        /// </summary>
        public bool AddToList {
            get;
            private set;
        }

        private bool ctrlDown = false;

        /// <summary>
        /// Handles CTRL+Enter and Escape to close the form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewLanguagePairWindow_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Escape) {
                e.Handled = true;
                cancelButton.PerformClick();
            }

            if ((e.KeyCode == Keys.Enter) && ctrlDown) {
                e.Handled = true;
                translateButton.PerformClick();
            }

            if (e.KeyCode == Keys.ControlKey) ctrlDown = true;
        }


        private void NewLanguagePairWindow_KeyUp(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.ControlKey) ctrlDown = false;
        }

        private void NewLanguagePairWindow_FormClosing(object sender, FormClosingEventArgs e) {
            if (sourceBox.SelectedIndex == 0) {
                SourceLanguage = string.Empty; // "auto" option was selected
            } else {
                SourceLanguage = displayedCultures[sourceBox.SelectedIndex - 1].TwoLetterISOLanguageName;
            }
            TargetLanguage = displayedCultures[targetBox.SelectedIndex].TwoLetterISOLanguageName;
            AddToList = addToListBox.Checked;
        }
    }
}
