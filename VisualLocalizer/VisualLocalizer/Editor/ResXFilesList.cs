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
            if (referenceExistingOnAdd) {
                item.FileRefOk = true;
                return item;
            }

            LargeImageList.Images.Add(item.Name, Editor.doc);
            SmallImageList.Images.Add(item.Name, Editor.doc);
            item.ImageKey = item.Name; // update icon

            FileInfo info = null;
            if (value.FileRef != null && File.Exists(value.FileRef.FileName)) {
                info = new FileInfo(value.FileRef.FileName);
            }

            if (value.FileRef != null && !File.Exists(value.FileRef.FileName) && showThumbnails) 
                item.FileRefOk = false;

            ListViewItem.ListViewSubItem subSize = new ListViewItem.ListViewSubItem();
            subSize.Name = "Size";
            if (info != null) {
                subSize.Text = GetFileSize(info.Length);
            } else {
                if (value.HasValue<string>()) {
                    var val = value.GetValue<string>();
                    if (val != null) subSize.Text = GetFileSize(Encoding.UTF8.GetBytes(val).Length);
                } else {
                    var val = value.GetValue<byte[]>();
                    if (val != null) subSize.Text = GetFileSize(val.Length); 
                }
            }
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
                if (item.DataNode.HasValue<string>()) {
                    var val = item.DataNode.GetValue<string>();
                    if (val != null) item.SubItems["Size"].Text = GetFileSize(Encoding.UTF8.GetBytes(val).Length);
                } else {
                    var val = item.DataNode.GetValue<byte[]>();
                    if (val != null) item.SubItems["Size"].Text = GetFileSize(val.Length);
                }        
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

        protected override string saveIntoTmpFile(ResXDataNode node, string directory) {
            byte[] bytes;
            string path;

            if (node.HasValue<string>()) {
                string value = node.GetValue<string>();
                string filename = node.Name + ".txt";
                
                path = Path.Combine(directory, filename);
                bytes = Encoding.UTF8.GetBytes(value);
            } else {
                bytes = node.GetValue<byte[]>(); 

                string filename = node.Name + ".bin"; 
                path = Path.Combine(directory, filename); 
            }                       

            FileStream fs = null;
            try {
                fs = new FileStream(path, FileMode.Create);

                fs.Write(bytes, 0, bytes.Length); 
            } finally {
                if (fs != null) fs.Close();
            }

            return path;
        }
    }
}
