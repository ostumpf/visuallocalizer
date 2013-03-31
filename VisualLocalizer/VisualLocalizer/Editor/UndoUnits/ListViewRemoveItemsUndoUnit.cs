using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using EnvDTE;
using VSLangProj;
using VisualLocalizer.Components;

namespace VisualLocalizer.Editor.UndoUnits {

    /// <summary>
    /// Represents undo unit for removing items from editor's list view
    /// </summary>
    [Guid("7C622BF2-3F55-4b21-83B0-3BB8B4AC1234")]
    internal sealed class ListViewRemoveItemsUndoUnit : AbstractUndoUnit {

        private List<ListViewKeyItem> Items { get; set; }
        private KeyValueIdentifierConflictResolver ConflictResolver { get; set; }
        private ResXEditorControl Control { get; set; }

        public ListViewRemoveItemsUndoUnit(ResXEditorControl control, List<ListViewKeyItem> items, KeyValueIdentifierConflictResolver conflictResolver) {
            if (control == null) throw new ArgumentNullException("control");
            if (items == null) throw new ArgumentNullException("items");
            if (conflictResolver == null) throw new ArgumentNullException("conflictResolver");

            this.Items = items;
            this.ConflictResolver = conflictResolver;
            this.Control = control;
        }

        public override void Undo() {
            try {
                HashSet<AbstractListView> usedLists = new HashSet<AbstractListView>();

                // check if files were deleted during the operation
                bool filesDeleted = false;
                foreach (var item in Items)
                    if ((item.RemoveKind & REMOVEKIND.DELETE_FILE) == REMOVEKIND.DELETE_FILE) filesDeleted = true;
                if (filesDeleted) {
                    // confirm adding references to non-existing files
                    DialogResult result = VisualLocalizer.Library.MessageBox.Show("Files were erased from disk during this operation. Some items will probably be improperly displayed and project will fail to compile. Proceed anyway?", null, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_WARNING);
                    if (result != DialogResult.Yes) return;
                }

                foreach (var item in Items.OrderBy((i) => { return i.IndexAtDeleteTime; })) {
                    // add excluded (but not deleted) files to project
                    if ((item.RemoveKind & REMOVEKIND.EXCLUDE) == REMOVEKIND.EXCLUDE &&
                        (item.RemoveKind & REMOVEKIND.DELETE_FILE) != REMOVEKIND.DELETE_FILE &&
                        item.NeighborItems != null &&
                        item.DataNode.FileRef != null && File.Exists(item.DataNode.FileRef.FileName)) {

                        ProjectItem newItem = item.NeighborItems.AddFromFile(Path.GetFullPath(item.DataNode.FileRef.FileName));

                        if (newItem.ContainingProject != null && newItem.ContainingProject.Kind.ToUpperInvariant() != StringConstants.WebSiteProject) {
                            newItem.Properties.Item("BuildAction").Value = prjBuildAction.prjBuildActionNone;
                        }
                    }

                    AbstractListView ListView = item.AbstractListView;
                    usedLists.Add(ListView);

                    // add the item back to the list view
                    if (item.IndexAtDeleteTime >= 0 && item.IndexAtDeleteTime < ListView.Items.Count) {
                        ListView.Items.Insert(item.IndexAtDeleteTime, item);
                    } else {
                        ListView.Items.Add(item);
                    }

                    item.BeforeEditKey = null;
                    item.AfterEditKey = item.Text;

                    // update the image for the item
                    if (!string.IsNullOrEmpty(item.ImageKey)) {
                        object bmp = GetImageFor(item);
                        if (bmp != null) {
                            item.FileRefOk = true;
                            if (bmp is Bitmap) {
                                ListView.LargeImageList.Images.Add(item.ImageKey, (Bitmap)bmp);
                                ListView.SmallImageList.Images.Add(item.ImageKey, (Bitmap)bmp);
                            } else {
                                ListView.LargeImageList.Images.Add(item.ImageKey, (Icon)bmp);
                                ListView.SmallImageList.Images.Add(item.ImageKey, (Icon)bmp);
                            }

                            string tmp = item.ImageKey;
                            item.ImageKey = null;
                            item.ImageKey = tmp;
                        } else {
                            item.FileRefOk = false;
                        }
                    }

                    ListView.Validate(item);
                }

                // update state of list views
                foreach (var usedList in usedLists) {
                    usedList.NotifyDataChanged();
                    usedList.NotifyItemsStateChanged();
                }

                if (Items.Count > 0 && Items[0].AbstractListView != null) Items[0].AbstractListView.SetContainingTabPageSelected();
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Returns image for the list view item based on its type
        /// </summary>        
        private object GetImageFor(ListViewKeyItem item) {
            ListView ListView = item.ListView;
            if (ListView is ResXSoundsList) {
                return Editor.play;
            } else if (ListView is ResXFilesList) {
                return Editor.doc;
            } else if (ListView is ResXImagesList) {
                return item.DataNode.GetValue<Bitmap>();
            } else {
                return item.DataNode.GetValue<Icon>();
            }
        }

        public override void Redo() {
            try {
                HashSet<AbstractListView> usedLists = new HashSet<AbstractListView>();

                foreach (ListViewKeyItem item in Items) {
                    // re-exclude and re-delete the files
                    if (item.RemoveKind != REMOVEKIND.REMOVE && item.DataNode.FileRef != null && item.NeighborItems != null) {
                        string file = item.DataNode.FileRef.FileName;
                        ProjectItem projItem = VisualLocalizerPackage.Instance.DTE.Solution.FindProjectItem(file);
                        if (projItem != null) {
                            if ((item.RemoveKind & REMOVEKIND.EXCLUDE) == REMOVEKIND.EXCLUDE) {
                                projItem.Remove();
                            } else {
                                projItem.Delete();
                                File.Delete(file);
                            }
                        }
                    }

                    AbstractListView ListView = item.AbstractListView;
                    usedLists.Add(ListView);

                    // unregister the item from the conflict resolver
                    ConflictResolver.TryAdd(item.Key, null, item, Control.Editor.ProjectItem, null);

                    // remove its image
                    if (!string.IsNullOrEmpty(item.ImageKey) && ListView.LargeImageList.Images.ContainsKey(item.ImageKey)) {
                        ListView.LargeImageList.Images.RemoveByKey(item.ImageKey);
                        ListView.SmallImageList.Images.RemoveByKey(item.ImageKey);
                    }

                    // remove the item from the list
                    ListView.Items.Remove(item);
                }

                // update state of list views
                foreach (var usedList in usedLists) {
                    usedList.NotifyDataChanged();
                    usedList.NotifyItemsStateChanged();
                }

                if (Items.Count > 0 && Items[0].AbstractListView != null) Items[0].AbstractListView.SetContainingTabPageSelected();
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        public override string GetUndoDescription() {
            return string.Format("Removed {0} media element(s)", Items.Count);
        }

        public override string GetRedoDescription() {
            return GetUndoDescription();
        }
    }
}
