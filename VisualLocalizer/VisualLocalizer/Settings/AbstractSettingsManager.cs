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

    [Guid("4047ABC0-DE2D-43dd-9872-4143ED94C928")]
    internal abstract class AbstractSettingsManager : DialogPage {
        protected const string REG_KEY = "VisualLocalizerSettings";
        protected const string FILTER_KEY = "BatchFilter";        
        protected const string EDITOR_KEY = "Editor";

        public override void LoadSettingsFromStorage() {
        }
        
        public override void LoadSettingsFromXml(IVsSettingsReader reader) {
        }
        
        public override void ResetSettings() {
        }
        
        public override void SaveSettingsToStorage() {
        }
        
        public override void SaveSettingsToXml(IVsSettingsWriter writer) {
        }

        protected bool ReadBoolFromRegKey(RegistryKey key, string name) {
            object o = key.GetValue(name);
            if (o == null) {
                return false;
            } else {
                return bool.Parse(o.ToString());
            }
        }

        protected int ReadIntFromRegKey(RegistryKey key, string name) {
            return ReadIntFromRegKey(key, name, 0);
        }

        protected int ReadIntFromRegKey(RegistryKey key, string name, int defaultValue) {
            object o = key.GetValue(name);
            if (o == null) {
                return defaultValue;
            } else {
                return int.Parse(o.ToString());
            }
        }

        protected bool ReadBoolFromXml(IVsSettingsReader reader, string name) {
            int p;
            int hr = reader.ReadSettingBoolean(name, out p);
            if (hr != VSConstants.S_OK) reader.ReportError(name + " value cannot be read", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);
            return p == 1;
        }

        protected int ReadIntFromXml(IVsSettingsReader reader, string name) {
            int p;
            int hr = reader.ReadSettingLong(name, out p);
            if (hr != VSConstants.S_OK) reader.ReportError(name + " value cannot be read", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);
            return p;
        }

        protected void WriteBoolToXml(IVsSettingsWriter writer, string name, bool value) {
            int hr = writer.WriteSettingBoolean(name, value ? 1 : 0);
            if (hr != VSConstants.S_OK) writer.ReportError(name + " value cannot be written", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);
        }

        protected void WriteBoolToRegKey(RegistryKey key, string name, bool value) {
            key.SetValue(name, value);
        }

        protected void WriteIntToRegKey(RegistryKey key, string name, int value) {
            key.SetValue(name, value);
        }

        protected void WriteIntToXml(IVsSettingsWriter writer, string name, int value) {
            int hr = writer.WriteSettingLong(name, value);
            if (hr != VSConstants.S_OK) writer.ReportError(name + " value cannot be written", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);
        }
    }

}
