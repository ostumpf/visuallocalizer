using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;

namespace VisualLocalizer.Library {
    public class NamespacesList : List<UsedNamespaceItem> {
        private const string WebSiteProjectGuid = "{E24C65DC-7377-472B-9ABA-BC803B73C61A}";
        private const string GlobalWebSiteResourcesNamespace = "Resources";

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

        public bool ResolveNewElement(string designerNamespace, string designerClass, string newKey, Project project, out ReferenceString referenceText) {
            referenceText = new ReferenceString(designerClass, newKey);
            bool addUsing = true;

           /* if (designerNamespace == GlobalWebSiteResourcesNamespace && project.Kind.ToUpperInvariant() == WebSiteProjectGuid) {
                referenceText.NamespacePart = GlobalWebSiteResourcesNamespace;
                return false;
            } else {*/
                foreach (UsedNamespaceItem item in this) {
                    string fullName = item.Namespace + "." + designerClass;
                    CodeType codeType = null;
                    try {
                        codeType = project.CodeModel.CodeTypeFromFullName(fullName);
                    } catch {
                        codeType = null;
                    }
                    if (codeType != null) {
                        addUsing = false;
                        if (item.Namespace == designerNamespace) {
                            if (!string.IsNullOrEmpty(item.Alias)) referenceText.NamespacePart = item.Alias;
                        } else {
                            string newAlias = GetAlias(designerNamespace);
                            if (!string.IsNullOrEmpty(newAlias)) {
                                referenceText.NamespacePart = newAlias;
                            } else {
                                referenceText.NamespacePart = designerNamespace;
                            }
                        }
                        break;
                    }

               // }
            }
            return addUsing;
        }

        public UsedNamespaceItem ResolveNewReference(string referenceClass, Project project) {
            foreach (UsedNamespaceItem item in this) {
                if (item.Namespace == GlobalWebSiteResourcesNamespace && project.Kind.ToUpperInvariant() == WebSiteProjectGuid) {
                    return item;
                } else {
                    string fullName = item.Namespace + "." + referenceClass;
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

    public class ReferenceString {

        public ReferenceString() : this(null, null, null) { }

        public ReferenceString(string classPart, string keyPart) : this(null, classPart, keyPart) {}
        
        public ReferenceString(string namespacePart, string classPart, string keyPart) {
            this.NamespacePart = namespacePart;
            this.ClassPart = classPart;
            this.KeyPart = keyPart;
        }

        public string NamespacePart { get; set; }
        public string ClassPart { get; set; }
        public string KeyPart { get; set; }

    }
}
