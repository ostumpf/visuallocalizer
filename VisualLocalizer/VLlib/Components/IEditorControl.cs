using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace VisualLocalizer.Library.Components {

    /// <summary>
    /// Interface implemented by those controls that represent GUI of an editor
    /// </summary>
    public interface IEditorControl  {

        /// <summary>
        /// Initializes instance of the editor
        /// </summary>
        /// <typeparam name="T">Type of the editor GUI control</typeparam>
        /// <param name="editor">Editor instance</param>
        void Init<T>(AbstractSingleViewEditor<T> editor) where T : Control, IEditorControl, new();
    }
}
