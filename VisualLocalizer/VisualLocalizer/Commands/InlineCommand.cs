using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;
using VisualLocalizer.Components;
using System.Globalization;

namespace VisualLocalizer.Commands {
    internal sealed class InlineCommand : AbstractCommand {

        private UnicodeCategory[] validIdentifierCategories = {UnicodeCategory.TitlecaseLetter,
                                                     UnicodeCategory.UppercaseLetter,
                                                     UnicodeCategory.LowercaseLetter,
                                                     UnicodeCategory.ModifierLetter,
                                                     UnicodeCategory.OtherLetter,
                                                     UnicodeCategory.LetterNumber,
                                                     UnicodeCategory.NonSpacingMark,
                                                     UnicodeCategory.SpacingCombiningMark,
                                                     UnicodeCategory.DecimalDigitNumber,
                                                     UnicodeCategory.ConnectorPunctuation,
                                                     UnicodeCategory.Format
                                                    };

        public InlineCommand(VisualLocalizerPackage package)
            : base(package) {
        }

        public override void Process() {
            TextSpan inlineSpan = GetInlineSpan();
            string referenceText = GetTextOfSpan(inlineSpan);
            referenceText = Utils.RemoveWhitespace(referenceText);

            string gg=null;
            bool ok = Utils.IsValidIdentifier(referenceText.Replace(".", ""),ref gg);
            if (!ok)
                throw new NotInlineableException(inlineSpan, null, "Selection is not reference to a key");



            VLOutputWindow.VisualLocalizerPane.WriteLine(referenceText);
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
                throw new NotInlineableException(selectionSpan, selectionText, "cannot inline string literal");
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
                    throw new NotInlineableException(default(TextSpan), string.Empty, "end of identifier cannot be found");

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

                    while (lineText[currentIndex] == '.' || char.IsWhiteSpace(lineText[currentIndex]) || isIdentifierChar(lineText[currentIndex])) {
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

        
        private bool isIdentifierChar(char p) {
            UnicodeCategory charCat=char.GetUnicodeCategory(p);
            foreach (UnicodeCategory c in validIdentifierCategories)
                if (c == charCat) return true;
            return false;
        }
    }
}
