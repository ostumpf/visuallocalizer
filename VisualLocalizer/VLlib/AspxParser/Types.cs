using Microsoft.VisualStudio.TextManager.Interop;
using System.Collections.Generic;
using System;

namespace VisualLocalizer.Library.AspxParser {

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
        public int StartLine { get; set; }
        public int StartIndex { get; set; }
        public int EndLine { get; set; }
        public int EndIndex { get; set; }
        public int AbsoluteCharOffset { get; set; }
        public int AbsoluteCharLength { get; set; }

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
        public BlockSpan BlockSpan { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsMarkedWithUnlocalizableComment { get; set; }
        public bool ContainsAspTags { get; set; }
    }

    /// <summary>
    /// Information about block of code
    /// </summary>
    public class CodeBlockContext {
        public string BlockText { get; set; }
        public BlockSpan InnerBlockSpan { get; set; }
        public BlockSpan OuterBlockSpan { get; set; }
        public bool WithinClientSideComment { get; set; }
    }

    /// <summary>
    /// Information about directive
    /// </summary>
    public class DirectiveContext {
        public string DirectiveName { get; set; }
        public List<AttributeInfo> Attributes { get; set; } 
        public BlockSpan BlockSpan { get; set; }
        public bool WithinClientSideComment { get; set; }
    }

    /// <summary>
    /// Information about inline output element
    /// </summary>
    public class OutputElementContext {
        public OutputElementKind Kind { get; set; }
        public string InnerText { get; set; }
        public BlockSpan InnerBlockSpan { get; set; }
        public BlockSpan OuterBlockSpan { get; set; } 
        public bool WithinClientSideComment { get; set; }
        public bool WithinElementsAttribute { get; set; }
    }

    /// <summary>
    /// Information about beginnig element tag
    /// </summary>
    public class ElementContext {
        public string Prefix { get; set; }
        public string ElementName { get; set; }
        public List<AttributeInfo> Attributes { get; set; }
        public BlockSpan BlockSpan { get; set; }
        public bool WithinClientSideComment { get; set; }
        public bool IsEnd { get; set; }
    }

    /// <summary>
    /// Information about end element tag
    /// </summary>
    public class EndElementContext {
        public string Prefix { get; set; }
        public string ElementName { get; set; }
        public BlockSpan BlockSpan { get; set; }
        public bool WithinClientSideComment { get; set; }
    }

    /// <summary>
    /// Information about plain text between elements
    /// </summary>
    public class PlainTextContext {
        public string Text { get; set; } 
        public BlockSpan BlockSpan { get; set; }
        public bool WithinClientSideComment { get; set; }
    }
}