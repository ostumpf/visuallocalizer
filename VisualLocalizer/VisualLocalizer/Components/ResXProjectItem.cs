using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using VSLangProj;
using System.Collections;
using System.IO;
using System.Resources;
using VisualLocalizer.Library;
using EnvDTE80;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using System.Globalization;
using System.ComponentModel.Design;
using VisualLocalizer.Extensions;
using VisualLocalizer.Commands;

namespace VisualLocalizer.Components {

    /// <summary>
    /// Enumeration used in GetKeyConflictType - represents possible relations between a new key and existing data.
    /// </summary>
    public enum CONTAINS_KEY_RESULT { 
        /// <summary>
        /// Key already exists in the file and has the same value
        /// </summary>
        EXISTS_WITH_SAME_VALUE, 

        /// <summary>
        /// Key already exists in the file but has different value
        /// </summary>
        EXISTS_WITH_DIFF_VALUE, 

        /// <summary>
        /// Key is not present in the file
        /// </summary>
        DOESNT_EXIST 
    }

    /// <summary>
    /// Represents ResX file in Solution Explorer's hierarchy.
    /// </summary>
    public class ResXProjectItem {

        /// <summary>
        /// Data stored in the ResX file
        /// </summary>
        private Dictionary<string, ResXDataNode> data;                

        /// <summary>
        /// When in batch mode, this indicates whether any modifying operation was performed
        /// </summary>
        private bool dataChangedInBatchMode;

        /// <summary>
        /// Creates new ResXProjectItem instance
        /// </summary>
        /// <param name="projectItem">Corresponding ProjectItem in file hierarchy</param>
        /// <param name="displayName">Display name for this item</param>
        /// <param name="internalInReferenced">True if this item should be marked as "internal in referenced project"</param>
        private ResXProjectItem(ProjectItem projectItem, string displayName, bool internalInReferenced) {
            if (projectItem == null) throw new ArgumentNullException("projectItem");
            if (displayName == null) throw new ArgumentNullException("displayName");

            this.DisplayName = displayName;
            this.InternalProjectItem = projectItem;

            string customToolOutput = InternalProjectItem.GetCustomToolOutput();
            this.DesignerItem = InternalProjectItem.ProjectItems.GetItem(customToolOutput);

            this.MarkedInternalInReferencedProject = internalInReferenced;
        }    

        /// <summary>
        /// Returns true if ResX file is culture specific
        /// </summary>        
        public bool IsCultureSpecific() {
            return InternalProjectItem.IsCultureSpecificResX();
        }

        /// <summary>
        /// If ResX file is culture specific, returns its culture neutral name (strips language info). Otherwise returns its name unchanged.
        /// </summary>        
        public string GetCultureNeutralName() {
            return InternalProjectItem.GetResXCultureNeutralName();
        }

        /// <summary>
        /// Returns language of designer item (if any), C# by default
        /// </summary>
        public LANGUAGE DesignerLanguage {
            get {
                ProjectItem designer = DesignerItem;
                if (IsCultureSpecific()) {
                    ProjectItem parent = InternalProjectItem.Collection.GetItem(GetCultureNeutralName());
                    if (parent != null) {
                        designer = parent.ProjectItems.GetItem(parent.GetCustomToolOutput());
                    }
                }

                if (designer != null && designer.GetCodeModel() != null) {
                    string lang = designer.GetCodeModel().Language;
                    return lang == CodeModelLanguageConstants.vsCMLanguageCSharp ? LANGUAGE.CSHARP : LANGUAGE.VB;
                } else {
                    LANGUAGE? lang = InternalProjectItem.ContainingProject.GetCurrentWebsiteLanguage();
                    return lang.HasValue ? lang.Value : LANGUAGE.CSHARP;
                }
            }
        }

        public bool HasImplicitDesignerFile {
            get {
                bool impliedDesignerItem = false;
                if (InternalProjectItem.ContainingProject.Kind.ToUpper() == StringConstants.WebSiteProject) { // must be located in ASP .NET WebSite project
                    string relative = InternalProjectItem.GetRelativeURL();
                    // must be in the App_GlobalResources folder
                    impliedDesignerItem = !string.IsNullOrEmpty(relative) && relative.StartsWith(StringConstants.GlobalWebSiteResourcesFolder);
                }
                return impliedDesignerItem;
            }
        }

