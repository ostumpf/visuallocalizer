﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using EnvDTE80;
using System.Collections;
using VisualLocalizer.Library;
using VisualLocalizer.Commands;
using VisualLocalizer.Library.Extensions;

namespace VisualLocalizer.Components.Code {

    /// <summary>
    /// Provides functionality for exploring C# code file using FileCodeModel. Content of each method and variable initializer
    /// is handled by some instance of AbstractBatchCommand, whose method LookupInCSharp() is called as callback.
    /// </summary>
    internal class CSharpCodeExplorer {

        protected CSharpCodeExplorer() { }

        private static CSharpCodeExplorer instance;
        public static CSharpCodeExplorer Instance {
            get {
                if (instance == null) instance = new CSharpCodeExplorer();
                return instance;
            }
        }        

        /// <summary>
        /// Recursively explores given file
        /// </summary>
        /// <param name="parentCommand">Command whose method is called when bubbled down to a method or a variable</param>
        /// <param name="exploreable">Predicate determining which CodeElements will be explored</param>
        /// <param name="codeModel">File's code model</param>
        public void Explore(AbstractBatchCommand parentCommand, Predicate<CodeElement> exploreable, FileCodeModel2 codeModel) {
            if (parentCommand == null) throw new ArgumentNullException("parentCommand");
            if (codeModel == null) throw new ArgumentNullException("codeModel");
            if (codeModel.CodeElements == null) return;

            foreach (CodeElement2 codeElement in codeModel.CodeElements) {
                // explore namespaces, classes, structs and modules
                if (codeElement.Kind == vsCMElement.vsCMElementNamespace || codeElement.Kind == vsCMElement.vsCMElementClass ||
                    codeElement.Kind == vsCMElement.vsCMElementStruct || codeElement.Kind == vsCMElement.vsCMElementModule) {
                    Explore(parentCommand, codeElement, null, exploreable, false);
                }
            }
        }

        /// <summary>
        /// Recursively explores given code element
        /// </summary>
        /// <param name="parentCommand"></param>
        /// <param name="currentElement">Element to explore</param>
        /// <param name="parentNamespace">Closest parent namespace of the element</param>
        /// <param name="exploreable"></param>
        /// <param name="isLocalizableFalse"></param>
        private void Explore(AbstractBatchCommand parentCommand, CodeElement2 currentElement, CodeElement2 parentNamespace, Predicate<CodeElement> exploreable, bool isLocalizableFalse) {
            if (currentElement == null) throw new ArgumentNullException("currentElement");

            bool isLocalizableFalseSetOnParent = currentElement.HasLocalizableFalseAttribute(); // is element decorated with [Localizable(false)] ?

            // continue exploring in case of namepsace, class, struct, module, function, variable or property
            foreach (CodeElement2 codeElement in currentElement.Children) {
                if (codeElement.Kind == vsCMElement.vsCMElementClass || codeElement.Kind == vsCMElement.vsCMElementModule || codeElement.Kind == vsCMElement.vsCMElementStruct) {
                    Explore(parentCommand, codeElement, currentElement.Kind == vsCMElement.vsCMElementNamespace ? currentElement : parentNamespace,
                        exploreable, isLocalizableFalse || isLocalizableFalseSetOnParent);
                }
                if (codeElement.Kind == vsCMElement.vsCMElementNamespace) {
                    Explore(parentCommand, codeElement, currentElement, exploreable, isLocalizableFalse || isLocalizableFalseSetOnParent);
                }
                if (codeElement.Kind == vsCMElement.vsCMElementVariable) {
                    Explore(parentCommand, codeElement as CodeVariable2, (CodeNamespace)parentNamespace, currentElement,
                        exploreable, isLocalizableFalse || isLocalizableFalseSetOnParent);
                }
                if (codeElement.Kind == vsCMElement.vsCMElementFunction) {
                    Explore(parentCommand, codeElement as CodeFunction2, (CodeNamespace)parentNamespace, currentElement,
                        exploreable, isLocalizableFalse || isLocalizableFalseSetOnParent);
                }
                if (codeElement.Kind == vsCMElement.vsCMElementProperty) {
                    Explore(parentCommand, codeElement as CodeProperty, (CodeNamespace)parentNamespace, currentElement,
                        exploreable, isLocalizableFalse || isLocalizableFalseSetOnParent);
                }
            }
        }        

        /// <summary>
        /// Explores given property
        /// </summary> 
        private void Explore(AbstractBatchCommand parentCommand, CodeProperty codeProperty, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct, Predicate<CodeElement> exploreable, bool isLocalizableFalse) {
            bool propertyLocalizableFalse = (codeProperty as CodeElement).HasLocalizableFalseAttribute();            
                
            if (codeProperty.Getter != null && !codeProperty.Getter.IsAutoGenerated()) Explore(parentCommand, codeProperty.Getter as CodeFunction2, parentNamespace, codeClassOrStruct, exploreable, isLocalizableFalse || propertyLocalizableFalse);
            if (codeProperty.Setter != null && !codeProperty.Setter.IsAutoGenerated()) Explore(parentCommand, codeProperty.Setter as CodeFunction2, parentNamespace, codeClassOrStruct, exploreable, isLocalizableFalse || propertyLocalizableFalse);
        }

