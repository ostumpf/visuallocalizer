using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace VisualLocalizer.Library {
    public class DataGridViewDynamicWrapCell : DataGridViewTextBoxCell {
        public int RelativeLine { get; set; }

        private string _FullText;
        private string[] FullTextLines;

        public string FullText {
            get {
                return _FullText;
            }
            set {
                _FullText = value;
                FullTextLines = _FullText.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            }
        }

        public void SetWrapContents(bool wrap) {
            if (!wrap) {                                
                Value = FullTextLines[RelativeLine];
                Style.WrapMode = DataGridViewTriState.False;
            } else {
                Value = FullText;
                Style.WrapMode = DataGridViewTriState.True;
            }
        }
    }

}
