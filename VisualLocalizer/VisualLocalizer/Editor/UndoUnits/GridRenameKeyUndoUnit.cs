using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Resources;
using VisualLocalizer.Library;
using VisualLocalizer.Commands;
using VisualLocalizer.Components;
using VisualLocalizer.Library.Components;
using VisualLocalizer.Library.Extensions;

namespace VisualLocalizer.Editor.UndoUnits {

    /// <summary>
    /// Represents action of renaming key of string resource in editor
    /// </summary>
    [Guid("A524A5E7-EF67-4b42-BBB1-25706700A1AD")]
    internal sealed class GridRenameKeyUndoUnit : RenameKeyUndoUnit {

        public GridRenameKeyUndoUnit(ResXStringGridRow sourceRow, ResXEditorControl control, string oldKey, string newKey) 
            : base(oldKey, newKey) {
            if (sourceRow == null) throw new ArgumentNullException("sourceRow");
            if (control == null) throw new ArgumentNullException("control");

            this.SourceRow = sourceRow;
            this.Control = control;
        }

        public ResXStringGridRow SourceRow { get; private set; }
        public ResXEditorControl Control { get; private set; }
       
        public override void Undo() {                       
            UpdateSourceReferences(NewKey, OldKey);
        }

        public override void Redo() {
            UpdateSourceReferences(OldKey, NewKey);
        }

        private void UpdateSourceReferences(string from, string to) {
            if (SourceRow.CodeReferenceContainsReadonly) {
                throw new Exception("This operation cannot be executed, because some of the references are located in readonly files.");                
            }
            if (Control.Editor.ReadOnly) throw new Exception("Cannot perform this operation - the document is readonly.");

            try {                
                // suspend the reference lookuper thread
                Control.ReferenceCounterThreadSuspended = true;
                Control.UpdateReferencesCount(SourceRow);

                ChangeColumnValue(from, to);

                // if the rows has no errors, perform pseudo-refactoring
                if (SourceRow.ConflictItems.Count == 0 && !string.IsNullOrEmpty(to)) {
                    int errors = 0;
                    int count = SourceRow.CodeReferences.Count;
                    SourceRow.CodeReferences.ForEach((item) => { item.KeyAfterRename = to.CreateIdentifier(Control.Editor.ProjectItem.DesignerLanguage); });

                    try {
                        Control.ReferenceCounterThreadSuspended = true;
                        BatchReferenceReplacer replacer = new BatchReferenceReplacer();
                        replacer.Inline(SourceRow.CodeReferences, true, ref errors);
                    } finally {
                        Control.ReferenceCounterThreadSuspended = false;
                    }

                    VLOutputWindow.VisualLocalizerPane.WriteLine("Renamed {0} key references in code", count);
                }
                
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                MessageBox.ShowException(ex);
            } finally {
                Control.ReferenceCounterThreadSuspended = false;
            }
        }

        private void ChangeColumnValue(string from, string to) {
            AbstractResXEditorGrid grid = (AbstractResXEditorGrid)SourceRow.DataGridView;
            SourceRow.Cells[grid.KeyColumnName].Tag = from;
            SourceRow.Cells[grid.KeyColumnName].Value = to;
            grid.ValidateRow(SourceRow);
            grid.NotifyDataChanged();
            grid.SetContainingTabPageSelected();

            if (SourceRow.ErrorMessages.Count > 0) {
                SourceRow.Status = KEY_STATUS.ERROR;
            } else {
                SourceRow.Status = KEY_STATUS.OK;
                SourceRow.DataSourceItem.Name = SourceRow.Key;
                SourceRow.LastValidKey = SourceRow.Key;
            }
        }
    }
}