        /// <summary>
        /// Explores given variable initializer using C# lookuper
        /// </summary>   
        protected virtual void Explore(AbstractBatchCommand parentCommand, CodeVariable2 codeVariable, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct, Predicate<CodeElement> exploreable, bool isLocalizableFalse) {
            if (codeVariable.InitExpression == null) return; // variable must have an initializer
            if (codeClassOrStruct.Kind == vsCMElement.vsCMElementStruct && !codeVariable.IsShared) return; // instance variable of structs cannot have initializers
            if (!exploreable(codeVariable as CodeElement)) return; // predicate must evaluate to true

            string initExpression = codeVariable.GetText(); // get text of initializer
            if (string.IsNullOrEmpty(initExpression)) return;

            TextPoint startPoint = codeVariable.StartPoint;

            // is variable decorated with Localizable(false)
            bool variableLocalizableFalse = (codeVariable as CodeElement).HasLocalizableFalseAttribute();

            // run lookuper using parent command
            var list=parentCommand.LookupInCSharp(initExpression, startPoint, parentNamespace, codeClassOrStruct, null, codeVariable.Name, isLocalizableFalse || variableLocalizableFalse);

            // add context to result items (surounding few lines of code)
            EditPoint2 editPoint = (EditPoint2)startPoint.CreateEditPoint();
            foreach (AbstractResultItem item in list) {
                item.IsConst = codeVariable.ConstKind == vsCMConstKind.vsCMConstKindConst;
                item.CodeModelSource = codeVariable;
                AddContextToItem(item, editPoint);
            }
        }

        /// <summary>
        /// Explores given method using C# lookuper
        /// </summary> 
        protected virtual void Explore(AbstractBatchCommand parentCommand, CodeFunction2 codeFunction, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct, Predicate<CodeElement> exploreable, bool isLocalizableFalse) {
            if (codeFunction.MustImplement) return; // method must not be abstract or declated in an interface            
            if (!exploreable(codeFunction as CodeElement)) return; // predicate must eval to true

            // there is no way of knowing whether a function is not declared 'extern'. In that case, following will throw an exception.
            string functionText = null;
            try {
                functionText = codeFunction.GetText(); // get method text
            } catch (Exception) { }
            if (string.IsNullOrEmpty(functionText)) return;

            TextPoint startPoint = codeFunction.GetStartPoint(vsCMPart.vsCMPartBody);

            // is method decorated with Localizable(false)
            bool functionLocalizableFalse = (codeFunction as CodeElement).HasLocalizableFalseAttribute();

            var list = parentCommand.LookupInCSharp(functionText, startPoint, parentNamespace, codeClassOrStruct, codeFunction.Name, null, isLocalizableFalse || functionLocalizableFalse);
            
            // add context to result items (surounding few lines of code)
            EditPoint2 editPoint = (EditPoint2)startPoint.CreateEditPoint();
            foreach (AbstractResultItem item in list) {
                item.CodeModelSource = codeFunction;
                AddContextToItem(item, editPoint);
            }
            
            // read optional arguments initializers (just to show them - they cannot be moved)
            TextPoint headerStartPoint = null;
            try {
                headerStartPoint = codeFunction.GetStartPoint(vsCMPart.vsCMPartHeader);
            } catch (Exception) { }
            if (headerStartPoint == null) return;

            string headerText = codeFunction.GetHeaderText();

            // search method header
            var headerList = parentCommand.LookupInCSharp(headerText, headerStartPoint, parentNamespace, codeClassOrStruct, codeFunction.Name, null, isLocalizableFalse || functionLocalizableFalse);
            
            // add to list
            editPoint = (EditPoint2)startPoint.CreateEditPoint();
            foreach (AbstractResultItem item in headerList) {
                item.IsConst = true;
                item.CodeModelSource = codeFunction;
                AddContextToItem(item, editPoint);
            }
        }

        /// <summary>
        /// Adds context to the result item, coming from code block starting at given position
        /// </summary> 
        protected void AddContextToItem(AbstractResultItem item, EditPoint2 editPoint) {
            StringBuilder context = new StringBuilder();
            // indices +1 !!
            
            int topLines = 0;

            int currentLine = item.ReplaceSpan.iStartLine;
            int contextRelativeLine = 0;

            // add NumericConstants.ContextLineRadius lines above the result item with at least 2 non-whitespace characters
            while (currentLine >= 1 && topLines < NumericConstants.ContextLineRadius) {
                editPoint.MoveToLineAndOffset(currentLine, 1);
                string lineText = editPoint.GetText(editPoint.LineLength);
                if (lineText.Trim().Length > 0) {
                    context.Insert(0, lineText + Environment.NewLine);
                    contextRelativeLine++;
                    if (lineText.Trim().Length > 1) topLines++;
                }
                currentLine--;
            }

            editPoint.MoveToLineAndOffset(item.ReplaceSpan.iStartLine + 1, 1);
            context.Append(editPoint.GetText(item.ReplaceSpan.iStartIndex));

            context.Append(StringConstants.ContextSubstituteText); // add text that will be displayed instead of actual result item

            editPoint.MoveToLineAndOffset(item.ReplaceSpan.iEndLine + 1, item.ReplaceSpan.iEndIndex + 1);
            context.Append(editPoint.GetText(editPoint.LineLength - item.ReplaceSpan.iEndIndex + 1));

            int botLines = 0;
            currentLine = item.ReplaceSpan.iEndLine + 2;
            // add NumericConstants.ContextLineRadius lines below the result item with at least 2 non-whitespace characters
            while (botLines < NumericConstants.ContextLineRadius) {
                editPoint.MoveToLineAndOffset(currentLine, 1);
                string lineText = editPoint.GetText(editPoint.LineLength);
                if (lineText.Trim().Length > 0) {
                    context.Append(Environment.NewLine + lineText);
                    if (lineText.Trim().Length > 1) botLines++;
                }
                editPoint.EndOfLine();
                if (editPoint.AtEndOfDocument) break;
                currentLine++;
            }

            item.Context = context.ToString();
            item.ContextRelativeLine = contextRelativeLine; // index of "middle" line
        }
    }
}