        /// <summary>
        /// Returns true if this ResX file is default for given project, i.e. its lower-case name is "resources.resx" and
        /// is located in the "Properties" folder.
        /// </summary>        
        public bool IsProjectDefault(Project project) {
            if (project == null) throw new ArgumentNullException("project");

            object parent = InternalProjectItem.Collection.Parent;
            if (!(parent is ProjectItem)) return false;

            if (InternalProjectItem.ContainingProject != project) return false;
            if (InternalProjectItem.Name.ToLower() != "resources.resx") return false;

            ProjectItem pitem = (ProjectItem)parent;
            return pitem.Name == "Properties" && pitem.Kind.ToUpper() == StringConstants.PhysicalFolder;
        }  
       
        /// <summary>
        /// Returns project item from file hierarchy corresponding to this object
        /// </summary>
        public ProjectItem InternalProjectItem {
            get;
            private set;
        }     

        /// <summary>
        /// Returns display name
        /// </summary>
        public string DisplayName {
            get;
            private set;
        }

        /// <summary>
        /// Returns namespace of designer file
        /// </summary>
        public string Namespace {
            get;
            private set;
        }

        /// <summary>
        /// Returns class of designer file
        /// </summary>
        public string Class {
            get;
            private set;
        }

        /// <summary>
        /// Returns true if designer file has "internal" modifier and comes from referenced project
        /// </summary>
        public bool MarkedInternalInReferencedProject {
            get;
            private set;
        }

        /// <summary>
        /// Returns instance of the generated designer file
        /// </summary>
        public ProjectItem DesignerItem {
            get;
            private set;
        }

        /// <summary>
        /// Returns true if content of the file was loaded into "data" field
        /// </summary>
        public bool IsLoaded {
            get;
            private set;
        }

        /// <summary>
        /// Returns true if this object is currently in "batch" state (i.e. BeginBatch() was called, all modifications of data
        /// are performed only in memory, no instant file flushes)
        /// </summary>
        public bool IsInBatchMode {
            get;
            private set;
        }

        /// <summary>
        /// Runs custom tool on this item
        /// </summary>
        public void RunCustomTool() {
            (InternalProjectItem.Object as VSProjectItem).RunCustomTool();
        }

        /// <summary>
        /// Puts this item in "batch" mode
        /// </summary>
        public void BeginBatch() {
            IsInBatchMode = true;
            dataChangedInBatchMode = false;
        }

        /// <summary>
        /// Flushes data to disk and terminates "batch" mode
        /// </summary>
        public void EndBatch() {            
            Flush();
            IsInBatchMode = false;
        }

        /// <summary>
        /// Returns data stored in this ResX file if they're loaded (IsLoaded). Keys are stored in lower-case because of case-insensitive comparison.
        /// </summary>
        public Dictionary<string, ResXDataNode> Data {
            get {
                return data;
            }
        }

        /// <summary>
        /// Flushes current Data. If the file is closed, it means write data to disk; otherwise corresponding file buffer is modified.
        /// </summary>
        public void Flush() {
            if (!dataChangedInBatchMode && IsInBatchMode) return;
            string path = InternalProjectItem.GetFullPath();
            
            if (RDTManager.IsFileOpen(path)) {
                VLDocumentViewsManager.SaveDataToBuffer(data, path); // modify in-memory buffer of this file
            } else {
                // write data to disk
                ResXResourceWriter writer = null;
                try {
                    writer = new ResXResourceWriter(path);
                    writer.BasePath = Path.GetDirectoryName(path);

                    foreach (var pair in data) {
                        writer.AddResource(pair.Value);
                    }
                    writer.Generate();
                } finally {
                    if (writer != null) writer.Close();
                }
            }

            // regenerate designer item
            if (DesignerItem != null) {
                RDTManager.SilentlyModifyFile(DesignerItem.GetFullPath(), (string p) => {
                    RunCustomTool();
                });
            }
        }

