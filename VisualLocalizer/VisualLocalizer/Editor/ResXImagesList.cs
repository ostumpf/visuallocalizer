using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Resources;
using VisualLocalizer.Library;
using System.Drawing;
using System.IO;

namespace VisualLocalizer.Editor {
    internal sealed class ResXImagesList : ListView {

        private ResXEditorControl editorControl;

        public ResXImagesList(ResXEditorControl editorControl) {
            this.editorControl = editorControl;
            this.Dock = DockStyle.Fill;
            this.MultiSelect = true;
            this.View = View.LargeIcon;
            this.FullRowSelect = true;
            this.GridLines = true;
            this.HeaderStyle = ColumnHeaderStyle.Clickable;
            this.HideSelection = true;
            this.LabelEdit = true;
            this.TileSize = new System.Drawing.Size(70, 70);

            ColumnHeader keyHeader = new ColumnHeader();
            keyHeader.Text = "Resource Key";
            keyHeader.Width = 200;
            keyHeader.Name = "Key";
            this.Columns.Add(keyHeader);            

            ColumnHeader fileHeader = new ColumnHeader();
            fileHeader.Text = "Corresponding File";
            fileHeader.Width = 250;
            fileHeader.Name = "Path";
            this.Columns.Add(fileHeader);

            ColumnHeader sizeHeader = new ColumnHeader();
            sizeHeader.Text = "Image Size";
            sizeHeader.Width = 80;
            sizeHeader.Name = "Size";
            this.Columns.Add(sizeHeader);

            ColumnHeader commentHeader = new ColumnHeader();
            commentHeader.Text = "Comment";
            commentHeader.Width = 250;
            commentHeader.Name = "Comment";
            this.Columns.Add(commentHeader);

            editorControl.ViewKindChanged += new Action<View>(ViewKindChanged);
        }

        private void ViewKindChanged(View view) {
            this.View = view;
        }

        public void SetData(Dictionary<string, ResXDataNode> newData) {
            this.Items.Clear();
            this.SuspendLayout();

            this.LargeImageList = new ImageList();
            this.SmallImageList = new ImageList();
            Uri currentFileUri = new Uri(editorControl.Editor.FileName);

            foreach (var pair in newData) {
                Bitmap bmp = pair.Value.GetImageValue();
                LargeImageList.Images.Add(pair.Key, bmp);
                SmallImageList.Images.Add(pair.Key, bmp);

                ListViewItem item = new ListViewItem();
                item.Text = pair.Key;
                item.ImageKey = pair.Key;

                ListViewItem.ListViewSubItem subKey = new ListViewItem.ListViewSubItem();
                subKey.Name = "Path";
                subKey.Text = currentFileUri.MakeRelativeUri(new Uri(pair.Value.FileRef.FileName)).ToString();
                item.SubItems.Add(subKey);

                ListViewItem.ListViewSubItem subSize = new ListViewItem.ListViewSubItem();
                subSize.Name = "Size";
                subSize.Text = string.Format("{0} x {1}", bmp.Width, bmp.Height);
                item.SubItems.Add(subSize);

                ListViewItem.ListViewSubItem subComment = new ListViewItem.ListViewSubItem();
                subComment.Name = "Comment";
                subComment.Text = pair.Value.Comment;
                item.SubItems.Add(subComment);

                Items.Add(item);
            }
            
            this.LargeImageList.ImageSize = new System.Drawing.Size(100, 100);
            this.ResumeLayout();
        }

        public Dictionary<string, ResXDataNode> GetData() {
            Dictionary<string, ResXDataNode> data = new Dictionary<string, ResXDataNode>(Items.Count);

            foreach (ListViewItem item in Items) {
                
            }

            return data;
        }
    }
}
