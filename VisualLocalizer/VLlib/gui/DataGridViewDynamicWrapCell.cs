using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace VisualLocalizer.Library {

    /// <summary>
    /// Enhances DataGridViewTextBoxCell with functionality enabling to shrink/expand content by lines
    /// </summary>
    public class DataGridViewDynamicWrapCell : DataGridViewTextBoxCell {        
        private string _FullText;
        private string[] FullTextLines;

        /// <summary>
        /// Content of the cell (lines, separated by Environment.NewLine)
        /// </summary>
        public string FullText {
            get {
                return _FullText;
            }
            set {
                _FullText = value;
                if (value!=null) FullTextLines = _FullText.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            }
        }

        /// <summary>
        /// Index of the line displayed in the cell when content is shrunk
        /// </summary>
        public int RelativeLine { get; set; }

        /// <summary>
        /// Switches display style from shrunk/expanded
        /// </summary>
        /// <param name="wrap">True to display full text, false to display one line</param>
        public void SetWrapContents(bool wrap) {
            if (FullTextLines == null) return;

            if (RelativeLine >= FullTextLines.Length || RelativeLine < 0)
                throw new InvalidOperationException("Insufficiently initialized DataGridViewDynamicWrapCell.");

            if (!wrap) {                                
                Value = FullTextLines[RelativeLine].Trim();
                Style.WrapMode = DataGridViewTriState.False;
            } else {
                Value = FullText;
                Style.WrapMode = DataGridViewTriState.True;
            }
        }
    }

}
