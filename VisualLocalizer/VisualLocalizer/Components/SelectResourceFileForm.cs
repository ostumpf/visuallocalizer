using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace VisualLocalizer.Components {
    internal partial class SelectResourceFileForm : Form {
        public SelectResourceFileForm() {
            InitializeComponent();
        }

        public void SetData(string key, string value, List<ResXProjectItem> options) {
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
            okButton.Enabled = keyBox.Text != string.Empty;
        }

       
    }
}
