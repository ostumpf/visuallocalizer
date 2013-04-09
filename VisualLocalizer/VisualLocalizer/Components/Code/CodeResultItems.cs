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

    /// <summary>
    /// Base class for all result items - found string literals or references to resources
    /// </summary>
    public abstract class AbstractResultItem {
        public AbstractResultItem() {
            MoveThisItem = true;
        }
                
        /// <summary>
        /// True if result item comes from generated file        
        /// </summary>
        public bool ComesFromDesignerFile { get; set; }  
      
        /// <summary>
        /// True if result item is located within commented section
        /// </summary>
        public bool ComesFromClientComment { get; set; }

        /// <summary>
        /// True if result item is located within code block decorated with Localizable(false)
        /// </summary>
        public bool IsWithinLocalizableFalse { get; set; }        

        /// <summary>
        /// True if result item is marked with "no-localization" comment
        /// </summary>
        public bool IsMarkedWithUnlocalizableComment { get; set; }        

        /// <summary>
        /// Set during toolwindow's editing - value of checkbox column
        /// </summary>
        public bool MoveThisItem { get; set; }

        /// <summary>
        /// Project item this result item comes from
        /// </summary>
        public ProjectItem SourceItem { get; set; }

        /// <summary>
        /// Set during toolwindow's editing - value of destination column
        /// </summary>
        public ResXProjectItem DestinationItem { get; set; }

        /// <summary>
        /// Position of the result item
        /// </summary>
        public TextSpan ReplaceSpan { get; set; }        

        /// <summary>
        /// Absolute position (number of characters from beginnig of the file)
        /// </summary>
        public int AbsoluteCharOffset { get; set; }

        /// <summary>
        /// Absolute length
        /// </summary>
        public int AbsoluteCharLength { get; set; }

        /// <summary>
        /// Either string literal value or value of referenced resource key
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Several lines providing context displayed in toolwindows
        /// </summary>
        public string Context { get; set; }

        /// <summary>
        /// Index of the line in the context where result item actualy is
        /// </summary>
        public int ContextRelativeLine { get; set; }

        /// <summary>
        /// Either key set in toolwindow or referenced resource key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Language of the result item context
        /// </summary>
        public abstract LANGUAGE Language { get; set; }

        /// <summary>
        /// Returns criteria displayed in toolwindow's filter, used to calculate localization probability
        /// </summary>        
        public static Dictionary<string, LocalizationCommonCriterion> GetCriteria() {
            var localizationCriteriaList = new Dictionary<string, LocalizationCommonCriterion>();

            var designerFilePredicate = new LocalizationCommonCriterion("ComesFromDesignerFile",
                "String comes from designer file",
                LocalizationCriterionAction.FORCE_DISABLE, 0,
                (item) => { return item.ComesFromDesignerFile; });

            var clientCommentPredicate = new LocalizationCommonCriterion("ComesFromClientComment",
                "String is located in commented code",
                LocalizationCriterionAction.FORCE_DISABLE, 0,
                (item) => { return item.ComesFromClientComment; });

            var localizableFalsePredicate = new LocalizationCommonCriterion("IsWithinLocalizableFalse",
                "String is within Localizable[false] block",
                LocalizationCriterionAction.FORCE_DISABLE, 0,
                (item) => { return item.IsWithinLocalizableFalse; });

            var unlocalizableCommentPredicate = new LocalizationCommonCriterion("IsMarkedWithUnlocalizableComment",
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

    /// <summary>
    /// Represents found string literal
    /// </summary>
    public abstract class CodeStringResultItem : AbstractResultItem {
        /// <summary>
        /// True if the literal was verbatim (@) string
        /// </summary>
        public bool WasVerbatim { get; set; }

        /// <summary>
        /// Error message set in toolwindow's grid
        /// </summary>
        public string ErrorText { get; set; }

        /// <summary>
        /// Name of the class, struct or module where the string literal comes from. In ASP .NET files, file name is used.
        /// </summary>
        public string ClassOrStructElementName { get; set; } 

        /// <summary>
        /// Returns composed reference text
        /// </summary>        
        public abstract string GetReferenceText(ReferenceString referenceText);
        
        /// <summary>
        /// Returns list of suggestions for a key name
        /// </summary>        
        public abstract List<string> GetKeyNameSuggestions();

        /// <summary>
        /// Get namespaces affecting this result item
        /// </summary>        
        public abstract NamespacesList GetUsedNamespaces();

        /// <summary>
        /// Returns "no-localization" commnent used to mark string literals for future reference
        /// </summary>
        public abstract string NoLocalizationComment { get; }

        /// <summary>
        /// Returns true if full reference must be used, because omitting namespace would cause compiler error
        /// </summary>
        public abstract bool MustUseFullName { get; }

        /// <summary>
        /// Adds result item's namespace (if any) in a import block
        /// </summary>        
        public abstract void AddUsingBlock(IVsTextLines textLines);
                
        /// <summary>
        /// Returns list of suggestions for a key name
        /// </summary>        
        protected List<string> InternalGetKeyNameSuggestions(string value, string namespaceElement, string classElement, string methodElement) {
            List<string> suggestions = new List<string>();

            StringBuilder builder1 = new StringBuilder();
            StringBuilder builder2 = new StringBuilder();
            bool upper = true;

            // replace any character that cannot be part of identifier with underscore and make camel-case
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
                if (!suggestions[i].IsValidIdentifier(Language)) {
                    if (Language == LANGUAGE.VB) {
                        suggestions[i] = "x" + suggestions[i];
                    } else {
                        suggestions[i] = "_" + suggestions[i];
                    }
                }

            return suggestions;
        }

        /// <summary>
        /// Calculates localization probability from given criteria
        /// </summary>
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

        /// <summary>
        /// Returns criteria displayed in toolwindow's filter, used to calculate localization probability
        /// </summary>      
        public static new Dictionary<string, LocalizationCommonCriterion> GetCriteria() {
            var localizationCriteriaList = AbstractResultItem.GetCriteria();

            var wasVerbatimPredicate = new LocalizationCommonCriterion("WasVerbatim",
                "Is verbatim string",
                LocalizationCriterionAction.VALUE, 10, 
                (item) => { return item.WasVerbatim; });

            localizationCriteriaList.Add(wasVerbatimPredicate.Name, wasVerbatimPredicate);

            return localizationCriteriaList;
        }
    }

    /// <summary>
    /// Represents C# or VB string literal result item
    /// </summary>
    public abstract class NetStringResultItem : CodeStringResultItem {

        /// <summary>
        /// Name of the method in which the result item is located
        /// </summary>
        public string MethodElementName { get; set; }

        /// <summary>
        /// Name of the variable which the string literal initializes
        /// </summary>
        public string VariableElementName { get; set; }

        /// <summary>
        /// Namespace where the result item belongs
        /// </summary>
        public CodeNamespace NamespaceElement { get; set; }

        /// <summary>
        /// Returns composed reference text
        /// </summary>   
        public override string GetReferenceText(ReferenceString referenceText) {
            return (string.IsNullOrEmpty(referenceText.NamespacePart) ? "" : referenceText.NamespacePart + ".") + referenceText.ClassPart + "." + referenceText.KeyPart;
        }

        /// <summary>
        /// Returns list of suggestions for a key name
        /// </summary>   
        public override List<string> GetKeyNameSuggestions() {
            return InternalGetKeyNameSuggestions(Value, NamespaceElement == null ? null : (NamespaceElement as CodeNamespace).FullName,
                        ClassOrStructElementName, MethodElementName == null ? VariableElementName : MethodElementName);
        }

        /// <summary>
        /// Get namespaces affecting this result item
        /// </summary>  
        public override NamespacesList GetUsedNamespaces() {
            return NamespaceElement.GetUsedNamespaces(SourceItem);
        }

        /// <summary>
        /// Returns true if full reference must be used, because omitting namespace would cause compiler error
        /// </summary>
        public override bool MustUseFullName {
            get { return false; }
        }

        /// <summary>
        /// Adds result item's namespace (if any) in a import block
        /// </summary>  
        public override void AddUsingBlock(IVsTextLines textLines) {
            SourceItem.Document.AddUsingBlock(DestinationItem.Namespace);
        }

        public static new Dictionary<string, LocalizationCommonCriterion> GetCriteria() {
            return CodeStringResultItem.GetCriteria();
        }
    }

    /// <summary>
    /// Represents C# string literal result item
    /// </summary>
    public class CSharpStringResultItem : NetStringResultItem {
        /// <summary>
        /// Returns "no-localization" commnent used to mark string literals for future reference
        /// </summary>
        public override string NoLocalizationComment { get { return StringConstants.CSharpLocalizationComment; } }

        /// <summary>
        /// Language of the result item context
        /// </summary>
        public override LANGUAGE Language {
            get { return LANGUAGE.CSHARP; }
            set { }
        }
    }

    /// <summary>
    /// Represents VB string literal result item
    /// </summary>
    public class VBStringResultItem : NetStringResultItem {
        /// <summary>
        /// This feature is not available in VB
        /// </summary>
        public override string NoLocalizationComment { get { return string.Empty; } }

        /// <summary>
        /// Language of the result item context
        /// </summary>
        public override LANGUAGE Language {
            get { return LANGUAGE.VB; }
            set { }
        }
    }

    /// <summary>
    /// Represents ASP .NET string literal result item
    /// </summary>
    public class AspNetStringResultItem : CodeStringResultItem {
        /// <summary>
        /// Namespaces imported in the document
        /// </summary>
        public NamespacesList DeclaredNamespaces { get; set; }
       
        /// <summary>
        /// True if result item comes from attribute's value
        /// </summary>
        public bool ComesFromElement { get; set; }      

        /// <summary>
        /// True if result item comes from &lt;%$ expression
        /// </summary>
        public bool ComesFromInlineExpression { get; set; }      

        /// <summary>
        /// True if attribute's type was successfuly evaluated as a string
        /// </summary>
        public bool LocalizabilityProved { get; set; }      

        /// <summary>
        /// True if result item comes from plain text
        /// </summary>
        public bool ComesFromPlainText { get; set; } 
      
        /// <summary>
        /// True if result item comes from &lt;%@ directive
        /// </summary>
        public bool ComesFromDirective { get; set; }    
   
        /// <summary>
        /// True if result item comes from &lt;% code block
        /// </summary>
        public bool ComesFromCodeBlock { get; set; }

        private LANGUAGE _Language;
        /// <summary>
        /// Language of the result item context
        /// </summary>
        public override LANGUAGE Language {
            get { return _Language; }
            set { _Language = value; }
        }

        /// <summary>
        /// Prefix of the element where this result item comes from
        /// </summary>
        public string ElementPrefix { get; set; }

        /// <summary>
        /// Name of the element where this result item comes from
        /// </summary>
        public string ElementName { get; set; }

        /// <summary>
        /// Returns composed reference text
        /// </summary>   
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

        /// <summary>
        /// Returns list of suggestions for a key name
        /// </summary>   
        public override List<string> GetKeyNameSuggestions() {
            return InternalGetKeyNameSuggestions(Value, null, ClassOrStructElementName, null);
        }

        /// <summary>
        /// Get namespaces affecting this result item
        /// </summary>
        public override NamespacesList GetUsedNamespaces() {
            return DeclaredNamespaces;
        }

        /// <summary>
        /// Returns "no-localization" commnent used to mark string literals for future reference
        /// </summary>
        public override string NoLocalizationComment {
            get {
                if (!ComesFromCodeBlock) {
                    return StringConstants.AspNetLocalizationComment;
                } else {
                    return StringConstants.CSharpLocalizationComment;
                }
            }
        }

        /// <summary>
        /// Returns true if result item comes from WebSite project and &lt;%$ expression is used
        /// </summary>
        public override bool MustUseFullName {
            get {
                bool forceAspExpression = SourceItem != null;
                forceAspExpression = forceAspExpression && SourceItem.ContainingProject.Kind.ToUpper() == StringConstants.WebSiteProject;
                forceAspExpression = forceAspExpression && !ComesFromCodeBlock && !ComesFromInlineExpression;
                return forceAspExpression;
            }
        }

        /// <summary>
        /// Adds result item's namespace (if any) in a import block
        /// </summary>  
        public override void AddUsingBlock(IVsTextLines textLines) {            
            string text = string.Format(StringConstants.AspImportDirectiveFormat, DestinationItem.Namespace);

            object otp;
            int hr = textLines.CreateTextPoint(0, 0, out otp);
            Marshal.ThrowExceptionForHR(hr);

            TextPoint tp = (TextPoint)otp;
            tp.CreateEditPoint().Insert(text);
        }

        /// <summary>
        /// Returns criteria displayed in toolwindow's filter, used to calculate localization probability
        /// </summary>   
        public static new Dictionary<string, LocalizationCommonCriterion> GetCriteria() {
            var localizationCriteriaList = CodeStringResultItem.GetCriteria();

            var comesFromElementPredicate = new LocalizationCommonCriterion("ComesFromElement",
                "String comes from ASP .NET element attribute",
                LocalizationCriterionAction.VALUE, 20,
                (item) => { var i = (item as AspNetStringResultItem); return i == null ? (bool?)null : i.ComesFromElement; });

            var comesFromInlineExpressionPredicate = new LocalizationCommonCriterion("ComesFromInlineExpression",
                "String comes from ASP .NET inline expression",
                LocalizationCriterionAction.FORCE_ENABLE, 0,
                (item) => { var i = (item as AspNetStringResultItem); return i == null ? (bool?)null : i.ComesFromInlineExpression; });

            var localizabilityProvedPredicate = new LocalizationCommonCriterion("LocalizabilityProved",
                "ASP.NET attribute's type is String",
                LocalizationCriterionAction.VALUE, 70,
                (item) => { var i = (item as AspNetStringResultItem); return i == null ? (bool?)null : i.LocalizabilityProved; });

            var comesFromPlainTextPredicate = new LocalizationCommonCriterion("ComesFromPlainText",
                "String literal comes from ASP .NET plain text",
                LocalizationCriterionAction.VALUE, 20,
                (item) => { var i = (item as AspNetStringResultItem); return i == null ? (bool?)null : i.ComesFromPlainText; });

            var comesFromDirectivePredicate = new LocalizationCommonCriterion("ComesFromDirective",
                "String literal comes from ASP .NET directive",
                LocalizationCriterionAction.VALUE, -10,
                (item) => { var i = (item as AspNetStringResultItem); return i == null ? (bool?)null : i.ComesFromDirective; });

            var comesFromCodeBlockPredicate = new LocalizationCommonCriterion("ComesFromCodeBlock",
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

    /// <summary>
    /// Represents found reference to a resource key
    /// </summary>
    public abstract class CodeReferenceResultItem : AbstractResultItem {
        /// <summary>
        /// Returns full text of the reference (including namespace)
        /// </summary>
        public string FullReferenceText { get; set; }

        /// <summary>
        /// Returns text of the reference as it was found (possibly without namespace)
        /// </summary>
        public string OriginalReferenceText { get; set; }

        /// <summary>
        /// Used when renaming keys - new name
        /// </summary>
        public string KeyAfterRename { get; set; }

        /// <summary>
        /// Returns value in the format in which it can be inserted in the code (escaped sequences)
        /// </summary>        
        public abstract string GetInlineValue();

        /// <summary>
        /// Returns position of the reference which can be used to replace with actual string literal
        /// </summary>
        /// <param name="strictText">Used in ASP .NET. True if &lt;%= Resources.key %> should be replaced whole</param>
        /// <param name="absoluteStartIndex">Start position of the reference</param>
        /// <param name="absoluteLength">Length of the reference</param>        
        public abstract TextSpan GetInlineReplaceSpan(bool strictText, out int absoluteStartIndex, out int absoluteLength);
        
        /// <summary>
        /// Returns reference built from KeyAfterRename
        /// </summary>       
        public abstract string GetReferenceAfterRename(string newKey);
    }

    /// <summary>
    /// Represents found reference to a resource key in a C# or VB code
    /// </summary>
    public abstract class NetCodeReferenceResultItem : CodeReferenceResultItem {

        /// <summary>
        /// Returns position of the reference which can be used to replace with actual string literal
        /// </summary>
        /// <param name="strictText">Used in ASP .NET. True if &lt;%= Resources.key %&gt; should be replaced whole</param>
        /// <param name="absoluteStartIndex">Start position of the reference</param>
        /// <param name="absoluteLength">Length of the reference</param>  
        public override TextSpan GetInlineReplaceSpan(bool strictText, out int absoluteStartIndex, out int absoluteLength) {
            absoluteStartIndex = AbsoluteCharOffset;
            absoluteLength = AbsoluteCharLength;
            return ReplaceSpan;
        }

        /// <summary>
        /// Returns reference built from KeyAfterRename
        /// </summary>  
        public override string GetReferenceAfterRename(string newKey) {
            string prefix = OriginalReferenceText.Substring(0, OriginalReferenceText.LastIndexOf('.'));
            return prefix + "." + newKey;
        }
    }

    /// <summary>
    /// Represents found reference to a resource key in a C# code
    /// </summary>
    public sealed class CSharpCodeReferenceResultItem : NetCodeReferenceResultItem {
        /// <summary>
        /// Returns value in the format in which it can be inserted in the code (escaped sequences)
        /// </summary>
        public override string GetInlineValue() {
            return "\"" + Value.ConvertCSharpUnescapeSequences() + "\"";
        }

        /// <summary>
        /// Language of the result item context
        /// </summary>
        public override LANGUAGE Language {
            get { return LANGUAGE.CSHARP; }
            set { }
        }
    }

    /// <summary>
    /// Represents found reference to a resource key in a VB code
    /// </summary>
    public sealed class VBCodeReferenceResultItem : NetCodeReferenceResultItem {
        /// <summary>
        /// Returns value in the format in which it can be inserted in the code (escaped sequences)
        /// </summary>
        public override string GetInlineValue() {
            StringBuilder b = new StringBuilder();

            bool first = true;
            bool firstEscaped = false;
            bool previousEscaped = false;
            foreach (char c in Value.ConvertVBUnescapeSequences()) {
                if (!char.IsControl(c) || c == '"' || c == '\'') {
                    if (previousEscaped) {
                        b.Append(" & \"");    
                    } 
                    b.Append(c);
                    
                    if (first) firstEscaped = false;
                    previousEscaped = false;
                } else {
                    if (previousEscaped) {
                        b.Append(" & ");
                    } else if(!first) {
                        b.Append("\" & ");
                    }
                    b.AppendFormat("Chr({0})", (int)c);
                    
                    if (first) firstEscaped = true;
                    previousEscaped = true;
                }
                first = false;
            }

            return (firstEscaped ? "" : "\"") + b.ToString() + (previousEscaped ? "" : "\"");
        }

        /// <summary>
        /// Language of the result item context
        /// </summary>
        public override LANGUAGE Language {
            get { return LANGUAGE.VB; }
            set { }
        }
    }

    /// <summary>
    /// Represents found reference to a resource key in a ASP .NET code
    /// </summary>
    public sealed class AspNetCodeReferenceResultItem : CodeReferenceResultItem {
        /// <summary>
        /// True if result item comes from &lt;%= expression
        /// </summary>
        public bool ComesFromInlineExpression { get; set; }

        /// <summary>
        /// True if result item comes from &lt;%$ expression
        /// </summary>
        public bool ComesFromWebSiteResourceReference { get; set; }

        /// <summary>
        /// Position of the expression this result item comes from
        /// </summary>
        public BlockSpan InlineReplaceSpan { get; set; }

        private LANGUAGE _Language;
        /// <summary>
        /// Language of the result item context
        /// </summary>
        public override LANGUAGE Language {
            get { return _Language; }
            set { _Language = value; }
        }

        /// <summary>
        /// Returns value in the format in which it can be inserted in the code (escaped sequences)
        /// </summary>  
        public override string GetInlineValue() {
            if (ComesFromInlineExpression) {
                return Value.ConvertAspNetUnescapeSequences(); // escape entities like &nbsp;
            } else {
                // comes from code block
                return "\"" + (Language == LANGUAGE.CSHARP ? Value.ConvertCSharpUnescapeSequences() : Value.ConvertVBUnescapeSequences()) + "\"";
            }
        }


        /// <summary>
        /// Returns position of the reference which can be used to replace with actual string literal
        /// </summary>
        /// <param name="strictText">Used in ASP .NET. True if &lt;%= Resources.key %&gt; should be replaced whole</param>
        /// <param name="absoluteStartIndex">Start position of the reference</param>
        /// <param name="absoluteLength">Length of the reference</param>
        /// <returns></returns>
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

        /// <summary>
        /// Returns reference built from KeyAfterRename
        /// </summary>
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
