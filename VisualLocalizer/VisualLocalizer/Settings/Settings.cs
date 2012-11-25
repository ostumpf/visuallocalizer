using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Settings {
    
    internal enum CHANGE_CATEGORY { FILTER = 1, EDITOR = 2 }

    internal sealed class SettingsObject {

        public event Action<CHANGE_CATEGORY> PropertyChanged;
        public event Action SettingsLoaded;

        private static SettingsObject instance;
        private SettingsObject() {
            FilterRegexps = new List<RegexpInstance>();
            LanguagePairs = new List<LanguagePair>();
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
                NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);
            }
        }

        private int _MarkNotLocalizableStringsIndex;
        public int MarkNotLocalizableStringsIndex {
            get {
                return _MarkNotLocalizableStringsIndex;
            }
            set {
                _MarkNotLocalizableStringsIndex = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);
            }
        }

        private int _BatchMoveSplitterDistance;
        public int BatchMoveSplitterDistance {
            get {
                return _BatchMoveSplitterDistance;
            }
            set {
                _BatchMoveSplitterDistance = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);
            }
        }

        private bool _FilterOutVerbatim;
        public bool FilterOutVerbatim {
            get {
                return _FilterOutVerbatim;
            }
            set {
                _FilterOutVerbatim = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);
            }
        }

        private bool _FilterOutSpecificComment;
        public bool FilterOutSpecificComment {
            get {
                return _FilterOutSpecificComment;
            }
            set {
                _FilterOutSpecificComment = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);
            }
        }

        private bool _FilterOutNoLetters;
        public bool FilterOutNoLetters {
            get {
                return _FilterOutNoLetters;
            }
            set {
                _FilterOutNoLetters = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);
            }
        }

        private bool _FilterOutUnlocalizable;
        public bool FilterOutUnlocalizable {
            get {
                return _FilterOutUnlocalizable;
            }
            set {
                _FilterOutUnlocalizable = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);
            }
        }

        private bool _FilterOutCaps;
        public bool FilterOutCaps {
            get {
                return _FilterOutCaps;
            }
            set {
                _FilterOutCaps = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);
            }
        }

        private bool _FilterOutAspNet;
        public bool FilterOutAspNet {
            get {
                return _FilterOutAspNet;
            }
            set {
                _FilterOutAspNet = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);
            }
        }

        private bool _FilterOutAspElement;
        public bool FilterOutAspElement {
            get {
                return _FilterOutAspElement;
            }
            set {
                _FilterOutAspElement = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);
            }
        }

        private bool _FilterOutAspClientComment;
        public bool FilterOutAspClientComment {
            get {
                return _FilterOutAspClientComment;
            }
            set {
                _FilterOutAspClientComment = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);
            }
        }

        private bool _FilterOutAspInlineExpr;
        public bool FilterOutAspInlineExpr {
            get {
                return _FilterOutAspInlineExpr;
            }
            set {
                _FilterOutAspInlineExpr = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);
            }
        }

        private bool _FilterOutDesignerFile;
        public bool FilterOutDesignerFile {
            get {
                return _FilterOutDesignerFile;
            }
            set {
                _FilterOutDesignerFile = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);
            }
        }

        private bool _ShowFilterContext;
        public bool ShowFilterContext {
            get {
                return _ShowFilterContext;
            }
            set {
                _ShowFilterContext = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);
            }
        }

        public List<RegexpInstance> FilterRegexps { get; private set; }

        public List<LanguagePair> LanguagePairs { get; private set; }

        private string _BingAppId;
        public string BingAppId {
            get {
                return _BingAppId;
            }
            set {
                _BingAppId = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.EDITOR);
            }
        }

        private int _ReferenceUpdateInterval;
        public int ReferenceUpdateInterval {
            get {
                return _ReferenceUpdateInterval;
            }
            set {
                _ReferenceUpdateInterval = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.EDITOR);
            }
        }

        public static SettingsObject Instance {
            get {
                if (instance == null) instance = new SettingsObject();
                return instance;
            }
        }

        public void NotifyPropertyChanged(CHANGE_CATEGORY category) {
            if (PropertyChanged != null && !IgnorePropertyChanges) PropertyChanged(category);
        }

        public void NotifySettingsLoaded() {
            if (SettingsLoaded != null) SettingsLoaded();
        }

        internal sealed class RegexpInstance {
            public string Regexp { get; set; }
            public bool MustMatch { get; set; }
        }

        internal sealed class LanguagePair {
            public string FromLanguage { get; set; }
            public string ToLanguage { get; set; }

            public override int GetHashCode() {
                return FromLanguage.GetHashCode() + ToLanguage.GetHashCode();
            }

            public override bool Equals(object obj) {
                if (obj == null) return false;
                if (!(obj is LanguagePair)) return false;

                LanguagePair copy = obj as LanguagePair;
                return (copy.FromLanguage == FromLanguage || (string.IsNullOrEmpty(copy.FromLanguage) && string.IsNullOrEmpty(FromLanguage))) 
                    && copy.ToLanguage == ToLanguage;
            }

            public override string ToString() {
                return (string.IsNullOrEmpty(FromLanguage) ? "(auto)":FromLanguage) + " => " + ToLanguage;
            }
        }
    }
}
