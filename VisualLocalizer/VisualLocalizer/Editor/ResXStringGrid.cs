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
using VisualLocalizer.Commands.Translate;
using VisualLocalizer.Commands.Inline;

namespace VisualLocalizer.Editor {    

    /// <summary>
    /// Represents String tab in the ResX editor
    /// </summary>
    internal sealed class ResXStringGrid : AbstractResXEditorGrid {

        private ImageMenuItem inlineContextMenu, translateMenu, inlineContextMenuItem, inlineRemoveContextMenuItem;
        public event Action<string, string> LanguagePairAdded;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXStringGrid"/> class.
        /// </summary>        
        public ResXStringGrid(ResXEditorControl editorControl) : base(editorControl) {
            this.editorControl.NewTranslatePairAdded+=new Action<TRANSLATE_PROVIDER>(EditorControl_TranslateRequested);
            this.editorControl.TranslateRequested+=new Action<TRANSLATE_PROVIDER,string,string>(EditorControl_TranslateRequested);
            this.editorControl.InlineRequested += new Action<INLINEKIND>(EditorControl_InlineRequested);            
        }

        /// <summary>
        /// Prepares context menu items and builds the context menu
        /// </summary>        
        protected override ContextMenu BuildContextMenu() {
            ContextMenu contextMenu = base.BuildContextMenu();

            inlineContextMenu = new ImageMenuItem("Inline");

            inlineContextMenuItem = new ImageMenuItem("Inline");
            inlineContextMenuItem.Shortcut = Shortcut.CtrlI;
            inlineContextMenuItem.Click += new EventHandler((o, e) => { EditorControl_InlineRequested(INLINEKIND.INLINE); });

            inlineRemoveContextMenuItem = new ImageMenuItem("Inline & remove");
            inlineRemoveContextMenuItem.Shortcut = Shortcut.CtrlShiftI;
            inlineRemoveContextMenuItem.Click += new EventHandler((o, e) => { EditorControl_InlineRequested(INLINEKIND.INLINE | INLINEKIND.REMOVE); });

            inlineContextMenu.MenuItems.Add(inlineContextMenuItem);
            inlineContextMenu.MenuItems.Add(inlineRemoveContextMenuItem);

            translateMenu = new ImageMenuItem("Translate");
            translateMenu.Image = Editor.translate;
            foreach (ToolStripMenuItem item in editorControl.translateButton.DropDownItems) {
                MenuItem mItem = new MenuItem();
                mItem.Tag = item.Tag;
                mItem.Text = item.Text;
                translateMenu.MenuItems.Add(mItem);
            }

            contextMenu.MenuItems.Add(editContextMenuItem);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(cutContextMenuItem);
            contextMenu.MenuItems.Add(copyContextMenuItem);
            contextMenu.MenuItems.Add(pasteContextMenuItem);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(showResultItemsMenuItem);
            contextMenu.MenuItems.Add(inlineContextMenu);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(translateMenu);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(deleteContextMenuItem);            
            return contextMenu;
        }
      
        #region IDataTabItem members

