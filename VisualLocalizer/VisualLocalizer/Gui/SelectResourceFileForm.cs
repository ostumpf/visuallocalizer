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
        private Dictionary<ResXProjectItem, List<string>> usedKeys = new Dictionary<ResXProjectItem, List<string>>();

        public SelectResourceFileForm() {
            InitializeComponent();
        }

        public void SetData(string key, string value, List<ResXProjectItem> resourceItems) {
            keyBox.Text = key;
            valueBox.Text = value;
            
            usedKeys.Clear();
            foreach (var item in resourceItems)
                usedKeys.Add(item, ResXFileHandler.GetKeys(item));

            comboBox.Items.AddRange(resourceItems.ToArray());
            if (comboBox.Items.Count > 0) 
                comboBox.SelectedIndex = 0;
        }

        private void SelectResourceFileForm_FormClosing(object sender, FormClosingEventArgs e) {
            Key = keyBox.Text;
            Value = valueBox.Text;
            SelectedItem = comboBox.SelectedItem as ResXProjectItem;
            UsingFullName = fullBox.Checked;
            ReferenceText = referenceLabel.Text;
        }

        private void keyBox_TextChanged(object sender, EventArgs e) {
            validate();
        }

        private void comboBox_SelectedIndexChanged(object sender, EventArgs e) {
            validate();
        }

        private void SelectResourceFileForm_Load(object sender, EventArgs e) {
            validate();
        }

        private void usingBox_CheckedChanged(object sender, EventArgs e) {
            fullBox.Checked = !usingBox.Checked;
            validate();
        }

        private void validate() {
            bool existsFile = comboBox.Items.Count > 0;
            string errorText = null;
            bool ok = true;

            if (!existsFile) {
                ok = false;
                errorText = "Project does not contain any useable resource files";
            } else {
                ResXProjectItem item = comboBox.SelectedItem as ResXProjectItem;
                bool ident = Utils.IsValidIdentifier(keyBox.Text, item, ref errorText);
                bool exists = existsKey(keyBox.Text, item);

                keyBox.BackColor = (ident ? Color.White : errorColor);
                if (exists)
                    errorText = "Key is already present in the dictionary";

                ok = ident && !exists;

                Namespace = item.Namespace;
                if (!usingBox.Checked) {
                    referenceLabel.Text = item.Namespace + "." + item.Class + "." + keyBox.Text;
                } else
                    referenceLabel.Text = item.Class + "." + keyBox.Text;

            }

            okButton.Enabled = ok;
            if (ok)
                errorLabel.Text = string.Empty;
            else
                errorLabel.Text = errorText;
        }


        public string Key {
            get;
            private set;
        }

        public string Value {
            get;
            private set;
        }

        public string Namespace {
            get;
            private set;
        }

        public ResXProjectItem SelectedItem {
            get;
            private set;
        }

        public bool UsingFullName {
            get;
            private set;
        }

        public string ReferenceText {
            get;
            private set;
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
