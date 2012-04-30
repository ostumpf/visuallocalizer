using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell;
using System.Diagnostics;
using EnvDTE;
using VisualLocalizer.Components;
using Microsoft.VisualStudio.Shell.Interop;
using System.Windows.Forms;
using System.IO;

namespace VisualLocalizer.Commands {
    internal class MoveToResourcesCommand {

        private static MoveToResourcesCommand instance;
        private IVsTextManager textManager;
        private VisualLocalizerPackage package;

        private MoveToResourcesCommand(VisualLocalizerPackage package) {
            this.package = package;
        }

        public static void Handle(VisualLocalizerPackage package) {
            if (instance == null)
                instance = new MoveToResourcesCommand(package);

            instance.ProcessCommand();
        }

        internal void ProcessCommand() {
            if (textManager==null)
                textManager = (IVsTextManager)Package.GetGlobalService(typeof(SVsTextManager));

            IVsTextView textView;
            textManager.GetActiveView(1, null, out textView);

            TextSpan replaceSpan = GetReplaceSpan(textView);
            string referenceValue = GetTextOfSpan(textView,replaceSpan);

            string newKey, newValue;
            ResXProjectItem resxItem;
            bool canceled;
            ResolveReference(referenceValue, out canceled, out newKey, out newValue, out resxItem);

            if (!canceled) {
                string resourceRoot = Path.GetFileNameWithoutExtension(resxItem.ProjectItem.Name);
                string reference = resourceRoot + "." + newKey;

                textView.ReplaceTextOnLine(replaceSpan.iStartLine, replaceSpan.iStartIndex,
                replaceSpan.iEndIndex - replaceSpan.iStartIndex, reference, reference.Length);

                textView.SetSelection(replaceSpan.iStartLine, replaceSpan.iStartIndex,
                    replaceSpan.iEndLine, replaceSpan.iStartIndex + reference.Length);

                ResXFileHandler.AddString(newKey, newValue, resxItem);
            }
        }

        private string GetTextOfSpan(IVsTextView textView,TextSpan replaceSpan) {
            IVsTextLines textLines;
            textView.GetBuffer(out textLines);

            string str;
            textLines.GetLineText(replaceSpan.iStartLine,replaceSpan.iStartIndex,
                replaceSpan.iEndLine,replaceSpan.iEndIndex,out str);

            return str;
        }

        private void ResolveReference(string oldValue,out bool canceled, out string newKey, out string newValue, out ResXProjectItem resourceItem) {
            if (oldValue.StartsWith("@")) oldValue = oldValue.Substring(1);
            oldValue = oldValue.Substring(1, oldValue.Length - 2);

            Projects allProjects = package.DTE.Solution.Projects;
            Project project=package.DTE.ActiveDocument.ProjectItem.ContainingProject;
            List<ResXProjectItem> resourceFiles = Utils.GetResourceFilesOf(allProjects,project);
            string key = Utils.CreateKeyFromValue(oldValue);

            SelectResourceFileForm f = new SelectResourceFileForm();
            f.SetData(key, oldValue, resourceFiles);
            DialogResult result=f.ShowDialog(Form.FromHandle(new IntPtr(package.DTE.MainWindow.HWnd)));

            canceled = result == DialogResult.Cancel;
            f.GetData(out newKey, out newValue, out resourceItem);           
        }       

        private TextSpan GetReplaceSpan(IVsTextView textView) {
            TextSpan[] spans = new TextSpan[1];
            textView.GetSelectionSpan(spans);
            TextSpan selectionSpan = spans[0];

            IVsTextLines textLines;
            textView.GetBuffer(out textLines);

            string lineText;
            int lineLength;
            textLines.GetLengthOfLine(selectionSpan.iStartLine, out lineLength);
            textLines.GetLineText(selectionSpan.iStartLine, 0, selectionSpan.iStartLine, lineLength, out lineText);

            selectionSpan = TrimSpan(selectionSpan, lineText);
          
            if (selectionSpan.iStartLine != selectionSpan.iEndLine)
                throw new Exception("This selection cannot be referenced!");                

            int newStartIndex = GetNewStartIndex(selectionSpan.iStartIndex,lineText);               
            if (newStartIndex < 0)
                throw new Exception("This selection cannot be referenced!");

            int newEndIndex = GetNewEndIndex(selectionSpan.iEndIndex,lineText);                                
            if (newEndIndex >= lineLength)
                throw new Exception("This selection cannot be referenced!");
            
            TextSpan span = new TextSpan();
            span.iStartLine = selectionSpan.iStartLine;
            span.iEndLine = selectionSpan.iStartLine;
            span.iStartIndex = newStartIndex;
            span.iEndIndex = newEndIndex+1;

            return span;
                     
        }

        private TextSpan TrimSpan(TextSpan selectionSpan, string textLine) {
            while (selectionSpan.iEndIndex>0 && char.IsWhiteSpace(textLine, selectionSpan.iEndIndex-1)) 
                selectionSpan.iEndIndex--;

            while (selectionSpan.iStartIndex < textLine.Length && char.IsWhiteSpace(textLine, selectionSpan.iStartIndex))
                selectionSpan.iStartIndex++;

            return selectionSpan;            
        }

        private int GetNewEndIndex(int p, string lineText) {
            p--;
            char prevChar = (p > 0 ? lineText[p - 1] : '?');
            char currentChar = lineText[p];
            while (p < lineText.Length) {
                if (currentChar == '\"' && prevChar != '\\') {                    
                    return p;
                }
                
                p++;
                if (p >= lineText.Length) break;

                prevChar = currentChar;
                currentChar = lineText[p];
            }
            return lineText.Length;                
        }

        private int GetNewStartIndex(int p, string lineText) {
            char prevChar = (p<lineText.Length-1 ? lineText[p+1]:'?');
            char currentChar = lineText[p];

            while (p >= 0) {                
                if (currentChar == '"' && p == 0)
                    return 0;

                if (currentChar != '\\' && prevChar == '\"') {
                    if (currentChar == '@')
                        return p;
                    else
                        return p + 1;
                }
                
                p--;
                if (p < 0) break;

                prevChar = currentChar;
                currentChar = lineText[p];
            }
            return -1;            
        }

       
    }
}
