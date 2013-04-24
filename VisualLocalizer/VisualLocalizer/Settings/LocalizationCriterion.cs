using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Components;
using System.Reflection;
using EnvDTE;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace VisualLocalizer.Settings {

    /// <summary>
    /// Set of actions assignable to criteria in the Tools/Options
    /// </summary>
    public enum LocalizationCriterionAction { 
        /// <summary>
        /// Force localize
        /// </summary>
        FORCE_ENABLE, 

        /// <summary>
        /// Force not localize
        /// </summary>
        FORCE_DISABLE, 
        
        /// <summary>
        /// Set specific value affecting localization probability calculation
        /// </summary>
        VALUE, 
        
        /// <summary>
        /// Ignore this criterion
        /// </summary>
        IGNORE
    }

    /// <summary>
    /// Set of actions assignable to criteria in the filter panel
    /// </summary>
    public enum LocalizationCriterionAction2 { 
        /// <summary>
        /// Check rows satisfying the criterion
        /// </summary>
        CHECK, 

        /// <summary>
        /// Check rows satisfying the criterion and remove the rest
        /// </summary>
        CHECK_REMOVE, 

        /// <summary>
        /// Uncheck rows satisfying the criterion
        /// </summary>
        UNCHECK,
 
        /// <summary>
        /// Remove rows satisfying the criterion
        /// </summary>
        REMOVE 
    }

    /// <summary>
    /// Represents target of evaluation - which part of the result item should be tested
    /// </summary>
    public enum LocalizationCriterionTarget { 
        /// <summary>
        /// Value of the string literal
        /// </summary>
        VALUE, 

        /// <summary>
        /// Name of the namespace the literal lies in
        /// </summary>
        NAMESPACE_NAME, 

        /// <summary>
        /// Name of the method the literal lies in
        /// </summary>
        METHOD_NAME, 

        /// <summary>
        /// Name of the class the literal lies in
        /// </summary>
        CLASS_NAME, 

        /// <summary>
        /// Element's prefix, if the result item comes from ASP .NET element
        /// </summary>
        ELEMENT_PREFIX,

        /// <summary>
        /// Element's name, if the result item comes from ASP .NET element
        /// </summary>
        ELEMENT_NAME,

        /// <summary>
        /// Name of the variable the string literal initializes
        /// </summary>
        VARIABLE_NAME,
 
        /// <summary>
        /// Name of the attribute, if the result item comes from ASP .NET element
        /// </summary>
        ATTRIBUTE_NAME
    }
    
    /// <summary>
    /// Predicate for custom criteria
    /// </summary>
    public enum LocalizationCriterionPredicate { 
        /// <summary>
        /// Target matches given regular expression
        /// </summary>
        MATCHES,

        /// <summary>
        /// Target doesn't match given regular expression
        /// </summary>
        DOESNT_MATCH,

        /// <summary>
        /// Target is null
        /// </summary>
        IS_NULL,

        /// <summary>
        /// Target contains no letters
        /// </summary>
        NO_LETTERS,

        /// <summary>
        /// Target contains only CAPITAL letters
        /// </summary>
        ONLY_CAPS,

        /// <summary>
        /// Target doesn't contain any whitespace characters
        /// </summary>
        NO_WHITESPACE 
    }
    
    /// <summary>
    /// Base class for localization criteria
    /// </summary>
    public abstract class AbstractLocalizationCriterion {
        /// <summary>
        /// Maximal reachable localization probability
        /// </summary>
        public const int MAX_LOC_PROBABILITY = 100;

        /// <summary>
        /// Treshold value - result items having LP at least this value will be checked when added to batch move toolwindow's grid
        /// </summary>
        public const int TRESHOLD_LOC_PROBABILITY = 50;

        /// <summary>
        /// Action to take when this criterion is met
        /// </summary>
        public LocalizationCriterionAction Action { get; set; }

        /// <summary>
        /// Parameter for "Set value" action
        /// </summary>
        public int Weight { get; set; }

        /// <summary>
        /// Name of this criterion
        /// </summary>
        public string Name { get; set; }

        protected AbstractLocalizationCriterion() { }

        /// <summary>
        /// Creates new instance
        /// </summary>        
        public AbstractLocalizationCriterion(string name, LocalizationCriterionAction action, int weight) {
            if (name == null) throw new ArgumentNullException("name");

            this.Action = action;
            this.Weight = weight;
            this.Name = name;
        }

        /// <summary>
        /// Evaluates given result item
        /// </summary>        
        /// <returns>Null if the criterion is not relevant for the result item (testing element prefix for C# string literal...), true if it is relevant and the result item satisfies the condition, false otherwise</returns>
        public abstract bool? Eval(CodeStringResultItem resultItem);        

        /// <summary>
        /// Returns human-readeble description of this criterion
        /// </summary>
        public abstract string Description { get; protected set; }

        /// <summary>
        /// Creates deep copy
        /// </summary>        
        public abstract AbstractLocalizationCriterion DeepCopy();

        /// <summary>
        /// Returns string that is used to save this criterion in the registry
        /// </summary>        
        public virtual string ToRegData() {
            return string.Format("{0}/{1}", (int)Action, Weight);
        }

        /// <summary>
        /// Treats given string as a result of ToRegData() method; parses it and sets values accordingly 
        /// </summary>        
        public virtual void FromRegData(string data) {
            if (data == null) throw new ArgumentNullException("data");

            string[] a = data.Split('/');
            if (a.Length < 2) throw new Exception("Invalid registry data '" + data + "'");

            Action = (LocalizationCriterionAction)int.Parse(a[0]);
            Weight = int.Parse(a[1]);            
        }

        /// <summary>
        /// Copies this object's data to the given criterion
        /// </summary>        
        protected void InternalDeepCopy(AbstractLocalizationCriterion crit) {
            crit.Action = Action;
            crit.Description = Description;
            crit.Name = Name;
            crit.Weight = Weight;
        }
    }

    /// <summary>
    /// Represents common criterion - these are hard-coded and user can edit only their actions
    /// </summary>
    public sealed class LocalizationCommonCriterion : AbstractLocalizationCriterion {        
        /// <summary>
        /// Predicate evaluating given result item
        /// </summary>
        public Func<CodeStringResultItem, bool?> Predicate { get; private set; }

        private LocalizationCommonCriterion() { }

        public LocalizationCommonCriterion(string name, string description, LocalizationCriterionAction option, int weight, Func<CodeStringResultItem, bool?> predicate)
            : base(name, option, weight) {
            if (description == null) throw new ArgumentNullException("description");
            if (predicate == null) throw new ArgumentNullException("predicate");

            this.Description = description;
            this.Predicate = predicate;
        }

        /// <summary>
        /// Evaluates given result item
        /// </summary>        
        /// <returns>
        /// Null if the criterion is not relevant for the result item (testing element prefix for C# string literal...), true if it is relevant and the result item satisfies the condition, false otherwise
        /// </returns>        
        public override bool? Eval(CodeStringResultItem resultItem) {
            if (resultItem == null) throw new ArgumentNullException("resultItem");
            return Predicate(resultItem);
        }
       
        private string _Description;
        /// <summary>
        /// Returns human-readeble description of this criterion
        /// </summary>
        public override string Description {
            get {
                return _Description;
            }
            protected set {
                _Description = value;
            }
        }

        /// <summary>
        /// Creates deep copy
        /// </summary>
        public override AbstractLocalizationCriterion DeepCopy() {
            LocalizationCommonCriterion crit = new LocalizationCommonCriterion();
            InternalDeepCopy(crit);
            crit.Predicate = Predicate;
            return crit;
        }
    }

    /// <summary>
    /// Represents custom criterion - these are created by the user in the Tools/Options settings
    /// </summary>
    public sealed class LocalizationCustomCriterion : AbstractLocalizationCriterion {

        /// <summary>
        /// Regular expression to test (if relevant)
        /// </summary>
        public string Regex { get; set; }        

        /// <summary>
        /// Target of the evaluation (what should be evaluated - method name, class name, value...)
        /// </summary>
        public LocalizationCriterionTarget Target { get; set; }

        /// <summary>
        /// Predicate for evaluation (matches, doesn't match, is null...)
        /// </summary>
        public LocalizationCriterionPredicate Predicate { get; set; }
        private static Random rnd = new Random();

        private LocalizationCustomCriterion() {                         
        }

        public LocalizationCustomCriterion(LocalizationCriterionAction action, int weight)
            : base("x" + rnd.Next().ToString(), action, weight) {            
        }

        /// <summary>
        /// Evaluates given result item
        /// </summary>        
        /// <returns>
        /// Null if the criterion is not relevant for the result item (testing element prefix for C# string literal...), true if it is relevant and the result item satisfies the condition, false otherwise
        /// </returns>        
        public override bool? Eval(CodeStringResultItem resultItem) {
            if (resultItem == null) throw new ArgumentNullException("resultItem");

            bool relevant;
            string testString = GetTarget(resultItem, out relevant);
            if (!relevant) return null;

            switch (Predicate) {
                case LocalizationCriterionPredicate.MATCHES:
                    if (testString == null) return false;
                    return System.Text.RegularExpressions.Regex.IsMatch(testString, Regex);                    
                case LocalizationCriterionPredicate.DOESNT_MATCH:
                    if (testString == null) return false;
                    return !System.Text.RegularExpressions.Regex.IsMatch(testString, Regex);                                        
                case LocalizationCriterionPredicate.IS_NULL:
                    return testString == null;                    
                case LocalizationCriterionPredicate.NO_LETTERS:
                    if (testString == null) return true;
                    bool containsLetter = false;
                    foreach (char c in testString) {
                        if (char.IsLetter(c)) { containsLetter = true; break; }
                    }
                    return !containsLetter;                    
                case LocalizationCriterionPredicate.ONLY_CAPS:
                    if (testString == null) return true;
                    bool ok = true;
                    foreach (char c in testString) {
                        if (!char.IsUpper(c) && !char.IsSymbol(c) && !char.IsPunctuation(c) && !char.IsDigit(c)) { 
                            ok = false; break;
                        }
                    }
                    return ok;
                case LocalizationCriterionPredicate.NO_WHITESPACE:
                    if (testString == null) return true;
                    bool containsWhitespace = false;
                    foreach (char c in testString) {
                        if (char.IsWhiteSpace(c)) { containsWhitespace = true; break; }
                    }
                    return !containsWhitespace;                       
            }

            return null;
        }

        /// <summary>
        /// Returns target property's value
        /// </summary>
        /// <param name="resultItem">Result item from which the value is taken</param>
        /// <param name="relevant">OUT - true if the target is relevant for the result item</param>        
        private string GetTarget(CodeStringResultItem resultItem, out bool relevant) {
            string testString = null;
            NetStringResultItem cResItem = resultItem as NetStringResultItem;
            AspNetStringResultItem aResItem = resultItem as AspNetStringResultItem;
            relevant = false;

            switch (Target) {
                case LocalizationCriterionTarget.VALUE:
                    relevant = true;
                    testString = resultItem.Value;
                    break;
                case LocalizationCriterionTarget.NAMESPACE_NAME:
                    if (cResItem == null) return null; // ASP .NET result items don't have namespaces                    
                    try {
                        CodeNamespace nmspc = cResItem.NamespaceElement;
                        if (nmspc != null) testString = nmspc.FullName;
                        relevant = true;
                    } catch (COMException) { }
                    break;
                case LocalizationCriterionTarget.METHOD_NAME:
                    if (cResItem == null) return null; // ASP .NET result items don't have methods
                    relevant = true;
                    testString = cResItem.MethodElementName;
                    break;
                case LocalizationCriterionTarget.CLASS_NAME:
                    if (cResItem == null) return null; // ASP .NET result items don't have classes
                    relevant = true;
                    testString = cResItem.ClassOrStructElementName;
                    break;
                case LocalizationCriterionTarget.ELEMENT_NAME:
                    if (aResItem == null) return null;  // C# or VB result items don't have element names
                    if (aResItem.ComesFromDirective || aResItem.ComesFromElement || aResItem.ComesFromPlainText) {
                        relevant = true;
                        testString = aResItem.ElementName;
                    } else return null;
                    break;
                case LocalizationCriterionTarget.ELEMENT_PREFIX:
                    if (aResItem == null) return null;  // C# or VB result items don't have element prefixes
                    if (aResItem.ComesFromDirective || aResItem.ComesFromElement || aResItem.ComesFromPlainText) {
                        relevant = true;
                        testString = aResItem.ElementPrefix;
                    } else return null;
                    break;
                case LocalizationCriterionTarget.VARIABLE_NAME:
                    if (cResItem == null) return null; ; // ASP .NET result items don't initialize variables
                    relevant = true;
                    testString = cResItem.VariableElementName;
                    break;
                case LocalizationCriterionTarget.ATTRIBUTE_NAME:
                    if (aResItem == null) return null;  // C# or VB result items don't originate from elements
                    if (aResItem.ComesFromDirective || aResItem.ComesFromElement) {
                        relevant = true;
                        testString = aResItem.AttributeName;
                    } else return null;
                    break;
            }
            return testString;
        }

        /// <summary>
        /// Returns string that is used to save this criterion in the registry
        /// </summary>
        public override string ToRegData() {
            string orig = base.ToRegData();
            orig += string.Format("/{0}/{1}/{2}/{3}/{4}",(int)Action, (int)Target, (int)Predicate, Name, Regex);
            return orig;
        }

        /// <summary>
        /// Treats given string as a result of ToRegData() method; parses it and sets values accordingly
        /// </summary>
        public override void FromRegData(string data) {
            base.FromRegData(data);
            string[] a = data.Split('/');
            if (a.Length < 7) throw new Exception("Invalid registry data '" + data + "'");

            Action = (LocalizationCriterionAction)int.Parse(a[2]);
            Target = (LocalizationCriterionTarget)int.Parse(a[3]);
            Predicate = (LocalizationCriterionPredicate)int.Parse(a[4]);
            Name = a[5];
            Regex = string.Join("/", a, 6, a.Length - 6);
        }

        /// <summary>
        /// Returns human-readeble description of this criterion
        /// </summary>
        public override string Description {
            get {
                string target = Target.ToHumanForm();
                string action = Action.ToHumanForm() + (Action == LocalizationCriterionAction.VALUE ? " " + Weight : "");

                if (Predicate == LocalizationCriterionPredicate.MATCHES || Predicate == LocalizationCriterionPredicate.DOESNT_MATCH) {
                    return string.Format("If {0} {1} '{2}'", target, Predicate.ToHumanForm(), Regex);
                } else {
                    return string.Format("If {0} {1}", target, Predicate.ToHumanForm());
                }
            }
            protected set { }
        }

        /// <summary>
        /// Creates deep copy
        /// </summary>
        public override AbstractLocalizationCriterion DeepCopy() {
            LocalizationCustomCriterion crit = new LocalizationCustomCriterion();
            InternalDeepCopy(crit);
            crit.Predicate = Predicate;
            crit.Regex = Regex == null ? null : (string)Regex.Clone();
            crit.Target = Target;
            return crit;
        }
    }

    /// <summary>
    /// Extension class for translating localization enums into human readeble form
    /// </summary>
    static class LocalizationEnumsTranslations {

        /// <summary>
        /// Returns human-readable name of the criterion action
        /// </summary>        
        public static string ToHumanForm(this LocalizationCriterionAction act) {
            switch (act) {
                case LocalizationCriterionAction.FORCE_ENABLE:
                    return "force localize";
                case LocalizationCriterionAction.FORCE_DISABLE:
                    return "force NOT localize";
                case LocalizationCriterionAction.VALUE:
                    return "set value";
                case LocalizationCriterionAction.IGNORE:
                    return "ignore";
                default:
                    throw new Exception("Unknown LocalizationCriterionAction: "+act);
            }
        }

        /// <summary>
        /// Returns human-readable representation of the criterion predicate
        /// </summary>        
        public static string ToHumanForm(this LocalizationCriterionPredicate pred) {
            switch (pred) {
                case LocalizationCriterionPredicate.MATCHES:
                    return "matches";
                case LocalizationCriterionPredicate.DOESNT_MATCH:
                    return "doesn't match";
                case LocalizationCriterionPredicate.IS_NULL:
                    return "is null";
                case LocalizationCriterionPredicate.NO_LETTERS:
                    return "contains no letters";
                case LocalizationCriterionPredicate.ONLY_CAPS:
                    return "contains only CAPS and symbols";
                case LocalizationCriterionPredicate.NO_WHITESPACE:
                    return "contains no whitespace";
                default:
                    throw new Exception("Unknown LocalizationCriterionPredicate: " + pred);
            }
        }

        /// <summary>
        /// Returns human-readable representation of the criterion target
        /// </summary>        
        public static string ToHumanForm(this LocalizationCriterionTarget target) {
            switch (target) {
                case LocalizationCriterionTarget.VALUE:
                    return "value";
                case LocalizationCriterionTarget.NAMESPACE_NAME:
                    return "namespace name";
                case LocalizationCriterionTarget.METHOD_NAME:
                    return "method name";
                case LocalizationCriterionTarget.CLASS_NAME:
                    return "class name";
                case LocalizationCriterionTarget.ELEMENT_PREFIX:
                    return "element prefix";
                case LocalizationCriterionTarget.ELEMENT_NAME:
                    return "element name";
                case LocalizationCriterionTarget.VARIABLE_NAME:
                    return "variable name";
                case LocalizationCriterionTarget.ATTRIBUTE_NAME:
                    return "attribute name";
                default:
                    throw new Exception("Unknown LocalizationCriterionTarget: " + target);
            }            
        }

        /// <summary>
        /// Returns human-readable representation of the criterion target
        /// </summary>   
        public static string ToHumanForm(this LocalizationCriterionAction2 act) {
            switch (act) {
                case LocalizationCriterionAction2.CHECK:
                    return "check rows";
                case LocalizationCriterionAction2.CHECK_REMOVE:
                    return "check rows & remove the rest";
                case LocalizationCriterionAction2.UNCHECK:
                    return "uncheck rows";
                case LocalizationCriterionAction2.REMOVE:
                    return "remove rows";
                default:
                    throw new Exception("Unknown LocalizationCriterionAction2: " + act);
            }            
        }
    }
}
