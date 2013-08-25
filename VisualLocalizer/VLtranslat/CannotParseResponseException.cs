using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Translate {

    /// <summary>
    /// Throw by ITranslatorService implementing classes when server response does not match expected format.
    /// </summary>
    public class CannotParseResponseException : Exception {

        /// <summary>
        /// HTTP response text from the translation service
        /// </summary>
        public string FullResponse { get; set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="CannotParseResponseException"/> class.
        /// </summary>
        /// <param name="response">Response text from the translation service</param>
        /// <param name="inner">The inner exception</param>
        public CannotParseResponseException(string response, Exception inner) : base(inner.Message, inner) {
            this.FullResponse = response;            
        }
    }
}
