using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;

namespace VisualLocalizer.Library {
    public class NamespacesList : List<UsedNamespaceItem> {
        public void Add(string namespaceName, string alias) {
            Add(new UsedNamespaceItem(namespaceName, alias));
        }

        public bool ContainsNamespace(string namespaceName) {
            foreach (var item in this)
                if (item.Namespace == namespaceName) return true;
            return false;
        }

        public string GetAlias(string namespaceName) {
            foreach (var item in this)
                if (item.Namespace == namespaceName) return item.Alias;
            return null;
        }

        public string GetNamespace(string alias) {
            foreach (var item in this)
                if (item.Alias == alias) return item.Namespace;
            return null;
        }

        public bool ResolveNewElement(string newNamespace, string newClass, string newKey, Project project, out string referenceText) {
            referenceText = newClass + "." + newKey;
            bool addNamespace = true;

            foreach (UsedNamespaceItem item in this) {
                string fullName = item.Namespace + "." + newClass;
                CodeType codeType = null;
                try {
                    codeType = project.CodeModel.CodeTypeFromFullName(fullName);
                } catch {
                    codeType = null;
                }
                if (codeType != null) {
                    if (item.Namespace == newNamespace) {
                        addNamespace = false;
                        if (!string.IsNullOrEmpty(item.Alias)) referenceText = item.Alias + "." + referenceText;
                    } else {
                        addNamespace = false;
                        string newAlias = GetAlias(newNamespace);
                        if (!string.IsNullOrEmpty(newAlias)) {
                            referenceText = newAlias + "." + referenceText;
                        } else {
                            referenceText = newNamespace + "." + referenceText;
                        }
                    }
                    break;
                }
            }

            return addNamespace;
        }

        public UsedNamespaceItem ResolveNewReference(string referenceClassAndNmspc, Project project) {
            foreach (UsedNamespaceItem item in this) {
                string fullName = item.Namespace + "." + referenceClassAndNmspc;
                CodeType codeType = null;
                try {
                    codeType = project.CodeModel.CodeTypeFromFullName(fullName);
                } catch {
                    codeType = null;
                }
                if (codeType != null) {
                    return item;              
                }
            }
            return null;
        }
    }

    public class UsedNamespaceItem {
        public UsedNamespaceItem(string namespaceName, string alias) {
            this.Namespace = namespaceName;
            this.Alias = alias;
        }

        public string Namespace { get; set; }
        public string Alias { get; set; }
    }
}
