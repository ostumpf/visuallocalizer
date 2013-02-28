using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using System.IO;
using System.Xml.XPath;
using System.Reflection;

namespace VisualLocalizer.Library.AspxParser {

    public class WebConfig {
        public const string WebConfigFilename = "web.config";
        public const string WebConfigDefaultLocFormat = @"{0}\Microsoft.NET\Framework\{1}\CONFIG\" + WebConfigFilename;
        private List<TagPrefixDefinition> definitions = new List<TagPrefixDefinition>();

        public WebConfig(ProjectItem projectItem, Solution solution) {
            List<string> configs = GetOrderedConfigFiles(projectItem);

            definitions.Add(new TagPrefixAssemblyDefinition(
                "System.Web, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", 
                "System.Web.UI.WebControls", 
                "asp"));

            definitions.Add(new TagPrefixAssemblyDefinition(
                "System.Web, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Web.UI",
                "asp"));
           
            XPathExpression expr = XPathExpression.Compile("/configuration/system.web/pages/controls/add");
            foreach (string file in configs) {
                XPathDocument doc = new XPathDocument(file);
                XPathNavigator navigator = doc.CreateNavigator();
                XPathNodeIterator result = (XPathNodeIterator)navigator.Evaluate(expr);

                while (result.MoveNext()) {            
                    string tagPrefix = result.Current.GetAttribute("tagPrefix", "");
                    string tagName = result.Current.GetAttribute("tagName", "");
                    string namespaceName = result.Current.GetAttribute("namespace", "");
                    string assembly = result.Current.GetAttribute("assembly", "");
                    string source = result.Current.GetAttribute("src", "");

                    if (!string.IsNullOrEmpty(source)) {
                        definitions.Add(new TagPrefixSourceDefinition(projectItem.ContainingProject, solution,
                            tagName, source, tagPrefix));
                    } else {
                        definitions.Add(new TagPrefixAssemblyDefinition(assembly, namespaceName, tagPrefix));
                    }
                    
                }
            }
            
        }

        public bool? IsTypeof(string prefix, string element, string attribute, Type targetType, out PropertyInfo propInfo) {
            propInfo = null;
            
            TagPrefixDefinition exclusiveDefinition = null;
            foreach (TagPrefixSourceDefinition definition in definitions.OfType<TagPrefixSourceDefinition>()) {
                if (definition.TagPrefix == prefix && definition.TagName == element) {
                    exclusiveDefinition = definition;
                    break;
                }
            }            
           
            if (exclusiveDefinition != null) {
                return exclusiveDefinition.Resolve(element, attribute, targetType, out propInfo);
            } else {
                foreach (TagPrefixDefinition definition in definitions) {
                    if (definition.TagPrefix == prefix) {
                        bool? result = definition.Resolve(element, attribute, targetType, out propInfo);
                        if (result != null) return result;
                    }
                }
            }
            
            return null;
        }

        public void AddTagPrefixDefinition(TagPrefixDefinition def) {
            definitions.Add(def);
        }

        public void ClearCache() {
            ReflectionCache.Instance.Clear();
        }

        private List<string> GetOrderedConfigFiles(ProjectItem projectItem) {
            List<string> list = new List<string>();
            ProjectItem currentProjectItem = projectItem;

            while (true) {
                foreach (ProjectItem item in currentProjectItem.Collection) {
                    if (item.Name.ToLower() == WebConfigFilename) {
                        list.Add(item.GetFullPath());
                    }
                }
                if (!(currentProjectItem.Collection.Parent is ProjectItem)) break;
                
                currentProjectItem = (ProjectItem)currentProjectItem.Collection.Parent;
            }

            string netDefault = string.Format(WebConfigDefaultLocFormat, Environment.GetEnvironmentVariable("systemroot"), 
                string.Format("v{0}.{1}.{2}", Environment.Version.Major, Environment.Version.Minor, Environment.Version.Build));
            if (File.Exists(netDefault)) list.Add(netDefault);      

            list.Reverse();
            return list;
        }
    }
}
