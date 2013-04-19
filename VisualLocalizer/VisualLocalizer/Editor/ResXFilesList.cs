using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;
using VisualLocalizer.Library;
using System.Windows.Forms;
using System.IO;

namespace VisualLocalizer.Editor {

    /// <summary>
    /// Represents Files tab in ResX editor. Can contain any content.
    /// </summary>
    internal sealed class ResXFilesList : AbstractListView {

        public ResXFilesList(ResXEditorControl editorControl)
            : base(editorControl) {
        }


        /// <summary>
        /// Returns true if given node's type matches the type of items this control holds
        /// </summary>
        public override bool CanContainItem(ResXDataNode node) {
            if (node == null) throw new ArgumentNullException("node");
            return true; // everything is allowed
        }

        /// <summary>
        /// Adds the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public override IKeyValueSource Add(string key, ResXDataNode value) {
            ListViewKeyItem item = base.Add(key, value) as ListViewKeyItem;
            if (referenceExistingOnAdd) {
                item.FileRefOk = true;
            } else {
                ListViewItem.ListViewSubItem subSize = new ListViewItem.ListViewSubItem();
                subSize.Name = "Size";
                item.SubItems.Insert(2, subSize);
            }

            UpdateDataOf(item, false);            

            return item;
        }

        /// <summary>
        /// Reloads displayed data from underlaying ResX node
        /// </summary>
        public override void UpdateDataOf(ListViewKeyItem item, bool reloadImages) {
            base.UpdateDataOf(item, reloadImages);
            
            LargeImageList.Images.Add(item.ImageKey, Editor.doc);
            SmallImageList.Images.Add(item.ImageKey, Editor.doc);

            FileInfo info = null;
            if (item.DataNode.FileRef != null && File.Exists(item.DataNode.FileRef.FileName)) {
                info = new FileInfo(item.DataNode.FileRef.FileName);
            }

            if (info != null) {
                item.SubItems["Size"].Text = GetFileSize(info.Length);
                item.FileRefOk = true;
            } else {
                if (item.DataNode.HasValue<string>()) {
                    var val = item.DataNode.GetValue<string>();
                    if (val != null) {
                        item.SubItems["Size"].Text = GetFileSize(Encoding.UTF8.GetBytes(val).Length);
                        item.FileRefOk = true;
                    } else item.FileRefOk = false;
                } else {
                    var val = item.DataNode.GetValue<byte[]>();
                    if (val != null) {
                        item.SubItems["Size"].Text = GetFileSize(val.Length);
                        item.FileRefOk = true;
                    } else item.FileRefOk = false;
                }        
            }
            
            item.UpdateErrorSetDisplay();            

            Validate(item);
            NotifyItemsStateChanged();

            if (item.ErrorMessages.Count > 0) {
                item.Status = KEY_STATUS.ERROR;
            } else {
                item.Status = KEY_STATUS.OK;
                item.LastValidKey = item.Key;
            }

            // update image display
            string p = item.ImageKey;
            item.ImageKey = null;
            item.ImageKey = p;
        }

        /// <summary>
        /// Create the GUI
        /// </summary>
        protected override void InitializeColumns() {
            base.InitializeColumns();

            ColumnHeader sizeHeader = new ColumnHeader();
            sizeHeader.Text = "File Size";
            sizeHeader.Width = 80;
            sizeHeader.Name = "Size";
            this.Columns.Insert(2, sizeHeader);
        }

        /// <summary>
        /// Saves given node's content into random file in specified directory and returns the file path
        /// </summary>   
        protected override string SaveIntoTmpFile(ResXDataNode node, string name, string directory) {
            byte[] bytes;
            string path;

            if (node.HasValue<string>()) { // contains text file
                string value = node.GetValue<string>();
                string filename = name + ".txt";
                
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
