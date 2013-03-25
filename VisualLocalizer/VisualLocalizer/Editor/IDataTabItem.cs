using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;
using VisualLocalizer.Library;

namespace VisualLocalizer.Editor {
    /// <summary>
    /// Common interface for all controls handling various resources (by types)
    /// </summary>
    internal interface IDataTabItem {

        /// <summary>
        /// Issued when data changed in GUI and the document should be marked dirty
        /// </summary>
        event EventHandler DataChanged;

        /// <summary>
        /// Issued when selected items collection changed and certain GUI elements should be enabled/disabled
        /// </summary>
        event EventHandler ItemsStateChanged;
        
        /// <summary>
        /// Returns current working data
        /// </summary>
        /// <param name="throwExceptions">False if no exceptions should be thrown on errors (used by reference lookuper thread)</param>        
        Dictionary<string, ResXDataNode> GetData(bool throwExceptions);

        /// <summary>
        /// Returns true if given node's type matches the type of items this control holds
        /// </summary>        
        bool CanContainItem(ResXDataNode node);

        /// <summary>
        /// Begins batch adding items
        /// </summary>
        void BeginAdd();

        /// <summary>
        /// Adds given resource to the control
        /// </summary>        
        IKeyValueSource Add(string key, ResXDataNode value);

        /// <summary>
        /// Ends batch adding items and refreshes the view
        /// </summary>
        void EndAdd();

        /// <summary>
        /// Returns status for Cut and Copy commands, based on currently selected items
        /// </summary>
        COMMAND_STATUS CanCutOrCopy { get; }

        /// <summary>
        /// Returns status for Paste command, based on currently selected items
        /// </summary>
        COMMAND_STATUS CanPaste { get; }

        /// <summary>
        /// Performs Copy command
        /// </summary>        
        bool Copy();

        /// <summary>
        /// Performs Cut command
        /// </summary>
        bool Cut();

        /// <summary>
        /// Performs Select All command
        /// </summary>
        bool SelectAllItems();

        /// <summary>
        /// Returns true if there are selected items in this list
        /// </summary>
        bool HasSelectedItems { get; }

        /// <summary>
        /// Returns true if this list is not empty
        /// </summary>
        bool HasItems { get; }

        /// <summary>
        /// Returns true if a resource is being edited
        /// </summary>
        bool IsEditing { get; }

        /// <summary>
        /// Gets/sets whether this control is readonly
        /// </summary>
        bool DataReadOnly { get; set; }

        /// <summary>
        /// Fires DataChanged() event
        /// </summary>
        void NotifyDataChanged();

        /// <summary>
        /// Fires ItemsStateChanged() event
        /// </summary>
        void NotifyItemsStateChanged();

        /// <summary>
        /// Selects this tab
        /// </summary>
        void SetContainingTabPageSelected();
    }
}
