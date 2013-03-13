using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace VisualLocalizer.Library {

    /// <summary>
    /// Interface implemented by those controls that represent GUI of an editor
    /// </summary>
    public interface IEditorControl  {
        void Init<T>(AbstractSingleViewEditor<T> editor) where T : Control, IEditorControl, new();
    }
}
