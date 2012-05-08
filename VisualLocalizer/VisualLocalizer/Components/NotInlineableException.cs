using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace VisualLocalizer.Components {
    internal sealed class NotInlineableException : Exception {
        public NotInlineableException(TextSpan span, string line,string reason)
            : base(string.Format("This selection cannot be inlined: {0}.", reason)) {
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
