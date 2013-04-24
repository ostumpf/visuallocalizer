using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using System.Windows.Forms;
using System.Drawing;

namespace VisualLocalizer.Settings {

    /// <summary>
    /// Represents parent of Editor and Filter settings managers (parent node in Tools/Options)
    /// </summary>
    internal sealed class GeneralSettingsManager : AbstractSettingsManager {

        private bool propertyChangedHooked = false;

        /// <summary>
        /// Instance of FilterSettingsManager
        /// </summary>
        private FilterSettingsManager filterManager = new FilterSettingsManager();

        /// <summary>
        /// Instance of EditorSettingsManager
        /// </summary>
        private EditorSettingsManager editorManager = new EditorSettingsManager();

        /// <summary>
        /// Loads settings from registry storage (on package load)
        /// </summary>
        public override void LoadSettingsFromStorage() {
            // add PropertyChanged handler - save changed settings category
            if (!propertyChangedHooked) {
                SettingsObject.Instance.PropertyChanged += new Action<CHANGE_CATEGORY>(Instance_PropertyChanged);
                propertyChangedHooked = true;
            }

             filterManager.LoadSettingsFromStorage();
             editorManager.LoadSettingsFromStorage();
        }

        /// <summary>
        /// Value of settings changed - saves the appropriate portion of settings into the registry
        /// </summary>
        /// <param name="category"></param>
        private void Instance_PropertyChanged(CHANGE_CATEGORY category) {
              if ((category & CHANGE_CATEGORY.FILTER) == CHANGE_CATEGORY.FILTER) filterManager.SaveSettingsToStorage();
              if ((category & CHANGE_CATEGORY.EDITOR) == CHANGE_CATEGORY.EDITOR) editorManager.SaveSettingsToStorage();
        }

        /// <summary>
        /// Blank - settings handled in EditorSettingsManager and FilterSettingsManager
        /// </summary>
        public override void LoadSettingsFromXml(IVsSettingsReader reader) {            
        }

        /// <summary>
        /// Blank - settings handled in EditorSettingsManager and FilterSettingsManager
        /// </summary>
        public override void ResetSettings() {            
        }

        /// <summary>
        /// Blank - settings handled in EditorSettingsManager and FilterSettingsManager
        /// </summary>
        public override void SaveSettingsToStorage() {            
        }

        /// <summary>
        /// Blank - settings handled in EditorSettingsManager and FilterSettingsManager
        /// </summary>
        public override void SaveSettingsToXml(IVsSettingsWriter writer) {            
        }
        
    }
}
