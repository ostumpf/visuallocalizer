using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;
using VisualLocalizer.Library;
using VisualLocalizer.Library.AspxParser;

namespace VisualLocalizer.Components {
    internal abstract class AbstractResultItem {
        public AbstractResultItem() {
            MoveThisItem = true;
        }

        public bool ComesFromDesignerFile { get; set; }
        public bool ComesFromClientComment { get; set; }
        public bool MoveThisItem { get; set; }
        public bool IsWithinLocalizableFalse { get; set; }        
        public bool IsMarkedWithUnlocalizableComment { get; set; }
        public ProjectItem SourceItem { get; set; }
        public ResXProjectItem DestinationItem { get; set; }
        public TextSpan ReplaceSpan { get; set; }        
        public int AbsoluteCharOffset { get; set; }
        public int AbsoluteCharLength { get; set; }
        public string Value { get; set; }
        public string Context { get; set; }
        public int ContextRelativeLine { get; set; }
        public string Key { get; set; }
    }

    internal abstract class CodeStringResultItem : AbstractResultItem {                
        public bool WasVerbatim { get; set; }
        public string ErrorText { get; set; }
        public string ClassOrStructElementName { get; set; }       

        public abstract string GetReferenceText(ReferenceString referenceText);
        public abstract List<string> GetKeyNameSuggestions();
        public abstract NamespacesList GetUsedNamespaces();
        public abstract string NoLocalizationComment { get; }

        protected List<string> InternalGetKeyNameSuggestions(string value, string namespaceElement, string classElement, string methodElement) {
            List<string> suggestions = new List<string>();

            StringBuilder builder1 = new StringBuilder();
            StringBuilder builder2 = new StringBuilder();
            bool upper = true;

            foreach (char c in value)
                if (c.CanBePartOfIdentifier()) {
                    if (upper) {
                        builder1.Append(char.ToUpperInvariant(c));
                    } else {
                        builder1.Append(c);
                    }
                    builder2.Append(c);
                    upper = false;
                } else {
                    upper = true;
                    builder2.Append('_');
                }

            string valueKey1 = builder1.ToString();
            string valueKey2 = builder2.ToString();

            suggestions.Add(valueKey1);
            suggestions.Add(valueKey2);

            if (!string.IsNullOrEmpty(methodElement)) {
                suggestions.Add(methodElement);

                suggestions.Add(methodElement + "_" + valueKey1);
                suggestions.Add(methodElement + "_" + valueKey2);
            }
            if (!string.IsNullOrEmpty(classElement)) {
                suggestions.Add(classElement + "_" + valueKey1);
                suggestions.Add(classElement + "_" + valueKey2);
            }
            if (!string.IsNullOrEmpty(classElement) && !string.IsNullOrEmpty(methodElement)) {
                suggestions.Add(classElement + "_" + methodElement + "_" + valueKey1);
                suggestions.Add(classElement + "_" + methodElement + "_" + valueKey2);
            }

            if (namespaceElement != null) {
                string nmspc = namespaceElement.Replace('.', '_');

                if (!string.IsNullOrEmpty(methodElement)) {
                    suggestions.Add(nmspc + "_" + methodElement + "_" + valueKey1);
                    suggestions.Add(nmspc + "_" + methodElement + "_" + valueKey2);

                    if (!string.IsNullOrEmpty(classElement)) {
                        suggestions.Add(nmspc + "_" + classElement + "_" + methodElement + "_" + valueKey1);
                        suggestions.Add(nmspc + "_" + classElement + "_" + methodElement + "_" + valueKey2);
                    }
                }
            }

            for (int i = 0; i < suggestions.Count; i++)
                if (!suggestions[i].IsValidIdentifier())
                    suggestions[i] = "_" + suggestions[i];

            return suggestions;
        }
    }

    internal sealed class CSharpStringResultItem : CodeStringResultItem {
        public string MethodElementName { get; set; }
        public string VariableElementName { get; set; }                
        public CodeNamespace NamespaceElement { get; set; }

        public override string GetReferenceText(ReferenceString referenceText) {
            return (string.IsNullOrEmpty(referenceText.NamespacePart) ? "" : referenceText.NamespacePart + ".") + referenceText.ClassPart + "." + referenceText.KeyPart;
        }

        public override List<string> GetKeyNameSuggestions() {
            return InternalGetKeyNameSuggestions(Value, NamespaceElement == null ? null : (NamespaceElement as CodeNamespace).FullName,
                        ClassOrStructElementName, MethodElementName == null ? VariableElementName : MethodElementName);
        }

        public override NamespacesList GetUsedNamespaces() {
            return NamespaceElement.GetUsedNamespaces(SourceItem);
        }

