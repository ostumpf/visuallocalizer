using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Resources;
using VisualLocalizer.Library;
using System.Drawing;
using System.IO;
using EnvDTE;
using VSLangProj;
using VisualLocalizer.Components;
using System.Drawing.Imaging;

namespace VisualLocalizer.Editor {
    internal sealed class ResXImagesList : AbstractListView {

        public ResXImagesList(ResXEditorControl editorControl) : base(editorControl) {
        }

        public override bool CanContainItem(ResXDataNode node) {
            return node.HasValue<Bitmap>();
        }

        public override IKeyValueSource Add(string key, ResXDataNode value, bool showThumbnails) {
            ListViewKeyItem item = base.Add(key, value, showThumbnails) as ListViewKeyItem;
            if (referenceExistingOnAdd) return item;

            Bitmap bmp = null;
            if (showThumbnails) bmp = value.GetValue<Bitmap>();
            if (bmp != null) {
                LargeImageList.Images.Add(item.Name, bmp);
                SmallImageList.Images.Add(item.Name, bmp);
                item.ImageKey = item.Name; // update icon
            } 
            
            if (bmp == null && showThumbnails) item.FileRefOk = false;

            ListViewItem.ListViewSubItem subSize = new ListViewItem.ListViewSubItem();
            subSize.Name = "Size";
            if (bmp != null) subSize.Text = string.Format("{0} x {1}", bmp.Width, bmp.Height);
            item.SubItems.Insert(2, subSize);

            return item;
        }

        public override ListViewKeyItem UpdateDataOf(string name) {
            ListViewKeyItem item = base.UpdateDataOf(name);
            if (item == null) return null;

            Bitmap bmp = item.DataNode.GetValue<Bitmap>();
            if (!string.IsNullOrEmpty(item.ImageKey) && LargeImageList.Images.ContainsKey(item.ImageKey)) {
                LargeImageList.Images.RemoveByKey(item.ImageKey);
                SmallImageList.Images.RemoveByKey(item.ImageKey);
            }

            if (bmp != null) {                
                LargeImageList.Images.Add(item.ImageKey, bmp);
                SmallImageList.Images.Add(item.ImageKey, bmp);

                item.SubItems["Size"].Text = string.Format("{0} x {1}", bmp.Width, bmp.Height);
            } else {                
                item.SubItems["Size"].Text = null;
            }
            
            string p = item.ImageKey;
            item.ImageKey = null;
            item.ImageKey = p;

            return item;
        }

        protected override void InitializeColumns() {
            base.InitializeColumns();

            ColumnHeader sizeHeader = new ColumnHeader();
            sizeHeader.Text = "Image Size";
            sizeHeader.Width = 80;
            sizeHeader.Name = "Size";
            this.Columns.Insert(2, sizeHeader);            
        }

        protected override string saveIntoTmpFile(ResXDataNode node, string directory) {            
            Bitmap value = node.GetValue<Bitmap>();
            string filename = node.Name + ".png";
            string path = Path.Combine(directory, filename);

            value.Save(path, ImageFormat.Png);

            return path;
        }
    }
}
