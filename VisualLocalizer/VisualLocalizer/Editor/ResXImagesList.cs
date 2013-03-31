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

    /// <summary>
    /// Represents Images tab in ResX editor. Can contain any Bitmap resources.
    /// </summary>
    internal sealed class ResXImagesList : AbstractListView {

        public ResXImagesList(ResXEditorControl editorControl) : base(editorControl) {
        }


        /// <summary>
        /// Returns true if given node's type matches the type of items this control holds
        /// </summary>
        public override bool CanContainItem(ResXDataNode node) {
            if (node == null) throw new ArgumentNullException("node");
            return node.HasValue<Bitmap>(); // only Bitmaps are allowed
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
            
            Bitmap bmp = item.DataNode.GetValue<Bitmap>();

            if (LargeImageList.Images.ContainsKey(item.ImageKey) && reloadImages) {
                LargeImageList.Images.RemoveByKey(item.ImageKey);
                SmallImageList.Images.RemoveByKey(item.ImageKey);                
            }

            // add the new image, if exists
            if (bmp != null) {                     
                LargeImageList.Images.Add(item.ImageKey, bmp);
                SmallImageList.Images.Add(item.ImageKey, bmp);                

                item.SubItems["Size"].Text = string.Format("{0} x {1}", bmp.Width, bmp.Height);
                item.FileRefOk = true;
            } else {                
                item.SubItems["Size"].Text = null;
                item.FileRefOk = false;
            }

            item.UpdateErrorSetDisplay();            

            Validate(item);
            NotifyItemsStateChanged();

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
            sizeHeader.Text = "Image Size";
            sizeHeader.Width = 80;
            sizeHeader.Name = "Size";
            this.Columns.Insert(2, sizeHeader);            
        }

        /// <summary>
        /// Saves given node's content into random file in specified directory and returns the file path
        /// </summary>      
        protected override string SaveIntoTmpFile(ResXDataNode node, string name, string directory) {            
            Bitmap value = node.GetValue<Bitmap>();
            string filename = name + ".png";
            string path = Path.Combine(directory, filename);

            value.Save(path, ImageFormat.Png);

            return path;
        }
    }
}
