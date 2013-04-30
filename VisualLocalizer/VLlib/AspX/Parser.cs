using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using VisualLocalizer.Library.Extensions;

namespace VisualLocalizer.Library.AspX {
    
    /// <summary>
    /// Parser of ASPX-like documents. Inspired by SAX technology, it requires IAspxHandler implementing object to be passed
    /// as handler of document objects. The parser reads the document from top and uses IAspxHandler methods to inform handler
    /// about content.
    /// </summary>
    public class Parser {

        private string text;
        private IAspxHandler handler;

        private int currentLine, currentIndex, currentOffset, maxLine, maxIndex, plaintTextStartCorrection;
        private char currentChar, quotesChar, lastNonWhitespaceChar;
        private bool withinAspElement, withinAspDirective, withinCodeBlock, withinOutputElement, withinAspTags, attributeValueContainsOutput,
            withinServerComment, withinClientComment, withinEndAspElement, withinAttributeName, withinAttributeValue, withinPlainText,
            hardStop, softStop, withinScriptBlock, scriptBeginTagContainedRunatServer;
        private OutputElementKind outputElementKind;
        private StringBuilder codeBuilder, backupBuilder, plainTextBuilder, attributeNameBuilder, attributeValueBuilder;
        private List<AttributeInfo> attributes;
        private string elementName, elementPrefix;
        private BlockSpan currentAttributeBlockSpan, externalSpan, internalSpan, backupSpan, plainTextSpan;

        /// <summary>
        /// Constructs new parser object
        /// </summary>
        /// <param name="text">Text to be parsed</param>
        /// <param name="handler">Handler object that gets informed about parsed content</param>
        /// <param name="maxLine">Maximal line number, after which the parser should stop</param>
        /// <param name="maxIndex">Maximal column number, after which the parser should stop</param>
        public Parser(string text, IAspxHandler handler, int maxLine, int maxIndex) {
            if (text == null) throw new ArgumentNullException("text");
            if (handler == null) throw new ArgumentNullException("handler");

            this.text = text;
            this.handler = handler;
            this.maxIndex = maxIndex;
            this.maxLine = maxLine;
        }

        /// <summary>
        /// Constructs new parser object with no limitations
        /// </summary>
        /// <param name="text">Text to be parsed</param>
        /// <param name="handler">Handler object that gets informed about parsed content</param>
        public Parser(string text, IAspxHandler handler) : this(text,handler, int.MaxValue, int.MaxValue) { }

        /// <summary>
        /// Parse given content using given handler
        /// </summary>
        public void Process() {
            currentLine = 0;
            currentIndex = 0;
            currentOffset = 0;
            currentChar = '?';
            codeBuilder = new StringBuilder(); // builder for C# or VB code
            backupBuilder = new StringBuilder();
            plainTextBuilder = new StringBuilder(); // builder for plain text
            attributeNameBuilder = new StringBuilder(); // builder for elements' attributes names
            attributeValueBuilder = new StringBuilder();// builder for elements' attributes values
            lastNonWhitespaceChar = '?';

            bool justEnteredAspTags = false;
            softStop = false; // stop is requested, but wait for current block to finish first
            hardStop = false; // stop will be performed right away

            for (int i = 0; i < text.Length; i++) {
                if (hardStop) break;

                currentChar = text[i];
                if (currentLine > maxLine || (currentLine == maxLine && currentIndex > maxIndex)) softStop = true; // outside scope
                if (handler.StopRequested) softStop = true; // stop requested by handler

                if (!withinServerComment) {
                    if (withinOutputElement || withinCodeBlock) {
                        codeBuilder.Append(currentChar);
                    } else if (!withinAspElement) {
                        if (withinPlainText) {
                            plainTextBuilder.Append(currentChar);
                        }
                    }
                }

                if (!withinServerComment && withinAspElement) {
                    ReactToWithinElementChar(); // read attribute name, value or element prefix
                }

                if (!withinServerComment && justEnteredAspTags && !char.IsWhiteSpace(currentChar)) {
                    ReactToBeginningOfAspTags(); // determine what kind of content it is - output element, code block...
                    justEnteredAspTags = false;
                }

                if (withinAspDirective && !withinOutputElement && currentChar == '>' && GetCodeBack(1) == '%') {
                    withinAspTags = true;
                }

                if (!withinAspTags) {                    
                    if (currentChar == '%' && GetCodeBack(1) == '<') {
                        EndPlainText(-1,2); // report plain text
                        if (GetCodeForth(1) == '-' && GetCodeForth(2) == '-') {
                            if (!withinAttributeValue) {
                                withinServerComment = true;
                                withinAspTags = true;
                            }
                        } else {
                            withinAspTags = true;
                            justEnteredAspTags = true;
                            withinCodeBlock = true;

                            HitStart(ref externalSpan, -1);
                            HitStart(ref internalSpan, 1);
                        }
                    }
                    if (!withinCodeBlock && !withinAspElement) {
                        CheckClientCommentState();
                    }
                } else {
                    if (currentChar == '>' && GetCodeBack(1) == '%') {
                        StartPlainText(1, 0);

                        if (!withinServerComment) {
                            withinAspTags = false;
                            justEnteredAspTags = false;
                            
                            HitEnd(ref externalSpan, 1);
                            if (internalSpan != null) HitEnd(ref internalSpan, -2);

                            ReactToEndOfAspTags(); // report content to the handler
                        } else if (GetCodeBack(2) == '-' && GetCodeBack(3) == '-') {
                            currentChar = '?';
                            withinServerComment = false;
                            withinAspTags = false;
                        }
                    }
                }
                
                if (!withinServerComment) {
                    HandleAspElements();
                }

                if (!char.IsWhiteSpace(currentChar)) lastNonWhitespaceChar = currentChar;
                Move();
            }
        }

