using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using System.Windows.Forms;
using System.Drawing;

namespace VisualLocalizer.Settings {
    internal sealed class GeneralSettingsManager : AbstractSettingsManager {

        private bool propertyChangedHooked = false;
        private FilterSettingsManager filterManager = new FilterSettingsManager();
        private EditorSettingsManager editorManager = new EditorSettingsManager();

        public override void LoadSettingsFromStorage() {
              if (!propertyChangedHooked) {
                  SettingsObject.Instance.PropertyChanged += new Action<CHANGE_CATEGORY>(Instance_PropertyChanged);
                  propertyChangedHooked = true;
              }

             filterManager.LoadSettingsFromStorage();
             editorManager.LoadSettingsFromStorage();
        }

        private void Instance_PropertyChanged(CHANGE_CATEGORY category) {
              if ((category & CHANGE_CATEGORY.FILTER) == CHANGE_CATEGORY.FILTER) filterManager.SaveSettingsToStorage();
              if ((category & CHANGE_CATEGORY.EDITOR) == CHANGE_CATEGORY.EDITOR) editorManager.SaveSettingsToStorage();
        }

        public override void LoadSettingsFromXml(IVsSettingsReader reader) {            
        }

        public override void ResetSettings() {            
        }

        public override void SaveSettingsToStorage() {            
        }

        public override void SaveSettingsToXml(IVsSettingsWriter writer) {            
        }
        
    }
}
