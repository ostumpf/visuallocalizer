namespace VisualLocalizer.Gui {
    partial class SelectResourceFileForm {
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.keyLabel = new System.Windows.Forms.Label();
            this.keyBox = new System.Windows.Forms.ComboBox();
            this.errorLabel = new System.Windows.Forms.Label();
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.inlineButton = new System.Windows.Forms.Button();
            this.overwriteButton = new System.Windows.Forms.Button();
            this.fileLabel = new System.Windows.Forms.Label();
            this.comboBox = new System.Windows.Forms.ComboBox();
            this.valueLabel = new System.Windows.Forms.Label();
            this.valueBox = new System.Windows.Forms.TextBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.fullBox = new System.Windows.Forms.RadioButton();
            this.usingBox = new System.Windows.Forms.RadioButton();
            this.referenceLabel = new System.Windows.Forms.Label();
            this.existingValueLabel = new System.Windows.Forms.Label();
            this.existingLabel = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel3.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 87F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 46.12737F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 53.87263F));
            this.tableLayoutPanel1.Controls.Add(this.keyLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.keyBox, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.errorLabel, 3, 3);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel3, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.fileLabel, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.comboBox, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.valueLabel, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.valueBox, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 2, 4);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel2, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.referenceLabel, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.existingValueLabel, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.existingLabel, 0, 3);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(647, 167);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // keyLabel
            // 
            this.keyLabel.AutoSize = true;
            this.keyLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.keyLabel.Location = new System.Drawing.Point(15, 0);
            this.keyLabel.Margin = new System.Windows.Forms.Padding(15, 0, 3, 0);
            this.keyLabel.Name = "keyLabel";
            this.keyLabel.Size = new System.Drawing.Size(69, 29);
            this.keyLabel.TabIndex = 0;
            this.keyLabel.Text = "Key:";
            this.keyLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // keyBox
            // 
            this.keyBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.keyBox.FormattingEnabled = true;
            this.keyBox.Location = new System.Drawing.Point(90, 3);
            this.keyBox.Name = "keyBox";
            this.keyBox.Size = new System.Drawing.Size(211, 21);
            this.keyBox.TabIndex = 0;
            this.keyBox.TextChanged += new System.EventHandler(this.keyBox_TextChanged);
            // 
            // errorLabel
            // 
            this.errorLabel.AutoEllipsis = true;
            this.errorLabel.AutoSize = true;
            this.errorLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.errorLabel.ForeColor = System.Drawing.Color.Red;
            this.errorLabel.Location = new System.Drawing.Point(402, 109);
            this.errorLabel.Margin = new System.Windows.Forms.Padding(10, 4, 10, 4);
            this.errorLabel.Name = "errorLabel";
            this.errorLabel.Size = new System.Drawing.Size(235, 22);
            this.errorLabel.TabIndex = 1015;
            this.errorLabel.Text = "error";
            this.errorLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // flowLayoutPanel3
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.flowLayoutPanel3, 2);
            this.flowLayoutPanel3.Controls.Add(this.inlineButton);
            this.flowLayoutPanel3.Controls.Add(this.overwriteButton);
            this.flowLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel3.Location = new System.Drawing.Point(3, 138);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.flowLayoutPanel3.Size = new System.Drawing.Size(298, 26);
            this.flowLayoutPanel3.TabIndex = 1012;
            // 
            // inlineButton
            // 
            this.inlineButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.inlineButton.Dock = System.Windows.Forms.DockStyle.Top;
            this.inlineButton.Location = new System.Drawing.Point(3, 3);
            this.inlineButton.Name = "inlineButton";
            this.inlineButton.Size = new System.Drawing.Size(140, 23);
            this.inlineButton.TabIndex = 8;
            this.inlineButton.Text = "Reference current value";
            this.inlineButton.UseVisualStyleBackColor = true;
            this.inlineButton.Click += new System.EventHandler(this.inlineButton_Click);
            // 
            // overwriteButton
            // 
            this.overwriteButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.overwriteButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.overwriteButton.Location = new System.Drawing.Point(149, 3);
            this.overwriteButton.Name = "overwriteButton";
            this.overwriteButton.Size = new System.Drawing.Size(133, 23);
            this.overwriteButton.TabIndex = 7;
            this.overwriteButton.Text = "Ovewrite current value";
            this.overwriteButton.UseVisualStyleBackColor = true;
            this.overwriteButton.Click += new System.EventHandler(this.overwriteButton_Click);
            // 
            // fileLabel
            // 
            this.fileLabel.AutoSize = true;
            this.fileLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fileLabel.Location = new System.Drawing.Point(314, 0);
            this.fileLabel.Margin = new System.Windows.Forms.Padding(10, 0, 3, 0);
            this.fileLabel.Name = "fileLabel";
            this.fileLabel.Size = new System.Drawing.Size(75, 29);
            this.fileLabel.TabIndex = 1017;
            this.fileLabel.Text = "Resource File:";
            this.fileLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // comboBox
            // 
            this.comboBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox.FormattingEnabled = true;
            this.comboBox.Location = new System.Drawing.Point(396, 4);
            this.comboBox.Margin = new System.Windows.Forms.Padding(4);
            this.comboBox.Name = "comboBox";
            this.comboBox.Size = new System.Drawing.Size(247, 21);
            this.comboBox.TabIndex = 1;
            this.comboBox.SelectedIndexChanged += new System.EventHandler(this.comboBox_SelectedIndexChanged);
            // 
            // valueLabel
            // 
            this.valueLabel.AutoSize = true;
            this.valueLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.valueLabel.Location = new System.Drawing.Point(3, 29);
            this.valueLabel.Name = "valueLabel";
            this.valueLabel.Size = new System.Drawing.Size(81, 40);
            this.valueLabel.TabIndex = 1018;
            this.valueLabel.Text = "Value:";
            this.valueLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // valueBox
            // 
            this.valueBox.AcceptsReturn = true;
            this.tableLayoutPanel1.SetColumnSpan(this.valueBox, 3);
            this.valueBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.valueBox.Location = new System.Drawing.Point(91, 33);
            this.valueBox.Margin = new System.Windows.Forms.Padding(4);
            this.valueBox.Multiline = true;
            this.valueBox.Name = "valueBox";
            this.valueBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.valueBox.Size = new System.Drawing.Size(552, 32);
            this.valueBox.TabIndex = 2;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.flowLayoutPanel1, 2);
            this.flowLayoutPanel1.Controls.Add(this.cancelButton);
            this.flowLayoutPanel1.Controls.Add(this.okButton);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(476, 138);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(168, 26);
            this.flowLayoutPanel1.TabIndex = 1011;
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Dock = System.Windows.Forms.DockStyle.Top;
            this.cancelButton.Location = new System.Drawing.Point(3, 3);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 6;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(84, 3);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 5;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // flowLayoutPanel2
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.flowLayoutPanel2, 2);
            this.flowLayoutPanel2.Controls.Add(this.fullBox);
            this.flowLayoutPanel2.Controls.Add(this.usingBox);
            this.flowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(3, 72);
            this.flowLayoutPanel2.MinimumSize = new System.Drawing.Size(300, 25);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(300, 30);
            this.flowLayoutPanel2.TabIndex = 1010;
            this.flowLayoutPanel2.TabStop = true;
            this.flowLayoutPanel2.WrapContents = false;
            // 
            // fullBox
            // 
            this.fullBox.AutoSize = true;
            this.fullBox.Location = new System.Drawing.Point(15, 3);
            this.fullBox.Margin = new System.Windows.Forms.Padding(15, 3, 3, 3);
            this.fullBox.Name = "fullBox";
            this.fullBox.Size = new System.Drawing.Size(116, 17);
            this.fullBox.TabIndex = 4;
            this.fullBox.TabStop = true;
            this.fullBox.Text = "Use full class name";
            this.fullBox.UseVisualStyleBackColor = true;
            // 
            // usingBox
            // 
            this.usingBox.AutoSize = true;
            this.usingBox.Checked = true;
            this.usingBox.Location = new System.Drawing.Point(137, 3);
            this.usingBox.Name = "usingBox";
            this.usingBox.Size = new System.Drawing.Size(160, 17);
            this.usingBox.TabIndex = 3;
            this.usingBox.TabStop = true;
            this.usingBox.Text = "Add using block if necessary";
            this.usingBox.UseVisualStyleBackColor = true;
            this.usingBox.CheckedChanged += new System.EventHandler(this.usingBox_CheckedChanged);
            // 
            // referenceLabel
            // 
            this.referenceLabel.AutoEllipsis = true;
            this.tableLayoutPanel1.SetColumnSpan(this.referenceLabel, 2);
            this.referenceLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.referenceLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.referenceLabel.Location = new System.Drawing.Point(307, 69);
            this.referenceLabel.Name = "referenceLabel";
            this.referenceLabel.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.referenceLabel.Size = new System.Drawing.Size(337, 36);
            this.referenceLabel.TabIndex = 1021;
            this.referenceLabel.Text = "reference";
            this.referenceLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // existingValueLabel
            // 
            this.existingValueLabel.AutoEllipsis = true;
            this.tableLayoutPanel1.SetColumnSpan(this.existingValueLabel, 2);
            this.existingValueLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.existingValueLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.existingValueLabel.Location = new System.Drawing.Point(90, 105);
            this.existingValueLabel.MaximumSize = new System.Drawing.Size(250, 60);
            this.existingValueLabel.Name = "existingValueLabel";
            this.existingValueLabel.Size = new System.Drawing.Size(250, 30);
            this.existingValueLabel.TabIndex = 1022;
            this.existingValueLabel.Text = "existingValue";
            // 
            // existingLabel
            // 
            this.existingLabel.AutoSize = true;
            this.existingLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.existingLabel.Location = new System.Drawing.Point(3, 105);
            this.existingLabel.Name = "existingLabel";
            this.existingLabel.Size = new System.Drawing.Size(81, 30);
            this.existingLabel.TabIndex = 1023;
            this.existingLabel.Text = "Current Value:";
            this.existingLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // SelectResourceFileForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(647, 167);
            this.Controls.Add(this.tableLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.KeyPreview = true;
            this.Name = "SelectResourceFileForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select destination file";
            this.Load += new System.EventHandler(this.SelectResourceFileForm_Load);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.SelectResourceFileForm_KeyUp);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SelectResourceFileForm_FormClosing);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SelectResourceFileForm_KeyDown);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel3.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label fileLabel;
        private System.Windows.Forms.Label keyLabel;
        private System.Windows.Forms.Label valueLabel;
        private System.Windows.Forms.TextBox valueBox;
        private System.Windows.Forms.ComboBox comboBox;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label errorLabel;
        private System.Windows.Forms.RadioButton fullBox;
        private System.Windows.Forms.RadioButton usingBox;
        private System.Windows.Forms.ComboBox keyBox;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
        private System.Windows.Forms.Button inlineButton;
        private System.Windows.Forms.Button overwriteButton;
        private System.Windows.Forms.Label referenceLabel;
        private System.Windows.Forms.Label existingValueLabel;
        private System.Windows.Forms.Label existingLabel;
    }
}