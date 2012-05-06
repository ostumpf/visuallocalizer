namespace VisualLocalizer.Editor {
    partial class ResXEditorControl {
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.tabPanel = new VisualLocalizer.Components.TabPanel();
            this.SuspendLayout();
            // 
            // tabPanel
            // 
            this.tabPanel.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.tabPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabPanel.Location = new System.Drawing.Point(0, 0);
            this.tabPanel.Name = "tabPanel";
            this.tabPanel.Size = new System.Drawing.Size(710, 535);
            this.tabPanel.TabIndex = 0;
            // 
            // ResXEditorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabPanel);
            this.Name = "ResXEditorControl";
            this.Size = new System.Drawing.Size(710, 535);
            this.ResumeLayout(false);

        }

        #endregion

        private VisualLocalizer.Components.TabPanel tabPanel;
    }
}
