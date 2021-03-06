﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE80;
using EnvDTE;
using System.Collections;
using VisualLocalizer.Library.Components;

namespace VisualLocalizer.Library.Extensions {

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
        /// Returns header text of the function
        /// </summary>        
        public static string GetHeaderText(this CodeFunction2 codeFunction) {
            if (codeFunction == null) throw new ArgumentNullException("codeFunction");

            TextPoint startPoint = codeFunction.GetStartPoint(vsCMPart.vsCMPartHeader);
            TextPoint endPoint = codeFunction.GetStartPoint(vsCMPart.vsCMPartBody);

            EditPoint ep = startPoint.CreateEditPoint();
            if (ep == null)
            {
                return null;
            }
            else
            {
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
        /// Returns inner text of the variable (modifiers, name and initializer)
        /// </summary> 
        public static string GetText(this CodeProperty codeProperty) {
            if (codeProperty == null) throw new ArgumentNullException("codeProperty");

            TextPoint startPoint = codeProperty.GetStartPoint(vsCMPart.vsCMPartBody);
            TextPoint endPoint = codeProperty.GetEndPoint(vsCMPart.vsCMPartBody);

            EditPoint ep = startPoint.CreateEditPoint();

            if (ep == null) {
                return null;
            } else {
                return ep.GetText(endPoint);
            }
        }

        /// <summary>
        /// Returns true if given getter or setter is auto-generated
        /// </summary>        
        public static bool IsAutoGenerated(this CodeFunction func) {
            if (func == null) throw new ArgumentNullException("func");
            try {
                func.GetStartPoint(vsCMPart.vsCMPartBody);
                return false;
            } catch (Exception) {
                return true;
            }
        }


        /// <summary>
        /// Returns true if given element is decorated with [Localizable(false)]
        /// </summary>        
        public static bool HasLocalizableFalseAttribute(this CodeElement element) {
            if (element == null) throw new ArgumentNullException("element");
            bool set = false;
            try {
                switch (element.Kind) {
                    case vsCMElement.vsCMElementClass:
                        set = AttributesContainLocalizableFalse((element as CodeClass).Attributes);
                        break;
                    case vsCMElement.vsCMElementStruct:
                        set = AttributesContainLocalizableFalse((element as CodeStruct).Attributes);
                        break;
                    case vsCMElement.vsCMElementModule:
                        set = AttributesContainLocalizableFalse((element as CodeClass).Attributes);
                        break;
                    case vsCMElement.vsCMElementProperty:
                        set = AttributesContainLocalizableFalse((element as CodeProperty).Attributes);
                        break;
                    case vsCMElement.vsCMElementFunction:
                        set = AttributesContainLocalizableFalse((element as CodeFunction).Attributes);
                        break;
                    case vsCMElement.vsCMElementVariable:
                        set = AttributesContainLocalizableFalse((element as CodeVariable).Attributes);
                        break;
                }
            } catch { }

            return set;
        }

        /// <summary>
        /// Returns true if given set of attributes contains Localizable(false) attribute.
        /// Technically, it is possible to put any compile-time computable expression in the attribute's argument,
        /// but that would be almost impossible to code - so only explicit "false" is taken in account.
        /// </summary>
        private static bool AttributesContainLocalizableFalse(CodeElements elements) {
            if (elements == null) return false;

            bool contains = false;
            foreach (CodeAttribute2 attr in elements) {
                if (attr.FullName == "System.ComponentModel.LocalizableAttribute" && attr.Arguments.Count == 1) {
                    IEnumerator enumerator = attr.Arguments.GetEnumerator();
                    enumerator.MoveNext();

                    CodeAttributeArgument arg = enumerator.Current as CodeAttributeArgument;                    
                    if (arg.Value.Trim().ToLower() == "false") {
                        contains = true;
                        break;
                    }
                }
            }

            return contains;
        }

        /// <summary>
        /// Returns class, struct or module, where given method in defined
        /// </summary>        
        public static CodeElement2 GetClass(this CodeFunction2 codeFunction) {
            if (codeFunction == null) throw new ArgumentNullException("codeFunction");

            CodeElement2 parent = codeFunction.Parent as CodeElement2;
            if (parent is CodeProperty) {
                return GetClassInternal((CodeElement2)((CodeProperty)parent).Parent);
            } else {
                return GetClassInternal(parent);
            }
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
                list.Add(codeNamespace.FullName, null, false);

                AddUsedNamespacesToList(codeNamespace.Children, list);

                CodeNamespace parent = GetNamespace(codeNamespace as CodeElement);
                GetUsedNamespacesInternal(parent, item, list);
            } else { // top level namespace - only add top level import statements
                bool fileOpened;
                FileCodeModel model = item.GetCodeModel(false, false, out fileOpened); 
                if (model != null) AddUsedNamespacesToList(model.CodeElements, list);
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
                    list.Add(codeImport.Namespace, codeImport.Alias, true);
                }
            }
        }
    }

    
}
