using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using EnvDTE80;
using System.Collections;
using VisualLocalizer.Library;
using VisualLocalizer.Commands;

namespace VisualLocalizer.Components {
    internal sealed class CSharpCodeExplorer {

        private CSharpCodeExplorer() { }

        private static CSharpCodeExplorer instance;
        public static CSharpCodeExplorer Instance {
            get {
                if (instance == null) instance = new CSharpCodeExplorer();
                return instance;
            }
        }

        public void Explore(AbstractBatchCommand parentCommand, Predicate<CodeElement> exploreable, FileCodeModel2 codeModel) {
            foreach (CodeElement2 codeElement in codeModel.CodeElements) {
                if (codeElement.Kind == vsCMElement.vsCMElementNamespace || codeElement.Kind == vsCMElement.vsCMElementClass ||
                    codeElement.Kind == vsCMElement.vsCMElementStruct) {
                    Explore(parentCommand, codeElement, null, exploreable, false);
                }
            }
        }

        private void Explore(AbstractBatchCommand parentCommand, CodeElement2 parentElement, CodeElement2 parentNamespace, Predicate<CodeElement> exploreable, bool isLocalizableFalse) {
            bool isLocalizableFalseSetOnParent = HasLocalizableFalseAttribute(parentElement);

            foreach (CodeElement2 codeElement in parentElement.Children) {
                if (codeElement.Kind == vsCMElement.vsCMElementClass) {
                    Explore(parentCommand, codeElement, parentElement.Kind == vsCMElement.vsCMElementNamespace ? parentElement : parentNamespace,
                        exploreable, isLocalizableFalse || isLocalizableFalseSetOnParent);
                }
                if (codeElement.Kind == vsCMElement.vsCMElementNamespace) {
                    Explore(parentCommand, codeElement, parentElement, exploreable, isLocalizableFalse || isLocalizableFalseSetOnParent);
                }
                if (codeElement.Kind == vsCMElement.vsCMElementVariable) {
                    Explore(parentCommand, codeElement as CodeVariable2, (CodeNamespace)parentNamespace, parentElement,
                        exploreable, isLocalizableFalse || isLocalizableFalseSetOnParent);
                }
                if (codeElement.Kind == vsCMElement.vsCMElementFunction) {
                    Explore(parentCommand, codeElement as CodeFunction2, (CodeNamespace)parentNamespace, parentElement,
                        exploreable, isLocalizableFalse || isLocalizableFalseSetOnParent);
                }
                if (codeElement.Kind == vsCMElement.vsCMElementProperty) {
                    Explore(parentCommand, codeElement as CodeProperty, (CodeNamespace)parentNamespace, parentElement,
                        exploreable, isLocalizableFalse || isLocalizableFalseSetOnParent);
                }
                if (codeElement.Kind == vsCMElement.vsCMElementStruct) {
                    Explore(parentCommand, codeElement, parentElement.Kind == vsCMElement.vsCMElementNamespace ? parentElement : parentNamespace,
                        exploreable, isLocalizableFalse || isLocalizableFalseSetOnParent);
                }
            }
        }

        private bool HasLocalizableFalseAttribute(CodeElement parentElement) {
            bool set = false;
            switch (parentElement.Kind) {
                case vsCMElement.vsCMElementClass:
                    set = AttributesContainLocalizableFalse((parentElement as CodeClass).Attributes);
                    break;
                case vsCMElement.vsCMElementStruct:
                    set = AttributesContainLocalizableFalse((parentElement as CodeStruct).Attributes);
                    break;
                case vsCMElement.vsCMElementProperty:
                    set = AttributesContainLocalizableFalse((parentElement as CodeProperty).Attributes);
                    break;
                case vsCMElement.vsCMElementFunction:
                    set = AttributesContainLocalizableFalse((parentElement as CodeFunction).Attributes);
                    break;
                case vsCMElement.vsCMElementVariable:
                    set = AttributesContainLocalizableFalse((parentElement as CodeVariable).Attributes);
                    break;
            }

            return set;
        }

        private bool AttributesContainLocalizableFalse(CodeElements elements) {
            if (elements == null) return false;

            bool contains = false;
            foreach (CodeAttribute2 attr in elements) {
                if (attr.FullName == "System.ComponentModel.LocalizableAttribute" && attr.Arguments.Count == 1) {
                    IEnumerator enumerator = attr.Arguments.GetEnumerator();
                    enumerator.MoveNext();

                    CodeAttributeArgument arg = enumerator.Current as CodeAttributeArgument;
                    if (arg.Value.Trim().ToLower() == "false") { // TODO
                        contains = true;
                        break;
                    }
                }
            }

            return contains;
        }

