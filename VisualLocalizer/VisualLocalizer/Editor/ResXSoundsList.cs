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
            return node.HasValue<MemoryStream>() && node.FileRef.FileName.ToLower().EndsWith(".wav");
        }

        public override IKeyValueSource Add(string key, ResXDataNode value, bool showThumbnails) {
            ListViewKeyItem item = base.Add(key, value, showThumbnails) as ListViewKeyItem;

            LargeImageList.Images.Add(item.Name, Editor.play);
            SmallImageList.Images.Add(item.Name, Editor.play);

            FileInfo info = null;
            if (value.FileRef != null && File.Exists(value.FileRef.FileName)) {
                info = new FileInfo(value.FileRef.FileName);
            }
            
            if (info == null && showThumbnails) item.FileRefOk = false;

            ListViewItem.ListViewSubItem subSize = new ListViewItem.ListViewSubItem();
            subSize.Name = "Size";
            if (info != null) subSize.Text = GetFileSize(info.Length);
            item.SubItems.Insert(2, subSize);

            ListViewItem.ListViewSubItem subLength = new ListViewItem.ListViewSubItem();
            subLength.Name = "Length";
            if (info != null) subLength.Text = GetSoundDigits(SoundInfo.GetSoundLength(info.FullName));
            item.SubItems.Insert(3, subLength);

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
                item.SubItems["Size"].Text = null;
                item.SubItems["Length"].Text = null;
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
    }
}
