using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using VisualLocalizer.Components;
using VisualLocalizer.Commands;
using VisualLocalizer.Library;
using VisualLocalizer.Library.Extensions;
using VisualLocalizer.Commands.Inline;

namespace VisualLocalizer.Editor.UndoUnits {

    /// <summary>
    /// Undo unit for key rename action in list view
    /// </summary>
    [Guid("406668D9-6E03-47c8-B838-FA4EE1EF1896")]
    internal sealed class ListViewRenameKeyUndoUnit : RenameKeyUndoUnit {

        private ListViewKeyItem Item { get; set; }
        private AbstractListView ListView { get; set; }
        private ResXEditorControl Control { get; set; }

        public ListViewRenameKeyUndoUnit(ResXEditorControl control, AbstractListView listView, ListViewKeyItem item, string oldKey, string newKey)
            : base(oldKey, newKey) {
            if (control == null) throw new ArgumentNullException("control");
            if (listView == null) throw new ArgumentNullException("listView");
            if (item == null) throw new ArgumentNullException("item");

            this.Item = item;
            this.ListView = listView;
            this.Control = control;
        }

        public override void Undo() {            
            UpdateSourceReferences(NewKey, OldKey);
        }

        public override void Redo() {
            UpdateSourceReferences(OldKey, NewKey);
        }

        private void UpdateSourceReferences(string from, string to) {
            if (Item.CodeReferenceContainsReadonly) {
                throw new Exception("This operation cannot be executed, because some of the references are located in readonly files.");                
            }
            if (Control.Editor.ReadOnly) throw new Exception("Cannot perform this operation - the document is readonly.");

            try {                
                // suspend reference lookuper thread and update references for the item
                Control.ReferenceCounterThreadSuspended = true;
                Control.UpdateReferencesCount(Item);

                // change the keys
                Item.Text = to;
                Item.BeforeEditKey = from;
                Item.AfterEditKey = to;
                ListView.Validate(Item);
                ListView.NotifyDataChanged();

                if (Item.ErrorMessages.Count > 0) {
                    Item.Status = KEY_STATUS.ERROR;
                } else {
                    Item.Status = KEY_STATUS.OK;
                    Item.DataNode.Name = Item.Key;
                    Item.LastValidKey = Item.Key;
                }

                VLOutputWindow.VisualLocalizerPane.WriteLine("Renamed from \"{0}\" to \"{1}\"", Item.BeforeEditKey, Item.AfterEditKey);
                if (Item.AbstractListView != null) Item.AbstractListView.SetContainingTabPageSelected();

                // if item has no errors, perform pseudo-refactoring
                if (Item.ConflictItems.Count == 0 && !string.IsNullOrEmpty(to)) {
                    int errors = 0;
                    int count = Item.CodeReferences.Count;
                    Item.CodeReferences.ForEach((i) => { i.KeyAfterRename = to.CreateIdentifier(Control.Editor.ProjectItem.DesignerLanguage); });

                    try {
                        Control.ReferenceCounterThreadSuspended = true;
                        BatchReferenceReplacer replacer = new BatchReferenceReplacer();
                        replacer.Inline(Item.CodeReferences, true, ref errors);
                    } finally {
                        Control.ReferenceCounterThreadSuspended = false;
                    }

                    VLOutputWindow.VisualLocalizerPane.WriteLine("Renamed {0} key references in code", count);
                }

            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            } finally {
                Control.ReferenceCounterThreadSuspended = false;
            }
        }
    }
}
