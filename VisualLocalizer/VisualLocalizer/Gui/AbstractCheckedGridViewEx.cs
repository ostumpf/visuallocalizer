using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Windows.Forms;

namespace VisualLocalizer.Gui {
    internal static class AbstractCheckedGridViewEx {
        public static void SetItemFinished<T>(this DataGridViewRowCollection rows, int index, int newLength) where T : AbstractResultItem {
            AbstractResultItem resultItem = (rows[index] as DataGridViewCheckedRow<T>).DataSourceItem;
            TextSpan currentReplaceSpan = resultItem.ReplaceSpan;

            int diff = currentReplaceSpan.iEndLine - currentReplaceSpan.iStartLine;
            for (int i = index - 1; i >= 0; i--) {
                AbstractResultItem item = (rows[i] as DataGridViewCheckedRow<T>).DataSourceItem;
                if (item.AbsoluteCharOffset < resultItem.AbsoluteCharOffset) continue;

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
