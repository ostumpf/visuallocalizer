using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE80;
using EnvDTE;

namespace VisualLocalizer.Library {

    /// <summary>
    /// Container for extension methods working with CodeModel-like objects. 
    /// </summary>
    public static class CodeModelEx {

        /// <summary>
        /// Returns inner text of the function (no header, no braces)
        /// </summary>        
        public static string GetText(this CodeFunction2 codeFunction) {
            if (codeFunction == null) throw new ArgumentNullException("codeFunction");
        
            TextPoint startPoint = codeFunction.GetStartPoint(vsCMPart.vsCMPartBody);
            TextPoint endPoint = codeFunction.GetEndPoint(vsCMPart.vsCMPartBody);

            EditPoint ep = startPoint.CreateEditPoint();
            if (ep == null) {
                return null;
            } else {
                return ep.GetText(endPoint);
            }                       
        }

        /// <summary>
        /// Returns inner text of the variable (modifiers, name and initializer)
        /// </summary> 
        public static string GetText(this CodeVariable2 codeVariable) {
            if (codeVariable == null) throw new ArgumentNullException("codeVariable");

            TextPoint startPoint = codeVariable.StartPoint;
            TextPoint endPoint = codeVariable.EndPoint;

            EditPoint ep = startPoint.CreateEditPoint();

            if (ep == null) {
                return null;
            } else {
                return ep.GetText(endPoint);
            }  
        }

        /// <summary>
        /// Returns class, struct or module, where given method in defined
        /// </summary>        
        public static CodeElement2 GetClass(this CodeFunction2 codeFunction) {
            if (codeFunction == null) throw new ArgumentNullException("codeFunction");

            CodeElement2 parent = codeFunction.Parent as CodeElement2;
            return GetClassInternal(parent);
        }

        /// <summary>
        /// Returns class, struct or module, where given variable in defined
        /// </summary>        
        public static CodeElement2 GetClass(this CodeVariable2 codeVariable) {
            if (codeVariable == null) throw new ArgumentNullException("codeVariable");

            CodeElement2 parent = codeVariable.Parent as CodeElement2;
            return GetClassInternal(parent);
        }

        /// <summary>
        /// Returns class, struct or module, where given property in defined
        /// </summary>    
        public static CodeElement2 GetClass(this CodeProperty codeProperty) {
            if (codeProperty == null) throw new ArgumentNullException("codeProperty");

            CodeElement2 parent = codeProperty.Parent as CodeElement2;
            return GetClassInternal(parent);
        }

        /// <summary>
        /// Returns given element if it's class, struct or module, null otherwise
        /// </summary>
        private static CodeElement2 GetClassInternal(CodeElement2 parent) {
            if (parent == null) return null;

            if (parent.Kind == vsCMElement.vsCMElementClass || parent.Kind == vsCMElement.vsCMElementModule ||
                parent.Kind == vsCMElement.vsCMElementStruct) {
                return parent;
            } else {
                return null;
            }     
        }

        /// <summary>
        /// Returns namespace, where code element belongs or null, if no such namespace exists
        /// </summary>        
        public static CodeNamespace GetNamespace(this CodeElement codeElement) {
            if (codeElement == null) throw new ArgumentNullException("codeElement");
            object parent = null;

            if (codeElement is CodeClass) {
                parent = (codeElement as CodeClass).Parent;
            } else if (codeElement is CodeStruct) {
                parent = (codeElement as CodeStruct).Parent;
            } else if (codeElement is CodeNamespace) {
                parent = (codeElement as CodeNamespace).Parent;
            } else if (codeElement is CodeVariable) {
                parent = (codeElement as CodeVariable).Parent;
            } else if (codeElement is CodeFunction) {
                parent = (codeElement as CodeFunction).Parent;
            } else if (codeElement is CodeProperty) {
                parent = (codeElement as CodeProperty).Parent;
            }
            
            if (parent is FileCodeModel) {
                return null;
            } else {
                if (parent is CodeNamespace) {
                    return parent as CodeNamespace; 
                } else {
                    return GetNamespace(parent as CodeElement);
                }
            }            
        }

        /// <summary>
        /// Returns list of namespaces that are 'used' from the given namespace. That is, all namespaces imported
        /// in that namespace, the namespace itself and all namespaces, in which the given one is included.
        /// </summary>        
        public static NamespacesList GetUsedNamespaces(this CodeNamespace codeNamespace, ProjectItem item) {
            NamespacesList list = new NamespacesList();
            GetUsedNamespacesInternal(codeNamespace, item, list);
            return list;
        }

        private static void GetUsedNamespacesInternal(CodeNamespace codeNamespace, ProjectItem item, NamespacesList list) {
            if (codeNamespace != null) { // add itself, its children and its parents
                list.Add(codeNamespace.FullName, null);

                AddUsedNamespacesToList(codeNamespace.Children, list);

                CodeNamespace parent = GetNamespace(codeNamespace as CodeElement);
                GetUsedNamespacesInternal(parent, item, list);
            } else { // top level namespace - only add top level import statements
                if (item.FileCodeModel != null) AddUsedNamespacesToList(item.FileCodeModel.CodeElements, list);
            }
        }

        /// <summary>
        /// Adds all import statements from list
        /// </summary>        
        private static void AddUsedNamespacesToList(CodeElements elements, NamespacesList list) {
            if (elements == null) return;

            foreach (CodeElement2 element in elements) {
                if (element.Kind == vsCMElement.vsCMElementImportStmt) {
                    CodeImport codeImport = (CodeImport)element;
                    list.Add(codeImport.Namespace, codeImport.Alias);
                }
            }
        }
    }

    
}
