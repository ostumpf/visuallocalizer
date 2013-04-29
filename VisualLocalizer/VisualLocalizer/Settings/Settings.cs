using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Components;
using System.IO;

/// Contains types related with saving, loading and editing settings.
namespace VisualLocalizer.Settings {
    
    /// <summary>
    /// Categories of settings values, corresponding to subfolders in Tools/Options pages.
    /// </summary>
    internal enum CHANGE_CATEGORY { 
        /// <summary>
        /// Settings involving "batch move" filter - localization criteria, split container splitter distance...
        /// </summary>
        FILTER = 1, 

        /// <summary>
        /// Settings involving ResX editor - language pairs, reference lookuper update interval...
        /// </summary>
        EDITOR = 2 
    }

    /// <summary>
    /// Holds all Visual Localizer settings saved in VS registry. Implemented as singleton.
    /// </summary>
    internal sealed class SettingsObject {

        /// <summary>
        /// Settings property changed
        /// </summary>
        public event Action<CHANGE_CATEGORY> PropertyChanged;

        /// <summary>
        /// Settings were changed (loaded or modified)
        /// </summary>
        public event Action SettingsLoaded;

        /// <summary>
        /// Identifiers policy changed - existing keys must be re-validated
        /// </summary>
        public event Action RevalidationRequested;

        /// <summary>
        /// True if PropertyChanged event should not be issued despite changes of the properties
        /// </summary>
        public bool IgnorePropertyChanges { get; set; }
             

        /// <summary>
        /// Unmodifiable set of filter criteria
        /// </summary>
        public Dictionary<string, LocalizationCommonCriterion> CommonLocalizabilityCriteria { get; private set; }

        /// <summary>
        /// Modifiable set of filter criteria
        /// </summary>
        public List<LocalizationCustomCriterion> CustomLocalizabilityCriteria { get; private set; }

        private static SettingsObject instance;
        private SettingsObject() {            
            LanguagePairs = new List<LanguagePair>();              
            CustomLocalizabilityCriteria = new List<LocalizationCustomCriterion>();

            ResetCriteria();
        }        

        /// <summary>
        /// Resets criteria to original state (default custom criteria, common criteria initialized with default values)
        /// </summary>
        public void ResetCriteria() {
            CustomLocalizabilityCriteria.Clear();

            // merge CSharpStringResultItem and AspNetStringResultItem criteria
            CommonLocalizabilityCriteria = CSharpStringResultItem.GetCriteria();
            var aspnetMembers = AspNetStringResultItem.GetCriteria();

            foreach (var pair in aspnetMembers)
                if (!CommonLocalizabilityCriteria.ContainsKey(pair.Key))
                    CommonLocalizabilityCriteria.Add(pair.Key, pair.Value);
            
        }


        private int _NamespacePolicyIndex;

        /// <summary>
        /// Index of option selected in "Batch move" toolwindow's "Namespace policy" combobox
        /// </summary>
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
        
        /// <summary>
        /// Index of option selected in "Batch move" toolwindow's "Mark unchecked strings" combobox
        /// </summary>
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

        /// <summary>
        /// Distance of SplitContainer's splitter in "Batch move" toolwindow
        /// </summary>
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

        /// <summary>
        /// True if reflection and web.config's settings should be used to determine object's types in ASP .NET
        /// </summary>
        public bool UseReflectionInAsp {
            get {
                return _UseReflectionInAsp;
            }
            set {
                _UseReflectionInAsp = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);
            }
        }

        private bool _ShowContextColumn;

        /// <summary>
        /// True if "context" column should be displayed in "batch" toolwindow's grid
        /// </summary>
        public bool ShowContextColumn {
            get {
                return _ShowContextColumn;
            }
            set {
                _ShowContextColumn = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.FILTER);
            }
        }

        /// <summary>
        /// Translation language pairs (source and target languages)
        /// </summary>
        public List<LanguagePair> LanguagePairs { get; private set; }

        private string _BingAppId;

        /// <summary>
        /// Identification necessary to consume the Bing translation service
        /// </summary>
        public string BingAppId {
            get {
                return _BingAppId;
            }
            set {
                _BingAppId = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.EDITOR);
            }
        }

        private bool _OptimizeSpecialSequencesInTranslation;

        /// <summary>
        /// Identification necessary to consume the Bing translation service
        /// </summary>
        public bool OptimizeSpecialSequencesInTranslation {
            get {
                return _OptimizeSpecialSequencesInTranslation;
            }
            set {
                _OptimizeSpecialSequencesInTranslation = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.EDITOR);
            }
        }

        private int _ReferenceUpdateInterval;

        /// <summary>
        /// Interval (ms) in which reference-counting thread looks up references (ResX editor)
        /// </summary>
        public int ReferenceUpdateInterval {
            get {
                return _ReferenceUpdateInterval;
            }
            set {
                _ReferenceUpdateInterval = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.EDITOR);
            }
        }

        private BAD_KEY_NAME_POLICY _BadKeyNamePolicy;

        /// <summary>
        /// How to handle invalid ResX keys
        /// </summary>
        public BAD_KEY_NAME_POLICY BadKeyNamePolicy {
            get {
                return _BadKeyNamePolicy;
            }
            set {
                _BadKeyNamePolicy = value;
                NotifyPropertyChanged(CHANGE_CATEGORY.EDITOR);
            }
        }

        public static SettingsObject Instance {
            get {
                if (instance == null) instance = new SettingsObject();
                return instance;
            }
        }

        /// <summary>
        /// Fire PropertyChanged event
        /// </summary>        
        public void NotifyPropertyChanged(CHANGE_CATEGORY category) {
            if (PropertyChanged != null && !IgnorePropertyChanges) PropertyChanged(category);
        }

        /// <summary>
        /// Fire SettingsLoaded event
        /// </summary>        
        public void NotifySettingsLoaded() {
            if (SettingsLoaded != null) SettingsLoaded();
        }

        /// <summary>
        /// Fire RevalidationRequested event
        /// </summary>        
        public void NotifyRevalidationRequested() {
            if (RevalidationRequested != null) RevalidationRequested();
        }

        /// <summary>
        /// Used to hold source and target language in translation process
        /// </summary>
        internal sealed class LanguagePair {
            public string FromLanguage { get; set; } // can be null - auto-detection of source language will be used if possible
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
