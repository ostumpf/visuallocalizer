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

    /// <summary>
    /// Dialog enabling user to create new image file by specifying dimensions and format
    /// </summary>
    internal partial class NewImageWindow : Form {

        /// <summary>
        /// Background color displayed in case of error
        /// </summary>
        private static readonly Color errorColor = Color.FromArgb(255, 213, 213);
        private bool widthOk, heightOk, nameOk;

        /// <summary>
        /// Creates new instance
        /// </summary>
        /// <param name="iconsOnly">True if image format is fixed as icon</param>
        public NewImageWindow(bool iconsOnly) {
            InitializeComponent();
            this.Icon = VSPackage._400;

            widthOk = true;
            heightOk = true;
            nameOk = true;

            // add available formats
            formatBox.Items.Add(new FormatBoxItem() { Text = "PNG", Value = System.Drawing.Imaging.ImageFormat.Png, Extensions = new string[] { ".png" } });
            formatBox.Items.Add(new FormatBoxItem() { Text = "JPEG", Value = System.Drawing.Imaging.ImageFormat.Jpeg, Extensions = new string[] { ".jpg", ".jpeg" } });
            formatBox.Items.Add(new FormatBoxItem() { Text = "GIF", Value = System.Drawing.Imaging.ImageFormat.Gif, Extensions = new string[] { ".gif" } });
            formatBox.Items.Add(new FormatBoxItem() { Text = "TIFF", Value = System.Drawing.Imaging.ImageFormat.Tiff, Extensions = new string[] { ".tiff", ".tif" } });
            formatBox.Items.Add(new FormatBoxItem() { Text = "BMP", Value = System.Drawing.Imaging.ImageFormat.Bmp, Extensions = new string[] { ".bmp" } });
            formatBox.Items.Add(new FormatBoxItem() { Text = "ICO", Value = System.Drawing.Imaging.ImageFormat.Icon, Extensions = new string[] { ".ico" } });

            if (iconsOnly) { // set format fixed to "ico"
                formatBox.SelectedIndex = 5;
                widthBox.Text = "32";
                heightBox.Text = "32";
                formatBox.Enabled = false;
            } else { // initialize values
                formatBox.SelectedIndex = 0;
                WidthBox_TextChanged(null, null);
                HeightBox_TextChanged(null, null);
                NameBox_TextChanged(null, null);
            }

            okButton.Focus();
        }

        /// <summary>
        /// Name of the image file
        /// </summary>
        public string ImageName {
            get;
            private set;
        }

        /// <summary>
        /// Width of the image in pixels
        /// </summary>
        public int ImageWidth {
            get;
            private set;
        }

        /// <summary>
        /// Height if the image in pixels
        /// </summary>
        public int ImageHeight {
            get;
            private set;
        }

        /// <summary>
        /// Image format
        /// </summary>
        public FormatBoxItem ImageFormat {
            get;
            private set;
        }

        /// <summary>
        /// Updates "OK" button state according to validity of input data
        /// </summary>
        private void UpdateOkEnabled() {
            okButton.Enabled = widthOk && heightOk && nameOk;
        }

        /// <summary>
        /// Text of the width box changed - try parse given content and set error if necessary
        /// </summary>        
        private void WidthBox_TextChanged(object sender, EventArgs e) {
            int result;
            bool ok = int.TryParse(widthBox.Text, out result);
            ImageWidth = result;
            widthOk = ok && result > 0; // dimensions must be positive
            widthBox.BackColor = ok ? Color.White : errorColor;

            UpdateOkEnabled();
        }

        /// <summary>
        /// Text of the height box changed - try parse given content and set error if necessary
        /// </summary>        
        private void HeightBox_TextChanged(object sender, EventArgs e) {
            int result;
            bool ok = int.TryParse(heightBox.Text, out result);
            ImageHeight = result;
            heightOk = ok && result > 0; // dimensions must be positive
            heightBox.BackColor = ok ? Color.White : errorColor;

            UpdateOkEnabled();
        }

        /// <summary>
        /// File name changed - check for invalid characters and set error if necessary
        /// </summary>        
        private void NameBox_TextChanged(object sender, EventArgs e) {
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
            nameOk = ok && name.Length > 0; // filename must be non-empty
            nameBox.BackColor = ok ? Color.White : errorColor;
            ImageName = name;

            UpdateOkEnabled();
        }

        /// <summary>
        /// Format changed
        /// </summary>        
        private void FormatBox_SelectedIndexChanged(object sender, EventArgs e) {
            ImageFormat = ((FormatBoxItem)formatBox.SelectedItem);
        }

        private bool ctrlDown = false;

        /// <summary>
        /// Handles closing the form by CTRL+Enter or Escape
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// An item in "Format" combobox
        /// </summary>
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

