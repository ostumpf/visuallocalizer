using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE80;
using EnvDTE;

namespace VisualLocalizer.Library {
    public static class CodeModelEx {

        public static string GetText(this CodeFunction2 codeFunction) {
            try {
                TextPoint startPoint = codeFunction.GetStartPoint(vsCMPart.vsCMPartBody);
                TextPoint endPoint = codeFunction.GetEndPoint(vsCMPart.vsCMPartBody);

                string functionText = startPoint.CreateEditPoint().GetText(endPoint);
                return functionText;
            } catch (Exception) {
                return null;
            }            
        }

        public static string GetText(this CodeProperty codeProperty) {
            try {
                TextPoint startPoint = codeProperty.GetStartPoint(vsCMPart.vsCMPartBody);
                TextPoint endPoint = codeProperty.GetEndPoint(vsCMPart.vsCMPartBody);

                string functionText = startPoint.CreateEditPoint().GetText(endPoint);
                return functionText;
            } catch (Exception) {
                return null;
            }     
        }

        public static string GetText(this CodeVariable2 codeVariable) {
            try {
                TextPoint startPoint = codeVariable.StartPoint;
                TextPoint endPoint = codeVariable.EndPoint;

                string functionText = startPoint.CreateEditPoint().GetText(endPoint);

                return functionText;
            } catch (Exception) {
                return null;
            }
        }

        public static CodeElement2 GetClass(this CodeFunction2 codeFunction) {
            object parent = codeFunction.Parent;
            CodeElement2 classElement = null;

            if (parent is CodeClass2) {
                classElement = (CodeElement2)parent;
            } else if (parent is CodeStruct2) {
                classElement = (CodeElement2)parent;
            }

            return classElement;
        }

        public static CodeElement2 GetClass(this CodeVariable2 codeVariable) {
            object parent = codeVariable.Parent;
            CodeElement2 classElement = null;

            if (parent is CodeClass2) {
                classElement = (CodeElement2)parent;
            } else if (parent is CodeStruct2) {
                classElement = (CodeElement2)parent;
            }

            return classElement;
        }

        public static CodeElement2 GetClass(this CodeProperty codeProperty) {
            object parent = codeProperty.Parent;
            CodeElement2 classElement = null;

            if (parent is CodeClass2) {
                classElement = (CodeElement2)parent;
            } else if (parent is CodeStruct2) {
                classElement = (CodeElement2)parent;
            }

            return classElement;
        }

        public static CodeNamespace GetNamespace(this CodeElement codeElement) {
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

        public static NamespacesList GetUsedNamespaces(this CodeNamespace codeNamespace, ProjectItem item) {
            NamespacesList list = new NamespacesList();
            GetUsedNamespacesInternal(codeNamespace, item, list);
            return list;
        }

        private static void GetUsedNamespacesInternal(CodeNamespace codeNamespace, ProjectItem item, NamespacesList list) {
            if (codeNamespace != null) {
                list.Add(codeNamespace.FullName, null);

                AddUsedNamespacesToList(codeNamespace.Children, list);

                CodeNamespace parent = GetNamespace(codeNamespace as CodeElement);
                GetUsedNamespacesInternal(parent, item, list);
            } else {
                AddUsedNamespacesToList(item.FileCodeModel.CodeElements, list);
            }
        }

        private static void AddUsedNamespacesToList(CodeElements elements, NamespacesList list) {
            foreach (CodeElement2 element in elements) {
                if (element.Kind == vsCMElement.vsCMElementImportStmt) {
                    CodeImport codeImport = (CodeImport)element;
                    list.Add(codeImport.Namespace, codeImport.Alias);
                }
            }
        }
    }

    
}
