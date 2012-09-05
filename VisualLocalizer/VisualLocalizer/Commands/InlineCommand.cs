using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;
using VisualLocalizer.Components;
using System.Globalization;
using EnvDTE;
using EnvDTE80;
using System.Reflection;
using System.IO;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.OLE.Interop;

namespace VisualLocalizer.Commands {
    internal sealed class InlineCommand : AbstractCommand {          

        public InlineCommand(VisualLocalizerPackage package)
            : base(package) {
        }

        public override void Process() {
            base.Process();

            TextSpan inlineSpan = GetInlineSpan();
            string referenceText = GetTextOfSpan(inlineSpan);
            referenceText = referenceText.RemoveWhitespace();
            CheckIsIdentifier(referenceText);
            
            string key;
            string value = string.Format("\"{0}\"", GetValueFor(inlineSpan, referenceText, currentDocument.ProjectItem.ContainingProject, out key));

            textLines.ReplaceLines(inlineSpan.iStartLine, inlineSpan.iStartIndex, inlineSpan.iEndLine, inlineSpan.iEndIndex,
                Marshal.StringToBSTR(value), value.Length, null);
            
            textView.SetSelection(inlineSpan.iStartLine, inlineSpan.iStartIndex, inlineSpan.iStartLine,
                inlineSpan.iStartIndex + value.Length);

            CreateInlineUndoUnit(key);            
        }

        private void CreateInlineUndoUnit(string key) {
            List<IOleUndoUnit> units = undoManager.RemoveTopFromUndoStack(1);
            InlineUndoUnit newUnit = new InlineUndoUnit(key);
            newUnit.AppendUnits.AddRange(units);
            undoManager.Add(newUnit);
        }

        private string GetCurrentNamespace(TextSpan inlineSpan) {
            object o;
            textLines.CreateTextPoint(inlineSpan.iStartLine, inlineSpan.iStartIndex, out o);
            CodeElement selectionNamespace = currentCodeModel.CodeElementFromPoint(o as TextPoint, vsCMElement.vsCMElementNamespace);
            return selectionNamespace.FullName;
        }

        private void CheckIsIdentifier(string text) {
            string gg = null;
            bool ok = text.Replace(".", "").IsValidIdentifier(ref gg);
            if (!ok)
                throw new NotInlineableException("selection is not valid reference to a key");            
        }

        private string GetValueFor(TextSpan inlineSpan, string referenceText,Project parentProject,out string key) {            
            string value = null;
            List<string> possibleFullNames = GetPossibleFullNames(inlineSpan, referenceText, out key);
            List<ProjectItem> items = parentProject.GetFiles(ResXProjectItem.IsItemResX, true);
            foreach (ProjectItem item in items) {
                ResXProjectItem resxItem = ResXProjectItem.ConvertToResXItem(item, parentProject);
                foreach (string fullName in possibleFullNames) {
                    if (resxItem.Namespace + "." + resxItem.Class==fullName) {
                        value = resxItem.GetString(key);
                        if (value == null) {
                            throw new NotInlineableException(String.Format("{0} does not contain definition for {1}", Path.GetFileName(fullName), key));
                        } else {
                            VLOutputWindow.VisualLocalizerPane.WriteLine("\"{0}\" inlined from \"{1}\"", key, resxItem.InternalProjectItem.Name);    
                        }                        
                        
                        break;
                    }
                }
                if (value != null) break;
            }
            if (value == null)
                throw new NotInlineableException(String.Format("Cannot inline {0}", key));
            return value;
        }

        private List<string> GetPossibleFullNames(TextSpan inlineSpan, string referenceText, out string key) {
            string[] tokens = referenceText.Split(new char[] {'.'}, StringSplitOptions.RemoveEmptyEntries);
            int numberOfDots = tokens.Length - 1;

            string className = null;
            string namespaceName = null;
            if (numberOfDots == 1) {
                key = tokens[1];
                className = tokens[0];
            } else if (numberOfDots > 1) {
                key = tokens[numberOfDots];
                className = tokens[numberOfDots - 1];
                namespaceName = "";
                for (int i = 0; i < numberOfDots - 1; i++)
                    namespaceName += (i > 0 ? "." : "") + tokens[i];
            } else throw new NotInlineableException("selection is not reference to a resource key");
            

            string currentNamespace = GetCurrentNamespace(inlineSpan);
            
            List<string> possibleFullNames = new List<string>();
            possibleFullNames.Add(currentNamespace + "." + className);

            if (namespaceName != null) {
                possibleFullNames.Add(namespaceName + "." + className);
            }
            

            foreach (CodeElement nmspcElement in currentCodeModel.CodeElements)
                if (nmspcElement.Kind == vsCMElement.vsCMElementImportStmt) {
                    string usingAlias, usingNmsName;
                    ParseUsing(nmspcElement.StartPoint, nmspcElement.EndPoint, out usingNmsName, out usingAlias);

                    if (usingAlias != string.Empty && namespaceName==usingAlias) {
                        possibleFullNames.Clear();
                        possibleFullNames.Add(usingNmsName + "." + className);
                        break;
                    } else if (usingAlias == string.Empty) {
                        possibleFullNames.Add(usingNmsName + "." + className);
                    }
                }
            return possibleFullNames;
        }

