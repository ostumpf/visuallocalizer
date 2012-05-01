using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell;
using System.Diagnostics;
using EnvDTE;
using VisualLocalizer.Editor;
using Microsoft.VisualStudio.Shell.Interop;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using VisualLocalizer.Gui;

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
            
            Project project=package.DTE.ActiveDocument.ProjectItem.ContainingProject;
            List<ResXProjectItem> resourceFiles = Utils.GetResourceFilesOf(project);
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
            int spanLength = selectionSpan.iEndIndex - selectionSpan.iStartIndex;

            if (selectionSpan.iStartLine != selectionSpan.iEndLine)
                throw new NotReferencableException(selectionSpan,lineText);

            int newStartIndex = 0, newEndIndex = 0;
            if (spanLength <= 0) {
                if (selectionSpan.iStartIndex > 0 && lineText[selectionSpan.iStartIndex - 1] == '@') 
                    selectionSpan.iStartIndex++;

                int rightCount = countAposRight(lineText, selectionSpan.iStartIndex, out newEndIndex);
                newEndIndex++;
                int leftCount = countAposLeft(lineText, selectionSpan.iStartIndex, out newStartIndex);
                
                if (rightCount % 2 == 0 || leftCount%2==0)
                    throw new NotReferencableException(selectionSpan, lineText);
            } else {
                if (selectionSpan.iStartIndex > 0 && lineText[selectionSpan.iStartIndex - 1] == '@')
                    selectionSpan.iStartIndex--;

                int rightCount = countAposRight(lineText, selectionSpan.iEndIndex, out newEndIndex);
                int leftCount = countAposLeft(lineText, selectionSpan.iStartIndex, out newStartIndex);

                if (rightCount % 2 == 0 && leftCount % 2 == 0) {
                    string text = GetTextOfSpan(textView, selectionSpan);
                    if ((text.StartsWith("\"") || text.StartsWith("@\"")) && text.EndsWith("\"") && !text.EndsWith("\\\"")) {
                        newEndIndex = selectionSpan.iEndIndex;
                        newStartIndex = selectionSpan.iStartIndex;
                    } else throw new NotReferencableException(selectionSpan, lineText);
                } else if (rightCount % 2 != 0 && leftCount % 2 != 0) {
                    newEndIndex++;
                } else throw new NotReferencableException(selectionSpan, lineText);
            }

            
            TextSpan span = new TextSpan();
            span.iStartLine = selectionSpan.iStartLine;
            span.iEndLine = selectionSpan.iStartLine;
            span.iStartIndex = newStartIndex;
            span.iEndIndex = newEndIndex;

            return span;
                     
        }

        private int countAposRight(string text,int beginIndex, out int firstIndex) {
            int count = 0;
            firstIndex = -1;            

            int p = beginIndex;

            char prevChar = (p - 1 >= 0 ? text[p - 1] : '?');
            char currentChar = text[p];

            while (p < text.Length) {
                if (currentChar == '"' && prevChar != '\\') {
                    if (count == 0) {
                        firstIndex = p;
                    }
                    count++;
                }

                p++;
                if (p >= text.Length) break;

                prevChar = currentChar;
                currentChar = text[p];
            }

            return count;
        }

        private int countAposLeft(string text, int beginIndex, out int firstIndex) {
            int count = 0;
            firstIndex = -1;

            int p = beginIndex-1;
            if (p <= 0) return 0;

            char prevChar = text[p];
            char currentChar = (p - 1 >= 0 ? text[p - 1] : '?');
            p--;

            while (p >= 0) {
                if (currentChar != '\\' && prevChar == '\"') {
                    if (count == 0) {
                        if (currentChar == '@')
                            firstIndex = p;
                        else
                            firstIndex = p+1;
                    }
                    count++;
                }

                p--;                
                prevChar = currentChar;

                if (p >= 0)
                    currentChar = text[p];
                else if (p == -1)
                    currentChar = '?';
                else break;
            }

            return count;
        }

        private TextSpan TrimSpan(TextSpan span, string textLine) {
            while (span.iEndIndex>0 && char.IsWhiteSpace(textLine, span.iEndIndex-1)) 
                span.iEndIndex--;

            while (span.iStartIndex < textLine.Length && char.IsWhiteSpace(textLine, span.iStartIndex))
                span.iStartIndex++;

            return span;            
        }   
       
    }
}
