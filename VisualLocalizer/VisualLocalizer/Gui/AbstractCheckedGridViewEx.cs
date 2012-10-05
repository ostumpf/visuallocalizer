using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.TextManager.Interop;

namespace VisualLocalizer.Gui {
    internal static class AbstractCheckedGridViewEx {
      /*  public static void SetItemFinished(AbstractCheckedGridView<AbstractResultItem> view, int index, bool ok, int newLength) {
            if (ok) {
                AbstractResultItem resultItem = (Rows[index] as CodeDataGridViewRow<AbstractResultItem>).DataSourceItem;
                TextSpan currentReplaceSpan = resultItem.ReplaceSpan;

                int diff = currentReplaceSpan.iEndLine - currentReplaceSpan.iStartLine;
                for (int i = index + 1; i < Rows.Count; i++) {
                    AbstractResultItem item = (Rows[i] as CodeDataGridViewRow<AbstractResultItem>).DataSourceItem;
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

                Rows.RemoveAt(index);
                CheckedRowsCount--;
                UpdateCheckHeader();
            }
        }*/
    }
}
