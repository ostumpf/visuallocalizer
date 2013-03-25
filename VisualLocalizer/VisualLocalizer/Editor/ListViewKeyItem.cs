using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Resources;
using System.Drawing;
using VisualLocalizer.Library;
using EnvDTE;
using VisualLocalizer.Components;

namespace VisualLocalizer.Editor {

    /// <summary>
    /// Represents resource item with image, icon, sound or file value, displayed in ResX editor.
    /// </summary>
    internal sealed class ListViewKeyItem : ListViewItem, IReferencableKeyValueSource {
        
        /// <summary>
        /// Background color displayed in case of error
        /// </summary>
        private Color ErrorColor = Color.FromArgb(255, 213, 213);

        public ListViewKeyItem(AbstractListView parent) {
            ItemsWithSameKey = new List<IKeyValueSource>();
            ConflictItems = new HashSet<IKeyValueSource>();
            ErrorMessages = new HashSet<string>();
            FileRefOk = true;
            CodeReferences = new List<CodeReferenceResultItem>();
            this.AbstractListView = parent;
        }

        /// <summary>
        /// Holds this item's content
        /// </summary>
        public ResXDataNode DataNode { get; set; }

        /// <summary>
        /// Value of the key before editting
        /// </summary>
        public string BeforeEditKey { get; set; }

        /// <summary>
        /// Value of the key after editting
        /// </summary>
        public string AfterEditKey { get; set; }

        /// <summary>
        /// Parent list view
        /// </summary>
        public AbstractListView AbstractListView { get; private set; }

        private bool _FileRefOk;

        /// <summary>
        /// True if this resource is external and FileRef points to existing file
        /// </summary>
        public bool FileRefOk {
            get {
                return _FileRefOk;
            }
            set {
                _FileRefOk = value;
                UpdateErrorSetDisplay();
            }
        }       

        public string Key {
            get {
                return this.Text;
            }
        }

        /// <summary>
        /// ListView items do not have string values
        /// </summary>
        public string Value {
            get { return null; }
        }
    
        public int IndexAtDeleteTime { get; set; }
        public REMOVEKIND RemoveKind { get; set; }
        public ProjectItems NeighborItems { get; set; }

        /// <summary>
        /// Items with the same key
        /// </summary>
        public List<IKeyValueSource> ItemsWithSameKey { get; set; }

        /// <summary>
        /// Items that are in conflict with this item (have the same key and possibly different values)
        /// </summary>
        public HashSet<IKeyValueSource> ConflictItems { get; private set; }

        /// <summary>
        /// Returns error messages associated with this item
        /// </summary>
        public HashSet<string> ErrorMessages { get; private set; }

        /// <summary>
        /// Updates display of errors for this item (called after change in ErrorMessages)
        /// </summary>
        public void UpdateErrorSetDisplay() {
            if (!FileRefOk) {
                this.BackColor = ErrorColor;
                this.ToolTipText = string.Format("Referenced file \"{0}\" does not exist", DataNode.FileRef != null ? DataNode.FileRef.FileName : "(null)");
            } else {
                if (ErrorMessages.Count > 0) {
                    this.ToolTipText = ErrorMessages.First();
                    this.BackColor = ErrorColor;
                } else {
                    this.ToolTipText = null;
                    this.BackColor = Color.White;
                }
            }          
        }

        /// <summary>
        /// List of references to the resource in code (used to display number and to enable key renaming)
        /// </summary>
        public List<CodeReferenceResultItem> CodeReferences {
            get;
            set;
        }

        /// <summary>
        /// Updates display of the references count
        /// </summary>
        /// <param name="determinated">True if number of references was successfuly calculated</param>
        public void UpdateReferenceCount(bool determinated) {
            ListView.Invoke(new Action<string>((s) => SubItems["References"].Text = s),
                ErrorMessages.Count == 0 && determinated ? CodeReferences.Count.ToString() : "?");             
        }
    }
}
