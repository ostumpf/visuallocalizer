using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel.Design;
using System.Resources;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using VisualLocalizer.Library;
using System.Drawing.Drawing2D;
using VisualLocalizer.Editor.UndoUnits;

namespace VisualLocalizer.Editor {
    internal sealed class ResXEditorControl : TableLayoutPanel {

        private ResXStringGrid stringGrid;
        private ResXTabControl tabs;
        private ToolStrip toolStrip;
        public event EventHandler DataChanged;

        public ResXEditorControl() {
            this.Dock = DockStyle.Fill;
            this.DoubleBuffered = true;
            
            initTabControl();
            initToolStrip();

            this.RowCount = 2;
            this.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            this.ColumnCount = 1;
            this.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            this.Controls.Add(toolStrip, 0, 0);
            this.Controls.Add(tabs, 0, 1);
        }      
        
        public ResXEditor Editor {
            get;
            set;
        }

        private void initToolStrip() {
            ToolStripManager.Renderer = new ToolStripProfessionalRenderer(new VsColorTable());

            toolStrip = new ToolStrip();
            toolStrip.Dock = DockStyle.Top;

            ToolStripSplitButton addButton = new ToolStripSplitButton("&Add resources");
            addButton.DropDownItems.Add("String");
            addButton.DropDownItems.Add("Image");
            addButton.DropDownItems.Add("Icon");
            addButton.DropDownItems.Add("Sound");
            addButton.DropDownItems.Add("File");
            toolStrip.Items.Add(addButton);

            ToolStripDropDownButton mergeButton = new ToolStripDropDownButton("&Merge with ResX file");
            mergeButton.DropDownItems.Add("Merge && &preserve both");
            mergeButton.DropDownItems.Add("Merge && &delete source");
            toolStrip.Items.Add(mergeButton);

            toolStrip.Items.Add(new ToolStripSeparator());

            ToolStripButton removeButton = new ToolStripButton("&Remove resources");
            toolStrip.Items.Add(removeButton);

            ToolStripSplitButton inlineButton = new ToolStripSplitButton("&Inline resources");
            inlineButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            inlineButton.DropDownItems.Add("Inline && &remove");
            toolStrip.Items.Add(inlineButton);

            toolStrip.Items.Add(new ToolStripSeparator());

            ToolStripLabel codeGenerationLabel = new ToolStripLabel();
            codeGenerationLabel.Text = "Access Modifier:";
            toolStrip.Items.Add(codeGenerationLabel);

            ToolStripComboBox codeGenerationBox = new ToolStripComboBox();
            codeGenerationBox.DropDownStyle = ComboBoxStyle.DropDownList;
            codeGenerationBox.FlatStyle = FlatStyle.Standard;
            codeGenerationBox.ComboBox.Items.Add("Internal");
            codeGenerationBox.ComboBox.Items.Add("Public");
            codeGenerationBox.ComboBox.Items.Add("No designer class");
            codeGenerationBox.SelectedIndex = 0;
            codeGenerationBox.SelectedIndexChanged += new EventHandler(noFocusBoxSelectedIndexChanged);
            codeGenerationBox.Margin = new Padding(2);
            toolStrip.Items.Add(codeGenerationBox);
        }

        private void noFocusBoxSelectedIndexChanged(object sender, EventArgs e) {
            toolStrip.Focus();
        }

        private void initTabControl() {
            tabs = new ResXTabControl();
            tabs.Dock = DockStyle.Fill;
            tabs.ItemSize = new Size(25, 80);
            tabs.Alignment = TabAlignment.Left;
            tabs.SizeMode = TabSizeMode.Fixed;

            TabPage stringTab = new TabPage("Strings");
            stringTab.BorderStyle = BorderStyle.None;
            
            stringGrid = new ResXStringGrid();
            stringGrid.Dock = DockStyle.Fill;
            stringGrid.BackColor = Color.White;
            stringGrid.ScrollBars = ScrollBars.Vertical;
            stringGrid.DataChanged += new EventHandler((o, args) => { DataChanged(o, args); });
            stringGrid.BorderStyle = BorderStyle.None;
            stringGrid.StringKeyRenamed += new Action<CodeDataGridViewRow<ResXDataNode>, string>(stringGrid_StringKeyRenamed);
            stringGrid.StringValueChanged += new Action<CodeDataGridViewRow<ResXDataNode>, string, string>(stringGrid_StringValueChanged);
            stringGrid.StringCommentChanged += new Action<CodeDataGridViewRow<ResXDataNode>, string, string>(stringGrid_StringCommentChanged);
            stringTab.Controls.Add(stringGrid);

            tabs.TabPages.Add(stringTab);
            tabs.TabPages.Add(new TabPage("Images"));
            tabs.TabPages.Add(new TabPage("Icons"));
            tabs.TabPages.Add(new TabPage("Sounds"));
            tabs.TabPages.Add(new TabPage("Files"));
        }

