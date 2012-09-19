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
    internal abstract class AbstractCommand {

        protected IVsTextManager textManager;        
        protected IVsTextLines textLines;
        protected IVsTextView textView;
        protected IOleUndoManager undoManager;        
        protected Document currentDocument;
        protected FileCodeModel2 currentCodeModel;
     
        public virtual void Process() {
            currentDocument = VisualLocalizerPackage.Instance.DTE.ActiveDocument;
            if (currentDocument == null)
                throw new Exception("No selected document");
            currentCodeModel = currentDocument.ProjectItem.FileCodeModel as FileCodeModel2;
            if (currentCodeModel == null)
                throw new Exception("Current document has no CodeModel.");
            if (currentDocument.ReadOnly)
                throw new Exception("Cannot perform this operation - active document is readonly");

            textManager = (IVsTextManager)Package.GetGlobalService(typeof(SVsTextManager));

            int hr = textManager.GetActiveView(1, null, out textView);
            Marshal.ThrowExceptionForHR(hr);
            
            hr = textView.GetBuffer(out textLines);
            Marshal.ThrowExceptionForHR(hr);

            hr = textLines.GetUndoManager(out undoManager);
            Marshal.ThrowExceptionForHR(hr);           
        }

        protected bool GetCodeBlockFromSelection(out string text, out TextPoint startPoint, out string codeFunctionName, out string codeVariableName, out CodeElement2 codeClass, out TextSpan selectionSpan) {
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

            try {
                CodeFunction2 codeFunction = (CodeFunction2)currentCodeModel.CodeElementFromPoint(selectionPoint, vsCMElement.vsCMElementFunction);
                codeFunctionName = codeFunction.Name;
                codeClass = codeFunction.GetClass();

                startPoint = codeFunction.GetStartPoint(vsCMPart.vsCMPartBody);
                text = codeFunction.GetText();
                ok = true;
            } catch (Exception) {
                try {
                    CodeProperty codeProperty = (CodeProperty)currentCodeModel.CodeElementFromPoint(selectionPoint, vsCMElement.vsCMElementProperty);
                    codeFunctionName = codeProperty.Name;
                    codeClass = codeProperty.GetClass();

                    startPoint = codeProperty.GetStartPoint(vsCMPart.vsCMPartBody);
                    text = codeProperty.GetText();
                    ok = true;
                } catch (Exception) {
                    try {
                        CodeVariable2 codeVariable = (CodeVariable2)currentCodeModel.CodeElementFromPoint(selectionPoint, vsCMElement.vsCMElementVariable);
                        if (codeVariable.ConstKind != vsCMConstKind.vsCMConstKindConst &&
                            codeVariable.Type.TypeKind == vsCMTypeRef.vsCMTypeRefString &&
                            codeVariable.InitExpression != null) {

                            codeVariableName = codeVariable.Name;
                            text = codeVariable.GetText();
                            startPoint = codeVariable.StartPoint;
                            codeClass = codeVariable.GetClass();
                            if (codeClass.Kind == vsCMElement.vsCMElementClass)
                                ok = true;
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
