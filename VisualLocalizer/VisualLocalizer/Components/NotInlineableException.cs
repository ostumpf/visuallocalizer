using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace VisualLocalizer.Components {
    internal sealed class NotInlineableException : Exception {
        public NotInlineableException(string reason)
            : base(string.Format("This selection cannot be inlined: {0}.", reason)) {           
        }
      
    }
}
