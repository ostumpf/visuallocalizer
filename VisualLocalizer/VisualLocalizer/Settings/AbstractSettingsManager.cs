using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;

namespace VisualLocalizer.Settings {

    /// <summary>
    /// Base class for settings editeable in Tools/Options
    /// </summary>
    [Guid("4047ABC0-DE2D-43dd-9872-4143ED94C928")]
    internal abstract class AbstractSettingsManager : DialogPage {
        protected const string REG_KEY = "VisualLocalizerSettings";
        protected const string FILTER_KEY = "BatchFilter";        
        protected const string EDITOR_KEY = "Editor";

        /// <summary>
        /// Loads settings from registry storage (on package load)
        /// </summary>
        public override void LoadSettingsFromStorage() {
        }
        
        /// <summary>
        /// Loads settings from XML (on import settings)
        /// </summary>        
        public override void LoadSettingsFromXml(IVsSettingsReader reader) {
        }
        
        /// <summary>
        /// Never called (bug?)
        /// </summary>
        public override void ResetSettings() {
        }
        
        /// <summary>
        /// Saves settings to registry storage
        /// </summary>
        public override void SaveSettingsToStorage() {
        }
        
        /// <summary>
        /// Saves settings to XML (on settings export)
        /// </summary>        
        public override void SaveSettingsToXml(IVsSettingsWriter writer) {
        }

        /// <summary>
        /// Reads boolean value from registry key value
        /// </summary>        
        protected bool ReadBoolFromRegKey(RegistryKey key, string name) {
            if (key == null) throw new ArgumentNullException("key");
            if (name == null) throw new ArgumentNullException("name");

            object o = key.GetValue(name);
            if (o == null) {
                return false;
            } else {
                return bool.Parse(o.ToString());
            }
        }

        /// <summary>
        /// Reads integer value from registry key value, setting 0 as default value if such key doesn't exist
        /// </summary>        
        protected int ReadIntFromRegKey(RegistryKey key, string name) {
            return ReadIntFromRegKey(key, name, 0);
        }

        /// <summary>
        /// Reads integer value from registry key value, using given default value if such key doesn't exist
        /// </summary>        
        protected int ReadIntFromRegKey(RegistryKey key, string name, int defaultValue) {
            if (key == null) throw new ArgumentNullException("key");
            if (name == null) throw new ArgumentNullException("name");

            object o = key.GetValue(name);
            if (o == null) {
                return defaultValue;
            } else {
                return int.Parse(o.ToString());
            }
        }

        /// <summary>
        /// Reads boolean value from XML settings key
        /// </summary>       
        protected bool ReadBoolFromXml(IVsSettingsReader reader, string name) {
            int p;
            int hr = reader.ReadSettingBoolean(name, out p);
            if (hr != VSConstants.S_OK) reader.ReportError(name + " value cannot be read", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);
            return p == 1;
        }

        /// <summary>
        /// Reads integer value from XML settings key
        /// </summary>
        protected int ReadIntFromXml(IVsSettingsReader reader, string name) {
            int p;
            int hr = reader.ReadSettingLong(name, out p);
            if (hr != VSConstants.S_OK) reader.ReportError(name + " value cannot be read", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);
            return p;
        }

        /// <summary>
        /// Writes boolean value to XML settings key
        /// </summary>
        protected void WriteBoolToXml(IVsSettingsWriter writer, string name, bool value) {
            int hr = writer.WriteSettingBoolean(name, value ? 1 : 0);
            if (hr != VSConstants.S_OK) writer.ReportError(name + " value cannot be written", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);
        }

        /// <summary>
        /// Writes boolean value to registry key
        /// </summary>        
        protected void WriteBoolToRegKey(RegistryKey key, string name, bool value) {
            if (key == null) throw new ArgumentNullException("key");
            if (name == null) throw new ArgumentNullException("name");

            key.SetValue(name, value);
        }

        /// <summary>
        /// Writes integer value to registry key
        /// </summary>        
        protected void WriteIntToRegKey(RegistryKey key, string name, int value) {
            if (key == null) throw new ArgumentNullException("key");
            if (name == null) throw new ArgumentNullException("name");

            key.SetValue(name, value);
        }

        /// <summary>
        /// Writes integer value to XML settings
        /// </summary>        
        protected void WriteIntToXml(IVsSettingsWriter writer, string name, int value) {
            int hr = writer.WriteSettingLong(name, value);
            if (hr != VSConstants.S_OK) writer.ReportError(name + " value cannot be written", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);
        }
    }

}
