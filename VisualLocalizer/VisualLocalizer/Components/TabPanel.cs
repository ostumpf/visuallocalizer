using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace VisualLocalizer.Components {
    internal sealed class TabPanel : Panel {

        public TabPanel() {
            Tabs = new List<TabPanelItem>();
            
        }

        public List<TabPanelItem> Tabs {
            get;
            private set;
        }

    }

    internal sealed class TabPanelItem {

        public string Name {
            get;
            set;
        }

        public Control Control {
            get;
            set;
        }
    }
}
