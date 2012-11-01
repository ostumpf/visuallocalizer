using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Translate {
    public class CannotParseResponseException : Exception {

        public string FullResponse { get; set; }

        public CannotParseResponseException(string response, Exception inner) : base(inner.Message, inner) {
            this.FullResponse = response;            
        }
    }
}
