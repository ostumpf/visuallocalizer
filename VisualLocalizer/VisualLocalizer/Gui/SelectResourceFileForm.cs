using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VisualLocalizer.Editor;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using EnvDTE;

namespace VisualLocalizer.Gui {

    internal enum SELECT_RESOURCE_FILE_RESULT { OK, OVERWRITE, INLINE }

    internal partial class SelectResourceFileForm : Form {

        private Color errorColor=Color.FromArgb(254,200,200);
      
        public SelectResourceFileForm(List<string> keys, string value,Project project) {
            InitializeComponent();

            keyBox.Items.AddRange(keys.ToArray());
            keyBox.SelectedIndex = 0;
            valueBox.Text = value;            

            List<ProjectItem> items = project.GetFiles(ResXProjectItem.IsItemResX, true);
            List<ResXProjectItem> resxItems = new List<ResXProjectItem>();
            foreach (ProjectItem item in items) {
                var resxItem = ResXProjectItem.ConvertToResXItem(item, project);                
                comboBox.Items.Add(resxItem);
            }

            if (comboBox.Items.Count > 0)
                comboBox.SelectedIndex = 0;

            overwriteButton.Visible = false;
            inlineButton.Visible = false;
            existingLabel.Visible = false;
            existingValueLabel.Visible = false;
            
            errorLabel.Text = "";
        }


        private void SelectResourceFileForm_FormClosing(object sender, FormClosingEventArgs e) {
            Key = keyBox.Text;
            Value = valueBox.Text.Replace(Environment.NewLine, "\\" + "n");
            SelectedItem = comboBox.SelectedItem as ResXProjectItem;
            UsingFullName = fullBox.Checked;
            ReferenceText = referenceLabel.Text;
            OverwrittenValue = existingValueLabel.Text;

            foreach (ResXProjectItem item in comboBox.Items)
                item.Unload();
        }

        private void keyBox_TextChanged(object sender, EventArgs e) {
            validate();
        }

        private void comboBox_SelectedIndexChanged(object sender, EventArgs e) {
            if (comboBox.SelectedItem != null) {
                var item = (comboBox.SelectedItem as ResXProjectItem);
                if (!item.IsLoaded)
                    item.Load();
            }
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
                bool ident = keyBox.Text.IsValidIdentifier(ref errorText);
                bool exists = item.ContainsKey(keyBox.Text);

                keyBox.BackColor = (ident ? Color.White : errorColor);                
                if (exists) {
                    errorText = "Key is already present in the dictionary";
                    existingValueLabel.Text = item.GetString(keyBox.Text);
                }
                overwriteButton.Visible = exists;
                inlineButton.Visible = exists;
                existingValueLabel.Visible = exists;
                existingLabel.Visible = exists;
                
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

        private bool ctrlDown = false;
        private void SelectResourceFileForm_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Escape) {
                e.Handled = true;
                cancelButton.PerformClick();                
            }

            if ((e.KeyCode == Keys.Enter) && ctrlDown) {
                e.Handled = true;
                okButton.PerformClick();                
            }

            if (e.KeyCode == Keys.ControlKey) ctrlDown = true;
        }


        private void SelectResourceFileForm_KeyUp(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.ControlKey) ctrlDown = false;
        }

        private void overwriteButton_Click(object sender, EventArgs e) {
            Result = SELECT_RESOURCE_FILE_RESULT.OVERWRITE;
        }

        private void inlineButton_Click(object sender, EventArgs e) {
            Result = SELECT_RESOURCE_FILE_RESULT.INLINE;
        }

        private void okButton_Click(object sender, EventArgs e) {
            Result = SELECT_RESOURCE_FILE_RESULT.OK;
        }

        public SELECT_RESOURCE_FILE_RESULT Result {
            get;
            private set;
        }

        public string Key {
            get;
            private set;
        }

        public string Value {
            get;
            private set;
        }

        public string OverwrittenValue {
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
