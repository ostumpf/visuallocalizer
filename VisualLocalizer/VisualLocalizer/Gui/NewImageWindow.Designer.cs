namespace VisualLocalizer.Gui {
    partial class NewImageWindow {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewImageWindow));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.formatLabel = new System.Windows.Forms.Label();
            this.formatBox = new System.Windows.Forms.ComboBox();
            this.dimensionsLabel = new System.Windows.Forms.Label();
            this.dimensionsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.widthBox = new System.Windows.Forms.TextBox();
            this.crossLabel = new System.Windows.Forms.Label();
            this.heightBox = new System.Windows.Forms.TextBox();
            this.nameLabel = new System.Windows.Forms.Label();
            this.nameBox = new System.Windows.Forms.TextBox();
            this.buttonsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.dimensionsPanel.SuspendLayout();
            this.buttonsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.formatLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.formatBox, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.dimensionsLabel, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.dimensionsPanel, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.nameLabel, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.nameBox, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.buttonsPanel, 1, 3);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(332, 123);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // formatLabel
            // 
            this.formatLabel.AutoSize = true;
            this.formatLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.formatLabel.Location = new System.Drawing.Point(3, 0);
            this.formatLabel.Name = "formatLabel";
            this.formatLabel.Size = new System.Drawing.Size(64, 27);
            this.formatLabel.TabIndex = 0;
            this.formatLabel.Text = "Format:";
            this.formatLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // formatBox
            // 
            this.formatBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.formatBox.FormattingEnabled = true;
            this.formatBox.Location = new System.Drawing.Point(73, 3);
            this.formatBox.Name = "formatBox";
            this.formatBox.Size = new System.Drawing.Size(103, 21);
            this.formatBox.TabIndex = 1;
            this.formatBox.SelectedIndexChanged += new System.EventHandler(this.formatBox_SelectedIndexChanged);
            // 
            // dimensionsLabel
            // 
            this.dimensionsLabel.AutoSize = true;
            this.dimensionsLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dimensionsLabel.Location = new System.Drawing.Point(3, 27);
            this.dimensionsLabel.Name = "dimensionsLabel";
            this.dimensionsLabel.Size = new System.Drawing.Size(64, 32);
            this.dimensionsLabel.TabIndex = 2;
            this.dimensionsLabel.Text = "Dimensions:";
            this.dimensionsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // dimensionsPanel
            // 
            this.dimensionsPanel.AutoSize = true;
            this.dimensionsPanel.Controls.Add(this.widthBox);
            this.dimensionsPanel.Controls.Add(this.crossLabel);
            this.dimensionsPanel.Controls.Add(this.heightBox);
            this.dimensionsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dimensionsPanel.Location = new System.Drawing.Point(73, 30);
            this.dimensionsPanel.Name = "dimensionsPanel";
            this.dimensionsPanel.Size = new System.Drawing.Size(256, 26);
            this.dimensionsPanel.TabIndex = 3;
            // 
            // widthBox
            // 
            this.widthBox.Location = new System.Drawing.Point(3, 3);
            this.widthBox.Name = "widthBox";
            this.widthBox.Size = new System.Drawing.Size(70, 20);
            this.widthBox.TabIndex = 0;
            this.widthBox.Text = "800";
            this.widthBox.WordWrap = false;
            this.widthBox.TextChanged += new System.EventHandler(this.widthBox_TextChanged);
            // 
            // crossLabel
            // 
            this.crossLabel.AutoSize = true;
            this.crossLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.crossLabel.Location = new System.Drawing.Point(79, 0);
            this.crossLabel.Name = "crossLabel";
            this.crossLabel.Size = new System.Drawing.Size(12, 26);
            this.crossLabel.TabIndex = 1;
            this.crossLabel.Text = "x";
            this.crossLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // heightBox
            // 
            this.heightBox.Location = new System.Drawing.Point(97, 3);
            this.heightBox.Name = "heightBox";
            this.heightBox.Size = new System.Drawing.Size(70, 20);
            this.heightBox.TabIndex = 2;
            this.heightBox.Text = "600";
            this.heightBox.WordWrap = false;
            this.heightBox.TextChanged += new System.EventHandler(this.heightBox_TextChanged);
            // 
            // nameLabel
            // 
            this.nameLabel.AutoSize = true;
            this.nameLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nameLabel.Location = new System.Drawing.Point(3, 59);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Size = new System.Drawing.Size(64, 26);
            this.nameLabel.TabIndex = 4;
            this.nameLabel.Text = "Name:";
            this.nameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // nameBox
            // 
            this.nameBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.nameBox.Location = new System.Drawing.Point(73, 62);
            this.nameBox.Name = "nameBox";
            this.nameBox.Size = new System.Drawing.Size(256, 20);
            this.nameBox.TabIndex = 5;
            this.nameBox.Text = "(new image)";
            this.nameBox.TextChanged += new System.EventHandler(this.nameBox_TextChanged);
            // 
            // buttonsPanel
            // 
            this.buttonsPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonsPanel.AutoSize = true;
            this.buttonsPanel.Controls.Add(this.okButton);
            this.buttonsPanel.Controls.Add(this.cancelButton);
            this.buttonsPanel.Location = new System.Drawing.Point(167, 91);
            this.buttonsPanel.Name = "buttonsPanel";
            this.buttonsPanel.Size = new System.Drawing.Size(162, 29);
            this.buttonsPanel.TabIndex = 6;
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(3, 3);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(84, 3);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // NewImageWindow
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(332, 123);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NewImageWindow";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add New Image";
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.NewImageWindow_KeyUp);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.NewImageWindow_KeyDown);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.dimensionsPanel.ResumeLayout(false);
            this.dimensionsPanel.PerformLayout();
            this.buttonsPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label formatLabel;
        private System.Windows.Forms.ComboBox formatBox;
        private System.Windows.Forms.Label dimensionsLabel;
        private System.Windows.Forms.FlowLayoutPanel dimensionsPanel;
        private System.Windows.Forms.TextBox widthBox;
        private System.Windows.Forms.Label crossLabel;
        private System.Windows.Forms.TextBox heightBox;
        private System.Windows.Forms.Label nameLabel;
        private System.Windows.Forms.TextBox nameBox;
        private System.Windows.Forms.FlowLayoutPanel buttonsPanel;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}