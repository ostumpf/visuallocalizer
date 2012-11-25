using Microsoft.VisualStudio.TextManager.Interop;
using System.Collections.Generic;
namespace VisualLocalizer.Components.AspxParser {
    public enum OutputElementKind { PLAIN, HTML_ESCAPED, EXPRESSION }

    public class BlockSpan {
        public int StartLine { get; set; }
        public int StartIndex { get; set; }
        public int EndLine { get; set; }
        public int EndIndex { get; set; }
        public int AbsoluteCharOffset { get; set; }
        public int AbsoluteCharLength { get; set; }

        public BlockSpan() {
        }

        public BlockSpan(BlockSpan copy) {
            AbsoluteCharLength = copy.AbsoluteCharLength;
            AbsoluteCharOffset = copy.AbsoluteCharOffset;
            StartLine = copy.StartLine;
            EndLine = copy.EndLine;
            EndIndex = copy.EndIndex;
            StartIndex = copy.StartIndex;
        }

        public void Move(int dx, int dy) {
            StartLine += dy;
            StartIndex += dx;
            EndLine += dy;
            EndIndex += dx;
        }

        public bool Contains(BlockSpan b) {
            return (b.AbsoluteCharOffset >= AbsoluteCharOffset) && 
                (b.AbsoluteCharOffset + b.AbsoluteCharLength <= AbsoluteCharOffset + AbsoluteCharLength);
        }

        public TextSpan GetTextSpan() {
            TextSpan ts = new TextSpan();
            ts.iStartIndex = StartIndex;
            ts.iStartLine = StartLine;
            ts.iEndIndex = EndIndex;
            ts.iEndLine = EndLine;
            return ts;
        }
    }

    public class AttributeInfo {
        public BlockSpan BlockSpan { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsMarkedWithUnlocalizableComment { get; set; }
        public bool ContainsAspTags { get; set; }
    }

    public class CodeBlockContext {
        public string BlockText { get; set; }
        public BlockSpan InnerBlockSpan { get; set; }
        public BlockSpan OuterBlockSpan { get; set; }
        public bool WithinClientSideComment { get; set; }
    }

    public class DirectiveContext {
        public string DirectiveName { get; set; }
        public List<AttributeInfo> Attributes { get; set; } 
        public BlockSpan BlockSpan { get; set; }
        public bool WithinClientSideComment { get; set; }
    }

    public class OutputElementContext {
        public OutputElementKind Kind { get; set; }
        public string InnerText { get; set; }
        public BlockSpan InnerBlockSpan { get; set; }
        public BlockSpan OuterBlockSpan { get; set; } 
        public bool WithinClientSideComment { get; set; }
        public bool WithinElementsAttribute { get; set; }
    }

    public class ElementContext {
        public string Prefix { get; set; }
        public string ElementName { get; set; }
        public List<AttributeInfo> Attributes { get; set; }
        public BlockSpan BlockSpan { get; set; }
        public bool WithinClientSideComment { get; set; }
    }

    public class EndElementContext {
        public string Prefix { get; set; }
        public string ElementName { get; set; }
        public BlockSpan BlockSpan { get; set; }
        public bool WithinClientSideComment { get; set; }
    }

    public class PlainTextContext {
        public string Text { get; set; } 
        public BlockSpan BlockSpan { get; set; }
        public bool WithinClientSideComment { get; set; }
    }
}