        /// <summary>
        /// Handles beginnings and ends of elements
        /// </summary>
        private void HandleAspElements() {
            if (currentChar == '>' && ((withinAspElement && !withinAttributeValue) || withinScriptBlock)) { // end of tag
                if (!withinScriptBlock) {
                    HitEnd(ref externalSpan, 0);
                    StartPlainText(1, 0);
                }
                // get element name
                if (string.IsNullOrEmpty(elementName) && attributeNameBuilder.Length > 0) {
                    elementName = attributeNameBuilder.ToString();
                    attributeNameBuilder.Length = 0;
                }
                
                if (withinEndAspElement) {        
                    // content of <script> tags report as code
                    if (elementName.ToLower() == "script" && withinScriptBlock) {
                        withinCodeBlock = false;
                        withinScriptBlock = false;
                        HitEnd(ref externalSpan, -9);
                        HitEnd(ref internalSpan, -9);
                        if (scriptBeginTagContainedRunatServer) {
                            handler.OnCodeBlock(new CodeBlockContext() {
                                BlockText = codeBuilder.ToString(0, codeBuilder.Length - 9),
                                InnerBlockSpan = internalSpan,
                                OuterBlockSpan = externalSpan,
                                WithinClientSideComment = withinClientComment
                            });
                        }
                        scriptBeginTagContainedRunatServer = false;
                        externalSpan = null;
                        internalSpan = null;
                        codeBuilder.Length = 0;
                    } else { // other elements
                        handler.OnElementEnd(new EndElementContext() {
                            BlockSpan = externalSpan,
                            ElementName = elementName,
                            Prefix = elementPrefix,
                            WithinClientSideComment = withinClientComment
                        });
                        codeBuilder.Length = 0;
                    }
                    
                    if (softStop) hardStop = true;
                    externalSpan = null;
                } else {
                    // begin code block       
                    if (elementName.ToLower() == "script") {
                        withinCodeBlock = true;
                        withinScriptBlock = true;
                        scriptBeginTagContainedRunatServer = ContainRunatServer(attributes);
                        externalSpan = null;
                        HitStart(ref externalSpan, 1);
                        HitStart(ref internalSpan, 1);
                    } else { // begin standard element
                        handler.OnElementBegin(new ElementContext() {
                            Attributes = attributes,
                            BlockSpan = externalSpan,
                            ElementName = elementName,
                            Prefix = elementPrefix,
                            WithinClientSideComment = withinClientComment,
                            IsEnd = lastNonWhitespaceChar == '/'
                        });
                        externalSpan = null;
                        codeBuilder.Length = 0;
                        if (softStop) hardStop = true;
                    }
                }
                
                withinAspElement = false;
                withinEndAspElement = false;                
            }

            if (!withinAspElement && !withinAspTags && GetCodeBack(1) == '<' &&
                (currentChar.CanBePartOfIdentifier() || currentChar == '/' || (currentChar == '!' && GetCodeForth(1) != '-'))) {
                EndPlainText(-1, 2);

                withinAspElement = true;
                elementName = null;
                elementPrefix = null;
                withinAttributeName = true;
                HitStart(ref externalSpan, -1);
               
                if (currentChar == '/') {
                    withinEndAspElement = true;
                    attributes = null;
                } else {
                    attributeNameBuilder.Append(currentChar);
                    attributes = new List<AttributeInfo>();
                }
            }        
        }

        private bool ContainRunatServer(List<AttributeInfo> list) {
            foreach (AttributeInfo info in list) {
                if (info.Name.ToLower() == "runat" && info.Value == "server") return true;
            }
            return false;
        }

        private void StartPlainText(int blockCorrection, int textCorrection) {
            HitStart(ref plainTextSpan, blockCorrection);
            plainTextBuilder.Length = 0;
            withinPlainText = true;
            plaintTextStartCorrection = Math.Abs(textCorrection);
        }

