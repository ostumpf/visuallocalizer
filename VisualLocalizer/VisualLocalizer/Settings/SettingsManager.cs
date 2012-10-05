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
    internal sealed class SettingsManager : Component, IProfileManager{

        private bool propertyChangedHooked = false;
        private const string REG_KEY = "VisualLocalizerSettings";

        public void LoadSettingsFromStorage() {
            if (!propertyChangedHooked) {
                SettingsObject.Instance.PropertyChanged += new Action(SaveSettingsToStorage);
                propertyChangedHooked = true;
            }

            VisualLocalizerPackage package = VisualLocalizerPackage.Instance;
            RegistryKey rootKey = package.UserRegistryRoot;
            RegistryKey settingsKey = null;
            try {
                settingsKey = rootKey.OpenSubKey(REG_KEY, false);
                if (settingsKey != null) {
                    SettingsObject.Instance.IgnorePropertyChanges = true;
                    SettingsObject.Instance.FilterOutVerbatim = ReadBoolFromRegKey(settingsKey, "FilterOutVerbatim");
                    SettingsObject.Instance.FilterOutUnlocalizable = ReadBoolFromRegKey(settingsKey, "FilterOutUnlocalizable");
                    SettingsObject.Instance.FilterOutCaps = ReadBoolFromRegKey(settingsKey, "FilterOutCaps");
                    SettingsObject.Instance.FilterOutNoLetters = ReadBoolFromRegKey(settingsKey, "FilterOutNoLetters");
                    SettingsObject.Instance.FilterOutSpecificComment = ReadBoolFromRegKey(settingsKey, "FilterOutSpecificComment");

                    SettingsObject.Instance.FilterRegexps.Clear();
                    int count = ReadIntFromRegKey(settingsKey, "RegexpCount");
                    for (int i = 0; i < count; i++) {
                        string regexp = settingsKey.GetValue("RegexpItem" + i).ToString();
                        bool mustMatch = bool.Parse(settingsKey.GetValue("RegexpMustMatch" + i).ToString());
                        SettingsObject.Instance.FilterRegexps.Add(new SettingsObject.RegexpInstance() {
                            MustMatch = mustMatch,
                            Regexp = regexp
                        });
                    }
                    SettingsObject.Instance.NamespacePolicyIndex = ReadIntFromRegKey(settingsKey, "NamespacePolicyIndex");
                    SettingsObject.Instance.MarkNotLocalizableStringsIndex = ReadIntFromRegKey(settingsKey, "MarkNotLocalizableStringsIndex");
                    SettingsObject.Instance.BatchMoveSplitterDistance = ReadIntFromRegKey(settingsKey, "BatchMoveSplitterDistance", 110);

                    SettingsObject.Instance.IgnorePropertyChanges = false;
                    SettingsObject.Instance.NotifySettingsLoaded();
                }
            } finally {
                if (settingsKey != null) settingsKey.Close();
            }
        }

        public void LoadSettingsFromXml(IVsSettingsReader reader) {
            SettingsObject.Instance.IgnorePropertyChanges = true;
            SettingsObject.Instance.FilterOutCaps = ReadBoolFromXml(reader, "FilterOutCaps");
            SettingsObject.Instance.FilterOutNoLetters = ReadBoolFromXml(reader, "FilterOutNoLetters");
            SettingsObject.Instance.FilterOutUnlocalizable = ReadBoolFromXml(reader, "FilterOutUnlocalizable");
            SettingsObject.Instance.FilterOutVerbatim = ReadBoolFromXml(reader, "FilterOutVerbatim");
            SettingsObject.Instance.FilterOutSpecificComment = ReadBoolFromXml(reader, "FilterOutSpecificComment");

            SettingsObject.Instance.MarkNotLocalizableStringsIndex = ReadIntFromXml(reader, "MarkNotLocalizableStringsIndex");
            SettingsObject.Instance.NamespacePolicyIndex = ReadIntFromXml(reader, "NamespacePolicyIndex");
            SettingsObject.Instance.BatchMoveSplitterDistance = ReadIntFromXml(reader, "BatchMoveSplitterDistance");

            int count = ReadIntFromXml(reader, "RegexpCount");
            for (int i = 0; i < count; i++) {
                string regexp;                
                int hr = reader.ReadSettingString("RegexpItem" + i, out regexp);
                if (hr != VSConstants.S_OK) reader.ReportError("RegexpItem value cannot be read", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);

                bool match = ReadBoolFromXml(reader, "RegexpMustMatch" + i);

                SettingsObject.Instance.FilterRegexps.Add(new SettingsObject.RegexpInstance() {
                    MustMatch = match,
                    Regexp = regexp
                });
            }
            SettingsObject.Instance.IgnorePropertyChanges = false;
            SettingsObject.Instance.NotifySettingsLoaded();
        }        

        public void ResetSettings() {
            SettingsObject.Instance.IgnorePropertyChanges = true;

            SettingsObject.Instance.MarkNotLocalizableStringsIndex = 0;
            SettingsObject.Instance.NamespacePolicyIndex = 0;
            SettingsObject.Instance.FilterOutCaps = false;
            SettingsObject.Instance.FilterOutNoLetters = false;
            SettingsObject.Instance.FilterOutUnlocalizable = false;
            SettingsObject.Instance.FilterOutVerbatim = false;
            SettingsObject.Instance.FilterOutSpecificComment = true;
            SettingsObject.Instance.BatchMoveSplitterDistance = 110;

            SettingsObject.Instance.FilterRegexps.Clear();            
            SettingsObject.Instance.IgnorePropertyChanges = false;
            SettingsObject.Instance.NotifyPropertyChanged();
            SettingsObject.Instance.NotifySettingsLoaded();
        }

        public void SaveSettingsToStorage() {
            VisualLocalizerPackage package = VisualLocalizerPackage.Instance;
            RegistryKey rootKey = package.UserRegistryRoot;
            RegistryKey settingsKey = null;
            try {
                settingsKey = rootKey.CreateSubKey(REG_KEY);

                WriteBoolToRegKey(settingsKey, "FilterOutVerbatim", SettingsObject.Instance.FilterOutVerbatim);
                WriteBoolToRegKey(settingsKey, "FilterOutUnlocalizable", SettingsObject.Instance.FilterOutUnlocalizable);
                WriteBoolToRegKey(settingsKey, "FilterOutNoLetters", SettingsObject.Instance.FilterOutNoLetters);
                WriteBoolToRegKey(settingsKey, "FilterOutCaps", SettingsObject.Instance.FilterOutCaps);
                WriteBoolToRegKey(settingsKey, "FilterOutSpecificComment", SettingsObject.Instance.FilterOutSpecificComment);

                int count = ReadIntFromRegKey(settingsKey, "RegexpCount");
                for (int i = 0; i < count; i++) {
                    settingsKey.DeleteValue("RegexpItem" + i, false);
                    settingsKey.DeleteValue("RegexpMustMatch" + i, false);
                }

                WriteIntToRegKey(settingsKey, "RegexpCount", SettingsObject.Instance.FilterRegexps.Count);
                WriteIntToRegKey(settingsKey, "NamespacePolicyIndex", SettingsObject.Instance.NamespacePolicyIndex);
                WriteIntToRegKey(settingsKey, "MarkNotLocalizableStringsIndex", SettingsObject.Instance.MarkNotLocalizableStringsIndex);
                WriteIntToRegKey(settingsKey, "BatchMoveSplitterDistance", SettingsObject.Instance.BatchMoveSplitterDistance);  

                for (int i = 0; i < SettingsObject.Instance.FilterRegexps.Count; i++) {
                    settingsKey.SetValue("RegexpItem" + i, SettingsObject.Instance.FilterRegexps[i].Regexp);
                    settingsKey.SetValue("RegexpMustMatch" + i, SettingsObject.Instance.FilterRegexps[i].MustMatch);
                }
            } finally {
                if (settingsKey != null) settingsKey.Close();
            }
        }

        public void SaveSettingsToXml(IVsSettingsWriter writer) {
            WriteBoolToXml(writer, "FilterOutVerbatim", SettingsObject.Instance.FilterOutVerbatim);
            WriteBoolToXml(writer, "FilterOutUnlocalizable", SettingsObject.Instance.FilterOutUnlocalizable);
            WriteBoolToXml(writer, "FilterOutCaps", SettingsObject.Instance.FilterOutCaps);
            WriteBoolToXml(writer, "FilterOutNoLetters", SettingsObject.Instance.FilterOutNoLetters);
            WriteBoolToXml(writer, "FilterOutSpecificComment", SettingsObject.Instance.FilterOutSpecificComment);

            WriteIntToXml(writer, "RegexpCount", SettingsObject.Instance.FilterRegexps.Count);
            WriteIntToXml(writer, "MarkNotLocalizableStringsIndex", SettingsObject.Instance.MarkNotLocalizableStringsIndex);
            WriteIntToXml(writer, "NamespacePolicyIndex", SettingsObject.Instance.NamespacePolicyIndex);
            WriteIntToXml(writer, "BatchMoveSplitterDistance", SettingsObject.Instance.BatchMoveSplitterDistance);

            for (int i = 0; i < SettingsObject.Instance.FilterRegexps.Count; i++) {
                int hr = writer.WriteSettingString("RegexpItem" + i, SettingsObject.Instance.FilterRegexps[i].Regexp);
                if (hr != VSConstants.S_OK) writer.ReportError("RegexpItem value cannot be written", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);

                WriteBoolToXml(writer, "RegexpMustMatch" + i, SettingsObject.Instance.FilterRegexps[i].MustMatch);                
            }
        }

        private bool ReadBoolFromRegKey(RegistryKey key, string name) {
            object o = key.GetValue(name);
            if (o == null) {
                return false;
            } else {
                return bool.Parse(o.ToString());
            }
        }

        private int ReadIntFromRegKey(RegistryKey key, string name) {
            return ReadIntFromRegKey(key, name, 0);
        }

        private int ReadIntFromRegKey(RegistryKey key, string name, int defaultValue) {
            object o = key.GetValue(name);
            if (o == null) {
                return defaultValue;
            } else {
                return int.Parse(o.ToString());
            }
        }

        private bool ReadBoolFromXml(IVsSettingsReader reader, string name) {
            int p;
            int hr = reader.ReadSettingBoolean(name, out p);
            if (hr != VSConstants.S_OK) reader.ReportError(name + " value cannot be read", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);
            return p == 1;
        }

        private int ReadIntFromXml(IVsSettingsReader reader, string name) {
            int p;
            int hr = reader.ReadSettingLong(name, out p);
            if (hr != VSConstants.S_OK) reader.ReportError(name + " value cannot be read", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);
            return p;
        }

        private void WriteBoolToXml(IVsSettingsWriter writer, string name, bool value) {
            int hr = writer.WriteSettingBoolean(name, value ? 1 : 0);
            if (hr != VSConstants.S_OK) writer.ReportError(name + " value cannot be written", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);
        }

        private void WriteBoolToRegKey(RegistryKey key, string name, bool value) {
            key.SetValue(name, value);
        }

        private void WriteIntToRegKey(RegistryKey key, string name, int value) {
            key.SetValue(name, value);
        }

        private void WriteIntToXml(IVsSettingsWriter writer, string name, int value) {
            int hr = writer.WriteSettingLong(name, value);
            if (hr != VSConstants.S_OK) writer.ReportError(name + " value cannot be written", (uint)__VSSETTINGSERRORTYPES.vsSettingsErrorTypeError);
        }
    }

}
