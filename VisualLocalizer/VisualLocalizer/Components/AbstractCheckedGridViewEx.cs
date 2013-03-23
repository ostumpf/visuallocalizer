using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Windows.Forms;
using System.Collections;

namespace VisualLocalizer.Components {

    /// <summary>
    /// Helper class
    /// </summary>
    internal static class AbstractCheckedGridViewEx {

        /// <summary>
        /// Moves position of unprocessed result items 
        /// </summary>
        /// <typeparam name="T">Type of the result item</typeparam>
        /// <param name="rows">List of rows</param>
        /// <param name="itemGetter">Function to get result item from specified row</param>
        /// <param name="index">Index of result item that was changed</param>
        /// <param name="newLength">New length of the result item</param>
        public static void SetItemFinished<T>(IList rows,Func<IList, int, T> itemGetter, int index, int newLength) where T : AbstractResultItem {
            if (rows == null) throw new ArgumentNullException("rows");
            if (itemGetter == null) throw new ArgumentNullException("itemGetter");
            if (index < 0 || index >= rows.Count) throw new IndexOutOfRangeException();

            T resultItem = itemGetter(rows, index); // get modified result item
            TextSpan currentReplaceSpan = resultItem.ReplaceSpan;

            int lineDiff = currentReplaceSpan.iEndLine - currentReplaceSpan.iStartLine; // number of lines current result items spans
            for (int i = index - 1; i >= 0; i--) { // for all unprocessed result items (with lower index, processing runs from the last row to the first)
                T item = itemGetter(rows, i); // item to be moved
                if (item.AbsoluteCharOffset < resultItem.AbsoluteCharOffset) continue; // item lies above the modified item - it is not affected by the change
                if (item.SourceItem != resultItem.SourceItem) continue; // item comes from different file

                item.AbsoluteCharOffset += newLength - resultItem.AbsoluteCharLength; // modify item's absolute position

                if (item.ReplaceSpan.iStartLine > currentReplaceSpan.iEndLine) { // item lies below the modified item - modify position
                    TextSpan newSpan = new TextSpan();
                    newSpan.iEndIndex = item.ReplaceSpan.iEndIndex;
                    newSpan.iStartIndex = item.ReplaceSpan.iStartIndex;
                    newSpan.iEndLine = item.ReplaceSpan.iEndLine - lineDiff;
                    newSpan.iStartLine = item.ReplaceSpan.iStartLine - lineDiff;
                    item.ReplaceSpan = newSpan;
                } else if (item.ReplaceSpan.iStartLine == currentReplaceSpan.iEndLine) { // item lies on the same line
                    TextSpan newSpan = new TextSpan();
                    newSpan.iStartIndex = currentReplaceSpan.iStartIndex + newLength + item.ReplaceSpan.iStartIndex - currentReplaceSpan.iEndIndex;
                    if (item.ReplaceSpan.iEndLine == item.ReplaceSpan.iStartLine) {
                        newSpan.iEndIndex = newSpan.iStartIndex + item.ReplaceSpan.iEndIndex - item.ReplaceSpan.iStartIndex;
                    } else {
                        newSpan.iEndIndex = item.ReplaceSpan.iEndIndex;
                    }
                    newSpan.iEndLine = item.ReplaceSpan.iEndLine - lineDiff;
                    newSpan.iStartLine = item.ReplaceSpan.iStartLine - lineDiff;
                    item.ReplaceSpan = newSpan;
                }
            }
        }
    }
}