        private void EndPlainText(int blockCorrection, int textCorrection) {
            withinPlainText = false;

            if (plainTextSpan == null) {
                plainTextBuilder.Length = 0;
                return;
            }

            HitEnd(ref plainTextSpan, blockCorrection);

            textCorrection = Math.Abs(textCorrection);
         
            if (plainTextBuilder.Length > 0 && plainTextBuilder.Length - textCorrection - plaintTextStartCorrection>= 0) {
                string text = plainTextBuilder.ToString(plaintTextStartCorrection, plainTextBuilder.Length - textCorrection - plaintTextStartCorrection); 
                if (text.Trim() != string.Empty) {
                    handler.OnPlainText(new PlainTextContext() {
                        Text = text,
                        WithinClientSideComment = withinClientComment,
                        BlockSpan = plainTextSpan
                    });
                    if (softStop) hardStop = true;
                }
            }
            
            plainTextSpan = null;
            plainTextBuilder.Length = 0;
        }

        /// <summary>
        /// Called after "&lt;%" has been read to determine what kind of content it is 
        /// </summary>
        private void ReactToBeginningOfAspTags() {
            if (currentChar == '=') {
                outputElementKind = OutputElementKind.PLAIN;
                withinOutputElement = true;                
            }
            if (currentChar == ':') {
                outputElementKind = OutputElementKind.HTML_ESCAPED;
                withinOutputElement = true;                
            }
            if (currentChar == '$') {
                outputElementKind = OutputElementKind.EXPRESSION;
                withinOutputElement = true;                
            }
            if (currentChar == '#') {
                outputElementKind = OutputElementKind.BIND;
                withinOutputElement = true;
            }
            if (currentChar == '@') {
                EndPlainText(-1, 2);

                withinAspDirective = true;
                withinAspElement = true;
                withinAspTags = false;
                withinAttributeName = true;
                attributes = new List<AttributeInfo>();
                elementName = null;
                elementPrefix = null;
                codeBuilder.Length = 0;
                withinCodeBlock = false;
                                
                HitStart(ref externalSpan, -1);
            }
            if (withinOutputElement) {
                if (withinAttributeValue) {
                    backupSpan = new BlockSpan(externalSpan);
                    backupBuilder.Length = 0;
                    backupBuilder.Append(codeBuilder);
                    codeBuilder.Length = 0;
                }
                if (withinCodeBlock) {
                    codeBuilder.Length = 0;
                    withinCodeBlock = false;
                }
                HitStart(ref internalSpan, 1);               
            }                              
        }

        /// <summary>
        /// Called after %>, reports the content to the handler
        /// </summary>
        private void ReactToEndOfAspTags() {
            if (withinAspElement) { // only expression within attribute value was read
                backupBuilder.Append(codeBuilder);                
            }
            if (withinOutputElement) {
                handler.OnOutputElement(new OutputElementContext() {
                    Kind = outputElementKind,
                    InnerText = codeBuilder.ToString(0, codeBuilder.Length - 2),
                    InnerBlockSpan = internalSpan,
                    OuterBlockSpan = externalSpan,
                    WithinClientSideComment = withinClientComment,
                    WithinElementsAttribute = withinAspElement && withinAttributeValue
                });

                if (softStop) hardStop = true;
                withinOutputElement = false;
                internalSpan = null;
                externalSpan = null;
                if (withinAttributeValue) attributeValueContainsOutput = true;
                codeBuilder.Length = 0;
            }
            if (withinAspDirective) {
                handler.OnPageDirective(new DirectiveContext() {
                    Attributes = attributes,
                    BlockSpan = externalSpan,
                    DirectiveName = elementName,
                    WithinClientSideComment = withinClientComment
                });

                if (softStop) hardStop = true;
                externalSpan = null;
                elementName = null;
                attributes = null;
                withinAspDirective = false;
                withinAspElement = false;
                codeBuilder.Length = 0;
            }
            if (withinCodeBlock) {
                handler.OnCodeBlock(new CodeBlockContext() {
                    BlockText = codeBuilder.ToString(0, codeBuilder.Length - 2),
                    InnerBlockSpan = internalSpan,
                    OuterBlockSpan = externalSpan,
                    WithinClientSideComment = withinClientComment
                });

                if (softStop) hardStop = true;
                externalSpan = null;
                internalSpan = null;
                withinCodeBlock = false;
                codeBuilder.Length = 0;
            }
            if (withinAspElement) {
                externalSpan = new BlockSpan(backupSpan);
                codeBuilder.Append(backupBuilder);
                backupBuilder.Length = 0;
            }
        }

