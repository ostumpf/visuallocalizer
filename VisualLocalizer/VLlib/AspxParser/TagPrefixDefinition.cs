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

    public abstract class TagPrefixDefinition {
        public string TagPrefix { get; private set; }

        public TagPrefixDefinition(string TagPrefix) {
            this.TagPrefix = TagPrefix;
        }

        public abstract bool? Resolve(string elementName, string attributeName, Type type, out PropertyInfo propInfo);
    }

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
            string fullname = Namespace + "." + elementName;
            Type elementType = null;

            if (ReflectionCache.Instance.Types.ContainsKey(fullname)) {
                elementType = ReflectionCache.Instance.Types[fullname];
            } else {
                if (!ReflectionCache.Instance.Assemblies.ContainsKey(AssemblyName)) {
                    ReflectionCache.Instance.Assemblies.Add(AssemblyName, Assembly.Load(AssemblyName));
                }
                Assembly a = ReflectionCache.Instance.Assemblies[AssemblyName];
                elementType = a.GetType(fullname);
            }
                        
            if (elementType != null) {
                propInfo = elementType.GetProperty(attributeName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
                return propInfo != null && propInfo.PropertyType == type;
            }

            return null;
        }
    }

    public sealed class TagPrefixSourceDefinition : TagPrefixDefinition {
        public string TagName { get; private set; }
        public string Source { get; private set; }

        private CodeType codeType;

        public TagPrefixSourceDefinition(Project Project, Solution solution, string TagName, string Source, string TagPrefix)
            : base(TagPrefix) {
            this.TagName = TagName;
            this.Source = Source;
      
            string projPath = (string)Project.Properties.Item("FullPath").Value;
            if (projPath.EndsWith("\\")) projPath = projPath.Substring(0, projPath.Length - 1); 
            string sourcePath = Source.Replace("~", projPath);
            ProjectItem sourceItem = solution.FindProjectItem(sourcePath);

            ControlDirectiveHandler handler = new ControlDirectiveHandler();
            Parser parser = new Parser(File.ReadAllText(sourcePath), handler);
            parser.Process();

            if (handler.ControlInfo == null || handler.ControlInfo.Inherits == null) return;

            codeType = Project.CodeModel.CodeTypeFromFullName(handler.ControlInfo.Inherits);
        }

        public override bool? Resolve(string elementName, string attributeName, Type type, out PropertyInfo propInfo) {
            propInfo = null;
            if (codeType == null) return null;
           
            if (!ReflectionCache.Instance.Types.ContainsKey(attributeName)) {
                CodeProperty property = null;
                foreach (CodeElement codeElement in codeType.Members) {
                    if (codeElement.Kind == vsCMElement.vsCMElementProperty && codeElement.Name==attributeName) {
                        property = (CodeProperty)codeElement;
                        break;
                    }
                }

                if (property != null) {
                    try {
                        Type t = Type.GetType(property.Type.AsFullName, false, false);
                        ReflectionCache.Instance.Types.Add(attributeName, t);
                    } catch (Exception) {
                        ReflectionCache.Instance.Types.Add(attributeName, null);
                    }
                } else {
                    ReflectionCache.Instance.Types.Add(attributeName, null);
                }
            }

            Type propertyType = ReflectionCache.Instance.Types[attributeName];

            return propertyType == type;
        }
    }

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
