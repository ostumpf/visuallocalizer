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
using System.Text.RegularExpressions;
using VisualLocalizer.Extensions;

namespace VisualLocalizer.Components {
    internal abstract class AbstractResultItem {
        public AbstractResultItem() {
            MoveThisItem = true;
        }
                
        public bool ComesFromDesignerFile { get; set; }        
        public bool ComesFromClientComment { get; set; }
        public bool IsWithinLocalizableFalse { get; set; }        
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
     
        public static Dictionary<string, LocalizationCriterion> GetCriteria() {
            var localizationCriteriaList = new Dictionary<string, LocalizationCriterion>();

            var designerFilePredicate = new LocalizationCriterion("ComesFromDesignerFile",
                "String comes from designer file",
                LocalizationCriterionAction.FORCE_DISABLE, 0,
                (item) => { return item.ComesFromDesignerFile; });

            var clientCommentPredicate = new LocalizationCriterion("ComesFromClientComment",
                "String is located in commented code",
                LocalizationCriterionAction.FORCE_DISABLE, 0,
                (item) => { return item.ComesFromClientComment; });

            var localizableFalsePredicate = new LocalizationCriterion("IsWithinLocalizableFalse",
                "String is within Localizable[false] block",
                LocalizationCriterionAction.FORCE_DISABLE, 0,
                (item) => { return item.IsWithinLocalizableFalse; });

            var unlocalizableCommentPredicate = new LocalizationCriterion("IsMarkedWithUnlocalizableComment",
                "String is marked with VL_NO_LOC",
                LocalizationCriterionAction.FORCE_DISABLE, 0,
                (item) => { return item.IsMarkedWithUnlocalizableComment; });

            localizationCriteriaList.Add(designerFilePredicate.Name, designerFilePredicate);
            localizationCriteriaList.Add(clientCommentPredicate.Name, clientCommentPredicate);
            localizationCriteriaList.Add(localizableFalsePredicate.Name, localizableFalsePredicate);
            localizationCriteriaList.Add(unlocalizableCommentPredicate.Name, unlocalizableCommentPredicate);

            return localizationCriteriaList;
        }
    }

    internal abstract class CodeStringResultItem : AbstractResultItem {
        public bool WasVerbatim { get; set; }
        public string ErrorText { get; set; }
        public string ClassOrStructElementName { get; set; }    // name of file in asp .net   

        public abstract string GetReferenceText(ReferenceString referenceText);
        public abstract List<string> GetKeyNameSuggestions();
        public abstract NamespacesList GetUsedNamespaces();
        public abstract string NoLocalizationComment { get; }
        public abstract bool MustUseFullName { get; }
        public abstract void AddUsingBlock(IVsTextLines textLines);
                
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

        public int GetLocalizationProbability(Dictionary<string, AbstractLocalizationCriterion> criteria) {
            bool disableRequest = false;
            float positiveSum = 0, negativeSum = 0;
            int positiveCount = 0, negativeCount = 0;

            foreach (var pair in criteria) {
                AbstractLocalizationCriterion crit = pair.Value;

                bool? result = crit.Eval(this);
                if (result.HasValue && result.Value) {
                    switch (crit.Action) {
                        case LocalizationCriterionAction.FORCE_ENABLE:
                            return AbstractLocalizationCriterion.MAX_LOC_PROBABILITY;
                        case LocalizationCriterionAction.FORCE_DISABLE:
                            disableRequest = true;
                            break;
                        case LocalizationCriterionAction.VALUE:
                            if (crit.Weight > 0) {
                                positiveSum += crit.Weight;
                                positiveCount++;
                            }
                            if (crit.Weight < 0) {
                                negativeSum += -crit.Weight;
                                negativeCount++;
                            }
                            break;
                        case LocalizationCriterionAction.IGNORE:                            
                            break;
                    }
                }
            }

            if (disableRequest) return 0;

            float s = 0;            
            if (positiveSum > negativeSum) {
                if (positiveCount != 0) s = positiveSum / positiveCount; 
            } else {
                if (negativeCount != 0) s = -negativeSum / negativeCount;
            }
            s /= 2;

            int x = (int)(AbstractLocalizationCriterion.MAX_LOC_PROBABILITY / 2 + s);
            if (x >= 0 && x <= AbstractLocalizationCriterion.MAX_LOC_PROBABILITY)
                return x;
            else
                return x < 0 ? 0 : AbstractLocalizationCriterion.MAX_LOC_PROBABILITY;
        }        

        public static new Dictionary<string, LocalizationCriterion> GetCriteria() {
            var localizationCriteriaList = AbstractResultItem.GetCriteria();

            var wasVerbatimPredicate = new LocalizationCriterion("WasVerbatim",
                "Is verbatim string",
                LocalizationCriterionAction.VALUE, 10, 
                (item) => { return item.WasVerbatim; });

            localizationCriteriaList.Add(wasVerbatimPredicate.Name, wasVerbatimPredicate);

            return localizationCriteriaList;
        }
    }