        /// <summary>
        /// Modifies state of client comment <!-- --> indicator
        /// </summary>
        private void CheckClientCommentState() {
            if (!withinClientComment) {
                if (currentChar == '-' && GetCodeBack(1) == '-' && GetCodeBack(2) == '!' && GetCodeBack(3) == '<') {
                    EndPlainText(-3, 4); 
                    withinClientComment = true;                    
                }
            } else {
                if (currentChar == '>' && GetCodeBack(1) == '-' && GetCodeBack(2) == '-') {                    
                    StartPlainText(1, 0);
                    withinClientComment = false;
                }
            }
        }

        /// <summary>
        /// Modifies state based on a character within an element
        /// </summary>
        private void ReactToWithinElementChar() {
            if (!withinAttributeValue) {
                if (currentChar == '"' || currentChar == '\'') { // begining of an attribute
                    withinAttributeValue = true;
                    quotesChar = currentChar;
                    currentAttributeBlockSpan = new BlockSpan();
                    currentAttributeBlockSpan.AbsoluteCharOffset = currentOffset - 1;
                    currentAttributeBlockSpan.StartIndex = currentIndex - 1;
                    currentAttributeBlockSpan.StartLine = currentLine;
                } else if (currentChar == ':') { // tag prefix
                    if (string.IsNullOrEmpty(elementName)) {
                        elementPrefix = attributeNameBuilder.ToString().Trim();
                        attributeNameBuilder.Length = 0;
                        return;
                    }
                } else {
                    if (!withinAttributeName) { // beginning of attribute name, possibly end of tag name
                        if (currentChar.CanBePartOfIdentifier() && !GetCodeBack(1).CanBePartOfIdentifier()) {
                            if (attributeNameBuilder.Length > 0) {
                                if (string.IsNullOrEmpty(elementName)) {
                                    elementName = attributeNameBuilder.ToString().Trim();
                                    if (elementName.StartsWith(":")) elementName = elementName.Substring(1);
                                }
                                attributeNameBuilder.Length = 0;
                            }
                            withinAttributeName = true;
                        }
                    } else { // end of attribute name
                        if (!currentChar.CanBePartOfIdentifier() && GetCodeBack(1).CanBePartOfIdentifier()) {
                            withinAttributeName = false;
                        }
                    }
                }
            } else {
                if (currentChar == quotesChar && !withinOutputElement) { // end of attribute value
                    currentAttributeBlockSpan.EndLine = currentLine;
                    currentAttributeBlockSpan.EndIndex = currentIndex; // " or '
                    currentAttributeBlockSpan.AbsoluteCharLength = currentOffset - currentAttributeBlockSpan.AbsoluteCharOffset;

                    // add attribute to the element's list
                    AttributeInfo nfo = new AttributeInfo() {
                        Name = attributeNameBuilder.ToString(),
                        Value = attributeValueBuilder.ToString().Substring(1), // " or '                        
                        BlockSpan = currentAttributeBlockSpan,
                        ContainsAspTags = attributeValueContainsOutput
                    };
                    
                    attributes.Add(nfo);

                    attributeValueContainsOutput = false;
                    currentAttributeBlockSpan = null;
                    withinAttributeValue = false;
                    attributeNameBuilder.Length = 0;
                    attributeValueBuilder.Length = 0;
                }

            }
            if (withinAttributeName) attributeNameBuilder.Append(currentChar);
            if (withinAttributeValue) attributeValueBuilder.Append(currentChar);            
        }        

        /// <summary>
        /// Marks current position as a beginning of a block (creates new if null passed)
        /// </summary>        
        private void HitStart(ref BlockSpan span, int correction) {
            if (span == null) span = new BlockSpan();

            span.StartLine = currentLine;
            span.StartIndex = currentIndex + correction;
            span.AbsoluteCharOffset = currentOffset + correction;
        }

        /// <summary>
        /// Marks current position as an end of a block
        /// </summary>        
        private void HitEnd(ref BlockSpan span, int correction) {
            span.EndLine = currentLine;
            span.EndIndex = currentIndex + correction;
            span.AbsoluteCharLength = currentOffset - span.AbsoluteCharOffset + correction;
        }  

        /// <summary>
        /// Returns char 'relative' characters back from current position
        /// </summary>        
        private char GetCodeBack(int relative) {
            if (currentOffset - relative < 0) return '?';
            return text[currentOffset - relative];
        }

        /// <summary>
        /// Returns char 'relative' characters forth from current position
        /// </summary>     
        private char GetCodeForth(int relative) {
            if (currentOffset + relative >= text.Length) return '?';
            return text[currentOffset + relative];
        }

        /// <summary>
        /// Moves current position by one forward
        /// </summary>
        private void Move() {            
            currentIndex++;
            currentOffset++;
            if (currentChar == '\n') {
                currentIndex = 0;
                currentLine++;
            }        
        }
    }
}