        /// <summary>
        /// Adds given string value with given key to this file. If it already exists (case-insensitive comparison), value is modified.
        /// </summary>        
        public void AddString(string key, string value) {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

            VLOutputWindow.VisualLocalizerPane.WriteLine("Adding \"{0}\":\"{1}\" to \"{2}\"", key, value, DisplayName);
            bool wasLoaded = IsLoaded;

            if (!IsLoaded) Load(); // load current file data
            string lowerKey = key.ToLower();

            // add/modify the record
            ResXDataNode node = new ResXDataNode(key, value);
            if (data.ContainsKey(lowerKey)) {
                data[lowerKey] = node;
            } else {
                data.Add(lowerKey, node);
            }

            if (IsInBatchMode) { // batch mode - just mark the file dirty
                dataChangedInBatchMode = true;
            } else { // not in batch mode - flush the file and unload data
                Flush();
                if (!wasLoaded) Unload();
            }
        }

        /// <summary>
        /// Removes resource with given key from the file
        /// </summary>        
        public void RemoveKey(string key) {
            if (key == null) throw new ArgumentNullException("key");

            VLOutputWindow.VisualLocalizerPane.WriteLine("Removing \"{0}\" from \"{1}\"", key, DisplayName);
            bool wasLoaded = IsLoaded;
            if (!IsLoaded) Load();
            
            data.Remove(key.ToLower());

            if (IsInBatchMode) {// batch mode - just mark the file dirty
                dataChangedInBatchMode = true;
            } else {// not in batch mode - flush the file and unload data
                Flush();
                if (!wasLoaded) Unload();
            }
        }
       
        /// <summary>
        /// Returns string value for given key
        /// </summary>        
        public string GetString(string key) {
            if (key == null) throw new ArgumentNullException("key");

            bool wasLoaded = IsLoaded;            
            if (!IsLoaded) Load(); // load data if necessary

            string value = data[key.ToLower()].GetValue<string>(); // get value
            if (!wasLoaded) Unload(); // unload data
            
            return value;
        }

        /// <summary>
        /// Returns value from CONTAINS_KEY_RESULT enumeration identifying conflict type for given key/value pair.
        /// </summary>     
        public CONTAINS_KEY_RESULT GetKeyConflictType(string key, string value) {
            bool wasLoaded = IsLoaded;
            if (!IsLoaded) Load(); // load data if necessary
            if (string.IsNullOrEmpty(key)) return CONTAINS_KEY_RESULT.DOESNT_EXIST; 

            string lowerKey = key.ToLower();

            CONTAINS_KEY_RESULT status;
            if (data.ContainsKey(lowerKey)) { // key exists
                if (data[lowerKey].HasValue<string>()) { // key has string value
                    if (string.Compare(data[lowerKey].GetValue<string>(), value, false, CultureInfo.CurrentCulture) == 0) {
                        status = CONTAINS_KEY_RESULT.EXISTS_WITH_SAME_VALUE;  // key exists and its value is the same
                    } else {
                        status = CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE; // key exists and its value is different
                    }
                } else status = CONTAINS_KEY_RESULT.EXISTS_WITH_DIFF_VALUE;
            } else status = CONTAINS_KEY_RESULT.DOESNT_EXIST; // key is not present in this file

            if (!wasLoaded) Unload(); // unload data

            return status;
        }

        /// <summary>
        /// Returns case-sensitive version of the key or the key itself, if it is not present in the file.
        /// </summary> 
        public string GetRealKey(string key) {
            if (key == null) throw new ArgumentNullException("key");

            bool wasLoaded = IsLoaded;
            if (!IsLoaded) Load();
            
            string lowerKey=key.ToLower();
            string realKey;

            if (data.ContainsKey(lowerKey)) {
                realKey = data[lowerKey].Name;
            } else {
                realKey = key;
            }

            if (!wasLoaded) Unload();
            return realKey;
        }

