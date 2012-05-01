using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;

namespace VisualLocalizer.Editor {
    internal sealed class ResXProjectItem {

        public ResXProjectItem(ProjectItem projectItem, string displayName) {
            this.DisplayName = displayName;
            this.ProjectItem = projectItem;
        }

        public ProjectItem ProjectItem {
            get;
            private set;
        }

        public string DisplayName {
            get;
            private set;
        }

        public override string ToString() {
            return DisplayName;
        }
    }
}
