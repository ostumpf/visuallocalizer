using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using System.IO;
using System.Xml.XPath;
using System.Reflection;

namespace VisualLocalizer.Library.AspxParser {

    /// <summary>
    /// Provides functionality for getting type of attribute's values in ASP .NET files from web.config files and
    /// page directives.
    /// </summary>
    public class WebConfig {
        /// <summary>
        /// Name of the web.config file
        /// </summary>
        public const string WebConfigFilename = "web.config";

        /// <summary>
        /// Name of the machinge.config file
        /// </summary>
        public const string MachineConfigFilename = "machine.config";

        /// <summary>
        /// Path to the default web.config file
        /// </summary>
        public const string WebConfigDefaultLocFormat = @"{0}\Microsoft.NET\Framework\{1}\CONFIG\" + WebConfigFilename;

        /// <summary>
        /// Path to the default machine.config file
        /// </summary>
        public const string MachineConfigDefaultLocFormat = @"{0}\Microsoft.NET\Framework\{1}\CONFIG\" + MachineConfigFilename;
        
        /// <summary>
        /// List of element and prefixes definitions loaded from the configuration files
        /// </summary>
        private HashSet<TagPrefixDefinition> definitions;

        /// <summary>
        /// Project item for which is the list relevant
        /// </summary>
        private ProjectItem projectItem;

        /// <summary>
        /// Solution in which the project item belongs
        /// </summary>
        private Solution solution;        

        /// <summary>
        /// Cache of WebConfig for various project items
        /// </summary>
        private static Dictionary<ProjectItem, WebConfig> cache;

        static WebConfig() {
            cache = new Dictionary<ProjectItem, WebConfig>();
        }

        /// <summary>
        /// Returns instance of WebConfig valid for given project item
        /// </summary>        
        public static WebConfig Get(ProjectItem projectItem, Solution solution) {
            if (projectItem == null) throw new ArgumentNullException("projectItem");
            if (solution == null) throw new ArgumentNullException("solution");
            
            if (!cache.ContainsKey(projectItem)) {
                cache.Add(projectItem, new WebConfig(projectItem, solution));
            }
            cache[projectItem].Update();           

            return cache[projectItem];
        }

        private WebConfig(ProjectItem projectItem, Solution solution) {
            if (projectItem == null) throw new ArgumentNullException("projectItem");
            if (solution == null) throw new ArgumentNullException("solution");

            this.projectItem = projectItem;
            this.solution = solution;
            this.definitions = new HashSet<TagPrefixDefinition>();
        }

        /// <summary>
        /// Creates new WebConfig object
        /// </summary>
        /// <param name="projectItem">Project item currently processed (used to determine, which web.config files apply)</param>
        /// <param name="solution">Parent solution</param>
        private void Update() {
            // get list of all web.config files
            List<string> configs = GetOrderedConfigFiles(projectItem);
            HashSet<TagPrefixDefinition> newDefinitions = new HashSet<TagPrefixDefinition>();
            HashSet<TagPrefixDefinition> refoundDefinitions = new HashSet<TagPrefixDefinition>();

            // add standard definitions
            var webControls = new TagPrefixAssemblyDefinition(
                "System.Web, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Web.UI.WebControls",
                "asp");
            if (!definitions.Contains(webControls)) newDefinitions.Add(webControls); else refoundDefinitions.Add(webControls);

            var ui = new TagPrefixAssemblyDefinition(
                "System.Web, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Web.UI",
                "asp");
            if (!definitions.Contains(ui)) newDefinitions.Add(ui); else refoundDefinitions.Add(ui);
           
            // add element definitions from web.config files
            XPathExpression expr = XPathExpression.Compile("/configuration/system.web/pages/controls/add");
            foreach (string file in configs) {
                XPathDocument doc = new XPathDocument(file);
                if (doc == null) continue;

                XPathNavigator navigator = doc.CreateNavigator();
                if (navigator == null) continue;

                XPathNodeIterator result = (XPathNodeIterator)navigator.Evaluate(expr);
                if (result == null) continue;

                while (result.MoveNext()) {            
                    string tagPrefix = result.Current.GetAttribute("tagPrefix", "");
                    string tagName = result.Current.GetAttribute("tagName", "");
                    string namespaceName = result.Current.GetAttribute("namespace", "");
                    string assembly = result.Current.GetAttribute("assembly", "");
                    string source = result.Current.GetAttribute("src", "");

                    TagPrefixDefinition def;
                    if (!string.IsNullOrEmpty(source)) { // source definition - specified tag name, tag prefix and source file with the definition of the type
                        def = new TagPrefixSourceDefinition(projectItem, solution, tagName, source, tagPrefix);                        
                    } else { // assembly definition - specified assembly fullname, type's namespace and tag prefix
                        def = new TagPrefixAssemblyDefinition(assembly, namespaceName, tagPrefix);                        
                    }
                    if (!definitions.Contains(def)) newDefinitions.Add(def); else refoundDefinitions.Add(def);
                }
            }           

            foreach (var def in definitions.Except(refoundDefinitions).ToList()) {
                definitions.Remove(def);
            }

            foreach (TagPrefixDefinition newDef in newDefinitions) {
                newDef.Load();
                definitions.Add(newDef);
            }
        }

