using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Components;

namespace VisualLocalizer.Settings {
    
    internal enum CHANGE_CATEGORY { FILTER = 1, EDITOR = 2 }

    internal sealed class SettingsObject {

        public event Action<CHANGE_CATEGORY> PropertyChanged;
        public event Action SettingsLoaded;

        private static SettingsObject instance;
        private SettingsObject() {            
            LanguagePairs = new List<LanguagePair>();  
            
            CustomLocalizabilityCriteria = new List<LocalizationCustomCriterion>();
            ResetCriteria();
        }

        public bool IgnorePropertyChanges { get; set; }
        public Dictionary<string, LocalizationCriterion> CommonLocalizabilityCriteria { get; private set; }
        public List<LocalizationCustomCriterion> CustomLocalizabilityCriteria { get; private set; }

        public void ResetCriteria() {
            CustomLocalizabilityCriteria.Clear();
            CommonLocalizabilityCriteria = CSharpStringResultItem.GetCriteria();
            var aspnetMembers = AspNetStringResultItem.GetCriteria();

            foreach (var pair in aspnetMembers)
                if (!CommonLocalizabilityCriteria.ContainsKey(pair.Key))
                    CommonLocalizabilityCriteria.Add(pair.Key, pair.Value);
        }

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


        private bool _UseReflectionInAsp;
        public bool UseReflectionInAsp {
            get {
                return _UseReflectionInAsp;
            }
            set {
                _UseReflectionInAsp = value;
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