        /// <summary>
        /// Returns current working data
        /// </summary>
        /// <param name="throwExceptions">False if no exceptions should be thrown on errors (used by reference lookuper thread)</param>
        public override Dictionary<string, ResXDataNode> GetData(bool throwExceptions) {
            EndEdit(); // cancel editting a cell

            Dictionary<string, ResXDataNode> data = new Dictionary<string, ResXDataNode>(RowCount);
            foreach (ResXStringGridRow row in Rows) {
                if (row.IsNewRow) continue;

                if (!string.IsNullOrEmpty(row.ErrorText)) {
                    if (throwExceptions) {
                        throw new Exception(row.ErrorText);
                    } else {
                        if (row.DataSourceItem != null) { // save under fake key (it may be null)
                            string rndFile = Path.GetRandomFileName().CreateIdentifier(LANGUAGE.CSHARP);
                            ResXDataNode newNode = new ResXDataNode(rndFile, row.DataSourceItem.GetValue<string>());
                            
                            // save all data in the comment
                            newNode.Comment = CreateMangledComment(row); // mangles all resource data to comment
                            data.Add(newNode.Name.ToLower(), newNode);
                        }
                    }
                } else if (row.DataSourceItem != null) {
                    if (row.DataSourceItem.GetValue<string>() == null) {
                        string cmt = row.DataSourceItem.Comment;
                        row.DataSourceItem = new ResXDataNode(row.DataSourceItem.Name, "");
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
            return node.HasValue<string>() && !node.HasLinkedFileContent();
        }
       
        #endregion       
        

        #region public members


        /// <summary>
        /// Extracts data from specified list for translation
        /// </summary>        
        public void AddToTranslationList(IEnumerable list, List<AbstractTranslateInfoItem> data) {
            if (list == null) throw new ArgumentNullException("list");
            if (data == null) throw new ArgumentNullException("data");

            foreach (ResXStringGridRow row in list) {
                if (!row.IsNewRow) {
                    if (!string.IsNullOrEmpty(row.Key)) {
                        StringGridTranslationInfoItem item = new StringGridTranslationInfoItem();
                        item.Row = row;
                        item.Value = row.Value;
                        item.ValueColumnName = ValueColumnName;
                        data.Add(item);
                    }
                }
            }
        }

        #endregion

        #region private members


        /// <summary>
        /// Called after editting a cell
        /// </summary>        
        protected override void OnCellEndEdit(DataGridViewCellEventArgs e) {
            try {
                if (e.RowIndex == Rows.Count - 1) return;

                base.OnCellEndEdit(e);

                if (e.ColumnIndex >= 0 && e.RowIndex >= 0) {
                    ResXStringGridRow row = Rows[e.RowIndex] as ResXStringGridRow;
                    bool isNewRow = false;
                    if (row.DataSourceItem == null) { // last empty row was edited, new row has been added
                        isNewRow = true;
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
                            ResXDataNode newNode;
                            if (string.IsNullOrEmpty(key)) {
                                newNode = new ResXDataNode("A", newValue);
                                row.Status = KEY_STATUS.ERROR;
                            } else {
                                newNode = new ResXDataNode(key, newValue);
                                row.Status = KEY_STATUS.OK;
                                row.LastValidKey = key;
                            }

                            newNode.Comment = (string)row.Cells[CommentColumnName].Value;
                            row.DataSourceItem = newNode;
                        }
                    } else if (Columns[e.ColumnIndex].Name == CommentColumnName) { // comment was edited
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
                NotifyItemsStateChanged();
            }
        }

        /// <summary>
        /// Updates context menu item's state (disabled/enabled)
        /// </summary>
        protected override void UpdateContextItemsEnabled() {
            base.UpdateContextItemsEnabled();
            inlineContextMenu.Enabled = SelectedRows.Count >= 1 && !ReadOnly && !IsEditing && AreReferencesKnownOnSelected;
            translateMenu.Enabled = SelectedRows.Count >= 1 && !ReadOnly && !IsEditing;
        }

        /// <summary>
        /// Called before displaying the context menu
        /// </summary>        
        protected override void ContextMenu_Popup(object sender, EventArgs e) {
            try {
                UpdateContextItemsEnabled();

                foreach (MenuItem menuItem in translateMenu.MenuItems) { // for each translation provider
                    menuItem.MenuItems.Clear(); // clear current language pair menu items
                    TRANSLATE_PROVIDER provider = (TRANSLATE_PROVIDER)menuItem.Tag;

                    // if the provider is Bing, AppId is required
                    bool enabled = true;
                    if (provider == TRANSLATE_PROVIDER.BING) {
                        enabled = !string.IsNullOrEmpty(SettingsObject.Instance.BingAppId);
                    }

                    menuItem.Enabled = enabled;

                    // add current language pairs from settings
                    foreach (var pair in SettingsObject.Instance.LanguagePairs) {
                        MenuItem newItem = new MenuItem(pair.ToString());
                        newItem.Tag = pair;
                        newItem.Click += new EventHandler((o, args) => {
                            SettingsObject.LanguagePair sentPair = (o as MenuItem).Tag as SettingsObject.LanguagePair;
                            EditorControl_TranslateRequested(provider, sentPair.FromLanguage, sentPair.ToLanguage);
                        });
                        newItem.Enabled = enabled;
                        menuItem.MenuItems.Add(newItem);
                    }

                    // add option to add a new language pair
                    MenuItem addItem = new MenuItem("New language pair...", new EventHandler((o, args) => {
                        EditorControl_TranslateRequested(provider);
                    }));
                    addItem.Enabled = enabled;
                    menuItem.MenuItems.Add(addItem);
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Called when "New language pair..." menu item was clicked
        /// </summary>        
        private void EditorControl_TranslateRequested(TRANSLATE_PROVIDER provider) {
            try {
                NewLanguagePairWindow win = new NewLanguagePairWindow(true); // select or create new language pair
                if (win.ShowDialog() == DialogResult.OK) {
                    if (win.AddToList && LanguagePairAdded != null) {
                        LanguagePairAdded(win.SourceLanguage, win.TargetLanguage); // add the language pair to the settings list
                    }
                    EditorControl_TranslateRequested(provider, win.SourceLanguage, win.TargetLanguage); // perform translation
                }
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Translate selected rows
        /// </summary>
        /// <param name="provider">Translation provider to handle the process</param>
        /// <param name="from">Source language (can be null)</param>
        /// <param name="to">Target language</param>
        private void EditorControl_TranslateRequested(TRANSLATE_PROVIDER provider, string from, string to) {
            try {                
                List<AbstractTranslateInfoItem> data = new List<AbstractTranslateInfoItem>();
                AddToTranslationList(SelectedRows, data); // collect data to translate

                TranslationHandler.Translate(data, provider, from, to);

                foreach (AbstractTranslateInfoItem item in data) {
                    item.ApplyTranslation(); // modify the editor's data
                }
            } catch (Exception ex) {
                Dictionary<string, string> add = null;
                if (ex is CannotParseResponseException) {
                    CannotParseResponseException cpex = ex as CannotParseResponseException;
                    add = new Dictionary<string, string>();
                    add.Add("Full response:", cpex.FullResponse);
                }

                VLOutputWindow.VisualLocalizerPane.WriteException(ex, add);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex, add);
            }
        }


        /// <summary>
        /// Called from editor when Inline operation is requested
        /// </summary>
        /// <param name="kind">Bitmask of INLINEKIND parameters</param>
        private void EditorControl_InlineRequested(INLINEKIND kind) {
            bool readonlyExists = false;
            foreach (ResXStringGridRow row in SelectedRows) {
                if (row.IsNewRow) continue;

                readonlyExists = readonlyExists || row.CodeReferenceContainsReadonly;
                if (readonlyExists) break;
            }
            if (readonlyExists) {
                VisualLocalizer.Library.Components.MessageBox.ShowError("This operation cannot be executed, because some of the references are located in readonly files.");
                return;
            }

            // show confirmation
            DialogResult result = VisualLocalizer.Library.Components.MessageBox.Show("This operation is irreversible, cannot be undone globally, only using undo managers in open files. Do you want to proceed?",
                null, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND, OLEMSGICON.OLEMSGICON_WARNING);
            
            if (result == DialogResult.Yes) {
                try {
                    editorControl.ReferenceCounterThreadSuspended = true; // suspend reference lookuper thread

                    if ((kind & INLINEKIND.INLINE) == INLINEKIND.INLINE) {
                        editorControl.UpdateReferencesCount((IEnumerable)SelectedRows); // update references for specified rows manually

                        List<CodeReferenceResultItem> totalList = new List<CodeReferenceResultItem>();

                        foreach (ResXStringGridRow row in SelectedRows) { 
                            if (!row.IsNewRow) {
                                totalList.AddRange(row.CodeReferences);
                            }
                        }
                        BatchInliner inliner = new BatchInliner();
                        
                        // run inliner
                        int errors = 0;
                        inliner.Inline(totalList, false, ref errors);
                        VLOutputWindow.VisualLocalizerPane.WriteLine("Inlining of selected rows finished - found {0} references, {1} finished successfuly", totalList.Count, totalList.Count - errors);
                    }
                    
                    if ((kind & INLINEKIND.REMOVE) == INLINEKIND.REMOVE) {
                        // remove the rows if requested
                        RemoveStringsUndoUnit removeUnit = null;
                        EditorControl_RemoveRequested(REMOVEKIND.REMOVE, false, out removeUnit);
                    }

                    StringInlinedUndoItem undoItem = new StringInlinedUndoItem(SelectedRows.Count);
                    editorControl.Editor.AddUndoUnit(undoItem);
                } catch (Exception ex) {
                    VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                    VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
                } finally {
                    editorControl.ReferenceCounterThreadSuspended = false;
                }
            }
        }

        #endregion

    }

   
}
