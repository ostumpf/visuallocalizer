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
using VisualLocalizer.Extensions;
using VisualLocalizer.Settings;

namespace VisualLocalizer.Gui {

    internal enum SELECT_RESOURCE_FILE_RESULT { OK, OVERWRITE, INLINE }

    /// <summary>
    /// Represents dialog displayed on "move to resources" command, enabling user to modify resource key, value,
    /// destination file and resolve potential name conflicts.
    /// </summary>
    internal partial class SelectResourceFileForm : Form {

        private static readonly Color ERROR_COLOR = Color.FromArgb(255, 200, 200);
        private static readonly Color EXISTING_KEY_COLOR = Color.FromArgb(213, 255, 213);
        
        private CONTAINS_KEY_RESULT keyConflict;        
        private CodeStringResultItem resultItem;
        private ReferenceString referenceText;
        
        public SelectResourceFileForm(ProjectItem sourceItem, CodeStringResultItem resultItem) {
            InitializeComponent();
            this.Icon = VSPackage._400;
            this.resultItem = resultItem;
            this.referenceText = new ReferenceString();
           // this.projectItemLang=sourceItem.GetFileType()==FILETYPE.

            // add suggestions to suggestions list
            foreach (string s in resultItem.GetKeyNameSuggestions())
                keyBox.Items.Add(s);

            keyBox.SelectedIndex = 0;
           
            valueBox.Text = resultItem.Value;
            // add possible destination files
            foreach (var item in sourceItem.ContainingProject.GetResXItemsAround(resultItem.SourceItem, true, false)) {
                comboBox.Items.Add(item);
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

            // in case of conflict
            if (Result == SELECT_RESOURCE_FILE_RESULT.INLINE || Result == SELECT_RESOURCE_FILE_RESULT.OVERWRITE)
                Key = SelectedItem.GetRealKey(Key);

            // free loaded project items
            foreach (ResXProjectItem item in comboBox.Items)
                item.Unload();
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

        /// <summary>
        /// Executed on every change in window form.
        /// </summary>
        private void validate() {
            bool existsFile = comboBox.Items.Count > 0; // there's at least one possible destination file
            string errorText = null;
            bool ok = true;

            if (!existsFile) { // no destination file
                ok = false;
                errorText = "Project does not contain any useable resource files";
            } else {
                ResXProjectItem item = comboBox.SelectedItem as ResXProjectItem;
                if (!item.IsLoaded) {
                    item.Load();
                    VLDocumentViewsManager.SetFileReadonly(item.InternalProjectItem.GetFullPath(), true);                    
                }

                bool empty = string.IsNullOrEmpty(keyBox.Text);
                bool validIdentifier = keyBox.Text.IsValidIdentifier(resultItem.Language);
                bool hasOwnDesigner = item.DesignerItem != null && !item.IsCultureSpecific();
                bool identifierError = false;

                switch (SettingsObject.Instance.BadKeyNamePolicy) {
                    case BAD_KEY_NAME_POLICY.IGNORE_COMPLETELY:
                        identifierError = empty;
                        break;
                    case BAD_KEY_NAME_POLICY.IGNORE_ON_NO_DESIGNER:
                        identifierError = empty || (!validIdentifier && hasOwnDesigner);
                        break;
                    case BAD_KEY_NAME_POLICY.WARN_ALWAYS:
                        identifierError = empty || !validIdentifier;
                        break;
                }
                                
                if (identifierError) errorText = "Key is not a valid identifier";
                keyBox.BackColor = (identifierError ? ERROR_COLOR : Color.White);

                if (!identifierError) {
                    keyConflict = item.StringKeyInConflict(keyBox.Text, valueBox.Text);
                    Color backColor = Color.White;
                    switch (keyConflict) {
                        case CONTAINS_KEY_RESULT.EXISTS_WITH_SAME_VALUE:
                            backColor = EXISTING_KEY_COLOR;
                            break;
                        case CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE:
                            errorText = "Key is already present and has different value";
                            existingValueLabel.Text = item.GetString(keyBox.Text);
                            backColor = ERROR_COLOR;
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

                ok = !identifierError && !overwriteButton.Visible;

                referenceText.ClassPart = item.Class;
                referenceText.KeyPart = keyBox.Text;

                if (string.IsNullOrEmpty(item.Namespace)) {
                    ok = false;
                    errorText = "Cannot reference resources in this file";
                } else {                    
                    if (!usingBox.Checked || resultItem.MustUseFullName) {
                        referenceText.NamespacePart = item.Namespace;
                    } else {
                        referenceText.NamespacePart = null;
                    }
                }

                referenceLabel.Text = resultItem.GetReferenceText(referenceText);
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
            if (keyConflict == CONTAINS_KEY_RESULT.EXISTS_WITH_SAME_VALUE) {
                Result = SELECT_RESOURCE_FILE_RESULT.INLINE;
            } else {
                Result = SELECT_RESOURCE_FILE_RESULT.OK;
            }
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
