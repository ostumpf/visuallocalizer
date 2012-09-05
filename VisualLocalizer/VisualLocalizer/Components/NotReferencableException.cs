using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace VisualLocalizer.Components {
    internal sealed class NotReferencableException : Exception {
        public NotReferencableException(string reason)
            : base(string.Format("This selection cannot be referenced: {0}.",reason)) {            
        }     
    }
}
