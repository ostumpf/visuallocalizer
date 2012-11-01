using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.IO;

namespace VisualLocalizer.Gui {
    internal partial class NewImageWindow : Form {

        private static readonly Color errorColor = Color.FromArgb(255, 213, 213);
        private bool widthOk, heightOk, nameOk;

        public NewImageWindow(bool iconsOnly) {
            InitializeComponent();

            widthOk = true;
            heightOk = true;
            nameOk = true;

            formatBox.Items.Add(new FormatBoxItem() { Text = "PNG", Value = System.Drawing.Imaging.ImageFormat.Png, Extensions = new string[] { ".png" } });
            formatBox.Items.Add(new FormatBoxItem() { Text = "JPEG", Value = System.Drawing.Imaging.ImageFormat.Jpeg, Extensions = new string[] { ".jpg", ".jpeg" } });
            formatBox.Items.Add(new FormatBoxItem() { Text = "GIF", Value = System.Drawing.Imaging.ImageFormat.Gif, Extensions = new string[] { ".gif" } });
            formatBox.Items.Add(new FormatBoxItem() { Text = "TIFF", Value = System.Drawing.Imaging.ImageFormat.Tiff, Extensions = new string[] { ".tiff", ".tif" } });
            formatBox.Items.Add(new FormatBoxItem() { Text = "BMP", Value = System.Drawing.Imaging.ImageFormat.Bmp, Extensions = new string[] { ".bmp" } });
            formatBox.Items.Add(new FormatBoxItem() { Text = "ICO", Value = System.Drawing.Imaging.ImageFormat.Icon, Extensions = new string[] { ".ico" } });

            if (iconsOnly) {
                formatBox.SelectedIndex = 5;
                widthBox.Text = "32";
                heightBox.Text = "32";
                formatBox.Enabled = false;
            } else {
                formatBox.SelectedIndex = 0;
                widthBox_TextChanged(null, null);
                heightBox_TextChanged(null, null);
                nameBox_TextChanged(null, null);
            }

            okButton.Focus();
        }

        public string ImageName {
            get;
            private set;
        }

        public int ImageWidth {
            get;
            private set;
        }

        public int ImageHeight {
            get;
            private set;
        }

        public FormatBoxItem ImageFormat {
            get;
            private set;
        }

        private void updateOkEnabled() {
            okButton.Enabled = widthOk && heightOk && nameOk;
        }

        private void widthBox_TextChanged(object sender, EventArgs e) {
            int result;
            bool ok = int.TryParse(widthBox.Text, out result);
            ImageWidth = result;
            widthOk = ok;
            widthBox.BackColor = ok ? Color.White : errorColor;

            updateOkEnabled();
        }

        private void heightBox_TextChanged(object sender, EventArgs e) {
            int result;
            bool ok = int.TryParse(heightBox.Text, out result);
            ImageHeight = result;
            heightOk = ok;
            heightBox.BackColor = ok ? Color.White : errorColor;

            updateOkEnabled();
        }

        private void nameBox_TextChanged(object sender, EventArgs e) {
            string name = nameBox.Text;
            bool ok = true;
            foreach (char c in Path.GetInvalidFileNameChars()) {
                foreach (char c2 in name)
                    if (c == c2) {
                        ok = false;
                        break;
                    }
                if (!ok) break;
            }
            nameOk = ok;
            nameBox.BackColor = ok ? Color.White : errorColor;
            ImageName = name;

            updateOkEnabled();
        }

        private void formatBox_SelectedIndexChanged(object sender, EventArgs e) {
            ImageFormat = ((FormatBoxItem)formatBox.SelectedItem);
        }

        private bool ctrlDown = false;
        private void NewImageWindow_KeyDown(object sender, KeyEventArgs e) {
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


        private void NewImageWindow_KeyUp(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.ControlKey) ctrlDown = false;
        }


        internal sealed class FormatBoxItem {
            public ImageFormat Value { get; set; }
            public string Text { get; set; }
            public string[] Extensions { get; set; }

            public override string ToString() {
                return Text;
            }
        }
        
    }
}

