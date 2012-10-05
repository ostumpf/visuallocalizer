using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {
    public sealed class CodeReferenceTrieElement : TrieElement {

        public CodeReferenceTrieElement() {
            Infos = new List<CodeReferenceInfo>();
        }

        public List<CodeReferenceInfo> Infos {
            get;
            private set;
        }

    }

    public class CodeReferenceInfo {
        public string Value { get; set; }
        public ResXProjectItem Origin { get; set; }
    }
}
