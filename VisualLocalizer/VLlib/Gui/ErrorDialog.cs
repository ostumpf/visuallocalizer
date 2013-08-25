using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Media;

namespace VisualLocalizer.Library.Gui {

    /// <summary>
    /// Dialog displaying error information.
    /// </summary>
    public partial class ErrorDialog : Form {

        /// <summary>
        /// True if details box is expanded
        /// </summary>
        private bool detailsDisplayed = false;

        /// <summary>
        /// Creates new instance
        /// </summary>
        /// <param name="ex">Exception for which this dialog is created</param>
        /// <param name="specialInfo">String key/value pairs, displayed in the details box. Can be null.</param>
        public ErrorDialog(Exception ex, Dictionary<string, string> specialInfo) {
            if (ex == null) throw new ArgumentNullException("ex");

            InitializeComponent();

            basicErrorLabel.Text = ex.Message;
            errorIconBox.Image = SystemIcons.Error.ToBitmap(); // display error icon
            errorIconBox.Size = errorIconBox.Image.Size;

            detailsBox.Text += string.Format("Exception type: {0}" + Environment.NewLine, ex.GetType().FullName);
            detailsBox.Text += string.Format("Message: {0}" + Environment.NewLine, ex.Message);
            detailsBox.Text += string.Format("Source: {0}" + Environment.NewLine, ex.Source);
            detailsBox.Text += string.Format("Inner exception type: {0}" + Environment.NewLine, ex.InnerException == null ? "(null)" : ex.InnerException.GetType().FullName);

            detailsBox.Text += "Stack trace:" + Environment.NewLine;
            detailsBox.Text += ex.StackTrace + Environment.NewLine;

            if (ex.Data != null) {
                foreach (DictionaryEntry pair in ex.Data) {
                    detailsBox.Text += string.Format("{0}: {1}" + Environment.NewLine, pair.Key == null ? "" : pair.Key.ToString(), pair.Value == null ? "" : pair.Value.ToString());
                }
            }

            if (specialInfo != null) {
                foreach (var pair in specialInfo) {
                    detailsBox.Text += string.Format("{0}: {1}" + Environment.NewLine, pair.Key, pair.Value);
                }
            }

            detailsBox.Hide();
        }

        /// <summary>
        /// Shows/hides the details box
        /// </summary>
        private void DetailsButton_Click(object sender, EventArgs e) {
            var rowStyle = tableLayoutPanel1.RowStyles[2];
            detailsDisplayed = !detailsDisplayed;

            SuspendLayout();
            if (detailsDisplayed) { // display details box
                rowStyle.Height = 200;                
                detailsButton.Text = "Hide details";
                tableLayoutPanel1.Controls.Add(detailsBox, 0, 2);
                tableLayoutPanel1.SetColumnSpan(detailsBox, 3);
                detailsBox.Show();
                if (this.Height < 400) this.Height = 400;
            } else { // hide details box
                rowStyle.Height = 0;                
                detailsButton.Text = "Show details";
                tableLayoutPanel1.Controls.Remove(detailsBox);
                this.Height = this.MinimumSize.Height;
            }
            ResumeLayout();
        }

        /// <summary>
        /// Plays the error sound
        /// </summary>        
        private void ErrorDialog_Load(object sender, EventArgs e) {
            SystemSounds.Hand.Play();
        }
    }
}
