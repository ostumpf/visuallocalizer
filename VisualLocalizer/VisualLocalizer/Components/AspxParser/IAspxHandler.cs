using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Components.AspxParser {

    public interface IAspxHandler {
        void OnCodeBlock(CodeBlockContext context);
        void OnPageDirective(DirectiveContext context);        
        void OnOutputElement(OutputElementContext context);
        void OnElementBegin(ElementContext context);        
        void OnElementEnd(EndElementContext context);
        void OnPlainText(PlainTextContext context);
    }
}
