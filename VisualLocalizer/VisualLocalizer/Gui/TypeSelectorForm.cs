using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using VisualLocalizer.Components;

namespace VisualLocalizer.Gui {

    /// <summary>
    /// Dialog allowing user select any Type from any Assembly
    /// </summary>
    public partial class TypeSelectorForm : Form {

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeSelectorForm"/> class.
        /// </summary>
        public TypeSelectorForm() {
            InitializeComponent();

            this.Icon = VSPackage._400;
        }

        /// <summary>
        /// Type previously set in the grid
        /// </summary>
        public Type OriginalType {
            get;
            set;
        }

        private Type _ResultType;

        /// <summary>
        /// User selected type
        /// </summary>
        public Type ResultType {
            get { return _ResultType; }
            private set {
                _ResultType = value;
                typeNameLabel.Text = value == null ? "(unknown)" : value.AssemblyQualifiedName;
                okButton.Enabled = value != null;
            }
        }

        /// <summary>
        /// Initializes list of assemblies and selects original type
        /// </summary>        
        private void TypeSelectorForm_Load(object sender, EventArgs e) {
            try {
                assemblyBox.Items.AddRange(AppDomain.CurrentDomain.GetAssemblies().OrderBy((assembly) => { return assembly.ToString(); }).ToArray());

                ResultType = OriginalType;

                if (OriginalType != null) {
                    assemblyBox.SelectedItem = OriginalType.Assembly;
                    typeBox.SelectedItem = OriginalType;
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Initializes list of types
        /// </summary>        
        private void assemblyBox_SelectedIndexChanged(object sender, EventArgs e) {
            try {
                Assembly a = (Assembly)assemblyBox.SelectedItem;
                if (a == null) ResultType = null;

                typeBox.Items.Clear();
                typeBox.Items.AddRange(a.GetTypes().OrderBy((type) => { return type.ToString(); }).ToArray());

                ResultType = null;
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Sets currently selected type as a result
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void typeBox_SelectedIndexChanged(object sender, EventArgs e) {
            Type type = (Type)typeBox.SelectedItem;
            ResultType = type;
        }

        /// <summary>
        /// Handle CTRL+Enter and Escape closing events
        /// </summary>
        private bool ctrlDown = false;
        private void TypeWindow_KeyDown(object sender, KeyEventArgs e) {
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


        private void TypeWindow_KeyUp(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.ControlKey) ctrlDown = false;
        }

    }    
}
