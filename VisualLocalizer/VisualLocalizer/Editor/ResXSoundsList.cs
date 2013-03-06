using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;
using System.IO;
using System.Windows.Forms;
using VisualLocalizer.Library;
using System.Media;

namespace VisualLocalizer.Editor {
    internal sealed class ResXSoundsList : AbstractListView {
        public ResXSoundsList(ResXEditorControl editorControl)
            : base(editorControl) {
        }

        public override bool CanContainItem(ResXDataNode node) {
            return node.HasValue<MemoryStream>() && (node.FileRef == null || node.FileRef.FileName.ToLower().EndsWith(".wav"));
        }

        public override IKeyValueSource Add(string key, ResXDataNode value, bool showThumbnails) { 
            ListViewKeyItem item = base.Add(key, value, showThumbnails) as ListViewKeyItem;
            if (referenceExistingOnAdd) return item;

            LargeImageList.Images.Add(item.Name, Editor.play);
            SmallImageList.Images.Add(item.Name, Editor.play);

            item.ImageKey = item.Name; // update icon

            FileInfo info = null;
            if (value.FileRef != null && File.Exists(value.FileRef.FileName)) {
                info = new FileInfo(value.FileRef.FileName);
            }

            if (value.FileRef != null && !File.Exists(value.FileRef.FileName) && showThumbnails)
                item.FileRefOk = false;

            ListViewItem.ListViewSubItem subSize = new ListViewItem.ListViewSubItem();
            subSize.Name = "Size";            
            item.SubItems.Insert(2, subSize);

            ListViewItem.ListViewSubItem subLength = new ListViewItem.ListViewSubItem();
            subLength.Name = "Length";            
            item.SubItems.Insert(3, subLength);

            if (info != null) {
                subSize.Text = GetFileSize(info.Length);
                subLength.Text = GetSoundDigits(SoundInfo.GetSoundLength(info.FullName));
            } else {
                var stream = value.GetValue<MemoryStream>();
                if (stream != null) {
                    subSize.Text = GetFileSize(stream.Length);
                    subLength.Text = null;
                }
            }

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
                item.SubItems["Length"].Text = GetSoundDigits(SoundInfo.GetSoundLength(info.FullName));
            } else {
                var stream = item.DataNode.GetValue<MemoryStream>();
                if (stream != null) {
                    item.SubItems["Size"].Text = GetFileSize(stream.Length);
                    item.SubItems["Length"].Text = null;
                }
            }

            return item;
        }

        private string GetSoundDigits(int milis) {
            if (milis < 1000) {
                return string.Format("{0} ms", milis);
            } else {
                int secs = milis / 1000;
                int realSecs = secs % 60;
                int minutes = secs / 60;
                int realMinutes = minutes % 60;
                int realHours = realMinutes / 60;

                return string.Format("{0}:{1}:{2}", realHours < 10 ? "0" + realHours : realHours.ToString(),
                    realMinutes < 10 ? "0" + realMinutes : realMinutes.ToString(), realSecs < 10 ? "0" + realSecs : realSecs.ToString());
            }
        }

        protected override void InitializeColumns() {
            base.InitializeColumns();

            ColumnHeader sizeHeader = new ColumnHeader();
            sizeHeader.Text = "File Size";
            sizeHeader.Width = 80;
            sizeHeader.Name = "Size";
            this.Columns.Insert(2, sizeHeader);

            ColumnHeader lengthHeader = new ColumnHeader();
            lengthHeader.Text = "Length";
            lengthHeader.Width = 80;
            lengthHeader.Name = "Length";
            this.Columns.Insert(3, lengthHeader);            
        }

        protected override string saveIntoTmpFile(ResXDataNode node, string directory) {
            MemoryStream ms = node.GetValue<MemoryStream>();
            string filename = node.Name + ".wav";
            string path = Path.Combine(directory, filename);
            
            FileStream fs = null;
            try {
                fs = new FileStream(path, FileMode.Create);

                byte[] buffer = new byte[8192];
                int read=0;
                while ((read = ms.Read(buffer, 0, buffer.Length)) > 0) {
                    fs.Write(buffer, 0, read);
                }

            } finally {
                if (fs != null) fs.Close();
                ms.Close();
            }

            return path;
        }
    }
}
