using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Library.AspxParser {
    
    /// <summary>
    /// Interface used to inform its implementation about parsed elements.
    /// </summary>
    public interface IAspxHandler {
        /// <summary>
        /// Should return true, when parsing should be stopped. Currently processed block/element is first finished,
        /// after that parser exits.
        /// </summary>
        bool StopRequested { get; } 

        /// <summary>
        /// Called after code block <% %>
        /// </summary>        
        void OnCodeBlock(CodeBlockContext context);

        /// <summary>
        /// Called after page directive <%@ %>
        /// </summary>        
        void OnPageDirective(DirectiveContext context);        

        /// <summary>
        /// Called after output element <%= %>, <%$ %> or <%: %>
        /// </summary>        
        void OnOutputElement(OutputElementContext context);

        /// <summary>
        /// Called after beginnnig tag is read
        /// </summary>        
        void OnElementBegin(ElementContext context);        

        /// <summary>
        /// Called after end tag is read
        /// </summary>        
        void OnElementEnd(EndElementContext context);

        /// <summary>
        /// Called after plain text (between elements) is read
        /// </summary>        
        void OnPlainText(PlainTextContext context);
    }
}
