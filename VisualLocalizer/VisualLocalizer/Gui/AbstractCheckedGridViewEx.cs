using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Windows.Forms;
using System.Collections;

namespace VisualLocalizer.Gui {
    internal static class AbstractCheckedGridViewEx {
        public static void SetItemFinished<T>(IList rows,Func<IList, int, T> itemGetter, int index, int newLength) where T : AbstractResultItem {
            T resultItem = itemGetter(rows, index); 
            TextSpan currentReplaceSpan = resultItem.ReplaceSpan;

            int diff = currentReplaceSpan.iEndLine - currentReplaceSpan.iStartLine;
            for (int i = index - 1; i >= 0; i--) {
                T item = itemGetter(rows, i); 
                if (item.AbsoluteCharOffset < resultItem.AbsoluteCharOffset) continue;
                if (item.SourceItem != resultItem.SourceItem) continue;

                item.AbsoluteCharOffset += newLength - resultItem.AbsoluteCharLength;

                if (item.ReplaceSpan.iStartLine > currentReplaceSpan.iEndLine) {
                    TextSpan newSpan = new TextSpan();
                    newSpan.iEndIndex = item.ReplaceSpan.iEndIndex;
                    newSpan.iStartIndex = item.ReplaceSpan.iStartIndex;
                    newSpan.iEndLine = item.ReplaceSpan.iEndLine - diff;
                    newSpan.iStartLine = item.ReplaceSpan.iStartLine - diff;
                    item.ReplaceSpan = newSpan;
                } else if (item.ReplaceSpan.iStartLine == currentReplaceSpan.iEndLine) {
                    TextSpan newSpan = new TextSpan();
                    newSpan.iStartIndex = currentReplaceSpan.iStartIndex + newLength + item.ReplaceSpan.iStartIndex - currentReplaceSpan.iEndIndex;
                    if (item.ReplaceSpan.iEndLine == item.ReplaceSpan.iStartLine) {
                        newSpan.iEndIndex = newSpan.iStartIndex + item.ReplaceSpan.iEndIndex - item.ReplaceSpan.iStartIndex;
                    } else {
                        newSpan.iEndIndex = item.ReplaceSpan.iEndIndex;
                    }
                    newSpan.iEndLine = item.ReplaceSpan.iEndLine - diff;
                    newSpan.iStartLine = item.ReplaceSpan.iStartLine - diff;
                    item.ReplaceSpan = newSpan;
                }
            }
        }
    }
}
