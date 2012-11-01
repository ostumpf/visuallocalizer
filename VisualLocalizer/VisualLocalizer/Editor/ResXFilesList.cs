using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;
using VisualLocalizer.Library;
using System.Windows.Forms;
using System.IO;

namespace VisualLocalizer.Editor {
    internal sealed class ResXFilesList : AbstractListView {

        public ResXFilesList(ResXEditorControl editorControl)
            : base(editorControl) {
        }

        public override bool CanContainItem(ResXDataNode node) {
            return true;
        }

        public override IKeyValueSource Add(string key, ResXDataNode value, bool showThumbnails) {
            ListViewKeyItem item = base.Add(key, value, showThumbnails) as ListViewKeyItem;

            LargeImageList.Images.Add(item.Name, Editor.doc);
            SmallImageList.Images.Add(item.Name, Editor.doc);

            FileInfo info = null;
            if (value.FileRef != null && File.Exists(value.FileRef.FileName)) {
                info = new FileInfo(value.FileRef.FileName);
            }
            
            if (info == null && showThumbnails) item.FileRefOk = false;

            ListViewItem.ListViewSubItem subSize = new ListViewItem.ListViewSubItem();
            subSize.Name = "Size";
            if (info != null) subSize.Text = GetFileSize(info.Length);
            item.SubItems.Insert(2, subSize);

            return item;
        }

        public override ListViewKeyItem UpdateDataOf(string name) {
            ListViewKeyItem item = base.UpdateDataOf(name);
            if (item == null) return null;

            FileInfo info = null;
            if (File.Exists(item.DataNode.FileRef.FileName)) {
                info = new FileInfo(item.DataNode.FileRef.FileName);
            }

            if (info != null) {
                item.SubItems["Size"].Text = GetFileSize(info.Length);                
            } else {
                item.SubItems["Size"].Text = null;                
            }

            return item;
        }

        protected override void InitializeColumns() {
            base.InitializeColumns();

            ColumnHeader sizeHeader = new ColumnHeader();
            sizeHeader.Text = "File Size";
            sizeHeader.Width = 80;
            sizeHeader.Name = "Size";
            this.Columns.Insert(2, sizeHeader);
        }
    }
}
