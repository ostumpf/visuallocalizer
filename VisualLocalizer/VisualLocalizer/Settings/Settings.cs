using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Settings {
    internal sealed class SettingsObject {

        public event Action PropertyChanged, SettingsLoaded;

        private static SettingsObject instance;
        private SettingsObject() {
            FilterRegexps = new List<RegexpInstance>();
            FilterOutSpecificComment = true;
        }

        public bool IgnorePropertyChanges { get; set; }

        private int _NamespacePolicyIndex;
        public int NamespacePolicyIndex {
            get {
                return _NamespacePolicyIndex;
            }
            set {
                _NamespacePolicyIndex = value;
                NotifyPropertyChanged();
            }
        }

        private int _MarkNotLocalizableStringsIndex;
        public int MarkNotLocalizableStringsIndex {
            get {
                return _MarkNotLocalizableStringsIndex;
            }
            set {
                _MarkNotLocalizableStringsIndex = value;
                NotifyPropertyChanged();
            }
        }

        private int _BatchMoveSplitterDistance;
        public int BatchMoveSplitterDistance {
            get {
                return _BatchMoveSplitterDistance;
            }
            set {
                _BatchMoveSplitterDistance = value;
                NotifyPropertyChanged();
            }
        }

        private bool _FilterOutVerbatim;
        public bool FilterOutVerbatim {
            get {
                return _FilterOutVerbatim;
            }
            set {
                _FilterOutVerbatim = value;
                NotifyPropertyChanged();
            }
        }

        private bool _FilterOutSpecificComment;
        public bool FilterOutSpecificComment {
            get {
                return _FilterOutSpecificComment;
            }
            set {
                _FilterOutSpecificComment = value;
                NotifyPropertyChanged();
            }
        }

        private bool _FilterOutNoLetters;
        public bool FilterOutNoLetters {
            get {
                return _FilterOutNoLetters;
            }
            set {
                _FilterOutNoLetters = value;
                NotifyPropertyChanged();
            }
        }

        private bool _FilterOutUnlocalizable;
        public bool FilterOutUnlocalizable {
            get {
                return _FilterOutUnlocalizable;
            }
            set {
                _FilterOutUnlocalizable = value;
                NotifyPropertyChanged();
            }
        }

        private bool _FilterOutCaps;
        public bool FilterOutCaps {
            get {
                return _FilterOutCaps;
            }
            set {
                _FilterOutCaps = value;
                NotifyPropertyChanged();
            }
        }

        public List<RegexpInstance> FilterRegexps { get; private set; }

        public static SettingsObject Instance {
            get {
                if (instance == null) instance = new SettingsObject();
                return instance;
            }
        }

        public void NotifyPropertyChanged() {
            if (PropertyChanged != null && !IgnorePropertyChanges) PropertyChanged();
        }

        public void NotifySettingsLoaded() {
            if (SettingsLoaded != null) SettingsLoaded();
        }

        internal sealed class RegexpInstance {
            public string Regexp { get; set; }
            public bool MustMatch { get; set; }
        }
    }
}