        /// <summary>
        /// Loads file's content into memory
        /// </summary>
        public void Load() {
            if (IsLoaded) return;

            string path = InternalProjectItem.GetFullPath();

            if (RDTManager.IsFileOpen(path)) { // file is open - read from VS buffer
                VLDocumentViewsManager.LoadDataFromBuffer(ref data, path);
            } else { // file is closed - read from disk
                ResXResourceReader reader = null;
                try {
                    data = new Dictionary<string, ResXDataNode>();
                    reader = new ResXResourceReader(path);
                    reader.BasePath = Path.GetDirectoryName(path);
                    reader.UseResXDataNodes = true;

                    foreach (DictionaryEntry entry in reader) {
                        data.Add(entry.Key.ToString().ToLower(), entry.Value as ResXDataNode);
                    }
                } finally {
                    if (reader != null) reader.Close();
                }
            }

            IsLoaded = true;
        }

        /// <summary>
        /// Removes loaded data from memory
        /// </summary>
        public void Unload() {
            if (data != null) {
                data.Clear();
                data = null;
            }
            IsLoaded = false;           
        }

        /// <summary>
        /// Returns list of all string key/value pair in the file.
        /// </summary>
        /// <param name="addClass">True if designer class name should be added to the key name</param>        
        public Dictionary<string, string> GetAllStringReferences(bool addClass) {
            Dictionary<string, string> AllReferences = new Dictionary<string, string>();
            bool wasLoaded = IsLoaded;
            if (!IsLoaded) Load();

            foreach (var pair in data) {
                if (pair.Value.HasValue<string>()) {
                    string reference = (addClass ? Class + ".":"") + pair.Value.Name;

                    if (!AllReferences.ContainsKey(reference)) {                       
                        AllReferences.Add(reference, pair.Value.GetValue<string>());
                    }
                }
            }
            if (!wasLoaded) Unload();

            return AllReferences;
        }            
        
        /// <summary>
        /// Returns new instance of ResXProjectItem using given project item
        /// </summary>
        /// <param name="item">Project item in Solution Explorer's file hierarchy</param>
        /// <param name="relationProject">Project used to created "referenced" relation</param>        
        public static ResXProjectItem ConvertToResXItem(ProjectItem item, Project relationProject) {
            if (item == null) throw new ArgumentNullException("item");
            if (relationProject == null) throw new ArgumentNullException("relationProject");
            
            string projectPath = item.ContainingProject.FullName;
            if (string.IsNullOrEmpty(projectPath)) {
                return new ResXProjectItem(item, item.Name, false);
            } else {
                // create relative path from the project to the item
                Uri projectUri = new Uri(projectPath, UriKind.Absolute);
                Uri itemUri = new Uri(item.GetFullPath());

                string displayName;
                if (item.ContainingProject.Kind.ToUpper() == StringConstants.WebSiteProject) {
                    displayName = Uri.UnescapeDataString(projectUri.MakeRelativeUri(itemUri).ToString());
                } else {
                    displayName = item.ContainingProject.Name + "/" + Uri.UnescapeDataString(projectUri.MakeRelativeUri(itemUri).ToString());
                }

                bool referenced = relationProject.UniqueName != item.ContainingProject.UniqueName;
                bool inter = item.GetCustomTool() != StringConstants.PublicResXTool;

                bool internalInReferenced = inter && referenced;

                return new ResXProjectItem(item, displayName, internalInReferenced);
            }
        }

