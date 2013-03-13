using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using EnvDTE;
using System.Web;
using System.IO;

namespace VisualLocalizer.Library.AspxParser {

    public sealed class ReflectionCache {
        private ReflectionCache() {
            Assemblies = new Dictionary<string, Assembly>();
            Types = new Dictionary<string, Type>();
        }

        private static ReflectionCache instance;
        public static ReflectionCache Instance {
            get {
                if (instance == null) instance = new ReflectionCache();
                return instance;
            }
        }

        public Dictionary<string, Assembly> Assemblies {
            get;
            private set;
        }

        public Dictionary<string, Type> Types {
            get;
            private set;
        }

        public void Clear() {
            Assemblies.Clear();
            Types.Clear();
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
        public abstract bool? Resolve(string elementName, string attributeName, Type type, out PropertyInfo propInfo);
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

        public override bool? Resolve(string elementName, string attributeName, Type type, out PropertyInfo propInfo) {
            propInfo = null;
            string fullname = Namespace + "." + elementName; // expected type name
            Type elementType = null; 

            if (ReflectionCache.Instance.Types.ContainsKey(fullname)) {
                elementType = ReflectionCache.Instance.Types[fullname];
            } else {
                if (!ReflectionCache.Instance.Assemblies.ContainsKey(AssemblyName)) {
                    // load given assembly and add it to cache
                    ReflectionCache.Instance.Assemblies.Add(AssemblyName, Assembly.Load(AssemblyName));
                }
                Assembly a = ReflectionCache.Instance.Assemblies[AssemblyName];
                
                // attempt to get type from assembly
                elementType = a.GetType(fullname);
            }
                        
            if (elementType != null) {
                // attempt to get property with the name of the attribute
                propInfo = elementType.GetProperty(attributeName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
                
                // if it exists, we have conclusive result
                if (propInfo != null) {
                    return propInfo.PropertyType == type;
                }                
            }

            // either element or property definition was not found
            return null;
        }
    }

    /// <summary>
    /// Represents element definition using tag name and source file
    /// </summary>
    public sealed class TagPrefixSourceDefinition : TagPrefixDefinition {
        public string TagName { get; private set; }
        public string Source { get; private set; }

        private CodeType codeType;

        public TagPrefixSourceDefinition(Project Project, Solution solution, string TagName, string Source, string TagPrefix)
            : base(TagPrefix) {
            if (TagName == null) throw new ArgumentNullException("TagName");
            if (Source == null) throw new ArgumentNullException("Source");

            this.TagName = TagName;
            this.Source = Source;

            // get full path of the definition file
            string projPath = Project.FullName;
            if (projPath.EndsWith("\\")) projPath = projPath.Substring(0, projPath.Length - 1); 
            string sourcePath = Source.Replace("~", projPath);
            
            // read the source file and stop after first Control directive (code behind class name)
            ControlDirectiveHandler handler = new ControlDirectiveHandler();
            Parser parser = new Parser(File.ReadAllText(sourcePath), handler);
            parser.Process();

            // no code behind class or no Control directive at all
            if (handler.ControlInfo == null || handler.ControlInfo.Inherits == null) return;

            // get definition class type
            codeType = Project.CodeModel.CodeTypeFromFullName(handler.ControlInfo.Inherits);
        }

        public override bool? Resolve(string elementName, string attributeName, Type type, out PropertyInfo propInfo) {
            if (attributeName == null) throw new ArgumentNullException("attributeName");

            propInfo = null;
            if (codeType == null) return null; // no code type was found - inconclusive
           
            if (!ReflectionCache.Instance.Types.ContainsKey(attributeName)) {
                CodeProperty property = null;
                // look for property with attribute's name
                foreach (CodeElement codeElement in codeType.Members) {
                    if (codeElement.Kind == vsCMElement.vsCMElementProperty && codeElement.Name==attributeName) {
                        property = (CodeProperty)codeElement;
                        break;
                    }
                }

                if (property != null) { // property found
                    try {
                        Type t = Type.GetType(property.Type.AsFullName, false, false); // get its name and save it in cache
                        ReflectionCache.Instance.Types.Add(attributeName, t);
                    } catch (Exception) {
                        ReflectionCache.Instance.Types.Add(attributeName, null);
                    }
                } else {
                    ReflectionCache.Instance.Types.Add(attributeName, null);
                }
            }

            Type propertyType = ReflectionCache.Instance.Types[attributeName];

            return propertyType == null ? null : (bool?)(propertyType == type);
        }
    }

    /// <summary>
    /// Used to parse ASPX definition file and read Control directive, where information about code behind class is stored
    /// </summary>
    public sealed class ControlDirectiveHandler : IAspxHandler {
        public ControlDirectiveInfo ControlInfo { get; private set; }
        
        public bool StopRequested { get; private set;  }

        public void OnCodeBlock(CodeBlockContext context) {            
        }

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
                }
                StopRequested = true;
            }
        }

        public void OnOutputElement(OutputElementContext context) {            
        }

        public void OnElementBegin(ElementContext context) {            
        }

        public void OnElementEnd(EndElementContext context) {            
        }

        public void OnPlainText(PlainTextContext context) {            
        }
    }

    public sealed class ControlDirectiveInfo {
        public string CodeBehind { get; set; }
        public string Inherits { get; set; }
    }
}
