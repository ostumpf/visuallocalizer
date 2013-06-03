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
using VisualLocalizer.Components.Code;
using VisualLocalizer.Library.Components;
using VisualLocalizer.Library.Extensions;

namespace VisualLocalizer.Gui {

    /// <summary>
    /// Represents possible results of this dialog
    /// </summary>
    internal enum SELECT_RESOURCE_FILE_RESULT { 
        /// <summary>
        /// New key will be added to the resource file
        /// </summary>
        OK, 

        /// <summary>
        /// Such key already exists, it will be overwritten with new provided value
        /// </summary>
        OVERWRITE, 

        /// <summary>
        /// Existing key will be referenced
        /// </summary>
        INLINE 
    }

    /// <summary>
    /// Represents dialog displayed on "move to resources" command, enabling user to modify resource key, value,
    /// destination file and resolve potential name conflicts.
    /// </summary>
    internal partial class SelectResourceFileForm : Form {

        /// <summary>
        /// Background color in case of error (red)
        /// </summary>
        private static readonly Color ERROR_COLOR = Color.FromArgb(255, 200, 200);

        /// <summary>
        /// Background color in case of existing key and same value (light green)
        /// </summary>
        private static readonly Color EXISTING_KEY_COLOR = Color.FromArgb(213, 255, 213);
        
        /// <summary>
        /// Type of conflict of key names
        /// </summary>
        private CONTAINS_KEY_RESULT keyConflict;        

        /// <summary>
        /// Result item this form is displayed for
        /// </summary>
        private CodeStringResultItem resultItem;

        /// <summary>
        /// Data from which reference text will be composed
        /// </summary>
        private ReferenceString referenceText;
        
        public SelectResourceFileForm(ProjectItem sourceItem, CodeStringResultItem resultItem) {
            if (sourceItem == null) throw new ArgumentNullException("sourceItem");
            if (resultItem == null) throw new ArgumentNullException("resultItem");

            InitializeComponent();            

            this.Icon = VSPackage._400;
            this.resultItem = resultItem;
            this.referenceText = new ReferenceString();
           
            // add suggestions of key names to suggestions list
            foreach (string s in resultItem.GetKeyNameSuggestions())
                keyBox.Items.Add(s);

            if (SettingsObject.Instance.SelectedKeyIndex >= 0 && SettingsObject.Instance.SelectedKeyIndex < keyBox.Items.Count) {
                keyBox.SelectedIndex = SettingsObject.Instance.SelectedKeyIndex;
            } else {
                keyBox.SelectedIndex = 0;
            }            
           
            valueBox.Text = resultItem.Value;            
            // add possible destination files

            int selectedPathIndex = -1;
            int i = 0;
            foreach (var item in sourceItem.ContainingProject.GetResXItemsAround(true, false)) {
                comboBox.Items.Add(item);
                if (string.Compare(item.InternalProjectItem.GetFullPath(), SettingsObject.Instance.SelectedResourceFilePath, true) == 0) {
                    selectedPathIndex = i;
                }
                i++;
            }

            if (selectedPathIndex >= 0 && selectedPathIndex < comboBox.Items.Count) {
                comboBox.SelectedIndex = selectedPathIndex;
            } else {
                if (comboBox.Items.Count > 0)
                    comboBox.SelectedIndex = 0;
            }

            usingBox.Checked = !SettingsObject.Instance.SelectedUseFullName;
            fullBox.Checked = SettingsObject.Instance.SelectedUseFullName;

            overwriteButton.Visible = false;
            inlineButton.Visible = false;
            existingLabel.Visible = false;
            existingValueBox.Visible = false;
            
            errorLabel.Text = "";            
        }

        /// <summary>
        /// Initializes output values from the controls
        /// </summary>        
        private void SelectResourceFileForm_FormClosing(object sender, FormClosingEventArgs e) {
            // initialize output values
            Key = keyBox.Text;
            Value = valueBox.Text;
            SelectedItem = comboBox.SelectedItem as ResXProjectItem;
            UsingFullName = fullBox.Checked;
            OverwrittenValue = existingValueBox.Text;

            SettingsObject.Instance.IgnorePropertyChanges = true;
            SettingsObject.Instance.SelectedUseFullName = fullBox.Checked;
            SettingsObject.Instance.SelectedResourceFilePath = (comboBox.SelectedItem == null ? null : ((ResXProjectItem)comboBox.SelectedItem).InternalProjectItem.GetFullPath());
            SettingsObject.Instance.SelectedKeyIndex = keyBox.SelectedIndex;
            SettingsObject.Instance.IgnorePropertyChanges = false;
            SettingsObject.Instance.NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);

            // in case of conflict
            if (Result == SELECT_RESOURCE_FILE_RESULT.INLINE || Result == SELECT_RESOURCE_FILE_RESULT.OVERWRITE)
                Key = SelectedItem.GetRealKey(Key); // get case-sensitive key name

            // free loaded project items
            foreach (ResXProjectItem item in comboBox.Items)
                item.Unload();
        }

        /// <summary>
        /// Validates data
        /// </summary>        
        private void KeyBox_TextChanged(object sender, EventArgs e) {
            ValidateData();
        }

        /// <summary>
        /// Validates data
        /// </summary>
        private void ComboBox_SelectedIndexChanged(object sender, EventArgs e) {            
            ValidateData();
        }

        /// <summary>
        /// Validates data
        /// </summary>
        private void ValueBox_TextChanged(object sender, EventArgs e) {
            ValidateData();
        }

        /// <summary>
        /// Validates data
        /// </summary>
        private void SelectResourceFileForm_Load(object sender, EventArgs e) {
            ValidateData();
        }

