using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Library.AspxParser {

    public interface IAspxHandler {
        bool StopRequested { get; }
        void OnCodeBlock(CodeBlockContext context);
        void OnPageDirective(DirectiveContext context);        
        void OnOutputElement(OutputElementContext context);
        void OnElementBegin(ElementContext context);        
        void OnElementEnd(EndElementContext context);
        void OnPlainText(PlainTextContext context);
    }
}
