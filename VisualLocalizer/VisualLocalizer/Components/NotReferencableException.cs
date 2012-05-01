using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace VisualLocalizer.Editor {
    internal sealed class NotReferencableException : Exception {
        public NotReferencableException(TextSpan span,string line)
            : base("This selection cannot be referenced (integrity violation).") {
            this.Text = line;
            this.Span = span;
        }

        public string Text {
            get;
            private set;
        }

        public TextSpan Span {
            get;
            private set;
        }
    }
}
