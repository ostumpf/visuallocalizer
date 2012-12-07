using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;
using VisualLocalizer.Library;
using VisualLocalizer.Library.AspxParser;
using System.Reflection;
using System.Runtime.InteropServices;
using VisualLocalizer.Settings;

namespace VisualLocalizer.Components {
    internal abstract class AbstractResultItem {
        public AbstractResultItem() {
            MoveThisItem = true;
        }

        [LocalizationWeight(0.2)]
        public bool ComesFromDesignerFile { get; set; }

        [LocalizationWeight(0.6)]
        public bool ComesFromClientComment { get; set; }        

        [LocalizationWeight(0.4)]
        public bool IsWithinLocalizableFalse { get; set; }

        [LocalizationWeight(0.8)]
        public bool IsMarkedWithUnlocalizableComment { get; set; }

        public bool MoveThisItem { get; set; }
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
        private const int MAX_RATIO = 100;

        public bool WasVerbatim { get; set; }
        public string ErrorText { get; set; }
        public string ClassOrStructElementName { get; set; }       

        public abstract string GetReferenceText(ReferenceString referenceText);
        public abstract List<string> GetKeyNameSuggestions();
        public abstract NamespacesList GetUsedNamespaces();
        public abstract string NoLocalizationComment { get; }
        public abstract bool MustUseFullName { get; }
        public abstract void AddUsingBlock(IVsTextLines textLines); 

        public int GetLocalizationRatio() {
            Type t = this.GetType();
        
            int count = 0;
            double sum = 0;

            countLocProbability(t.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Instance), ref count, ref sum);
            countLocProbability(t.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.GetField | BindingFlags.Instance), ref count, ref sum);
            CustomLocProbabilityEval(ref count, ref sum);

            bool onlyWhitespace = true;
            bool onlyDigits = true;
            bool onlyCapitalsAndInterpunction = true;

            foreach (char c in Value) {
                if (!char.IsWhiteSpace(c)) {
                    onlyWhitespace = false;
                    if (!char.IsDigit(c) && c != ',' && c != '.') onlyDigits = false;
                    if (!char.IsUpper(c) && !char.IsPunctuation(c) && !char.IsSymbol(c)) onlyCapitalsAndInterpunction = false;
                }
            }

            if (onlyWhitespace || onlyDigits || onlyCapitalsAndInterpunction) {
                count++;
                sum += 1;
            }            

            if (count == 0) {
                return MAX_RATIO;
            } else {
                int result = (int)(MAX_RATIO - MAX_RATIO * (sum / count));
                return Math.Min(100, Math.Max(0, result));
            }
        }

        private void countLocProbability(MemberInfo[] members, ref int count, ref double sum) {
            foreach (MemberInfo info in members) {
                if (info.MemberType != MemberTypes.Field && info.MemberType != MemberTypes.Property) continue;

                PropertyInfo propInfo = info as PropertyInfo;
                FieldInfo fieldInfo = info as FieldInfo;
                bool value = false;

                if (propInfo != null && propInfo.PropertyType.IsAssignableFrom(typeof(bool))) value = (bool)propInfo.GetValue(this, null);
                if (fieldInfo != null && fieldInfo.FieldType.IsAssignableFrom(typeof(bool))) value = (bool)fieldInfo.GetValue(this);
                if (!value) continue;

                object[] attrs = info.GetCustomAttributes(typeof(LocalizationWeightAttribute), true);
                if (attrs.Length == 1) {
                    LocalizationWeightAttribute lw = (LocalizationWeightAttribute)attrs[0];
                    count++;
                    sum += lw.Weight;
                }
            }

        }

        protected virtual void CustomLocProbabilityEval(ref int count, ref double sum) {
        }

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

        public override bool MustUseFullName { 
            get { return false; }
        }

        public override void AddUsingBlock(IVsTextLines textLines) {
            SourceItem.Document.AddUsingBlock(DestinationItem.Namespace);            
        }
    }

    internal sealed class AspNetStringResultItem : CodeStringResultItem {
        public NamespacesList DeclaredNamespaces { get; set; }
                
        public bool ComesFromElement { get; set; }

        [LocalizationWeight(0.8)]
        public bool ComesFromInlineExpression { get; set; }
        
        public bool LocalizabilityProved { get; set; }

        [LocalizationWeight(0.4)]
        public bool ComesFromPlainText { get; set; }

        [LocalizationWeight(0.8)]
        public bool ComesFromDirective { get; set; }

        [LocalizationWeight(0.1)]
        public bool ComesFromCodeBlock { get; set; }

        public string ElementPrefix { get; set; }        
        
        public override string GetReferenceText(ReferenceString referenceText) {
            if (!ComesFromCodeBlock && !ComesFromInlineExpression) {
                string reference;
                if (SourceItem.ContainingProject.Kind.ToUpper() == StringConstants.WebSiteProject) {
                    reference = string.Format(StringConstants.AspElementExpressionFormat, referenceText.NamespacePart, referenceText.ClassPart, referenceText.KeyPart);
                } else {
                    reference = string.Format(StringConstants.AspElementReferenceFormat,
                        (string.IsNullOrEmpty(referenceText.NamespacePart) ? "" : referenceText.NamespacePart + ".") + referenceText.ClassPart + "." + referenceText.KeyPart);
                }
                if (ComesFromPlainText) {
                    return string.Format(StringConstants.AspLiteralFormat, reference);
                } else {
                    return reference;
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

        protected override void CustomLocProbabilityEval(ref int count, ref double sum) {
            if (string.IsNullOrEmpty(ElementPrefix) && ComesFromElement) {
                count++;
                sum += 2;
            }
            if (ComesFromElement && !LocalizabilityProved && SettingsObject.Instance.UseReflectionInAsp) {
                count++;
                sum += 1;
            }
        }

        public override bool MustUseFullName {
            get {
                bool forceAspExpression = SourceItem != null;
                forceAspExpression = forceAspExpression && SourceItem.ContainingProject.Kind.ToUpper() == StringConstants.WebSiteProject;
                forceAspExpression = forceAspExpression && !ComesFromCodeBlock && !ComesFromInlineExpression;
                return forceAspExpression;
            }
        }

        public override void AddUsingBlock(IVsTextLines textLines) {            
            string text = string.Format(StringConstants.AspImportDirectiveFormat, DestinationItem.Namespace);

            object otp;
            int hr = textLines.CreateTextPoint(0, 0, out otp);
            Marshal.ThrowExceptionForHR(hr);

            TextPoint tp = (TextPoint)otp;
            tp.CreateEditPoint().Insert(text);
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

    [AttributeUsage(AttributeTargets.Property|AttributeTargets.Field)]
    internal sealed class LocalizationWeightAttribute : System.Attribute {
        public LocalizationWeightAttribute(double weight) {
            if (weight < 0 || weight > 1) throw new ArgumentOutOfRangeException("weight");

            this.Weight = weight;
        }

        public double Weight {
            get;
            private set;
        }
    }
}
