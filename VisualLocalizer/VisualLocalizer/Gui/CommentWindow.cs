using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace VisualLocalizer.Gui {
    public partial class CommentWindow : Form {
        public CommentWindow(string oldComment) {
            InitializeComponent();
            this.Icon = VSPackage._400;

            commentBox.Text = oldComment;
        }

        public string Comment { get; private set; }

        private void CommentWindow_FormClosing(object sender, FormClosingEventArgs e) {
            Comment = commentBox.Text;
        }

        private bool ctrlDown = false;
        private void CommentWindow_KeyDown(object sender, KeyEventArgs e) {
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


        private void CommentWindow_KeyUp(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.ControlKey) ctrlDown = false;
        }
    }
}
