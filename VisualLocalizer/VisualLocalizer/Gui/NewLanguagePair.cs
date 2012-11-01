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
    public partial class NewLanguagePairWindow : Form {

        private CultureInfo[] displayedCultures;

        public NewLanguagePairWindow(bool displayOptionalAddToList) {
            InitializeComponent();

            displayedCultures = CultureInfo.GetCultures(CultureTypes.FrameworkCultures);
            sourceBox.Items.Add("(auto)");
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

        public string SourceLanguage {
            get;
            private set;
        }

        public string TargetLanguage {
            get;
            private set;
        }

        public bool AddToList {
            get;
            private set;
        }

        private bool ctrlDown = false;
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
                SourceLanguage = string.Empty;
            } else {
                SourceLanguage = displayedCultures[sourceBox.SelectedIndex - 1].TwoLetterISOLanguageName;
            }
            TargetLanguage = displayedCultures[targetBox.SelectedIndex].TwoLetterISOLanguageName;
            AddToList = addToListBox.Checked;
        }
    }
}