        /// <summary>
        /// Validates data
        /// </summary>
        private void UsingBox_CheckedChanged(object sender, EventArgs e) {
            fullBox.Checked = !usingBox.Checked;
            ValidateData();
        }

        /// <summary>
        /// Executed on every change in window form
        /// </summary>
        private void ValidateData() {
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
                resultItem.DestinationItem = item;

                bool isKeyEmpty = string.IsNullOrEmpty(keyBox.Text);
                bool isValidIdentifier = keyBox.Text.IsValidIdentifier(resultItem.Language);
                bool hasOwnDesigner = (item.DesignerItem != null || item.HasImplicitDesignerFile) && !item.IsCultureSpecific();
                bool identifierErrorExists = false;

                // determine whether current key name is valid
                switch (SettingsObject.Instance.BadKeyNamePolicy) {
                    case BAD_KEY_NAME_POLICY.IGNORE_COMPLETELY:
                        identifierErrorExists = isKeyEmpty; // only empty keys are invalid
                        break;
                    case BAD_KEY_NAME_POLICY.IGNORE_ON_NO_DESIGNER:
                        identifierErrorExists = isKeyEmpty || (!isValidIdentifier && hasOwnDesigner); // empty keys and invalid identifiers in ResX files with their own designer file
                        break;
                    case BAD_KEY_NAME_POLICY.WARN_ALWAYS:
                        identifierErrorExists = isKeyEmpty || !isValidIdentifier; // empty keys and invalid identifiers
                        break;
                }
                                               
                if (!identifierErrorExists) { // identifier ok - check for key name conflicts
                    keyConflict = item.GetKeyConflictType(keyBox.Text, valueBox.Text, true);
                    
                    Color backColor = Color.White;
                    switch (keyConflict) {
                        case CONTAINS_KEY_RESULT.EXISTS_WITH_SAME_VALUE: // key already exists and has the same value - ok
                            backColor = EXISTING_KEY_COLOR;
                            break;
                        case CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE: // key exists with different value - error
                            errorText = "Key is already present and has different value";
                            existingValueBox.Text = item.GetString(keyBox.Text);
                            backColor = ERROR_COLOR;
                            break;
                        case CONTAINS_KEY_RESULT.DOESNT_EXIST: // key doesn't exists - ok
                            backColor = Color.White;
                            break;
                    }

                    overwriteButton.Visible = keyConflict == CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE;
                    inlineButton.Visible = keyConflict == CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE;
                    existingValueBox.Visible = keyConflict == CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE;
                    existingLabel.Visible = keyConflict == CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE;

                    keyBox.BackColor = backColor;
                    valueBox.BackColor = backColor;
                } else {
                    errorText = "Key is not a valid identifier";
                    keyBox.BackColor = ERROR_COLOR;
                    valueBox.BackColor = Color.White;
                }

                ok = !identifierErrorExists && !overwriteButton.Visible;

                referenceText.ClassPart = item.Class;
                referenceText.KeyPart = keyBox.Text;

                if (string.IsNullOrEmpty(item.Namespace)) { // no namespace was found in designer file - error
                    ok = false;
                    errorText = "Cannot reference resources in this file, missing namespace";
                } else {                    
                    if (!usingBox.Checked || resultItem.MustUseFullName) { // force using full reference
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
        /// <summary>
        /// Handle closing the form on CTRL+Enter or Escape
        /// </summary>        
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

        /// <summary>
        /// Closes the form with Overwrite result
        /// </summary>        
        private void OverwriteButton_Click(object sender, EventArgs e) {            
            Result = SELECT_RESOURCE_FILE_RESULT.OVERWRITE;
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Closes the form with Inline result
        /// </summary>
        private void InlineButton_Click(object sender, EventArgs e) {            
            Result = SELECT_RESOURCE_FILE_RESULT.INLINE;
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Closes the form with Ok or Inline result, based on key name conflicts
        /// </summary>
        private void OkButton_Click(object sender, EventArgs e) {
            if (keyConflict == CONTAINS_KEY_RESULT.EXISTS_WITH_SAME_VALUE) {
                Result = SELECT_RESOURCE_FILE_RESULT.INLINE;
            } else {
                Result = SELECT_RESOURCE_FILE_RESULT.OK;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Type of operation this form requests
        /// </summary>
        public SELECT_RESOURCE_FILE_RESULT Result {
            get;
            private set;
        }

        /// <summary>
        /// Resource key name
        /// </summary>
        public string Key {
            get;
            private set;
        }

        /// <summary>
        /// Resource value
        /// </summary>
        public string Value {
            get;
            private set;
        }

        /// <summary>
        /// Original value, before overwritting
        /// </summary>
        public string OverwrittenValue {
            get;
            private set;
        }
       
        /// <summary>
        /// Destination ResX file
        /// </summary>
        public ResXProjectItem SelectedItem {
            get;
            private set;
        }

        /// <summary>
        /// True if full reference (including namespace) should be used
        /// </summary>
        public bool UsingFullName {
            get;
            private set;
        }                                   

        /// <summary>
        /// Resizes the text box vertically
        /// </summary>        
        private void ExistingValueBox_TextChanged(object sender, EventArgs e) {
            Size sz = new Size(existingValueBox.ClientSize.Width, int.MaxValue);
            TextFormatFlags flags = TextFormatFlags.WordBreak;
            int padding = 3;
            int borders = existingValueBox.Height - existingValueBox.ClientSize.Height;
            sz = TextRenderer.MeasureText(existingValueBox.Text, existingValueBox.Font, sz, flags);
            int h = sz.Height + borders + padding;
            if (existingValueBox.Top + h > this.ClientSize.Height - 10) {
                h = this.ClientSize.Height - 10 - existingValueBox.Top;
            }
            existingValueBox.Height = h;
        }

       
    }
}
