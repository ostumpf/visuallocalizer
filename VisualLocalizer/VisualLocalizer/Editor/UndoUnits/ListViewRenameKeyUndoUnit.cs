using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using VisualLocalizer.Components;
using VisualLocalizer.Commands;

namespace VisualLocalizer.Editor.UndoUnits {

    [Guid("406668D9-6E03-47c8-B838-FA4EE1EF1896")]
    internal sealed class ListViewRenameKeyUndoUnit : RenameKeyUndoUnit {

        private ListViewKeyItem Item { get; set; }
        private AbstractListView ListView { get; set; }
        private ResXEditorControl Control { get; set; }

        public ListViewRenameKeyUndoUnit(ResXEditorControl control, AbstractListView listView, ListViewKeyItem item, string oldKey, string newKey)
            : base(oldKey, newKey) {
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
            try {
                Control.ReferenceCounterThreadSuspended = true;
                Control.UpdateReferencesCount(Item);

                Item.Text = to;
                Item.BeforeEditValue = from;
                Item.AfterEditValue = to;
                ListView.Validate(Item);
                ListView.NotifyDataChanged();

                VLOutputWindow.VisualLocalizerPane.WriteLine("Renamed from \"{0}\" to \"{1}\"", Item.BeforeEditValue, Item.AfterEditValue);
                if (Item.AbstractListView != null) Item.AbstractListView.SetContainingTabPageSelected();

                if (Item.ErrorMessages.Count == 0) {
                    int errors = 0;
                    int count = Item.CodeReferences.Count;
                    Item.CodeReferences.ForEach((i) => { i.KeyAfterRename = to; });

                    BatchReferenceReplacer replacer = new BatchReferenceReplacer(Item.CodeReferences);
                    replacer.Inline(Item.CodeReferences, true, ref errors);

                    VLOutputWindow.VisualLocalizerPane.WriteLine("Renamed {0} key references in code", count);
                }

            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            } finally {
                Control.ReferenceCounterThreadSuspended = false;
            }
        }
    }
}