        public override string NoLocalizationComment { get { return StringConstants.CSharpLocalizationComment; } }
    }

    internal sealed class AspNetStringResultItem : CodeStringResultItem {
        public NamespacesList DeclaredNamespaces { get; set; }
        public bool ComesFromElement { get; set; }
        public bool ComesFromInlineExpression { get; set; }
        public string ElementPrefix { get; set; }
        public bool LocalizabilityProved { get; set; }
        public bool ComesFromPlainText { get; set; }
        public bool ComesFromDirective { get; set; }
        public bool ComesFromCodeBlock { get; set; }

        public override string GetReferenceText(ReferenceString referenceText) {
            if (!ComesFromCodeBlock) {
                if (SourceItem.ContainingProject.Kind.ToUpper() == StringConstants.WebSiteProject) {
                    return string.Format(StringConstants.AspElementExpressionFormat, referenceText.NamespacePart, referenceText.ClassPart, referenceText.KeyPart);
                } else {
                    return string.Format(StringConstants.AspElementReferenceFormat,
                        (string.IsNullOrEmpty(referenceText.NamespacePart) ? "" : referenceText.NamespacePart + ".") + referenceText.ClassPart + "." + referenceText.KeyPart);
                }
            } else {
                return (string.IsNullOrEmpty(referenceText.NamespacePart) ? "" : referenceText.NamespacePart + ".") + referenceText.ClassPart + "." + referenceText.KeyPart;
            }
        }

        public override List<string> GetKeyNameSuggestions() {
            return InternalGetKeyNameSuggestions(Value, null, ClassOrStructElementName, null);
        }

        public override NamespacesList GetUsedNamespaces() {
            return DeclaredNamespaces;
        }

        public override string NoLocalizationComment {
            get {
                if (!ComesFromCodeBlock) {
                    return StringConstants.AspNetLocalizationComment;
                } else {
                    return StringConstants.CSharpLocalizationComment;
                }
            }
        }
    }

    internal abstract class CodeReferenceResultItem : AbstractResultItem {
        public string FullReferenceText { get; set; }
        public string OriginalReferenceText { get; set; }
        public string KeyAfterRename { get; set; }

        public abstract string GetInlineValue();
        public abstract TextSpan GetInlineReplaceSpan(bool strictText, out int absoluteStartIndex, out int absoluteLength);
        public abstract string GetReferenceAfterRename(string newKey);
    }

    internal sealed class CSharpCodeReferenceResultItem : CodeReferenceResultItem {
        public override string GetInlineValue() {
            return "\"" + Value.ConvertCSharpUnescapeSequences() + "\"";
        }

        public override TextSpan GetInlineReplaceSpan(bool strictText, out int absoluteStartIndex, out int absoluteLength) {
            absoluteStartIndex = AbsoluteCharOffset;
            absoluteLength = AbsoluteCharLength;
            return ReplaceSpan;            
        }

        public override string GetReferenceAfterRename(string newKey) {
            string prefix = OriginalReferenceText.Substring(0, OriginalReferenceText.LastIndexOf('.'));
            return prefix + "." + newKey;
        }
    }

    internal sealed class AspNetCodeReferenceResultItem : CodeReferenceResultItem {
        public bool ComesFromInlineExpression { get; set; }
        public bool ComesFromWebSiteResourceReference { get; set; }
        public BlockSpan InlineReplaceSpan { get; set; }        

        public override string GetInlineValue() {
            if (ComesFromInlineExpression) {
                return Value.ConvertAspNetUnescapeSequences();
            } else {
                return "\"" + Value.ConvertCSharpUnescapeSequences() + "\"";
            }
        }

        public override TextSpan GetInlineReplaceSpan(bool strictText, out int absoluteStartIndex, out int absoluteLength) {
            if (strictText) {
                absoluteStartIndex = AbsoluteCharOffset;
                absoluteLength = AbsoluteCharLength;
                return ReplaceSpan;
            } else {
                if (ComesFromInlineExpression) {
                    absoluteStartIndex = InlineReplaceSpan.AbsoluteCharOffset;
                    absoluteLength = InlineReplaceSpan.AbsoluteCharLength;
                    return InlineReplaceSpan.GetTextSpan();
                } else {
                    absoluteStartIndex = AbsoluteCharOffset;
                    absoluteLength = AbsoluteCharLength;
                    return ReplaceSpan;
                }
            }
        }

        public override string GetReferenceAfterRename(string newKey) {
            char splitChar;
            if (ComesFromWebSiteResourceReference) {
                splitChar = ',';
            } else {
                splitChar = '.';
            }

            string prefix = OriginalReferenceText.Substring(0, OriginalReferenceText.LastIndexOf(splitChar));
            return prefix + splitChar + newKey;
        }
    }
}
