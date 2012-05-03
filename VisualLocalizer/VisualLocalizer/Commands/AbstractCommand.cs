using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using EnvDTE80;

namespace VisualLocalizer.Commands {
    internal abstract class AbstractCommand {

        protected VisualLocalizerPackage package;
        protected IVsTextManager textManager;
        protected IVsTextLines textLines;
        protected IVsTextView textView;

        public AbstractCommand(VisualLocalizerPackage package) {
            this.package = package;

            InitTextView();
        }

        private void InitTextView() {
            if (textManager == null)
                textManager = (IVsTextManager)Package.GetGlobalService(typeof(SVsTextManager));

            textManager.GetActiveView(1, null, out textView);
            textView.GetBuffer(out textLines);
        }

        protected void AddUsingBlock(string newNamespace) {
            Document doc = package.DTE.ActiveDocument;
            TextSelection selection = doc.Selection as TextSelection;
            FileCodeModel2 model = doc.ProjectItem.FileCodeModel as FileCodeModel2;

            CodeElement selectionNamespace = model.CodeElementFromPoint(selection.ActivePoint, vsCMElement.vsCMElementNamespace);
            string currentNamespace = selectionNamespace.FullName;

            if (currentNamespace != newNamespace) {
                bool alreadyUsing = false;
                int lastLine = 0;
                foreach (CodeElement t in model.CodeElements)
                    if (t.Kind == vsCMElement.vsCMElementImportStmt) {
                        string usingNmsName = GetNamespaceFromUsing(t.StartPoint, t.EndPoint);
                        if (usingNmsName == newNamespace) {
                            alreadyUsing = true;
                            lastLine = t.EndPoint.Line;
                            break;
                        }
                    }

                if (!alreadyUsing) {
                    model.AddImport(newNamespace, 0, string.Empty);
                }
            }
        }

        private string GetNamespaceFromUsing(TextPoint start, TextPoint end) {
            string utext = "using";

            string text;
            textLines.GetLineText(start.Line - 1, start.DisplayColumn - 1, end.Line - 1, end.DisplayColumn - 1, out text);
            text = text.Trim();
            if (!text.StartsWith(utext) || !text.EndsWith(";"))
                throw new Exception("Error while parsing using statement: " + text);

            text = text.Substring(utext.Length, text.LastIndexOf(';') - utext.Length);
            text = text.Trim();

            return text;
        }

        public abstract void Process();
    }
}
