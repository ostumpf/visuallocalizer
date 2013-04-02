using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using EnvDTE;
using System.Web;
using System.IO;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace VisualLocalizer.Library.AspxParser {

    /// <summary>
    /// Caches information about elements, attributes and their types, obtained from web.config files and code-behind definitions.
    /// </summary>
    public sealed class ReflectionCache {
        private ReflectionCache() {
            Assemblies = new Dictionary<string, Assembly>();
            Types = new Dictionary<string, Type>();
            Localizables = new Dictionary<string, bool>();
        }

        private static ReflectionCache instance;
        public static ReflectionCache Instance {
            get {
                if (instance == null) instance = new ReflectionCache();
                return instance;
            }
        }

        /// <summary>
        /// Cache for loaded assemblies
        /// </summary>
        public Dictionary<string, Assembly> Assemblies {
            get;
            private set;
        }

        /// <summary>
        /// Cache for determined types (may return null)
        /// </summary>
        private Dictionary<string, Type> Types {
            get;
            set;
        }

        /// <summary>
        /// Cache for [Localizable(false)] values
        /// </summary>
        private Dictionary<string, bool> Localizables {
            get;
            set;
        }

        public bool IsDefined(string typeName, string propertyName) {
            if (typeName == null) throw new ArgumentNullException("typeName");
            if (propertyName == null) throw new ArgumentNullException("propertyName");

            return Types.ContainsKey(typeName + "." + propertyName);
        }

        public Type GetType(string typeName, string propertyName) {
            if (typeName == null) throw new ArgumentNullException("typeName");
            if (propertyName == null) throw new ArgumentNullException("propertyName");

            return Types[typeName + "." + propertyName];
        }

        public bool HasLocalizableFalse(string typeName, string propertyName) {
            if (typeName == null) throw new ArgumentNullException("typeName");
            if (propertyName == null) throw new ArgumentNullException("propertyName");

            return Localizables.ContainsKey(typeName + "." + propertyName) && Localizables[typeName + "." + propertyName];
        }

        public void AddType(string typeName, string propertyName, CodeProperty property) {
            if (typeName == null) throw new ArgumentNullException("typeName");
            if (propertyName == null) throw new ArgumentNullException("propertyName");

            bool hasLocalizableFalseSet = false;
            
            Type propertyType = null;
            try {
                propertyType = Type.GetType(property.Type.AsFullName, false, false); // get its name and save it in cache
                hasLocalizableFalseSet = (property as CodeElement).HasLocalizableFalseAttribute();
            } catch (Exception) {  }

            string fullName = typeName + "." + propertyName;
            if (!Types.ContainsKey(fullName)) {
                Types.Add(fullName, propertyType);
                Localizables.Add(fullName, hasLocalizableFalseSet);
            } else {
                Types[fullName] = propertyType;
                Localizables[fullName] = hasLocalizableFalseSet;
            }
        }

        public void AddType(string typeName, string propertyName, PropertyInfo info) {
            if (typeName == null) throw new ArgumentNullException("typeName");
            if (propertyName == null) throw new ArgumentNullException("propertyName");

            Type propertyType;
            bool loc;

            if (info == null) {
                propertyType = null;
                loc = false;
            } else {
                propertyType = info.PropertyType;
                loc = HasLocalizableFalse(info);
            }

            string fullName = typeName + "." + propertyName;
            if (!Types.ContainsKey(fullName)) {
                Types.Add(fullName, propertyType);
                Localizables.Add(fullName, loc);
            } else {
                Types[fullName] = propertyType;
                Localizables[fullName] = loc;
            }
        }

        public void Clear() {
            Assemblies.Clear();
            Types.Clear();
            Localizables.Clear();
        }


        /// <summary>
        /// Returns true if given property is decorated with Localizable(false)
        /// </summary>        
        private bool HasLocalizableFalse(PropertyInfo propInfo) {
            if (propInfo == null) return false;

            object[] objects = propInfo.GetCustomAttributes(typeof(LocalizableAttribute), true);
            if (objects != null && objects.Length > 0) {
                bool hasFalse = false;
                foreach (LocalizableAttribute attr in objects)
                    if (!attr.IsLocalizable) hasFalse = true;

                return hasFalse;
            } else return false;
        }
    }

    /// <summary>
    /// Represents definition of element type, as found in web.config files or Register directive
    /// </summary>
    public abstract class TagPrefixDefinition {
        public string TagPrefix { get; private set; }

        public TagPrefixDefinition(string TagPrefix) {
            this.TagPrefix = TagPrefix;
        }

        /// <summary>
        /// Attempts to resolve attribute's type, returns true/false in case of conclusive result, null otherwise
        /// </summary>        
        public abstract bool? Resolve(string elementName, string attributeName, Type type, out bool hasLocalizableFalseSet);

        /// <summary>
        /// Loads types specified for this definition into types cache
        /// </summary>
        public virtual void Load() {
        }
    }

    /// <summary>
    /// Represents element definition using prefix, namespace and assembly strong name
    /// </summary>
    public sealed class TagPrefixAssemblyDefinition : TagPrefixDefinition {
        public string Namespace { get; private set; }
        public string AssemblyName { get; private set; }

        public TagPrefixAssemblyDefinition(string AssemblyName, string Namespace, string TagPrefix)
            : base(TagPrefix) {
            this.AssemblyName = AssemblyName;
            this.Namespace = Namespace;
        }

        /// <summary>
        /// Attempts to resolve attribute's type, returns true/false in case of conclusive result, null otherwise
        /// </summary>
        public override bool? Resolve(string elementName, string attributeName, Type type, out bool hasLocalizableFalseSet) {            
            string fullname = Namespace + "." + elementName; // expected type name
            Type propertyType = null;
            hasLocalizableFalseSet = false;

            if (ReflectionCache.Instance.IsDefined(fullname, attributeName)) {
                propertyType = ReflectionCache.Instance.GetType(fullname, attributeName);
                hasLocalizableFalseSet = ReflectionCache.Instance.HasLocalizableFalse(fullname, attributeName);
            } else {
                if (!ReflectionCache.Instance.Assemblies.ContainsKey(AssemblyName)) {
                    // load given assembly and add it to cache
                    ReflectionCache.Instance.Assemblies.Add(AssemblyName, Assembly.Load(AssemblyName));
                }
                Assembly a = ReflectionCache.Instance.Assemblies[AssemblyName];
                
                // attempt to get type from assembly
                Type elementType = a.GetType(fullname);

                if (elementType != null) {
                    // attempt to get property with the name of the attribute
                    PropertyInfo propInfo = elementType.GetProperty(attributeName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);                    
                    ReflectionCache.Instance.AddType(fullname, attributeName, propInfo);

                    propertyType = ReflectionCache.Instance.GetType(fullname, attributeName);
                    hasLocalizableFalseSet = ReflectionCache.Instance.HasLocalizableFalse(fullname, attributeName);
                }
            }
            
            
            return propertyType == null ? null : (bool?)(propertyType == type);
        }

    }

    /// <summary>
    /// Represents element definition using tag name and source file
    /// </summary>
    public sealed class TagPrefixSourceDefinition : TagPrefixDefinition {
        public string TagName { get; private set; }
        public string Source { get; private set; }
        private ProjectItem projectItem;
        private Solution solution;

        public TagPrefixSourceDefinition(ProjectItem projectItem, Solution solution, string TagName, string Source, string TagPrefix)
            : base(TagPrefix) {
            if (TagName == null) throw new ArgumentNullException("TagName");
            if (projectItem == null) throw new ArgumentNullException("Source");
            if (Source == null) throw new ArgumentNullException("projectItem");

            this.TagName = TagName;
            this.Source = Source;
            this.projectItem = projectItem;
            this.solution = solution;
        }

        /// <summary>
        /// Loads types specified for this definition into types cache
        /// </summary>        
        public override void Load() {
            // get full path of the definition file
            string projPath = projectItem.ContainingProject.FullName;
            if (projPath.EndsWith("\\")) projPath = projPath.Substring(0, projPath.Length - 1);
            string sourcePath = Source.Replace("~", projPath);

            // read the source file and stop after first Control directive (code behind class name)
            ControlDirectiveHandler handler = new ControlDirectiveHandler();
            Parser parser = new Parser(File.ReadAllText(sourcePath), handler);
            parser.Process();

            // no code behind class or no Control directive at all
            if (handler.ControlInfo == null || handler.ControlInfo.Inherits == null) return;

            // get definition class type
            string codeFile = handler.ControlInfo.CodeFile == null ? handler.ControlInfo.CodeBehind : handler.ControlInfo.CodeFile;
            if (codeFile == null) return;


            Uri codeItemUri;
            Uri.TryCreate(new Uri(sourcePath, UriKind.Absolute), new Uri(codeFile, UriKind.Relative), out codeItemUri);
            ProjectItem codeItem = solution.FindProjectItem(Uri.UnescapeDataString(codeItemUri.ToString()));
            if (codeItem == null) throw new InvalidOperationException("Cannot find declared code behind file " + codeItem.GetFullPath());

            // ensure the window is open to get the code model            
            if (codeItem.GetCodeModel() == null) {
                return;
            }

            CodeClass classElement = null;
            foreach (CodeElement el in codeItem.GetCodeModel().CodeElements) {
                if (el.Kind == vsCMElement.vsCMElementClass && el.Name == handler.ControlInfo.Inherits) {
                    classElement = (CodeClass)el;
                    break;
                }
            }
            if (classElement != null) {
                foreach (CodeElement el in classElement.Children) {
                    if (el.Kind == vsCMElement.vsCMElementProperty) {
                        CodeProperty property = (CodeProperty)el;
                        ReflectionCache.Instance.AddType(classElement.FullName, property.Name, property);
                        ReflectionCache.Instance.AddType(TagName, property.Name, property);
                    }
                }
            }
            
        }

        /// <summary>
        /// Attempts to resolve attribute's type, returns true/false in case of conclusive result, null otherwise
        /// </summary>
        public override bool? Resolve(string elementName, string attributeName, Type type, out bool hasLocalizableFalseSet) {
            if (attributeName == null) throw new ArgumentNullException("attributeName");
            hasLocalizableFalseSet = false;
            
            Type propertyType;
            if (ReflectionCache.Instance.IsDefined(elementName, attributeName)) {
                propertyType = ReflectionCache.Instance.GetType(elementName, attributeName);
                hasLocalizableFalseSet = ReflectionCache.Instance.HasLocalizableFalse(elementName, attributeName);
            } else {
                propertyType = null;
            }

            return propertyType == null ? null : (bool?)(propertyType == type);
        }
    }

    /// <summary>
    /// Used to parse ASPX definition file and read Control directive, where information about code behind class is stored
    /// </summary>
    public sealed class ControlDirectiveHandler : IAspxHandler {
        public ControlDirectiveInfo ControlInfo { get; private set; }
        
        public bool StopRequested { get; private set;  }


        /// <summary>
        /// Called after code block &lt;% %&gt;
        /// </summary>
        /// <param name="context"></param>
        public void OnCodeBlock(CodeBlockContext context) {            
        }

        /// <summary>
        /// Called after page directive &lt;%@ %&gt;
        /// </summary>  
        public void OnPageDirective(DirectiveContext context) {
            if (context.DirectiveName == "Control") {
                ControlInfo = new ControlDirectiveInfo();
                foreach (var attr in context.Attributes) {
                    if (attr.Name == "CodeBehind" || attr.Name == "CodeFile") {
                        ControlInfo.CodeBehind = attr.Value;
                    }
                    if (attr.Name == "Inherits") {
                        ControlInfo.Inherits = attr.Value;
                    }
                    if (attr.Name == "CodeFile") {
                        ControlInfo.CodeFile = attr.Value;
                    }
                }
                StopRequested = true;
            }
        }

        /// <summary>
        /// Called after output element &lt;%= %&gt;, &lt;%$ %&gt; or &lt;%: %&gt;
        /// </summary> 
        public void OnOutputElement(OutputElementContext context) {            
        }

        /// <summary>
        /// Called after beginnnig tag is read
        /// </summary>   
        public void OnElementBegin(ElementContext context) {            
        }

        /// <summary>
        /// Called after end tag is read
        /// </summary>
        public void OnElementEnd(EndElementContext context) {            
        }

        /// <summary>
        /// Called after plain text (between elements) is read
        /// </summary>
        public void OnPlainText(PlainTextContext context) {            
        }
    }

    public sealed class ControlDirectiveInfo {
        public string CodeBehind { get; set; }
        public string Inherits { get; set; }
        public string CodeFile { get; set; }
    }
}