        private TextSpan GetInlineSpan() {
            TextSpan[] spans = new TextSpan[1];
            int hr = textView.GetSelectionSpan(spans);
            Marshal.ThrowExceptionForHR(hr);

            TextSpan selectionSpan = spans[0];            
            int spanLength = selectionSpan.iEndIndex - selectionSpan.iStartIndex + selectionSpan.iEndLine - selectionSpan.iStartLine;
            
            string selectionText;
            int endLineLength;
            hr = textLines.GetLengthOfLine(selectionSpan.iEndLine, out endLineLength);
            Marshal.ThrowExceptionForHR(hr);

            hr = textLines.GetLineText(selectionSpan.iStartLine, 0, selectionSpan.iEndLine, endLineLength, out selectionText);
            Marshal.ThrowExceptionForHR(hr);

            int beginLine = 0, beginIndex = 0, endLine = 0, endIndex = 0;
            int sum = 0;
            for (int i = selectionSpan.iStartLine; i < selectionSpan.iEndLine; i++) {
                int pom;
                textLines.GetLengthOfLine(i, out pom);
                sum += pom;
            }

            int t;
            int rightCount = countAposRight(selectionText, sum+selectionSpan.iEndIndex, out t);
            int leftCount = countAposLeft(selectionText, selectionSpan.iStartIndex, out t);

            if (rightCount % 2 != 0 || leftCount % 2 != 0) {
                throw new NotInlineableException("cannot inline string literal");
            } else {
                GetIdentifierStart(selectionSpan.iStartLine, selectionSpan.iStartIndex - 1, -1, out beginLine, out beginIndex);
                GetIdentifierStart(selectionSpan.iEndLine, selectionSpan.iEndIndex, 1, out endLine, out endIndex);
                beginIndex++;
            }
          

            TextSpan returnSpan = new TextSpan();
            returnSpan.iStartLine = beginLine;
            returnSpan.iStartIndex = beginIndex;
            returnSpan.iEndLine = endLine;
            returnSpan.iEndIndex = endIndex;            

            return returnSpan;
        }

        private void GetIdentifierStart(int startLine, int startIndex, int step, out int iline, out int iindex) {                        
            int currentIndex=startIndex;
            int currentLine=startLine;
            bool foundStart = false;
            bool eol = false;
            int lineCount;
            textLines.GetLineCount(out lineCount);

            while (!foundStart) {                
                string lineText;
                int length;
                if (currentLine >= lineCount || currentLine < 0)
                    throw new NotInlineableException("end of identifier cannot be found");

                textLines.GetLengthOfLine(currentLine, out length);
                textLines.GetLineText(currentLine, 0, currentLine, length, out lineText);
                if (eol) {
                    currentIndex = step == 1 ? 0 : length - 1;
                }
                eol = false;

                if ((currentIndex >= length && step==1) || (currentIndex < 0 && step==-1) || length==0) {
                    eol = true;
                    currentLine += step;                    
                } else {
                    if (currentIndex >= length) currentIndex = length - 1;
                    if (currentIndex < 0) currentIndex = 0;

                    while (lineText[currentIndex] == '.' || char.IsWhiteSpace(lineText[currentIndex]) || lineText[currentIndex].CanBePartOfIdentifier()) {
                        currentIndex += step;
                        if (currentIndex >= length || currentIndex < 0) {
                            eol = true;
                            currentLine += step;
                            break;
                        }
                    }
                }
                if (!eol) {
                    foundStart = true;
                }
            }

            iline = currentLine;
            iindex = currentIndex;
        }
               
    }
}
