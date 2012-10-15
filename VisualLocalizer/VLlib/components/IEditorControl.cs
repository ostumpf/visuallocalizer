using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace VisualLocalizer.Library {
    public interface IEditorControl  {

        void Init<T>(AbstractSingleViewEditor<T> editor) where T : Control, IEditorControl, new();
    }
}
