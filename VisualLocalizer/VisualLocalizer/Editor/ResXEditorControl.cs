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
using System.IO;

namespace VisualLocalizer.Editor {

    [Flags]
    internal enum REMOVEKIND {REMOVE=1,EXCLUDE=2,DELETE_FILE=4}

    internal sealed class ResXEditorControl : TableLayoutPanel {

        private ResXStringGrid stringGrid;
        private ResXTabControl tabs;
        private ToolStrip toolStrip;
        private ToolStripMenuItem removeExcludeItem, removeDeleteItem;
        private ToolStripSplitButton removeButton, inlineButton;
        private ToolStripDropDownButton viewButton;
        private ResXImagesList imagesListView;
        private TabPage stringTab, imagesTab;

        public event EventHandler DataChanged;
        public event Action<REMOVEKIND> RemoveRequested;
        public event Action<View> ViewKindChanged;

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

            ToolStripSplitButton addButton = new ToolStripSplitButton("&Add Resource");
            addButton.ButtonClick += new EventHandler(addExistingResources);
            addButton.DropDownItems.Add("Existing File", null, new EventHandler(addExistingResources));
            addButton.DropDownItems.Add(new ToolStripSeparator());
            ToolStripMenuItem newItem = new ToolStripMenuItem("New");
            newItem.DropDownItems.Add("String",null,new EventHandler(addNewString));
            newItem.DropDownItems.Add(new ToolStripSeparator());
            newItem.DropDownItems.Add("Icon");
            newItem.DropDownItems.Add(new ToolStripSeparator());
            newItem.DropDownItems.Add("PNG Image");
            newItem.DropDownItems.Add("BMP Image");
            newItem.DropDownItems.Add("JPEG Image");
            newItem.DropDownItems.Add("GIF Image");
            addButton.DropDownItems.Add(newItem);
            toolStrip.Items.Add(addButton);

            ToolStripDropDownButton mergeButton = new ToolStripDropDownButton("&Merge with ResX File");
            mergeButton.DropDownItems.Add("Merge && &Preserve Both");
            mergeButton.DropDownItems.Add("Merge && &Delete Source");
            toolStrip.Items.Add(mergeButton);

            toolStrip.Items.Add(new ToolStripSeparator());

            removeButton = new ToolStripSplitButton("&Remove Resources");
            removeButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            removeExcludeItem = new ToolStripMenuItem("Remove && Exclude from Project");
            removeDeleteItem = new ToolStripMenuItem("Remove && Delete File");
            removeButton.DropDownItems.Add(removeExcludeItem);
            removeButton.DropDownItems.Add(removeDeleteItem);
            removeButton.ButtonClick += new EventHandler((o, e) => { notifyRemoveRequested(REMOVEKIND.REMOVE); });
            removeDeleteItem.Click += new EventHandler((o, e) => { notifyRemoveRequested(REMOVEKIND.REMOVE | REMOVEKIND.DELETE_FILE | REMOVEKIND.EXCLUDE); });
            removeExcludeItem.Click += new EventHandler((o, e) => { notifyRemoveRequested(REMOVEKIND.REMOVE | REMOVEKIND.EXCLUDE); });
            toolStrip.Items.Add(removeButton);

            inlineButton = new ToolStripSplitButton("&Inline resources");
            inlineButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            inlineButton.DropDownItems.Add("Inline && &remove");
            toolStrip.Items.Add(inlineButton);

            toolStrip.Items.Add(new ToolStripSeparator());

            viewButton = new ToolStripDropDownButton("&View");
            ToolStripMenuItem viewDetailsItem = new ToolStripMenuItem("Details");
            viewDetailsItem.CheckState = CheckState.Unchecked;
            viewDetailsItem.CheckOnClick = true;
            viewDetailsItem.CheckStateChanged += new EventHandler(ViewCheckStateChanged);
            viewDetailsItem.Tag = View.Details;
            viewButton.DropDownItems.Add(viewDetailsItem);
            ToolStripMenuItem viewListItem = new ToolStripMenuItem("List");
            viewListItem.CheckState = CheckState.Unchecked;
            viewListItem.CheckOnClick = true;
            viewListItem.CheckStateChanged += new EventHandler(ViewCheckStateChanged);
            viewListItem.Tag = View.List;
            viewButton.DropDownItems.Add(viewListItem);
            ToolStripMenuItem viewIconsItem = new ToolStripMenuItem("Icons");
            viewIconsItem.CheckState = CheckState.Checked;
            viewIconsItem.CheckOnClick = true;
            viewIconsItem.CheckStateChanged += new EventHandler(ViewCheckStateChanged);
            viewIconsItem.Tag = View.LargeIcon;
            viewButton.DropDownItems.Add(viewIconsItem);
            toolStrip.Items.Add(viewButton);

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

