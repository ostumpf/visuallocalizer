using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;
using VisualLocalizer.Library;

namespace VisualLocalizer.Editor {
    internal interface IDataTabItem {

        event EventHandler DataChanged;
        event EventHandler ItemsStateChanged;
        
        Dictionary<string, ResXDataNode> GetData(bool throwExceptions);
        bool CanContainItem(ResXDataNode node);
        void BeginAdd();
        IKeyValueSource Add(string key, ResXDataNode value, bool showThumbnails);
        void EndAdd();
        COMMAND_STATUS CanCutOrCopy { get; }
        COMMAND_STATUS CanPaste { get; }
        bool Copy();
        bool Cut();
        bool SelectAllItems();
        bool HasSelectedItems { get; }
        bool HasItems { get; }
        bool IsEditing { get; }
        bool DataReadOnly { get; set; }
        void NotifyDataChanged();
        void NotifyItemsStateChanged();
        void SetContainingTabPageSelected();
    }
}
