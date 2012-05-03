using System  ;
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
using EnvDTE80;

namespace VisualLocalizer.Commands {
    internal class MoveToResourcesCommand : AbstractCommand {

        public MoveToResourcesCommand(VisualLocalizerPackage package)
            : base(package) {
        }

        public override void Process() {
            TextSpan replaceSpan = GetReplaceSpan();
            string referenceValue = GetTextOfSpan(replaceSpan);
            referenceValue = GetReferencedValue(referenceValue);
            
            Project project = package.DTE.ActiveDocument.ProjectItem.ContainingProject;
            List<ResXProjectItem> resourceFiles = Utils.GetResourceFilesOf(project);
          
            SelectResourceFileForm f = new SelectResourceFileForm();
            f.SetData(Utils.CreateKeyFromValue(referenceValue), referenceValue, resourceFiles);          
            DialogResult result = f.ShowDialog(Form.FromHandle(new IntPtr(package.DTE.MainWindow.HWnd)));
            
            if (result==DialogResult.OK) {
                textView.ReplaceTextOnLine(replaceSpan.iStartLine, replaceSpan.iStartIndex,
                replaceSpan.iEndIndex - replaceSpan.iStartIndex, f.ReferenceText, f.ReferenceText.Length);

                textView.SetSelection(replaceSpan.iStartLine, replaceSpan.iStartIndex,
                    replaceSpan.iEndLine, replaceSpan.iStartIndex + f.ReferenceText.Length);

                ResXFileHandler.AddString(f.Key, f.Value, f.SelectedItem);

                if (!f.UsingFullName) {
                    AddUsingBlock(f.Namespace);
                }
            }
        }                

        private string GetReferencedValue(string value) {
            if (value.StartsWith("@")) value = value.Substring(1);
            return value.Substring(1, value.Length - 2);
        }

        private string GetTextOfSpan(TextSpan span) {
            string str;
            textLines.GetLineText(span.iStartLine,span.iStartIndex,
                span.iEndLine,span.iEndIndex,out str);

            return str;
        }        

        private TextSpan GetReplaceSpan() {
            TextSpan[] spans = new TextSpan[1];
            textView.GetSelectionSpan(spans);
            TextSpan selectionSpan = spans[0];
          
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
                    string text = GetTextOfSpan(selectionSpan);
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
