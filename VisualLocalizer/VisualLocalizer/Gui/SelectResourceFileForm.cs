using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VisualLocalizer.Editor;

namespace VisualLocalizer.Gui {
    internal partial class SelectResourceFileForm : Form {

        private Color errorColor=Color.FromArgb(254,200,200);
        private Dictionary<ResXProjectItem, List<string>> usedKeys;

        public SelectResourceFileForm() {
            InitializeComponent();
        }

        public void SetData(string key, string value, List<ResXProjectItem> options) {
            usedKeys = new Dictionary<ResXProjectItem, List<string>>();

            foreach (var item in options)
                usedKeys.Add(item, ResXFileHandler.GetKeys(item));

            keyBox.Text = key;
            valueBox.Text = value;
            comboBox.Items.AddRange(options.ToArray());
            comboBox.SelectedIndex = 0;
        }

        public void GetData(out string key, out string value, out ResXProjectItem item) {
            key = keyBox.Text;
            value = valueBox.Text;
            item = comboBox.SelectedItem as ResXProjectItem;
        }

        private void keyBox_TextChanged(object sender, EventArgs e) {
            validate();
        }

        private void comboBox_SelectedIndexChanged(object sender, EventArgs e) {
            validate();
        }

        private void validate() {
            if (comboBox.SelectedIndex < 0) return;

            string errorText = null;
            bool ident = Utils.IsValidIdentifier(keyBox.Text, comboBox.SelectedItem as ResXProjectItem, ref errorText);
            bool exists=existsKey(keyBox.Text, comboBox.SelectedItem as ResXProjectItem);
            if (exists)
                errorText = "Key is already present in the dictionary";

            ident = ident && !exists;

            okButton.Enabled = ident;
            keyBox.BackColor = (ident ? Color.White : errorColor);

            if (ident)
                errorLabel.Text = "";
            else
                errorLabel.Text = errorText;
        }

        private bool existsKey(string key, ResXProjectItem item) {
            return usedKeys[item].Contains(key, CaseInsensitiveComparer.Instance);
        }

        private class CaseInsensitiveComparer : IEqualityComparer<string> {

            private static CaseInsensitiveComparer instance;

            public static CaseInsensitiveComparer Instance {
                get {
                    if (instance == null) instance = new CaseInsensitiveComparer();
                    return instance;
                }
            }

            public bool Equals(string x, string y) {
                return x.ToLower() == y.ToLower();
            }

            public int GetHashCode(string obj) {
                return obj.GetHashCode();
            }
        }
    }
}
