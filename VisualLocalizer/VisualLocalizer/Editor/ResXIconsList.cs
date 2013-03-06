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
    internal sealed class ResXIconsList : AbstractListView {

        public ResXIconsList(ResXEditorControl editorControl) : base(editorControl) {
        }

        public override bool CanContainItem(ResXDataNode node) {
            return node.HasValue<Icon>();
        }

        public override IKeyValueSource Add(string key, ResXDataNode value, bool showThumbnails) {
            ListViewKeyItem item = base.Add(key, value, showThumbnails) as ListViewKeyItem;
            if (referenceExistingOnAdd) return item;

            Icon ico = null;
            if (showThumbnails) ico = value.GetValue<Icon>();

            if (ico != null) {
                LargeImageList.Images.Add(item.Name, ico);
                SmallImageList.Images.Add(item.Name, ico);
                item.ImageKey = item.Name; // update icon
            }
            
            if (ico == null && showThumbnails) item.FileRefOk = false;

            ListViewItem.ListViewSubItem subSize = new ListViewItem.ListViewSubItem();
            subSize.Name = "Size";
            if (ico != null) {
                subSize.Text = string.Format("{0} x {1}", ico.Width, ico.Height);
            }
            item.SubItems.Insert(2, subSize);

            return item;
        }

        public override ListViewKeyItem UpdateDataOf(string name) {
            ListViewKeyItem item = base.UpdateDataOf(name);
            if (item == null) return null;

            Icon ico = item.DataNode.GetValue<Icon>();
            if (!string.IsNullOrEmpty(item.ImageKey) && LargeImageList.Images.ContainsKey(item.ImageKey)) {
                LargeImageList.Images.RemoveByKey(item.ImageKey);
                SmallImageList.Images.RemoveByKey(item.ImageKey);
            }

            if (ico != null) {
                LargeImageList.Images.Add(item.ImageKey, ico);
                SmallImageList.Images.Add(item.ImageKey, ico);

                item.SubItems["Size"].Text = string.Format("{0} x {1}", ico.Width, ico.Height);
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
            sizeHeader.Text = "Icon Size";
            sizeHeader.Width = 80;
            sizeHeader.Name = "Size";
            this.Columns.Insert(2, sizeHeader);            
        }

        protected override string saveIntoTmpFile(ResXDataNode node, string directory) {
            Icon value = node.GetValue<Icon>();
            string filename = node.Name + ".ico";
            string path = Path.Combine(directory, filename);

            FileStream fs = null;
            try {
                fs = new FileStream(path, FileMode.Create);
                value.Save(fs);
            } finally {
                if (fs != null) fs.Close();         
            }

            return path;
        }
    }
}
