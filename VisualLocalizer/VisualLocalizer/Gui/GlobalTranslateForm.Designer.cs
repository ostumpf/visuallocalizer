using VisualLocalizer.Library;
namespace VisualLocalizer.Gui {
    partial class GlobalTranslateForm {
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
            this.mainTable = new System.Windows.Forms.TableLayoutPanel();
            this.resxListBox = new VisualLocalizer.Library.DisableableCheckedListBox();
            this.languageInfoPanel = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.savedLanguagePairPanel = new System.Windows.Forms.TableLayoutPanel();
            this.label4 = new System.Windows.Forms.Label();
            this.languagePairsBox = new System.Windows.Forms.ComboBox();
            this.useSavedPairBox = new System.Windows.Forms.RadioButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.newLanguagePairPanel = new System.Windows.Forms.TableLayoutPanel();
            this.souceLanguageBox = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.targetLanguageBox = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.useNewPairBox = new System.Windows.Forms.RadioButton();
            this.addLanguagePairBox = new System.Windows.Forms.CheckBox();
            this.providerBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.translateButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.mainTable.SuspendLayout();
            this.languageInfoPanel.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.savedLanguagePairPanel.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.newLanguagePairPanel.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainTable
            // 
            this.mainTable.ColumnCount = 1;
            this.mainTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTable.Controls.Add(this.resxListBox, 0, 1);
            this.mainTable.Controls.Add(this.languageInfoPanel, 0, 0);
            this.mainTable.Controls.Add(this.flowLayoutPanel1, 0, 2);
            this.mainTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTable.Location = new System.Drawing.Point(0, 0);
            this.mainTable.Name = "mainTable";
            this.mainTable.RowCount = 3;
            this.mainTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.mainTable.Size = new System.Drawing.Size(287, 341);
            this.mainTable.TabIndex = 0;
            // 
            // resxListBox
            // 
            this.resxListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resxListBox.FormattingEnabled = true;
            this.resxListBox.Location = new System.Drawing.Point(3, 194);
            this.resxListBox.Name = "resxListBox";
            this.resxListBox.ScrollAlwaysVisible = true;
            this.resxListBox.Size = new System.Drawing.Size(281, 109);
            this.resxListBox.TabIndex = 0;
            this.resxListBox.SelectedValueChanged += new System.EventHandler(this.ResxListBox_SelectedValueChanged);
            // 
            // languageInfoPanel
            // 
            this.languageInfoPanel.AutoSize = true;
            this.languageInfoPanel.ColumnCount = 2;
            this.languageInfoPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.languageInfoPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.languageInfoPanel.Controls.Add(this.groupBox1, 1, 1);
            this.languageInfoPanel.Controls.Add(this.groupBox2, 1, 2);
            this.languageInfoPanel.Controls.Add(this.providerBox, 1, 0);
            this.languageInfoPanel.Controls.Add(this.label1, 0, 0);
            this.languageInfoPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.languageInfoPanel.Location = new System.Drawing.Point(3, 3);
            this.languageInfoPanel.Name = "languageInfoPanel";
            this.languageInfoPanel.RowCount = 4;
            this.languageInfoPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.languageInfoPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.languageInfoPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.languageInfoPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.languageInfoPanel.Size = new System.Drawing.Size(281, 185);
            this.languageInfoPanel.TabIndex = 1;
            // 
            // groupBox1
            // 
            this.groupBox1.AutoSize = true;
            this.languageInfoPanel.SetColumnSpan(this.groupBox1, 2);
            this.groupBox1.Controls.Add(this.savedLanguagePairPanel);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(3, 30);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(275, 46);
            this.groupBox1.TabIndex = 9;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Use saved language pair";
            // 
            // savedLanguagePairPanel
            // 
            this.savedLanguagePairPanel.AutoSize = true;
            this.savedLanguagePairPanel.ColumnCount = 3;
            this.savedLanguagePairPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.savedLanguagePairPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.savedLanguagePairPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.savedLanguagePairPanel.Controls.Add(this.label4, 1, 0);
            this.savedLanguagePairPanel.Controls.Add(this.languagePairsBox, 2, 0);
            this.savedLanguagePairPanel.Controls.Add(this.useSavedPairBox, 0, 0);
            this.savedLanguagePairPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.savedLanguagePairPanel.Location = new System.Drawing.Point(3, 16);
            this.savedLanguagePairPanel.Name = "savedLanguagePairPanel";
            this.savedLanguagePairPanel.RowCount = 1;
            this.savedLanguagePairPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.savedLanguagePairPanel.Size = new System.Drawing.Size(269, 27);
            this.savedLanguagePairPanel.TabIndex = 0;
            // 
            // label4
            // 
            this.label4.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.label4.Location = new System.Drawing.Point(23, 7);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(115, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Saved language pair:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // languagePairsBox
            // 
            this.languagePairsBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.languagePairsBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.languagePairsBox.FormattingEnabled = true;
            this.languagePairsBox.Location = new System.Drawing.Point(144, 3);
            this.languagePairsBox.Name = "languagePairsBox";
            this.languagePairsBox.Size = new System.Drawing.Size(122, 21);
            this.languagePairsBox.TabIndex = 10;
            this.languagePairsBox.SelectedIndexChanged += new System.EventHandler(this.LanguagePairsBox_SelectedIndexChanged);
            // 
            // useSavedPairBox
            // 
            this.useSavedPairBox.AutoSize = true;
            this.useSavedPairBox.Location = new System.Drawing.Point(3, 3);
            this.useSavedPairBox.Name = "useSavedPairBox";
            this.useSavedPairBox.Size = new System.Drawing.Size(14, 13);
            this.useSavedPairBox.TabIndex = 11;
            this.useSavedPairBox.TabStop = true;
            this.useSavedPairBox.UseVisualStyleBackColor = true;
            this.useSavedPairBox.CheckedChanged += new System.EventHandler(this.UseSavedPairBox_CheckedChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.AutoSize = true;
            this.languageInfoPanel.SetColumnSpan(this.groupBox2, 2);
            this.groupBox2.Controls.Add(this.newLanguagePairPanel);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(3, 82);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(275, 100);
            this.groupBox2.TabIndex = 10;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Create new language pair";
            // 
            // newLanguagePairPanel
            // 
            this.newLanguagePairPanel.AutoSize = true;
            this.newLanguagePairPanel.ColumnCount = 3;
            this.newLanguagePairPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.newLanguagePairPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.newLanguagePairPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.newLanguagePairPanel.Controls.Add(this.souceLanguageBox, 2, 0);
            this.newLanguagePairPanel.Controls.Add(this.label2, 1, 0);
            this.newLanguagePairPanel.Controls.Add(this.targetLanguageBox, 2, 1);
            this.newLanguagePairPanel.Controls.Add(this.label3, 1, 1);
            this.newLanguagePairPanel.Controls.Add(this.useNewPairBox, 0, 0);
            this.newLanguagePairPanel.Controls.Add(this.addLanguagePairBox, 1, 2);
            this.newLanguagePairPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.newLanguagePairPanel.Location = new System.Drawing.Point(3, 16);
            this.newLanguagePairPanel.Name = "newLanguagePairPanel";
            this.newLanguagePairPanel.RowCount = 3;
            this.newLanguagePairPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.newLanguagePairPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.newLanguagePairPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.newLanguagePairPanel.Size = new System.Drawing.Size(269, 81);
            this.newLanguagePairPanel.TabIndex = 0;
            // 
            // souceLanguageBox
            // 
            this.souceLanguageBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.souceLanguageBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.souceLanguageBox.FormattingEnabled = true;
            this.souceLanguageBox.Location = new System.Drawing.Point(144, 3);
            this.souceLanguageBox.Name = "souceLanguageBox";
            this.souceLanguageBox.Size = new System.Drawing.Size(122, 21);
            this.souceLanguageBox.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.label2.Location = new System.Drawing.Point(23, 7);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(115, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Source language:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // targetLanguageBox
            // 
            this.targetLanguageBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.targetLanguageBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.targetLanguageBox.FormattingEnabled = true;
            this.targetLanguageBox.Location = new System.Drawing.Point(144, 30);
            this.targetLanguageBox.Name = "targetLanguageBox";
            this.targetLanguageBox.Size = new System.Drawing.Size(122, 21);
            this.targetLanguageBox.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.label3.Location = new System.Drawing.Point(23, 34);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(115, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Target language:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // useNewPairBox
            // 
            this.useNewPairBox.AutoSize = true;
            this.useNewPairBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.useNewPairBox.Location = new System.Drawing.Point(3, 3);
            this.useNewPairBox.Name = "useNewPairBox";
            this.newLanguagePairPanel.SetRowSpan(this.useNewPairBox, 3);
            this.useNewPairBox.Size = new System.Drawing.Size(14, 75);
            this.useNewPairBox.TabIndex = 6;
            this.useNewPairBox.TabStop = true;
            this.useNewPairBox.UseVisualStyleBackColor = true;
            this.useNewPairBox.CheckedChanged += new System.EventHandler(this.UseNewPairBox_CheckedChanged);
            // 
            // addLanguagePairBox
            // 
            this.addLanguagePairBox.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.addLanguagePairBox.AutoSize = true;
            this.addLanguagePairBox.Checked = true;
            this.addLanguagePairBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.newLanguagePairPanel.SetColumnSpan(this.addLanguagePairBox, 2);
            this.addLanguagePairBox.Location = new System.Drawing.Point(56, 57);
            this.addLanguagePairBox.Name = "addLanguagePairBox";
            this.addLanguagePairBox.Size = new System.Drawing.Size(176, 17);
            this.addLanguagePairBox.TabIndex = 7;
            this.addLanguagePairBox.Text = "Add this language pair to the list";
            this.addLanguagePairBox.UseVisualStyleBackColor = true;
            // 
            // providerBox
            // 
            this.providerBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.providerBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.providerBox.FormattingEnabled = true;
            this.providerBox.Location = new System.Drawing.Point(149, 3);
            this.providerBox.Margin = new System.Windows.Forms.Padding(3, 3, 8, 3);
            this.providerBox.Name = "providerBox";
            this.providerBox.Size = new System.Drawing.Size(124, 21);
            this.providerBox.TabIndex = 3;
            this.providerBox.SelectedIndexChanged += new System.EventHandler(this.ProviderBox_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(140, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Translation service provider:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.Controls.Add(this.translateButton);
            this.flowLayoutPanel1.Controls.Add(this.cancelButton);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(122, 309);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(162, 29);
            this.flowLayoutPanel1.TabIndex = 2;
            // 
            // translateButton
            // 
            this.translateButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.translateButton.Location = new System.Drawing.Point(3, 3);
            this.translateButton.Name = "translateButton";
            this.translateButton.Size = new System.Drawing.Size(75, 23);
            this.translateButton.TabIndex = 0;
            this.translateButton.Text = "Translate";
            this.translateButton.UseVisualStyleBackColor = true;
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
            // GlobalTranslateForm
            // 
            this.AcceptButton = this.translateButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(287, 341);
            this.Controls.Add(this.mainTable);
            this.MinimizeBox = false;
            this.Name = "GlobalTranslateForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Global Translate";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.GlobalTranslateForm_FormClosing);
            this.mainTable.ResumeLayout(false);
            this.mainTable.PerformLayout();
            this.languageInfoPanel.ResumeLayout(false);
            this.languageInfoPanel.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.savedLanguagePairPanel.ResumeLayout(false);
            this.savedLanguagePairPanel.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.newLanguagePairPanel.ResumeLayout(false);
            this.newLanguagePairPanel.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainTable;
        private DisableableCheckedListBox resxListBox;
        private System.Windows.Forms.TableLayoutPanel languageInfoPanel;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button translateButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TableLayoutPanel savedLanguagePairPanel;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox languagePairsBox;
        private System.Windows.Forms.RadioButton useSavedPairBox;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TableLayoutPanel newLanguagePairPanel;
        private System.Windows.Forms.ComboBox souceLanguageBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox targetLanguageBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RadioButton useNewPairBox;
        private System.Windows.Forms.CheckBox addLanguagePairBox;
        private System.Windows.Forms.ComboBox providerBox;
        private System.Windows.Forms.Label label1;
    }
}