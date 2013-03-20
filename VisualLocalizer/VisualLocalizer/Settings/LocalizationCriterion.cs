using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Components;
using System.Reflection;
using EnvDTE;
using System.Text.RegularExpressions;

namespace VisualLocalizer.Settings {
    internal enum LocalizationCriterionAction { FORCE_ENABLE, FORCE_DISABLE, VALUE, IGNORE }
    internal enum LocalizationCriterionAction2 { CHECK, CHECK_REMOVE, UNCHECK, REMOVE }
    internal enum LocalizationCriterionTarget { VALUE, NAMESPACE_NAME, METHOD_NAME, CLASS_NAME, ELEMENT_PREFIX, ELEMENT_NAME, VARIABLE_NAME }
    internal enum LocalizationCriterionPredicate { MATCHES, DOESNT_MATCH, IS_NULL, NO_LETTERS, ONLY_CAPS, NO_WHITESPACE }
    
    internal abstract class AbstractLocalizationCriterion {
        public const int MAX_LOC_PROBABILITY = 100;
        public const int TRESHOLD_LOC_PROBABILITY = 50;

        public LocalizationCriterionAction Action { get; set; }
        public int Weight { get; set; }
        public string Name { get; set; }

        protected AbstractLocalizationCriterion() { }

        public AbstractLocalizationCriterion(string name, LocalizationCriterionAction action, int weight) {
            this.Action = action;
            this.Weight = weight;
            this.Name = name;
        }

        public abstract bool? Eval(CodeStringResultItem resultItem);        
        public abstract string Description { get; protected set; }
        public abstract AbstractLocalizationCriterion DeepCopy();

        public virtual string ToRegData() {
            return string.Format("{0}/{1}", (int)Action, Weight);
        }

        public virtual void FromRegData(string data) {
            string[] a = data.Split('/');
            if (a.Length < 2) throw new Exception("Invalid registry data '" + data + "'");

            Action = (LocalizationCriterionAction)int.Parse(a[0]);
            Weight = int.Parse(a[1]);            
        }

        protected void internalDeepCopy(AbstractLocalizationCriterion crit) {
            crit.Action = Action;
            crit.Description = Description;
            crit.Name = Name;
            crit.Weight = Weight;
        }
    }

    internal sealed class LocalizationCriterion : AbstractLocalizationCriterion {        
        public Func<CodeStringResultItem, bool?> Predicate { get; private set; }

        private LocalizationCriterion() { }

        public LocalizationCriterion(string name, string description, LocalizationCriterionAction option, int weight, Func<CodeStringResultItem, bool?> predicate)
            : base(name, option, weight) {     
            this.Description = description;
            this.Predicate = predicate;
        }

        public override bool? Eval(CodeStringResultItem resultItem) {
            return Predicate(resultItem);
        }
       
        private string _Description;
        public override string Description {
            get {
                return _Description;
            }
            protected set {
                _Description = value;
            }
        }

        public override AbstractLocalizationCriterion DeepCopy() {
            LocalizationCriterion crit = new LocalizationCriterion();
            internalDeepCopy(crit);
            crit.Predicate = Predicate;
            return crit;
        }
    }

    internal sealed class LocalizationCustomCriterion : AbstractLocalizationCriterion {
        public string Regex { get; set; }        
        public LocalizationCriterionTarget Target { get; set; }
        public LocalizationCriterionPredicate Predicate { get; set; }
        private static Random rnd = new Random();

        protected LocalizationCustomCriterion() {             
        }

        public LocalizationCustomCriterion(LocalizationCriterionAction action, int weight)
            : base("x" + rnd.Next().ToString(), action, weight) {            
        }

        public override bool? Eval(CodeStringResultItem resultItem) {
            bool relevant;
            string testString = getTarget(resultItem, out relevant);
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
                        if (!char.IsUpper(c) && !char.IsSymbol(c) && !char.IsPunctuation(c)) { 
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

        private string getTarget(CodeStringResultItem resultItem, out bool relevant) {
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
                    if (cResItem == null) return null;
                    relevant = true;
                    CodeNamespace nmspc = cResItem.NamespaceElement;
                    if (nmspc != null) testString = nmspc.FullName;
                    break;
                case LocalizationCriterionTarget.METHOD_NAME:
                    if (cResItem == null) return null;
                    relevant = true;
                    testString = cResItem.MethodElementName;
                    break;
                case LocalizationCriterionTarget.CLASS_NAME:
                    if (cResItem == null) return null;
                    relevant = true;
                    testString = cResItem.ClassOrStructElementName;
                    break;
                case LocalizationCriterionTarget.ELEMENT_NAME:
                    if (aResItem == null) return null;
                    relevant = true;
                    testString = aResItem.ElementName;
                    break;
                case LocalizationCriterionTarget.ELEMENT_PREFIX:
                    if (aResItem == null) return null;
                    relevant = true;
                    testString = aResItem.ElementPrefix;
                    break;
                case LocalizationCriterionTarget.VARIABLE_NAME:
                    if (cResItem == null) return null;
                    relevant = true;
                    testString = cResItem.VariableElementName;
                    break;
            }
            return testString;
        }

        public override string ToRegData() {
            string orig = base.ToRegData();
            orig += string.Format("/{0}/{1}/{2}/{3}/{4}",(int)Action, (int)Target, (int)Predicate, Name, Regex);
            return orig;
        }

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

        public override AbstractLocalizationCriterion DeepCopy() {
            LocalizationCustomCriterion crit = new LocalizationCustomCriterion();
            internalDeepCopy(crit);
            crit.Predicate = Predicate;
            crit.Regex = (string)Regex.Clone();
            crit.Target = Target;
            return crit;
        }
    }

    static class LocalizationEnumsTranslations {
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
                default:
                    throw new Exception("Unknown LocalizationCriterionTarget: " + target);
            }            
        }

        public static string ToHumanForm(this LocalizationCriterionAction2 act) {
            switch (act) {
                case LocalizationCriterionAction2.CHECK:
                    return "check rows";
                case LocalizationCriterionAction2.CHECK_REMOVE:
                    return "check rows & remove rest";
                case LocalizationCriterionAction2.UNCHECK:
                    return "uncheck rows";
                case LocalizationCriterionAction2.REMOVE:
                    return "remove rows";
            }
            return null;
        }
    }
}