        private void addExistingResources(object sender, EventArgs e) {
            try {
                string[] files = VisualLocalizer.Library.MessageBox.SelectFilesViaDlg("Select files", Path.GetDirectoryName(Editor.FileName),
                    "Image files (*.bmp;*.gif;*.jpg;*.png)\0*.bmp;*.gif;*.jpg;*.png\0", 0, OPENFILENAME.OFN_ALLOWMULTISELECT);
                if (files == null) return;


            } catch (Exception ex) {
                VisualLocalizer.Library.MessageBox.ShowError(ex.Message);
            }
        }

        private void addNewString(object sender, EventArgs e) {
            tabs.SelectedTab = stringTab;
            stringGrid.ClearSelection();

            DataGridViewCell cell = stringGrid.Rows[stringGrid.Rows.Count - 1].Cells[stringGrid.KeyColumnName];            
            cell.Value = "new value";

            stringGrid.CurrentCell = cell;
            stringGrid.BeginEdit(true);

            stringGrid.NotifyDataChanged();
        }

        private void ViewCheckStateChanged(object sender, EventArgs e) {
            ToolStripMenuItem senderItem = sender as ToolStripMenuItem;
            if (senderItem.CheckState == CheckState.Unchecked) return;

            foreach (ToolStripMenuItem item in viewButton.DropDownItems)
                if (item != senderItem) item.CheckState = CheckState.Unchecked;

            notifyViewKindChanged((View)senderItem.Tag);
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
            tabs.SelectedIndexChanged += new EventHandler(UpdateToolStripButtonsEnable);
            
            stringTab = new TabPage("Strings");            
            stringTab.BorderStyle = BorderStyle.None;            
            stringGrid = new ResXStringGrid(this);
            stringGrid.Dock = DockStyle.Fill;
            stringGrid.BackColor = Color.White;
            stringGrid.ScrollBars = ScrollBars.Vertical;
            stringGrid.DataChanged += new EventHandler((o, args) => { DataChanged(o, args); });
            stringGrid.BorderStyle = BorderStyle.None;
            stringGrid.StringKeyRenamed += new Action<ResXStringGridRow, string>(stringGrid_StringKeyRenamed);
            stringGrid.StringValueChanged += new Action<ResXStringGridRow, string, string>(stringGrid_StringValueChanged);
            stringGrid.StringCommentChanged += new Action<ResXStringGridRow, string, string>(stringGrid_StringCommentChanged);            
            stringTab.Controls.Add(stringGrid);
            tabs.TabPages.Add(stringTab);

            imagesTab = new TabPage("Images");
            imagesTab.BorderStyle = BorderStyle.None;
            imagesListView = new ResXImagesList(this);
            imagesListView.Dock = DockStyle.Fill;
            imagesListView.BackColor = Color.White;
            imagesTab.Controls.Add(imagesListView);
            tabs.TabPages.Add(imagesTab);

            tabs.TabPages.Add(new TabPage("Icons"));
            tabs.TabPages.Add(new TabPage("Sounds"));
            tabs.TabPages.Add(new TabPage("Files"));
        }

        private void UpdateToolStripButtonsEnable(object sender, EventArgs e) {
            bool selectedString = tabs.SelectedTab.Text.Equals("Strings");
            inlineButton.Enabled = selectedString;
            removeDeleteItem.Enabled = !selectedString;
            removeExcludeItem.Enabled = !selectedString;
            viewButton.Enabled = !selectedString;
        }