        private void Explore(AbstractBatchCommand parentCommand, CodeProperty codeProperty, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct, Predicate<CodeElement> exploreable, bool isLocalizableFalse) {
            bool propertyLocalizableFalse = HasLocalizableFalseAttribute(codeProperty as CodeElement);            
            if (codeProperty.Getter != null) Explore(parentCommand, codeProperty.Getter as CodeFunction2, parentNamespace, codeClassOrStruct, exploreable, isLocalizableFalse || propertyLocalizableFalse);
            if (codeProperty.Setter != null) Explore(parentCommand, codeProperty.Setter as CodeFunction2, parentNamespace, codeClassOrStruct, exploreable, isLocalizableFalse || propertyLocalizableFalse);
        }

        private void Explore(AbstractBatchCommand parentCommand, CodeVariable2 codeVariable, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct, Predicate<CodeElement> exploreable, bool isLocalizableFalse) {
            if (codeVariable.ConstKind == vsCMConstKind.vsCMConstKindConst) return;
            if (codeVariable.Type.TypeKind != vsCMTypeRef.vsCMTypeRefString) return;
            if (codeVariable.InitExpression == null) return;
            if (codeClassOrStruct.Kind == vsCMElement.vsCMElementStruct) return;
            if (!exploreable(codeVariable as CodeElement)) return;

            string initExpression = codeVariable.GetText();
            if (string.IsNullOrEmpty(initExpression)) return;

            TextPoint startPoint = codeVariable.StartPoint;
            bool variableLocalizableFalse = HasLocalizableFalseAttribute(codeVariable as CodeElement);

            var list=parentCommand.LookupInCSharp(initExpression, startPoint, parentNamespace, codeClassOrStruct, null, codeVariable.Name, isLocalizableFalse || variableLocalizableFalse);

            EditPoint2 editPoint = (EditPoint2)startPoint.CreateEditPoint();
            foreach (AbstractResultItem item in list)
                AddContextToItem(item, editPoint);
        }

        private void Explore(AbstractBatchCommand parentCommand, CodeFunction2 codeFunction, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct, Predicate<CodeElement> exploreable, bool isLocalizableFalse) {
            if (codeFunction.MustImplement) return;
            if (!exploreable(codeFunction as CodeElement)) return;

            string functionText = codeFunction.GetText();
            if (string.IsNullOrEmpty(functionText)) return;

            TextPoint startPoint = codeFunction.GetStartPoint(vsCMPart.vsCMPartBody);
            bool functionLocalizableFalse = HasLocalizableFalseAttribute(codeFunction as CodeElement);

            var list = parentCommand.LookupInCSharp(functionText, startPoint, parentNamespace, codeClassOrStruct, codeFunction.Name, null, isLocalizableFalse || functionLocalizableFalse);
            EditPoint2 editPoint = (EditPoint2)startPoint.CreateEditPoint();
            foreach (AbstractResultItem item in list)
                AddContextToItem(item, editPoint);
        }

        public void AddContextToItem(AbstractResultItem item, EditPoint2 editPoint) {
            StringBuilder context = new StringBuilder();
            // indices +1 !!
            
            int topLines = 0;

            int currentLine = item.ReplaceSpan.iStartLine;
            int contextRelativeLine = 0;
            while (currentLine >= 1 && topLines < NumericConstants.ContextLineRadius) {
                editPoint.MoveToLineAndOffset(currentLine, 1);
                string lineText = editPoint.GetText(editPoint.LineLength).Trim();
                if (lineText.Length > 0) {
                    context.Insert(0, lineText + Environment.NewLine);
                    contextRelativeLine++;
                    if (lineText.Length > 1) topLines++;
                }
                currentLine--;
            }

            editPoint.MoveToLineAndOffset(item.ReplaceSpan.iStartLine + 1, 1);
            context.Append(editPoint.GetText(item.ReplaceSpan.iStartIndex).Trim());

            context.Append(StringConstants.ContextSubstituteText);

            editPoint.MoveToLineAndOffset(item.ReplaceSpan.iEndLine + 1, item.ReplaceSpan.iEndIndex + 1);
            context.Append(editPoint.GetText(editPoint.LineLength - item.ReplaceSpan.iEndIndex + 1).Trim());

            int botLines = 0;
            currentLine = item.ReplaceSpan.iEndLine + 2;
            while (botLines < NumericConstants.ContextLineRadius) {
                editPoint.MoveToLineAndOffset(currentLine, 1);
                string lineText = editPoint.GetText(editPoint.LineLength).Trim();
                if (lineText.Length > 0) {
                    context.Append(Environment.NewLine + lineText);
                    if (lineText.Length > 1) botLines++;
                }
                editPoint.EndOfLine();
                if (editPoint.AtEndOfDocument) break;
                currentLine++;
            }

            item.Context = context.ToString();
            item.ContextRelativeLine = contextRelativeLine;
        }
    }
}
