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
using Microsoft.VisualStudio.OLE.Interop;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;

namespace VisualLocalizer.Commands {
    internal sealed class MoveToResourcesCommand : AbstractCommand {

        public MoveToResourcesCommand(VisualLocalizerPackage package)
            : base(package) {
        }       

        public override void Process() {
            TextSpan replaceSpan = GetReplaceSpan();
            bool isInAttribute = IsInAttribute(replaceSpan);
            if (isInAttribute) throw new NotReferencableException(replaceSpan, null);

            string referenceValue = GetTextOfSpan(replaceSpan);            
            referenceValue = GetReferencedValue(referenceValue);
            
            Project project = currentDocument.ProjectItem.ContainingProject;
            List<ProjectItem> items = project.GetFilesOf(ResXProjectItem.IsItemResX);
            List<ResXProjectItem> resxItems = new List<ResXProjectItem>();
            foreach (ProjectItem item in items)
                resxItems.Add(ResXProjectItem.ConvertToResXItem(item, project));

            SelectResourceFileForm f = new SelectResourceFileForm();
            f.SetData(Utils.CreateKeyFromValue(referenceValue), referenceValue, resxItems);          
            DialogResult result = f.ShowDialog(Form.FromHandle(new IntPtr(package.DTE.MainWindow.HWnd)));
            
            if (result==DialogResult.OK) {
                string referenceText;
                bool addNamespace = false;

                if (!f.UsingFullName) {
                    string usedAlias;
                    bool alreadyUsed = IsNamespaceUsed(f.Namespace, out usedAlias);
                    if (alreadyUsed) {
                        referenceText = (usedAlias == string.Empty ? string.Empty : usedAlias + ".") + f.ReferenceText;
                    } else {
                        addNamespace = true;                        
                        referenceText = f.ReferenceText;
                    }
                } else {
                    referenceText = f.ReferenceText;
                }

                int hr = textView.ReplaceTextOnLine(replaceSpan.iStartLine, replaceSpan.iStartIndex,
                replaceSpan.iEndIndex - replaceSpan.iStartIndex, referenceText, referenceText.Length);
                Marshal.ThrowExceptionForHR(hr);

                hr = textView.SetSelection(replaceSpan.iStartLine, replaceSpan.iStartIndex,
                    replaceSpan.iEndLine, replaceSpan.iStartIndex + referenceText.Length);
                Marshal.ThrowExceptionForHR(hr);

                ResXFileHandler.AddString(f.Key, referenceValue, f.SelectedItem);
                
                if (addNamespace)
                    AddUsingBlock(f.Namespace);

                CreateMoveToResourcesUndoUnit(f.Key, referenceValue, f.SelectedItem,addNamespace);
            }
        }

        private void CreateMoveToResourcesUndoUnit(string key,string value, ResXProjectItem resXProjectItem,bool addNamespace) {            
            List<IOleUndoUnit> units = undoManager.RemoveTopFromUndoStack(addNamespace ? 2 : 1);
            MoveToResourcesUndoUnit newUnit = new MoveToResourcesUndoUnit(key, value, resXProjectItem);
            newUnit.AppendUnits.AddRange(units);
            undoManager.Add(newUnit);
        }        
        
        private string GetReferencedValue(string value) {
            if (value.StartsWith("@")) value = value.Substring(1);
            return value.Substring(1, value.Length - 2);
        }                

        private TextSpan GetReplaceSpan() {
            TextSpan[] spans = new TextSpan[1];
            int hr = textView.GetSelectionSpan(spans);
            Marshal.ThrowExceptionForHR(hr);

            TextSpan selectionSpan = spans[0];

            string lineText;
            int lineLength;
            hr = textLines.GetLengthOfLine(selectionSpan.iStartLine, out lineLength);
            Marshal.ThrowExceptionForHR(hr);

            hr = textLines.GetLineText(selectionSpan.iStartLine, 0, selectionSpan.iStartLine, lineLength, out lineText);
            Marshal.ThrowExceptionForHR(hr);

            selectionSpan = TrimSpan(selectionSpan, lineText);
            int spanLength = selectionSpan.iEndIndex - selectionSpan.iStartIndex;

            if (selectionSpan.iStartLine != selectionSpan.iEndLine)
                throw new NotReferencableException(selectionSpan,lineText);

            if (selectionSpan.iStartIndex > 0 && lineText[selectionSpan.iStartIndex - 1] == '@')
                selectionSpan.iStartIndex--;

            int newStartIndex = 0, newEndIndex = 0;
            if (spanLength <= 0) {                
                int rightCount = countAposRight(lineText, selectionSpan.iStartIndex, out newEndIndex);
                newEndIndex++;
                int leftCount = countAposLeft(lineText, selectionSpan.iStartIndex, out newStartIndex);
                
                if (rightCount % 2 == 0 || leftCount%2==0)
                    throw new NotReferencableException(selectionSpan, lineText);
            } else {
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

               
    }
}
