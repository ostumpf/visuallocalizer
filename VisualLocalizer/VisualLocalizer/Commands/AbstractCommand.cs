using System;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using EnvDTE80;
using VisualLocalizer.Components;
using Microsoft.VisualStudio.OLE.Interop;
using System.Collections;
using System.Collections.Generic;

namespace VisualLocalizer.Commands {
    internal abstract class AbstractCommand {

        protected VisualLocalizerPackage package;
        protected IVsTextManager textManager;
        protected IVsTextLines textLines;
        protected IVsTextView textView;
        protected IOleUndoManager undoManager;
        protected Document doc;

        public AbstractCommand(VisualLocalizerPackage package) {
            this.package = package;

            InitTextView();
        }

        private void InitTextView() {
            if (textManager == null)
                textManager = (IVsTextManager)Package.GetGlobalService(typeof(SVsTextManager));

            textManager.GetActiveView(1, null, out textView);
            textView.GetBuffer(out textLines);            
            textLines.GetUndoManager(out undoManager);
            doc = package.DTE.ActiveDocument;
        }

        protected bool IsNamespaceUsed(string newNamespace, out string alias) {
            alias = string.Empty;

            TextSelection selection = doc.Selection as TextSelection;
            FileCodeModel2 model = doc.ProjectItem.FileCodeModel as FileCodeModel2;

            CodeElement selectionNamespace = model.CodeElementFromPoint(selection.ActivePoint, vsCMElement.vsCMElementNamespace);
            string currentNamespace = selectionNamespace.FullName;

            if (currentNamespace == newNamespace) return true;

            bool alreadyUsing = false;
            foreach (CodeElement t in model.CodeElements)
                if (t.Kind == vsCMElement.vsCMElementImportStmt) {
                    string usingAlias, usingNmsName;
                    ParseUsing(t.StartPoint, t.EndPoint, out usingNmsName, out usingAlias);
                    if (usingNmsName == newNamespace) {
                        alreadyUsing = true;
                        alias = usingAlias;
                        break;
                    }
                }
            return alreadyUsing;
        }

        protected CodeImport AddUsingBlock(string newNamespace) {                        
            FileCodeModel2 model = doc.ProjectItem.FileCodeModel as FileCodeModel2;
            
            return model.AddImport(newNamespace, 0, string.Empty);
        }      

        protected void ParseUsing(TextPoint start, TextPoint end,out string namespc,out string alias) {
            alias = string.Empty;

            string text;
            textLines.GetLineText(start.Line - 1, start.DisplayColumn - 1, end.Line - 1, end.DisplayColumn - 1, out text);
            text = text.Trim();
            if (!text.StartsWith(StringConstants.UsingStatement) || !text.EndsWith(";"))
                throw new Exception("Error while parsing using statement: " + text);

            text = text.Substring(StringConstants.UsingStatement.Length, text.LastIndexOf(';') - StringConstants.UsingStatement.Length);
            text = Utils.RemoveWhitespace(text);
            int eqIndex = text.IndexOf('=');
            if (eqIndex > 0) {
                alias = text.Substring(0, eqIndex);
                namespc = text.Substring(eqIndex + 1);
            } else {
                namespc = text;
            }            
        }
        
        protected string GetTextOfSpan(TextSpan span) {
            string str;
            textLines.GetLineText(span.iStartLine, span.iStartIndex,
                span.iEndLine, span.iEndIndex, out str);

            return str;
        }

        protected TextSpan TrimSpan(TextSpan span, string textLine) {
            while (span.iEndIndex > 0 && char.IsWhiteSpace(textLine, span.iEndIndex - 1))
                span.iEndIndex--;

            while (span.iStartIndex < textLine.Length && char.IsWhiteSpace(textLine, span.iStartIndex))
                span.iStartIndex++;

            return span;
        }   

        public abstract void Process();
    }
}
