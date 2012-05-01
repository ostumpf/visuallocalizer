using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using VisualLocalizer.Library;

namespace VisualLocalizer.Editor {
    [Guid("163D9FB6-68C6-4801-9CA0-3C53241D7855")]
    internal class ResXEditorFactory : EditorFactory<ResXEditor,ResXEditorControl> {
    }

    internal sealed class ResXEditor : MonoEditor<ResXEditorControl> {
        public override string Extension {
            get { return ".resx"; }
        }

        public override Guid EditorFactoryGuid {
            get { return typeof(ResXEditorFactory).GUID; }
        }
    }


}
