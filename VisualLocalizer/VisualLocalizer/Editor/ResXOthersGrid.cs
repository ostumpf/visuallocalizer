using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Gui;
using VisualLocalizer.Components;
using System.Windows.Forms;
using System.Resources;
using System.ComponentModel.Design;
using VisualLocalizer.Library;
using System.IO;
using VisualLocalizer.Editor.UndoUnits;
using VisualLocalizer.Translate;
using System.Globalization;
using VisualLocalizer.Settings;
using VisualLocalizer.Commands;
using EnvDTE;
using System.Collections;
using System.Drawing;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using VisualLocalizer.Extensions;
using VisualLocalizer.Components.Code;
using VisualLocalizer.Library.Gui;
using VisualLocalizer.Library.Components;
using VisualLocalizer.Library.Extensions;
using System.Runtime.Serialization.Formatters.Binary;
using System.ComponentModel;

namespace VisualLocalizer.Editor {    

    /// <summary>
    /// Represents Others tab in the ResX editor
    /// </summary>
    internal sealed class ResXOthersGrid : AbstractResXEditorGrid {
        /// <summary>
        /// Error displayed when the type cannot parse given value
        /// </summary>
        private const string TYPE_ERROR = "Invalid type and value combination";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXOthersGrid"/> class.
        /// </summary>        
        public ResXOthersGrid(ResXEditorControl editorControl)
            : base(editorControl) {

            ResXOthersGridRow rowTemplate = new ResXOthersGridRow();
            rowTemplate.MinimumHeight = 24;
            this.RowTemplate = rowTemplate;
        }

        /// <summary>
        /// Prepares context menu items and builds the context menu
        /// </summary>        
        protected override ContextMenu BuildContextMenu() {
            ContextMenu contextMenu = base.BuildContextMenu();            

            contextMenu.MenuItems.Add(editContextMenuItem);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(cutContextMenuItem);
            contextMenu.MenuItems.Add(copyContextMenuItem);
            contextMenu.MenuItems.Add(pasteContextMenuItem);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(showResultItemsMenuItem);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(deleteContextMenuItem);            
            return contextMenu;
        }

        /// <summary>
        /// Initializes grid columns
        /// </summary>
        protected override void InitializeColumns() {
            base.InitializeColumns();

            ignoreColumnWidthChange = true;

            DataGridViewTextBoxColumn typeColumn = new DataGridViewTextBoxColumn();
            typeColumn.MinimumWidth = 50;
            typeColumn.Width = 180;
            typeColumn.HeaderText = "Type";
            typeColumn.Name = TypeColumnName;
            typeColumn.Frozen = false;
            typeColumn.SortMode = DataGridViewColumnSortMode.Automatic;
            this.Columns.Insert(1, typeColumn);

            ignoreColumnWidthChange = false;
        }

        #region IDataTabItem members

        /// <summary>
        /// Returns current working data
        /// </summary>
        /// <param name="throwExceptions">False if no exceptions should be thrown on errors (used by reference lookuper thread)</param>
        public override Dictionary<string, ResXDataNode> GetData(bool throwExceptions) {
            EndEdit(); // cancel editting a cell

            Dictionary<string, ResXDataNode> data = new Dictionary<string, ResXDataNode>(RowCount);
            foreach (ResXOthersGridRow row in Rows) {
                if (row.IsNewRow) continue;

                if (!string.IsNullOrEmpty(row.ErrorText)) {
                    if (throwExceptions) {
                        throw new Exception(row.ErrorText);
                    } else {
                        if (row.DataSourceItem != null) { // save under fake key (it may be null)
                            string rndFile = Path.GetRandomFileName().CreateIdentifier(LANGUAGE.CSHARP);
                            ResXDataNode newNode = new ResXDataNode(rndFile, row.DataSourceItem.GetValue((ITypeResolutionService)null));
                            
                            // save all data in the comment
                            newNode.Comment = CreateMangledComment(row); // mangles all resource data to comment
                            data.Add(newNode.Name.ToLower(), newNode);
                        }
                    }
                } else if (row.DataSourceItem != null) {
                    if (row.DataSourceItem.GetValue((ITypeResolutionService)null) == null) {
                        string cmt = row.DataSourceItem.Comment;
                        row.DataSourceItem = new ResXDataNode(row.DataSourceItem.Name, Activator.CreateInstance(row.DataType));
                        row.DataSourceItem.Comment = cmt;
                    }
                    data.Add(row.DataSourceItem.Name.ToLower(), row.DataSourceItem);
                    if (!CanContainItem(row.DataSourceItem) && throwExceptions) 
                        throw new Exception("Error saving '" + row.DataSourceItem.Name + "' - cannot preserve type."); 
                }
            }

            return data;
        }

