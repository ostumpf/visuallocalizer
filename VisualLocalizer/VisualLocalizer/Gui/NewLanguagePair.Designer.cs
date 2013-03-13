namespace VisualLocalizer.Gui {
    partial class NewLanguagePairWindow {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.tablePanel = new System.Windows.Forms.TableLayoutPanel();
            this.sourceLabel = new System.Windows.Forms.Label();
            this.targetLabel = new System.Windows.Forms.Label();
            this.rememberLabel = new System.Windows.Forms.Label();
            this.translateButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.sourceBox = new System.Windows.Forms.ComboBox();
            this.targetBox = new System.Windows.Forms.ComboBox();
            this.addToListBox = new System.Windows.Forms.CheckBox();
            this.tablePanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // tablePanel
            // 
            this.tablePanel.ColumnCount = 4;
            this.tablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tablePanel.Controls.Add(this.sourceLabel, 0, 0);
            this.tablePanel.Controls.Add(this.targetLabel, 0, 1);
            this.tablePanel.Controls.Add(this.rememberLabel, 0, 2);
            this.tablePanel.Controls.Add(this.translateButton, 2, 3);
            this.tablePanel.Controls.Add(this.cancelButton, 3, 3);
            this.tablePanel.Controls.Add(this.sourceBox, 1, 0);
            this.tablePanel.Controls.Add(this.targetBox, 1, 1);
            this.tablePanel.Controls.Add(this.addToListBox, 1, 2);
            this.tablePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tablePanel.Location = new System.Drawing.Point(0, 0);
            this.tablePanel.Name = "tablePanel";
            this.tablePanel.RowCount = 4;
            this.tablePanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tablePanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tablePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tablePanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tablePanel.Size = new System.Drawing.Size(355, 122);
            this.tablePanel.TabIndex = 0;
            // 
            // sourceLabel
            // 
            this.sourceLabel.AutoSize = true;
            this.sourceLabel.Dock = System.Windows.Forms.DockStyle.Right;
            this.sourceLabel.Location = new System.Drawing.Point(18, 3);
            this.sourceLabel.Margin = new System.Windows.Forms.Padding(3);
            this.sourceLabel.Name = "sourceLabel";
            this.sourceLabel.Padding = new System.Windows.Forms.Padding(3);
            this.sourceLabel.Size = new System.Drawing.Size(97, 27);
            this.sourceLabel.TabIndex = 2;
            this.sourceLabel.Text = "Source language:";
            this.sourceLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // targetLabel
            // 
            this.targetLabel.AutoSize = true;
            this.targetLabel.Dock = System.Windows.Forms.DockStyle.Right;
            this.targetLabel.Location = new System.Drawing.Point(21, 36);
            this.targetLabel.Margin = new System.Windows.Forms.Padding(3);
            this.targetLabel.Name = "targetLabel";
            this.targetLabel.Padding = new System.Windows.Forms.Padding(3);
            this.targetLabel.Size = new System.Drawing.Size(94, 27);
            this.targetLabel.TabIndex = 3;
            this.targetLabel.Text = "Target language:";
            this.targetLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // rememberLabel
            // 
            this.rememberLabel.AutoSize = true;
            this.rememberLabel.Dock = System.Windows.Forms.DockStyle.Right;
            this.rememberLabel.Location = new System.Drawing.Point(3, 69);
            this.rememberLabel.Margin = new System.Windows.Forms.Padding(3);
            this.rememberLabel.Name = "rememberLabel";
            this.rememberLabel.Padding = new System.Windows.Forms.Padding(3);
            this.rememberLabel.Size = new System.Drawing.Size(112, 21);
            this.rememberLabel.TabIndex = 4;
            this.rememberLabel.Text = "Add to dropdown list:";
            this.rememberLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // translateButton
            // 
            this.translateButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.translateButton.Location = new System.Drawing.Point(196, 96);
            this.translateButton.Name = "translateButton";
            this.translateButton.Size = new System.Drawing.Size(75, 23);
            this.translateButton.TabIndex = 0;
            this.translateButton.Text = "Translate";
            this.translateButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(277, 96);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // sourceBox
            // 
            this.tablePanel.SetColumnSpan(this.sourceBox, 2);
            this.sourceBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sourceBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.sourceBox.FormattingEnabled = true;
            this.sourceBox.Location = new System.Drawing.Point(124, 6);
            this.sourceBox.Margin = new System.Windows.Forms.Padding(6);
            this.sourceBox.Name = "sourceBox";
            this.sourceBox.Size = new System.Drawing.Size(144, 21);
            this.sourceBox.TabIndex = 5;
            // 
            // targetBox
            // 
            this.tablePanel.SetColumnSpan(this.targetBox, 2);
            this.targetBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.targetBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.targetBox.FormattingEnabled = true;
            this.targetBox.Location = new System.Drawing.Point(124, 39);
            this.targetBox.Margin = new System.Windows.Forms.Padding(6);
            this.targetBox.Name = "targetBox";
            this.targetBox.Size = new System.Drawing.Size(144, 21);
            this.targetBox.TabIndex = 6;
            // 
            // addToListBox
            // 
            this.addToListBox.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.addToListBox.AutoSize = true;
            this.addToListBox.Checked = true;
            this.addToListBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.addToListBox.Location = new System.Drawing.Point(148, 72);
            this.addToListBox.Name = "addToListBox";
            this.addToListBox.Size = new System.Drawing.Size(15, 14);
            this.addToListBox.TabIndex = 7;
            this.addToListBox.UseVisualStyleBackColor = true;
            // 
            // NewLanguagePairWindow
            // 
            this.AcceptButton = this.translateButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(355, 122);
            this.Controls.Add(this.tablePanel);
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NewLanguagePairWindow";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "New Language Pair";
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.NewLanguagePairWindow_KeyUp);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.NewLanguagePairWindow_FormClosing);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.NewLanguagePairWindow_KeyDown);
            this.tablePanel.ResumeLayout(false);
            this.tablePanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tablePanel;
        private System.Windows.Forms.Label sourceLabel;
        private System.Windows.Forms.Label targetLabel;
        private System.Windows.Forms.Label rememberLabel;
        private System.Windows.Forms.Button translateButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.ComboBox sourceBox;
        private System.Windows.Forms.ComboBox targetBox;
        private System.Windows.Forms.CheckBox addToListBox;
    }
}