        private void stringGrid_StringCommentChanged(CodeDataGridViewRow<ResXDataNode> row, string oldComment, string newComment) {
            string key = (int?)row.Tag == ResXStringGrid.NULL_KEY ? null : row.DataSourceItem.Name;
            StringChangeCommentUndoUnit unit = new StringChangeCommentUndoUnit(row, stringGrid, key, oldComment, newComment);
            Editor.AddUndoUnit(unit);
        }

        private void stringGrid_StringValueChanged(CodeDataGridViewRow<ResXDataNode> row, string oldValue, string newValue) {
            string key = (int?)row.Tag == ResXStringGrid.NULL_KEY ? null : row.DataSourceItem.Name;
            StringChangeValueUndoUnit unit = new StringChangeValueUndoUnit(row, stringGrid, key, oldValue, newValue, row.DataSourceItem.Comment);
            Editor.AddUndoUnit(unit);
        }

        private void stringGrid_StringKeyRenamed(CodeDataGridViewRow<ResXDataNode> row, string newKey) {
            string oldKey = (int?)row.Tag == ResXStringGrid.NULL_KEY ? null : row.DataSourceItem.Name;
            StringRenameKeyUndoUnit unit = new StringRenameKeyUndoUnit(row, stringGrid, oldKey, newKey); 
            Editor.AddUndoUnit(unit);
        }        

        public void SetData(Dictionary<string,ResXDataNode> data) {
            Dictionary<string, ResXDataNode> stringData = new Dictionary<string, ResXDataNode>();

            foreach (var pair in data)
                if (pair.Value.HasStringValue()) stringData.Add(pair.Key, pair.Value);
            
            stringGrid.SetData(stringData);
        }

        public Dictionary<string, ResXDataNode> GetData(bool throwExceptions) {
            Dictionary<string, ResXDataNode> data = new Dictionary<string, ResXDataNode>();

            foreach (var pair in stringGrid.GetData(throwExceptions))
                data.Add(pair.Key, pair.Value);

            return data;
        }

        public void SetReadOnly(bool readOnly) {
            toolStrip.Enabled = !readOnly;
            stringGrid.ReadOnly = readOnly;
        }

        private class VsColorTable : ProfessionalColorTable {

            private Color beginColor, middleColor, endColor;

            public VsColorTable() {
                IVsShell shell = (IVsShell)Package.GetGlobalService(typeof(SVsShell));
                object o;
                int hr = shell.GetProperty((int)__VSSPROPID2.VSSPROPID_SqmRegistryRoot, out o);
                Marshal.ThrowExceptionForHR(hr);
                
                string registry = o.ToString();
                if (registry.EndsWith("9.0\\SQM")) {
                    beginColor = ColorTranslator.FromHtml("#FAFAFD");
                    middleColor = ColorTranslator.FromHtml("#E9ECFA");
                    endColor = ColorTranslator.FromHtml("#C1C8D9");
                } else if (registry.EndsWith("10.0\\SQM")) {
                    beginColor = ColorTranslator.FromHtml("#BCC7D8");
                    middleColor = ColorTranslator.FromHtml("#BCC7D8");
                    endColor = ColorTranslator.FromHtml("#BCC7D8");
                } else if (registry.EndsWith("11.0\\SQM")) {
                    beginColor = ColorTranslator.FromHtml("#D0D2D3");
                    middleColor = ColorTranslator.FromHtml("#D0D2D3");
                    endColor = ColorTranslator.FromHtml("#D0D2D3");
                } else {
                    beginColor = ToolStripGradientBegin;
                    middleColor = ToolStripGradientMiddle;
                    endColor = ToolStripGradientEnd;
                }
            }

            public override Color ToolStripGradientBegin { get { return beginColor; } }
            public override Color ToolStripGradientMiddle { get { return middleColor; } }
            public override Color ToolStripGradientEnd { get { return endColor; } }
        }
    }

}
    