        /// <summary>
        /// Returns true if given node's type matches the type of items this control holds
        /// </summary>
        public override bool CanContainItem(ResXDataNode node) {
            if (node == null) throw new ArgumentNullException("node");
            return !node.HasLinkedFileContent();
        }

        /// <summary>
        /// Adds given resource to the control
        /// </summary>   
        public override IKeyValueSource Add(string key, ResXDataNode value) {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

            ResXOthersGridRow row = new ResXOthersGridRow();
            PopulateRow(row, value);

            Rows.Add(row);
            Validate(row);

            if (row.ErrorMessages.Count > 0) {
                row.Status = KEY_STATUS.ERROR;
            } else {
                row.Status = KEY_STATUS.OK;
                row.LastValidKey = row.Key;
            }

            return row;
        }

        #endregion                      

        #region public members

        /// <summary>
        /// Adds given clipboard text to the grid - values separated with , and rows separated with ; format is expected
        /// </summary>
        public override void AddClipboardText(string text, bool isCsv) {
            if (text == null) throw new ArgumentNullException("text");

            string[] rows = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries); // get the rows
            List<ResXStringGridRow> addedRows = new List<ResXStringGridRow>();
            foreach (string row in rows) {
                string[] columns = row.Split(isCsv ? ';' : '\t'); // get the columns
                if (columns.Length == 0) continue;

                string key = columns.Length >= 1 ? columns[0].CreateIdentifier(editorControl.Editor.ProjectItem.DesignerLanguage) : ""; // modify key so that is a valid identifier
                string assemblyQualifiedTypeName = columns.Length >= 2 ? columns[1] : "";
                string value = columns.Length >= 3 ? columns[2] : "";
                string comment = columns.Length >= 4 ? columns[3] : "";

                Type type = Type.GetType(assemblyQualifiedTypeName);
                ResXDataNode node = new ResXDataNode(key, TypeDescriptor.GetConverter(type).ConvertFromString(value)); // create new resource
                node.Comment = comment;

                ResXOthersGridRow newRow = Add(key, node) as ResXOthersGridRow; // add a row with the resource
                newRow.DataType = type;
                addedRows.Add(newRow);
            }

            if (addedRows.Count > 0) {
                NewRowsAdded(addedRows);
                NotifyDataChanged();
                NotifyItemsStateChanged();

                VLOutputWindow.VisualLocalizerPane.WriteLine("Added {0} new rows from clipboard", addedRows.Count);
            }
        }

        /// <summary>
        /// Copies selected rows to clipboard, using both CSV format and tab-separated format in order to cooperate with Excel and text editors
        /// </summary>
        public override bool Copy() {
            DataObject dataObject = new DataObject();

            StringBuilder tabbedContent = new StringBuilder();
            StringBuilder csvContent = new StringBuilder();
            foreach (ResXOthersGridRow row in SelectedRows) {
                if (row.IsNewRow) continue;
                tabbedContent.AppendFormat("{0}\t{1}\t{2}\t{3}" + Environment.NewLine, (string)row.Cells[KeyColumnName].Value, row.DataType.AssemblyQualifiedName, (string)row.Cells[ValueColumnName].Value, (string)row.Cells[CommentColumnName].Value);
                csvContent.AppendFormat("{0};{1};{2};{3}" + Environment.NewLine, (string)row.Cells[KeyColumnName].Value, row.DataType.AssemblyQualifiedName, (string)row.Cells[ValueColumnName].Value, (string)row.Cells[CommentColumnName].Value);
            }
            dataObject.SetText(tabbedContent.ToString());

            MemoryStream ms = new MemoryStream(Encoding.Default.GetBytes(csvContent.ToString()));
            dataObject.SetData(DataFormats.CommaSeparatedValue, ms);

            Clipboard.SetDataObject(dataObject, true);
            return true;
        }

        /// <summary>
        /// Returns name of the "Type" column
        /// </summary>
        public string TypeColumnName {
            get { return "Type"; }
        }

        #endregion

        #region private members

