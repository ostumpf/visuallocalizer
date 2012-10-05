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

        private Color errorColor = Color.FromArgb(255, 200, 200);
        private Color existingKeyColor = Color.FromArgb(213, 255, 213);

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
            Value = valueBox.Text;
            SelectedItem = comboBox.SelectedItem as ResXProjectItem;
            UsingFullName = fullBox.Checked;
            OverwrittenValue = existingValueLabel.Text;

            if (Result == SELECT_RESOURCE_FILE_RESULT.INLINE || Result == SELECT_RESOURCE_FILE_RESULT.OVERWRITE)
                Key = SelectedItem.GetRealKey(Key);

            foreach (ResXProjectItem item in comboBox.Items)
                item.Unload();

            VLDocumentViewsManager.ReleaseLocks();
        }

        private void keyBox_TextChanged(object sender, EventArgs e) {
            validate();
        }

        private void comboBox_SelectedIndexChanged(object sender, EventArgs e) {            
            validate();
        }

        private void valueBox_TextChanged(object sender, EventArgs e) {
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
                if (!item.IsLoaded) {
                    item.Load();
                    VLDocumentViewsManager.SetFileReadonly(item.InternalProjectItem.Properties.Item("FullPath").Value.ToString(), true);                    
                }

                bool ident = keyBox.Text.IsValidIdentifier(ref errorText);                
                keyBox.BackColor = (ident ? Color.White : errorColor);

                if (ident) {
                    CONTAINS_KEY_RESULT keyConflict = item.StringKeyInConflict(keyBox.Text, valueBox.Text);
                    Color backColor = Color.White;
                    switch (keyConflict) {
                        case CONTAINS_KEY_RESULT.EXISTS_WITH_SAME_VALUE:
                            backColor = existingKeyColor;
                            break;
                        case CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE:
                            errorText = "Key is already present and has different value";
                            existingValueLabel.Text = item.GetString(keyBox.Text);
                            backColor = errorColor;
                            break;
                        case CONTAINS_KEY_RESULT.DOESNT_EXIST:
                            backColor = Color.White;
                            break;
                    }

                    overwriteButton.Visible = keyConflict == CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE;
                    inlineButton.Visible = keyConflict == CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE;
                    existingValueLabel.Visible = keyConflict == CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE;
                    existingLabel.Visible = keyConflict == CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE;

                    keyBox.BackColor = backColor;
                    valueBox.BackColor = backColor;
                }

                ok = ident && !overwriteButton.Visible;

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
            DialogResult = DialogResult.OK;
            Close();
        }

        private void inlineButton_Click(object sender, EventArgs e) {            
            Result = SELECT_RESOURCE_FILE_RESULT.INLINE;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void okButton_Click(object sender, EventArgs e) {
            Result = SELECT_RESOURCE_FILE_RESULT.OK;
            DialogResult = DialogResult.OK;
            Close();
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
       
        public ResXProjectItem SelectedItem {
            get;
            private set;
        }

        public bool UsingFullName {
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