        private void stringGrid_StringCommentChanged(ResXStringGridRow row, string oldComment, string newComment) {
            string key = row.Status==ResXStringGridRow.STATUS.KEY_NULL ? null : row.DataSourceItem.Name;
            StringChangeCommentUndoUnit unit = new StringChangeCommentUndoUnit(row, stringGrid, key, oldComment, newComment);
            Editor.AddUndoUnit(unit);
        }

        private void stringGrid_StringValueChanged(ResXStringGridRow row, string oldValue, string newValue) {
            string key = row.Status == ResXStringGridRow.STATUS.KEY_NULL ? null : row.DataSourceItem.Name;
            StringChangeValueUndoUnit unit = new StringChangeValueUndoUnit(row, stringGrid, key, oldValue, newValue, row.DataSourceItem.Comment);
            Editor.AddUndoUnit(unit);
        }

        private void stringGrid_StringKeyRenamed(ResXStringGridRow row, string newKey) {
            string oldKey = row.Status == ResXStringGridRow.STATUS.KEY_NULL ? null : row.DataSourceItem.Name;
            StringRenameKeyUndoUnit unit = new StringRenameKeyUndoUnit(row, stringGrid, oldKey, newKey); 
            Editor.AddUndoUnit(unit);
        }

        private void notifyRemoveRequested(REMOVEKIND kind) {
            if (RemoveRequested != null) RemoveRequested(kind);
        }

        private void notifyViewKindChanged(View newView) {            
            if (ViewKindChanged != null) ViewKindChanged(newView);
        }

        public void SetData(Dictionary<string,ResXDataNode> data) {
            Dictionary<string, ResXDataNode> stringData = new Dictionary<string, ResXDataNode>();
            Dictionary<string, ResXDataNode> imageData = new Dictionary<string, ResXDataNode>();

            foreach (var pair in data) {
                if (pair.Value.HasStringValue()) stringData.Add(pair.Key, pair.Value);
                if (pair.Value.HasImageValue()) imageData.Add(pair.Key, pair.Value);
            }

            stringGrid.SetData(stringData);
            imagesListView.SetData(imageData);
        }

        public Dictionary<string, ResXDataNode> GetData(bool throwExceptions) {
            Dictionary<string, ResXDataNode> data = new Dictionary<string, ResXDataNode>();

            foreach (var pair in stringGrid.GetData(throwExceptions))
                data.Add(pair.Key, pair.Value);

            foreach (var pair in imagesListView.GetData()) {
                data.Add(pair.Key, pair.Value);
            }

            return data;
        }

        public void SetReadOnly(bool readOnly) {
            toolStrip.Enabled = !readOnly;
            stringGrid.ReadOnly = readOnly;
            if (!readOnly) UpdateToolStripButtonsEnable(null, null);
        }

        private class VsColorTable : ProfessionalColorTable {

            private Color beginColor, middleColor, endColor;

            public VsColorTable() {
                switch (VisualLocalizerPackage.VisualStudioVersion) {
                    case VS_VERSION.VS2008:
                        beginColor = ColorTranslator.FromHtml("#FAFAFD");
                        middleColor = ColorTranslator.FromHtml("#E9ECFA");
                        endColor = ColorTranslator.FromHtml("#C1C8D9");
                        break;
                    case VS_VERSION.VS2010:
                        beginColor = ColorTranslator.FromHtml("#BCC7D8");
                        middleColor = ColorTranslator.FromHtml("#BCC7D8");
                        endColor = ColorTranslator.FromHtml("#BCC7D8");
                        break;
                    case VS_VERSION.VS2011:
                        beginColor = ColorTranslator.FromHtml("#D0D2D3");
                        middleColor = ColorTranslator.FromHtml("#D0D2D3");
                        endColor = ColorTranslator.FromHtml("#D0D2D3");
                        break;
                    case VS_VERSION.UNKNOWN:
                        beginColor = ToolStripGradientBegin;
                        middleColor = ToolStripGradientMiddle;
                        endColor = ToolStripGradientEnd;
                        break;
                 
                }               
            }

            public override Color ToolStripGradientBegin { get { return beginColor; } }
            public override Color ToolStripGradientMiddle { get { return middleColor; } }
            public override Color ToolStripGradientEnd { get { return endColor; } }
        }
    }

}
    