        /// <summary>
        /// Determines whether specified attribute has specified type
        /// </summary>
        /// <param name="prefix">Prefix of the element where attribute is located</param>
        /// <param name="element">Element name</param>
        /// <param name="attribute">Attribute name</param>
        /// <param name="targetType">Type to check</param>
        /// <param name="propInfo">Output - true, if given property has [Localizable(false)] set</param>
        /// <returns>True, if attribute has specified type, null in case of inconclusive results, false otherwise</returns>
        public bool? IsTypeof(string prefix, string element, string attribute, Type targetType, out bool isLocalizableFalse) {
            isLocalizableFalse = false;
            
            // looking for exact match in source definitions
            TagPrefixDefinition exclusiveDefinition = null;
            foreach (TagPrefixSourceDefinition definition in definitions.OfType<TagPrefixSourceDefinition>()) {
                if (definition.TagPrefix == prefix && definition.TagName == element) {
                    exclusiveDefinition = definition;
                    break;
                }
            }            
           
            if (exclusiveDefinition != null) { // found -> resolve the type
                return exclusiveDefinition.Resolve(element, attribute, targetType, out isLocalizableFalse);
            } else { // not found -> try all definitions and return first conclusive (true or false) result
                foreach (TagPrefixDefinition definition in definitions) {
                    if (definition.TagPrefix == prefix) {
                        bool? result = definition.Resolve(element, attribute, targetType, out isLocalizableFalse);
                        if (result != null) return result;
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// Adds the definitions to the list (called on Register directive)
        /// </summary>        
        public void AddTagPrefixDefinition(TagPrefixDefinition def) {
            if (def == null) throw new ArgumentNullException("def");

            definitions.Add(def);
        }
       
        /// <summary>
        /// Returns list of web.config paths that apply to the given project item, ordered from the least significant (top directory)
        /// to the most significant (same directory)
        /// </summary>        
        private List<string> GetOrderedConfigFiles(ProjectItem projectItem) {
            List<string> list = new List<string>();
            ProjectItem currentProjectItem = projectItem;

            // list directories up and look for web.config files
            while (true) {
                foreach (ProjectItem item in currentProjectItem.Collection) {
                    if (item.Name.ToLower() == WebConfigFilename) {
                        list.Add(item.GetFullPath());
                    }
                }
                if (!(currentProjectItem.Collection.Parent is ProjectItem)) break;
                
                currentProjectItem = (ProjectItem)currentProjectItem.Collection.Parent;
            }         
            
            // add .NET default machine.config file, located in %SYSTEMROOT%\Microsoft.NET\Framework\v1.2.3\CONFIG\
            string machineDefault = string.Format(MachineConfigDefaultLocFormat, Environment.GetEnvironmentVariable("systemroot"),
                string.Format("v{0}.{1}.{2}", Environment.Version.Major, Environment.Version.Minor, Environment.Version.Build));
            if (File.Exists(machineDefault)) list.Add(machineDefault);  

            // add .NET default web.config file, located in %SYSTEMROOT%\Microsoft.NET\Framework\v1.2.3\CONFIG\
            string netDefault = string.Format(WebConfigDefaultLocFormat, Environment.GetEnvironmentVariable("systemroot"), 
                string.Format("v{0}.{1}.{2}", Environment.Version.Major, Environment.Version.Minor, Environment.Version.Build));
            if (File.Exists(netDefault)) list.Add(netDefault);

            // put least significant configuration first and thus enable to override settings
            list.Reverse();
            return list;
        }
    }
}