        /// <summary>
        /// Attempts to obtain class and namespace name of the designer file (if it exists)
        /// </summary>
        /// <param name="neutralItems">List of relevant culture-neutral ResX files</param>
        public void ResolveNamespaceClass(List<ResXProjectItem> neutralItems) {
            if (neutralItems == null) throw new ArgumentNullException("neutralItems");
            Class = null;
            Namespace = null;

            // if this file is culture-specific it's designer file is empty - we need to find designer item if corresponding culture-neutral file
            if (IsCultureSpecific()) DesignerItem = GetNeutralDesignerItem(neutralItems);

            if (DesignerItem != null) {  // designer file is set
                if (!File.Exists(DesignerItem.GetFullPath())) RunCustomTool(); 

                // select first namespace in the designer file
                CodeElement nmspcElemet = null;
                foreach (CodeElement element in DesignerItem.GetCodeModel().CodeElements) {
                    if (element.Kind == vsCMElement.vsCMElementNamespace) {
                        Namespace = element.FullName;
                        nmspcElemet = element;
                        break;
                    }                    
                }
                if (nmspcElemet != null) { // namespace found
                    // select first class/module in the namespace
                    foreach (CodeElement child in nmspcElemet.Children) {
                        if (child.Kind == vsCMElement.vsCMElementClass || child.Kind == vsCMElement.vsCMElementModule) {
                            Class = child.Name;
                            break;
                        }
                    }
                }
            } else { // designer file doesn't exist   

                // if ResX files is contained in ASP .NET website project, designer files are implicit
                if (InternalProjectItem.ContainingProject != null && InternalProjectItem.ContainingProject.Kind.ToUpper() == StringConstants.WebSiteProject) {
                    string relative = InternalProjectItem.GetRelativeURL();

                    // file must be located in App_GlobalResources folder
                    if (!string.IsNullOrEmpty(relative) && relative.StartsWith(StringConstants.GlobalWebSiteResourcesFolder)) {
                        Class = Path.GetFileNameWithoutExtension(InternalProjectItem.GetFullPath()); // take file name as a class name
                        if (IsCultureSpecific()) { // strip culture-specific info
                            Class = GetCultureNeutralName();
                            Class = Class.Substring(0, Class.IndexOf('.'));
                        }
                        Namespace = StringConstants.GlobalWebSiteResourcesNamespace; // namespace is hard-coded as "Resources"
                    } else {
                        Class = null;
                        Namespace = null;
                    }
                }
            }         
        }

        /// <summary>
        /// Selects designer file for this (culture-specific) item from the list of culture-neutral ResX files
        /// </summary>        
        private ProjectItem GetNeutralDesignerItem(List<ResXProjectItem> neutralItems) {            
            string cultureNeutralName = GetCultureNeutralName(); // get name of culture-neutral ResX
            string thisDir = Path.GetFullPath(Path.GetDirectoryName(InternalProjectItem.GetFullPath())); // get full path of this item's directory

            ResXProjectItem neutralItem = null;
            foreach (ResXProjectItem item in neutralItems) {
                if (item.IsCultureSpecific()) continue; // we are looking for culture-neutral file

                // directory of tested item
                string neutralDir = Path.GetFullPath(Path.GetDirectoryName(item.InternalProjectItem.GetFullPath()));                

                // we need the item to be located in the same directory and have proper name
                if (neutralDir == thisDir && item.InternalProjectItem.Name.ToLowerInvariant() == cultureNeutralName.ToLowerInvariant()) {
                    neutralItem = item;
                    break;
                }
            }

            if (neutralItem != null) { // we found culture-neutral item
                return neutralItem.DesignerItem;
            } else return null;
        }

        /// <summary>
        /// Adds all string resources to the list for translation
        /// </summary>        
        internal void AddAllStringReferencesUnique(List<AbstractTranslateInfoItem> outputData) {
            bool wasLoaded = IsLoaded;
            if (!IsLoaded) Load();

            foreach (var pair in data) {
                if (pair.Value.HasValue<string>()) {
                    ResXTranslateInfoItem key = new ResXTranslateInfoItem();
                    key.ResXItem = this;
                    key.ResourceKey = pair.Key;
                    key.DataKey = pair.Value.Name;
                    key.Value = pair.Value.GetValue<string>();
                    outputData.Add(key);
                }
            }
            if (!wasLoaded) Unload();
        }

        public override string ToString() {
            return (MarkedInternalInReferencedProject ? "(internal) " : "") + DisplayName;
        }

        public override int GetHashCode() {
            return InternalProjectItem.GetHashCode();
        }

        public override bool Equals(object obj) {
            if (obj == null) return false;
            ResXProjectItem copy = obj as ResXProjectItem;

            if (copy == null) return false;
            return InternalProjectItem.Equals(copy.InternalProjectItem);
        }        
    }

    
}
