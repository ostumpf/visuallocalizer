namespace VisualLocalizer.Gui {
    partial class TypeSelectorForm {
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.assemblyBox = new System.Windows.Forms.ComboBox();
            this.typeBox = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.typeNameLabel = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.assemblyBox, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.typeBox, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.typeNameLabel, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 1, 4);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(498, 157);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(61, 10);
            this.label1.Margin = new System.Windows.Forms.Padding(6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(54, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Assembly:";
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(81, 37);
            this.label2.Margin = new System.Windows.Forms.Padding(6);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Type:";
            // 
            // assemblyBox
            // 
            this.assemblyBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.assemblyBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.assemblyBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.assemblyBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.assemblyBox.DropDownWidth = 500;
            this.assemblyBox.FormattingEnabled = true;
            this.assemblyBox.Location = new System.Drawing.Point(124, 6);
            this.assemblyBox.Margin = new System.Windows.Forms.Padding(3, 3, 9, 3);
            this.assemblyBox.Name = "assemblyBox";
            this.assemblyBox.Size = new System.Drawing.Size(365, 21);
            this.assemblyBox.TabIndex = 3;
            this.assemblyBox.SelectedIndexChanged += new System.EventHandler(this.assemblyBox_SelectedIndexChanged);
            // 
            // typeBox
            // 
            this.typeBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.typeBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.typeBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.typeBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.typeBox.DropDownWidth = 500;
            this.typeBox.FormattingEnabled = true;
            this.typeBox.Location = new System.Drawing.Point(124, 33);
            this.typeBox.Margin = new System.Windows.Forms.Padding(3, 3, 9, 3);
            this.typeBox.Name = "typeBox";
            this.typeBox.Size = new System.Drawing.Size(365, 21);
            this.typeBox.TabIndex = 4;
            this.typeBox.SelectedIndexChanged += new System.EventHandler(this.typeBox_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 63);
            this.label3.Margin = new System.Windows.Forms.Padding(6);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(109, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Qualified Type Name:";
            // 
            // typeNameLabel
            // 
            this.typeNameLabel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.typeNameLabel.AutoEllipsis = true;
            this.typeNameLabel.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.typeNameLabel, 2);
            this.typeNameLabel.Location = new System.Drawing.Point(220, 88);
            this.typeNameLabel.Margin = new System.Windows.Forms.Padding(6);
            this.typeNameLabel.Name = "typeNameLabel";
            this.typeNameLabel.Size = new System.Drawing.Size(57, 13);
            this.typeNameLabel.TabIndex = 6;
            this.typeNameLabel.Text = "(unknown)";
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.Controls.Add(this.okButton);
            this.flowLayoutPanel1.Controls.Add(this.cancelButton);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(333, 125);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(162, 29);
            this.flowLayoutPanel1.TabIndex = 0;
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
            // TypeSelectorForm
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(498, 157);
            this.Controls.Add(this.tableLayoutPanel1);
            this.MinimizeBox = false;
            this.Name = "TypeSelectorForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select Type";
            this.Load += new System.EventHandler(this.TypeSelectorForm_Load);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TypeWindow_KeyUp);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TypeWindow_KeyDown);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox assemblyBox;
        private System.Windows.Forms.ComboBox typeBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label typeNameLabel;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}