using System;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using EnvDTE80;
using VisualLocalizer.Components;
using Microsoft.VisualStudio.OLE.Interop;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using VisualLocalizer.Library;

namespace VisualLocalizer.Commands {

    /// <summary>
    /// Base class for ad-hoc commands - Inline and Move, invoked from code context menu.
    /// </summary>
    internal abstract class AbstractCommand {

        protected IVsTextManager textManager;        
        protected IVsTextLines textLines;
        protected IVsTextView textView;
        protected IOleUndoManager undoManager;        
        protected Document currentDocument;
        protected FileCodeModel2 currentCodeModel;
     
        /// <summary>
        /// Called on click - overriden in derived class to provide desired functionality.
        /// Initializes basic variables, common for all derived commands.
        /// </summary>
        public virtual void Process() {
            currentDocument = VisualLocalizerPackage.Instance.DTE.ActiveDocument;
            if (currentDocument == null)
                throw new Exception("No selected document");
            if (currentDocument.ReadOnly)
                throw new Exception("Cannot perform this operation - active document is readonly");

            currentCodeModel = currentDocument.ProjectItem.FileCodeModel as FileCodeModel2;
            
            textManager = (IVsTextManager)Package.GetGlobalService(typeof(SVsTextManager));
            if (textManager == null)
                throw new Exception("Cannot consume IVsTextManager service");

            int hr = textManager.GetActiveView(1, null, out textView);
            Marshal.ThrowExceptionForHR(hr);
            
            hr = textView.GetBuffer(out textLines);
            Marshal.ThrowExceptionForHR(hr);

            hr = textLines.GetUndoManager(out undoManager);
            Marshal.ThrowExceptionForHR(hr);           
        }

        /// <summary>
        /// Used by C# ad-hoc methods (move and inline) to get information about the block of code, where right-click was performed.
        /// This methods makes use of document's FileCodeModel, which is not available for ASP .NET files - these files are thus
        /// handled by their own parser.
        /// </summary>
        /// <param name="text">Text of the block (e.g. code of a method, declaration of a variable...)</param>
        /// <param name="startPoint">Beginning of the text</param>
        /// <param name="codeFunctionName">Name of the function, where right-click was performed, null if right-click was performed on a variable.</param>
        /// <param name="codeVariableName">Name of the variable, where right-click was performed, null otherwise.</param>
        /// <param name="codeClass">Name of the class, where the code block is located.</param>
        /// <param name="selectionSpan">Current selection span.</param>
        /// <returns>True, if all necessary information was succesfully obtained, false otherwise.</returns>
        protected bool GetCodeBlockFromSelection(out string text, out TextPoint startPoint, out string codeFunctionName, out string codeVariableName, out CodeElement2 codeClass, out TextSpan selectionSpan) {
            // get current selection span
            TextSpan[] spans = new TextSpan[1];
            int hr = textView.GetSelectionSpan(spans);
            Marshal.ThrowExceptionForHR(hr);            

            selectionSpan = spans[0];
            
            object o;
            hr = textLines.CreateTextPoint(selectionSpan.iStartLine, selectionSpan.iStartIndex, out o);
            Marshal.ThrowExceptionForHR(hr);
            TextPoint selectionPoint = (TextPoint)o;
            
            startPoint = null;
            text = null;
            bool ok = false;            
            codeFunctionName = null;
            codeVariableName = null;
            codeClass = null;
            
            // It is impossible to find out the code block, where right-click was performed. Following code
            // assumes that valid string literals or references can only be found in a method, in a class variable (as initializers)
            // or in a property code. C# syntax permits more locations (attributes, default argument values in .NET 4+) but we can ignore
            // these, because they are all evaluated at compile-time, making resource references impossible.

            // assume we are in a function (method)
            try {
                CodeFunction2 codeFunction = (CodeFunction2)currentCodeModel.CodeElementFromPoint(selectionPoint, vsCMElement.vsCMElementFunction);
                codeFunctionName = codeFunction.Name;
                codeClass = codeFunction.GetClass(); // extension
                
                text = codeFunction.GetText();
                if (!string.IsNullOrEmpty(text)) {
                    startPoint = codeFunction.GetStartPoint(vsCMPart.vsCMPartBody);
                    ok = true;
                }                
            } catch (Exception) {
                // it's not a method - maybe a property?                
                try {
                    CodeProperty codeProperty = (CodeProperty)currentCodeModel.CodeElementFromPoint(selectionPoint, vsCMElement.vsCMElementProperty);
                    codeFunctionName = codeProperty.Name;
                    codeClass = codeProperty.GetClass();
                    
                    text = codeProperty.GetText();
                    if (!string.IsNullOrEmpty(text)) {
                        startPoint = codeProperty.GetStartPoint(vsCMPart.vsCMPartBody);
                        ok = true;
                    }                    
                } catch (Exception) {
                    // not a property, either. It must be a variable - or there's no valid code block
                    try {
                        CodeVariable2 codeVariable = (CodeVariable2)currentCodeModel.CodeElementFromPoint(selectionPoint, vsCMElement.vsCMElementVariable);
                        if (codeVariable.ConstKind != vsCMConstKind.vsCMConstKindConst &&
                            codeVariable.Type.TypeKind == vsCMTypeRef.vsCMTypeRefString &&
                            codeVariable.InitExpression != null) {
                        
                            codeVariableName = codeVariable.Name;
                            codeClass = codeVariable.GetClass();
                            
                            startPoint = codeVariable.StartPoint;
                            text = codeVariable.GetText();
                            if (codeClass.Kind == vsCMElement.vsCMElementClass && !string.IsNullOrEmpty(text)) {
                                ok = true;                            
                            }
                        }                        
                    } catch (Exception) {                        
                        return false;
                    }
                }
            }

            return ok;
        }
      
    }
}