        /// <summary>
        /// Serializes resource data to a string
        /// </summary>
        protected override string CreateMangledComment(ResXStringGridRow r) {
            if (r == null) throw new ArgumentNullException("row");
            ResXOthersGridRow row = (ResXOthersGridRow)r;

            return string.Format("@@@{0}-@-{1}-@-{2}-@-{3}-@-{4}", (int)row.Status, row.DataSourceItem.Name, row.DataSourceItem.Comment, row.Value, row.DataType.AssemblyQualifiedName);
        }

        /// <summary>
        /// Parses given string created by CreateMangledComment() method and returns stored data
        /// </summary>
        protected override string[] GetMangledCommentData(string comment) {
            if (comment == null) throw new ArgumentNullException("comment");

            string p = comment.Substring(3); // remove @@@
            string[] data = p.Split(new string[] { "-@-" }, StringSplitOptions.None);
            if (data.Length != 5) throw new InvalidOperationException("Mangled comment is invalid: " + comment);

            return data;
        }

        /// <summary>
        /// Called before editting a cell - checks whether a type cell is edited and if so, displays dialog. Otherwise calls base.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnCellBeginEdit(DataGridViewCellCancelEventArgs e) {
            try {
                if (Columns[e.ColumnIndex].Name == TypeColumnName) { // type column is edited
                    e.Cancel = true;
                    ResXOthersGridRow row = Rows[e.RowIndex] as ResXOthersGridRow;

                    // display type-selecting dialog
                    TypeSelectorForm form = new TypeSelectorForm();
                    form.OriginalType = row.DataType;

                    DialogResult result = form.ShowDialog();
                    if (result == DialogResult.OK) {   
                        // update underlying data
                        Type oldType=row.DataType;
                        row.DataType = form.ResultType;
                        row.Cells[TypeColumnName].Value = form.ResultType.FullName;
                        TypeColumnChanged(row, oldType, row.DataType);

                        NotifyItemsStateChanged();
                        NotifyDataChanged();

                        // check for errors and update node
                        Validate(row);
                    }
                } else { // call standard cell edit procedure
                    base.OnCellBeginEdit(e);
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Called after editting a cell
        /// </summary>        
        protected override void OnCellEndEdit(DataGridViewCellEventArgs e) {
            ResXOthersGridRow row = null;
            try {
                if (e.RowIndex == Rows.Count - 1) return;                

                base.OnCellEndEdit(e);

                if (e.ColumnIndex >= 0 && e.RowIndex >= 0) {
                    row = Rows[e.RowIndex] as ResXOthersGridRow;
                    
                    bool isNewRow = false;
                    if (row.DataSourceItem == null) { // last empty row was edited, new row has been added
                        isNewRow = true;
                        row.DataType = typeof(string);
                        row.Cells[TypeColumnName].Value = row.DataType.FullName;
                        row.DataSourceItem = new ResXDataNode("(new)", string.Empty);
                    }
                    ResXDataNode node = row.DataSourceItem;

                    if (Columns[e.ColumnIndex].Name == KeyColumnName) { // key was edited
                        string newKey = (string)row.Cells[KeyColumnName].Value;

                        if (isNewRow) {
                            SetNewKey(row, newKey);
                            row.Cells[ReferencesColumnName].Value = "?";
                            NewRowAdded(row);
                            NotifyDataChanged();
                        } else if (string.Compare(newKey, node.Name) != 0) {
                            // key has changed
                            KeyRenamed(row, newKey);
                            SetNewKey(row, newKey);
                            NotifyDataChanged();
                        }
                    } else if (Columns[e.ColumnIndex].Name == ValueColumnName) { // value was edited
                        string newValue = (string)row.Cells[ValueColumnName].Value;
                        if (newValue == null) newValue = string.Empty;

                        if (isNewRow) {
                            row.Status = KEY_STATUS.ERROR;
                            row.Cells[ReferencesColumnName].Value = "?";
                            NewRowAdded(row);
                            NotifyDataChanged();
                        } else if (string.Compare(newValue, node.GetValue<string>()) != 0) {
                            // value has changed
                            ValueChanged(row, node.GetValue<string>(), newValue);
                            NotifyDataChanged();

                            string key = (string)row.Cells[KeyColumnName].Value;
                            ResXDataNode newNode = null;
                            try {
                                if (string.IsNullOrEmpty(key)) {
                                    newNode = new ResXDataNode("A", TypeDescriptor.GetConverter(row.DataType).ConvertFromString(newValue));
                                    row.Status = KEY_STATUS.ERROR;
                                } else {
                                    newNode = new ResXDataNode(key, TypeDescriptor.GetConverter(row.DataType).ConvertFromString(newValue));
                                    row.Status = KEY_STATUS.OK;
                                    row.LastValidKey = key;
                                }
                            } catch {                                
                            }
                            
                            if (newNode != null) {
                                newNode.Comment = (string)row.Cells[CommentColumnName].Value;
                                row.DataSourceItem = newNode;
                            }
                        }
                    } else { // comment was edited
                        string newComment = (string)row.Cells[CommentColumnName].Value;
                        if (isNewRow) {
                            row.Status = KEY_STATUS.ERROR;
                            row.Cells[ReferencesColumnName].Value = "?";
                            NewRowAdded(row);
                            NotifyDataChanged();
                        } else if (string.Compare(newComment, node.Comment) != 0) {
                            CommentChanged(row, node.Comment, newComment);
                            NotifyDataChanged();

                            node.Comment = newComment;
                        }
                    }
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            } finally {
                editorControl.ReferenceCounterThreadSuspended = false;
                if (row != null) row.UpdateErrorSetDisplay();
                NotifyItemsStateChanged();
            }
        }

        /// <summary>
        /// Populates given row with data from given ResX node
        /// </summary>
        protected override void PopulateRow(ResXStringGridRow row, ResXDataNode node) {
            if (row == null) throw new ArgumentNullException("row");
            if (node == null) throw new ArgumentNullException("node");

            string name, value, comment;
            Type type;

            if (node.Comment.StartsWith("@@@")) { // it's a mangled comment (row was not valid when saving)
                string[] data = GetMangledCommentData(node.Comment);

                row.Status = (KEY_STATUS)int.Parse(data[0]);
                name = data[1];
                comment = data[2];
                value = data[3];
                type = Type.GetType(data[4]);

                // set key
                if (row.Status == KEY_STATUS.OK) {
                    node.Name = name;
                } else {
                    name = string.Empty;
                }

                node = new ResXDataNode(name, value);
                node.Comment = comment;
            } else { // the node is ok
                name = node.Name;
                value = node.GetValue<string>();
                comment = node.Comment;
                type = node.GetValue((ITypeResolutionService)null).GetType();
            }

            DataGridViewTextBoxCell keyCell = new DataGridViewTextBoxCell();
            keyCell.Value = name;

            DataGridViewTextBoxCell typeCell = new DataGridViewTextBoxCell();
            typeCell.Value = type.FullName;
            
            DataGridViewTextBoxCell valueCell = new DataGridViewTextBoxCell();
            valueCell.Value = value;

            DataGridViewTextBoxCell commentCell = new DataGridViewTextBoxCell();
            commentCell.Value = comment;

            DataGridViewTextBoxCell referencesCell = new DataGridViewTextBoxCell();
            referencesCell.Value = "?";

            row.Cells.Add(keyCell);
            row.Cells.Add(typeCell);
            row.Cells.Add(valueCell);
            row.Cells.Add(commentCell);
            row.Cells.Add(referencesCell);
            row.DataSourceItem = node;

            referencesCell.ReadOnly = true;
            row.MinimumHeight = 25;
                      
            ((ResXOthersGridRow)row).DataType = type;
        }

        /// <summary>
        /// Validates the specified row
        /// </summary>
        protected override void Validate(DataGridViewKeyValueRow<ResXDataNode> r) {
            ResXOthersGridRow row = (ResXOthersGridRow)r;            
            TypeConverter converter = null;

            try {
                row.ErrorMessages.Remove(TYPE_ERROR);
                
                converter = TypeDescriptor.GetConverter(row.DataType);
                converter.ConvertFromString((string)row.Cells[ValueColumnName].Value);                
            } catch {
                row.ErrorMessages.Add(TYPE_ERROR);
            }

            base.Validate(row);
        }

        /// <summary>
        /// Called after type was changed - adds undo unit
        /// </summary> 
        private void TypeColumnChanged(ResXOthersGridRow row, Type oldValue, Type newValue) {
            string key = row.Status == KEY_STATUS.ERROR ? null : row.DataSourceItem.Name;
            OthersChangeTypeUndoUnit unit = new OthersChangeTypeUndoUnit(row, this, key, oldValue, newValue, (string)row.Cells[TypeColumnName].Value, row.DataSourceItem.Comment);
            editorControl.Editor.AddUndoUnit(unit);

            VLOutputWindow.VisualLocalizerPane.WriteLine("Edited type of \"{0}\"", key);
        }

        #endregion

     
    }

   
}
