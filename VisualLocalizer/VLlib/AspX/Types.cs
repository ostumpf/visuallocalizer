using Microsoft.VisualStudio.TextManager.Interop;
using System.Collections.Generic;
using System;

namespace VisualLocalizer.Library.AspX {

    /// <summary>
    /// Specifies type of ASP .NET output elements
    /// </summary>
    public enum OutputElementKind { 
        /// <summary>
        /// Blocks starting with &lt;%=  
        /// </summary>
        PLAIN,

        /// <summary>
        /// Blocks starting with &lt;%:  
        /// </summary>
        HTML_ESCAPED,

        /// <summary>
        /// Blocks starting with &lt;%$  
        /// </summary>
        EXPRESSION,

        /// <summary>
        /// Blocks starting with &lt;%#  
        /// </summary>
        BIND
    }

    /// <summary>
    /// Represents position of a block in a document
    /// </summary>
    public class BlockSpan {
        /// <summary>
        /// Number of the line where the span begins
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// Number of the column where the span begins
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// Number of the line where the span ends
        /// </summary>
        public int EndLine { get; set; }

        /// <summary>
        /// Number of the column where the span ends
        /// </summary>
        public int EndIndex { get; set; }

        /// <summary>
        /// Absolute number of characters from the beginning of the text to the beginnning of this block
        /// </summary>
        public int AbsoluteCharOffset { get; set; }

        /// <summary>
        /// Absolute length (number of the characters this span contains)
        /// </summary>
        public int AbsoluteCharLength { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockSpan"/> class.
        /// </summary>
        public BlockSpan() {
        }

        /// <summary>
        /// Copy constructor
        /// </summary>        
        public BlockSpan(BlockSpan copy) {
            if (copy == null) throw new ArgumentNullException("copy");

            AbsoluteCharLength = copy.AbsoluteCharLength;
            AbsoluteCharOffset = copy.AbsoluteCharOffset;
            StartLine = copy.StartLine;
            EndLine = copy.EndLine;
            EndIndex = copy.EndIndex;
            StartIndex = copy.StartIndex;
        }

        /// <summary>
        /// Moves block by specified offset
        /// </summary>        
        public void Move(int dx, int dy) {
            StartLine += dy;
            StartIndex += dx;
            EndLine += dy;
            EndIndex += dx;
        }

        /// <summary>
        /// Returns true when specified block is contained within this one
        /// </summary>        
        public bool Contains(BlockSpan childBlock) {
            if (childBlock == null) throw new ArgumentNullException("childBlock");

            return (childBlock.AbsoluteCharOffset >= AbsoluteCharOffset) && 
                (childBlock.AbsoluteCharOffset + childBlock.AbsoluteCharLength <= AbsoluteCharOffset + AbsoluteCharLength);
        }

        /// <summary>
        /// Returns this block in a TextSpan format
        /// </summary>        
        public TextSpan GetTextSpan() {
            TextSpan ts = new TextSpan();
            ts.iStartIndex = StartIndex;
            ts.iStartLine = StartLine;
            ts.iEndIndex = EndIndex;
            ts.iEndLine = EndLine;
            return ts;
        }
    }

    /// <summary>
    /// Information about element attribute
    /// </summary>
    public class AttributeInfo {
        /// <summary>
        /// Position of the attribute
        /// </summary>
        public BlockSpan BlockSpan { get; set; }

        /// <summary>
        /// Name of the attribute
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Value of the attribute
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// True if ASP tags are present with the attribute value
        /// </summary>
        public bool ContainsAspTags { get; set; }
    }

    /// <summary>
    /// Information about block of code
    /// </summary>
    public class CodeBlockContext {
        /// <summary>
        /// Literal content of the code block
        /// </summary>
        public string BlockText { get; set; }

        /// <summary>
        /// Position of the inner content (not counting ASP tags)
        /// </summary>
        public BlockSpan InnerBlockSpan { get; set; }

        /// <summary>
        /// Position of the whole block (including ASP tags)
        /// </summary>
        public BlockSpan OuterBlockSpan { get; set; }

        /// <summary>
        /// True if the code block is commented out using client-side comment
        /// </summary>
        public bool WithinClientSideComment { get; set; }
    }

    /// <summary>
    /// Information about directive
    /// </summary>
    public class DirectiveContext {
        /// <summary>
        /// The name of the directive
        /// </summary>
        public string DirectiveName { get; set; }

        /// <summary>
        /// List of attributes of the directive
        /// </summary>
        public List<AttributeInfo> Attributes { get; set; } 

        /// <summary>
        /// Position of the directive
        /// </summary>
        public BlockSpan BlockSpan { get; set; }

        /// <summary>
        /// True if the directive is commented out using client-side comment
        /// </summary>
        public bool WithinClientSideComment { get; set; }
    }

    /// <summary>
    /// Information about inline output element
    /// </summary>
    public class OutputElementContext {
        /// <summary>
        /// Kind of the output element
        /// </summary>
        public OutputElementKind Kind { get; set; }

        /// <summary>
        /// Content of the output element
        /// </summary>
        public string InnerText { get; set; }

        /// <summary>
        /// Position of the inner content (not counting ASP tags)
        /// </summary>
        public BlockSpan InnerBlockSpan { get; set; }

        /// <summary>
        /// Position of the whole element (including ASP tags)
        /// </summary>
        public BlockSpan OuterBlockSpan { get; set; } 

        /// <summary>
        /// True if the element is commented out using client-side comment
        /// </summary>
        public bool WithinClientSideComment { get; set; }

        /// <summary>
        /// True if the element is located within an attribute's value
        /// </summary>
        public bool WithinElementsAttribute { get; set; }
    }

    /// <summary>
    /// Information about beginnig element tag
    /// </summary>
    public class ElementContext {
        /// <summary>
        /// Prefix of the element name
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Name of the element
        /// </summary>
        public string ElementName { get; set; }

        /// <summary>
        /// List of the attribute's elements
        /// </summary>
        public List<AttributeInfo> Attributes { get; set; }

        /// <summary>
        /// Position of the element
        /// </summary>
        public BlockSpan BlockSpan { get; set; }

        /// <summary>
        /// True if the element is commented out using client-side comment
        /// </summary>
        public bool WithinClientSideComment { get; set; }

        /// <summary>
        /// True if the element includes the end tag (&lt;br/>)
        /// </summary>
        public bool IsEnd { get; set; }
    }

    /// <summary>
    /// Information about end element tag
    /// </summary>
    public class EndElementContext {
        /// <summary>
        /// Prefix of the element name
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Name of the element
        /// </summary>
        public string ElementName { get; set; }

        /// <summary>
        /// Position of the element
        /// </summary>
        public BlockSpan BlockSpan { get; set; }

        /// <summary>
        /// True if the element is commented out using client-side comment
        /// </summary>
        public bool WithinClientSideComment { get; set; }
    }

    /// <summary>
    /// Information about plain text between elements
    /// </summary>
    public class PlainTextContext {
        /// <summary>
        /// The literal text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Position of the plain text
        /// </summary>
        public BlockSpan BlockSpan { get; set; }

        /// <summary>
        /// True if the element is commented out using client-side comment
        /// </summary>
        public bool WithinClientSideComment { get; set; }
    }
}