    internal abstract class NetStringResultItem : CodeStringResultItem {
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

        public override bool MustUseFullName {
            get { return false; }
        }

        public override void AddUsingBlock(IVsTextLines textLines) {
            SourceItem.Document.AddUsingBlock(DestinationItem.Namespace);
        }

        public static new Dictionary<string, LocalizationCriterion> GetCriteria() {
            return CodeStringResultItem.GetCriteria();
        }
    }

    internal class CSharpStringResultItem : NetStringResultItem {
        public override string NoLocalizationComment { get { return StringConstants.CSharpLocalizationComment; } }       
    }

    internal sealed class VBStringResultItem : NetStringResultItem {
        public override string NoLocalizationComment { get { return string.Empty; } }       
    }

    internal sealed class AspNetStringResultItem : CodeStringResultItem {
        public NamespacesList DeclaredNamespaces { get; set; }
       
        public bool ComesFromElement { get; set; }      
        public bool ComesFromInlineExpression { get; set; }      
        public bool LocalizabilityProved { get; set; }      
        public bool ComesFromPlainText { get; set; }       
        public bool ComesFromDirective { get; set; }       
        public bool ComesFromCodeBlock { get; set; }

        public string ElementPrefix { get; set; }
        public string ElementName { get; set; }  
        
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

        public static new Dictionary<string, LocalizationCriterion> GetCriteria() {
            var localizationCriteriaList = CodeStringResultItem.GetCriteria();

            var comesFromElementPredicate = new LocalizationCriterion("ComesFromElement",
                "String comes from ASP .NET element attribute",
                LocalizationCriterionAction.VALUE, 20,
                (item) => { var i = (item as AspNetStringResultItem); return i == null ? (bool?)null : i.ComesFromElement; });

            var comesFromInlineExpressionPredicate = new LocalizationCriterion("ComesFromInlineExpression",
                "String comes from ASP .NET inline expression",
                LocalizationCriterionAction.FORCE_ENABLE, 0,
                (item) => { var i = (item as AspNetStringResultItem); return i == null ? (bool?)null : i.ComesFromInlineExpression; });

            var localizabilityProvedPredicate = new LocalizationCriterion("LocalizabilityProved",
                "ASP.NET attribute's type is String",
                LocalizationCriterionAction.VALUE, 70,
                (item) => { var i = (item as AspNetStringResultItem); return i == null ? (bool?)null : i.LocalizabilityProved; });

            var comesFromPlainTextPredicate = new LocalizationCriterion("ComesFromPlainText",
                "String literal comes from ASP .NET plain text",
                LocalizationCriterionAction.VALUE, 20,
                (item) => { var i = (item as AspNetStringResultItem); return i == null ? (bool?)null : i.ComesFromPlainText; });

            var comesFromDirectivePredicate = new LocalizationCriterion("ComesFromDirective",
                "String literal comes from ASP .NET directive",
                LocalizationCriterionAction.VALUE, -10,
                (item) => { var i = (item as AspNetStringResultItem); return i == null ? (bool?)null : i.ComesFromDirective; });

            var comesFromCodeBlockPredicate = new LocalizationCriterion("ComesFromCodeBlock",
                "String literal comes from ASP .NET code block",
                LocalizationCriterionAction.VALUE, 0,
                (item) => { var i = (item as AspNetStringResultItem); return i == null ? (bool?)null : i.ComesFromCodeBlock; });

            localizationCriteriaList.Add(comesFromElementPredicate.Name, comesFromElementPredicate);
            localizationCriteriaList.Add(comesFromInlineExpressionPredicate.Name, comesFromInlineExpressionPredicate);
            localizationCriteriaList.Add(localizabilityProvedPredicate.Name, localizabilityProvedPredicate);
            localizationCriteriaList.Add(comesFromPlainTextPredicate.Name, comesFromPlainTextPredicate);
            localizationCriteriaList.Add(comesFromDirectivePredicate.Name, comesFromDirectivePredicate);
            localizationCriteriaList.Add(comesFromCodeBlockPredicate.Name, comesFromCodeBlockPredicate);
            return localizationCriteriaList;
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

    internal abstract class NetCodeReferenceResultItem : CodeReferenceResultItem {
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

    internal class CSharpCodeReferenceResultItem : NetCodeReferenceResultItem {
        public override string GetInlineValue() {
            return "\"" + Value.ConvertCSharpUnescapeSequences() + "\"";
        }   
    }

    internal class VBCodeReferenceResultItem : NetCodeReferenceResultItem {
        public override string GetInlineValue() {
            return "\"" + Value.ConvertVBEscapeSequences() + "\"";
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
