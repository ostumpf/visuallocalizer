using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;

namespace VisualLocalizer.Library.AspxParser {
    public sealed class Parser {

        private string text;
        private IAspxHandler handler;

        private int currentLine, currentIndex, currentOffset, aposCount, maxLine, maxIndex, plaintTextStartCorrection;
        private char currentChar;
        private bool withinAspElement, withinAspDirective, withinCodeBlock, withinOutputElement, withinAspTags, attributeValueContainsOutput,
            withinServerComment, withinClientComment, withinEndAspElement, withinAttributeName, withinAttributeValue, withinPlainText;
        private OutputElementKind outputElementKind;
        private StringBuilder codeBuilder, backupBuilder, plainTextBuilder, attributeNameBuilder, attributeValueBuilder;
        private List<AttributeInfo> attributes;
        private string elementName, elementPrefix;
        private BlockSpan currentAttributeBlockSpan, externalSpan, internalSpan, backupSpan, plainTextSpan;

        public Parser(string text, IAspxHandler handler, int maxLine, int maxIndex) {
            this.text = text;
            this.handler = handler;
            this.maxIndex = maxIndex;
            this.maxLine = maxLine;
        }

        public Parser(string text, IAspxHandler handler) : this(text,handler, int.MaxValue, int.MaxValue) { }

        public void Process() {
            currentLine = 0;
            currentIndex = 0;
            currentOffset = 0;
            currentChar = '?';
            codeBuilder = new StringBuilder();
            backupBuilder = new StringBuilder();
            plainTextBuilder = new StringBuilder();
            attributeNameBuilder = new StringBuilder();
            attributeValueBuilder = new StringBuilder();
            
            bool justEnteredAspTags = false;
            bool endRequested = false;

            for (int i = 0; i < text.Length; i++) {
                currentChar = text[i];
                if (currentLine > maxLine || (currentLine == maxLine && currentIndex > maxIndex)) endRequested = true;
                if (handler.StopRequested) endRequested = true;

                if (!withinServerComment)
                    if (withinOutputElement || withinCodeBlock) {
                        codeBuilder.Append(currentChar);
                    } else if (!withinAspElement && !withinAspDirective) {
                        if (endRequested) {
                            break;
                        } else if (withinPlainText) {
                            plainTextBuilder.Append(currentChar);
                        }
                    }                

                if (!withinServerComment && (withinAspDirective || withinAspElement)) {
                    ReactToWithinElementChar();
                }

                if (!withinServerComment && justEnteredAspTags && !char.IsWhiteSpace(currentChar)) {
                    ReactToBeginningOfAspTags();
                    justEnteredAspTags = false;
                }
                
                if (!withinAspTags) {                    
                    if (currentChar == '%' && GetCodeBack(1) == '<') {
                        EndPlainText(-1,2);
                        if (GetCodeForth(1) == '-' && GetCodeForth(2) == '-') {
                            withinServerComment = true;
                            withinAspTags = true;
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
                            
                            HitEnd(ref externalSpan, 0);
                            if (internalSpan != null) HitEnd(ref internalSpan, -2);

                            ReactToEndOfAspTags();
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

                Move();
            }
        }

        private void HandleAspElements() {
            if (withinAspElement && currentChar == '\"') aposCount++;

            if (withinAspElement && currentChar == '>' && aposCount % 2 == 0) {
                HitEnd(ref externalSpan, 0);
                StartPlainText(1, 0);

                if (string.IsNullOrEmpty(elementName) && attributeNameBuilder.Length > 0) {
                    elementName = attributeNameBuilder.ToString();
                    attributeNameBuilder.Length = 0;
                }               

                if (withinEndAspElement) {
                    if (elementName.ToLower() == "script") {
                        withinCodeBlock = false;
                        HitEnd(ref externalSpan, -8);
                        HitEnd(ref internalSpan, -8);
                        handler.OnCodeBlock(new CodeBlockContext() {
                            BlockText = codeBuilder.ToString(0, codeBuilder.Length-8),
                            InnerBlockSpan = internalSpan,
                            OuterBlockSpan = externalSpan,
                            WithinClientSideComment = withinClientComment
                        });
                        externalSpan = null;
                        internalSpan = null;
                        codeBuilder.Length = 0;
                    } else {
                        handler.OnElementEnd(new EndElementContext() {
                            BlockSpan = externalSpan,
                            ElementName = elementName,
                            Prefix = elementPrefix,
                            WithinClientSideComment = withinClientComment
                        });                        
                    }
                    externalSpan = null;
                } else {
                    if (elementName.ToLower() == "script") {
                        withinCodeBlock = true;
                        externalSpan = null;
                        HitStart(ref externalSpan, 0);
                        HitStart(ref internalSpan, 0);
                    } else {
                        handler.OnElementBegin(new ElementContext() {
                            Attributes = attributes,
                            BlockSpan = externalSpan,
                            ElementName = elementName,
                            Prefix = elementPrefix,
                            WithinClientSideComment = withinClientComment
                        });
                        externalSpan = null;
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
                aposCount = 0;               
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
                }
            }
            plainTextSpan = null;
            plainTextBuilder.Length = 0;
        }

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
            if (currentChar == '@') {
                withinAspDirective = true;
                attributes = new List<AttributeInfo>();
                elementName = null;
            }
            if (withinAspDirective || withinOutputElement) {
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

        private void ReactToEndOfAspTags() {
            if (withinAspElement) {
                backupBuilder.Append(codeBuilder);                
            }
            if (withinOutputElement) {
                handler.OnOutputElement(new OutputElementContext() {
                    Kind = outputElementKind,
                    InnerText = codeBuilder.ToString(0, codeBuilder.Length - 2),
                    InnerBlockSpan = internalSpan,
                    OuterBlockSpan = externalSpan,
                    WithinClientSideComment = withinClientComment,
                    WithinElementsAttribute = withinAspElement && aposCount % 2 == 1
                });
                
                withinOutputElement = false;
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

                externalSpan = null;
                elementName = null;
                attributes = null;
                withinAspDirective = false;
                codeBuilder.Length = 0;
            }
            if (withinCodeBlock) {
                handler.OnCodeBlock(new CodeBlockContext() {
                    BlockText = codeBuilder.ToString(0, codeBuilder.Length - 2),
                    InnerBlockSpan = internalSpan,
                    OuterBlockSpan = externalSpan,
                    WithinClientSideComment = withinClientComment
                });

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

        private void CheckClientCommentState() {
            if (!withinClientComment) {
                if (currentChar == '-' && GetCodeBack(1) == '-' && GetCodeBack(2) == '!' && GetCodeBack(3) == '<') {
                    withinClientComment = true;
                    EndPlainText(-3, 4);
                }
            } else {
                if (currentChar == '>' && GetCodeBack(1) == '-' && GetCodeBack(2) == '-') {
                    withinClientComment = false;
                    StartPlainText(1, 0);
                }
            }
        }

        private void ReactToWithinElementChar() {
            if (!withinAttributeValue) {
                if (currentChar == '\"') {
                    withinAttributeValue = true;
                    currentAttributeBlockSpan = new BlockSpan();
                    currentAttributeBlockSpan.AbsoluteCharOffset = currentOffset;
                    currentAttributeBlockSpan.StartIndex = currentIndex - 1;
                    currentAttributeBlockSpan.StartLine = currentLine;
                } else if (currentChar == ':') {
                    if (string.IsNullOrEmpty(elementName)) {
                        elementPrefix = attributeNameBuilder.ToString().Trim();
                        attributeNameBuilder.Length = 0;
                    }
                } else {
                    if (!withinAttributeName) {
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
                    } else {
                        if (!currentChar.CanBePartOfIdentifier() && GetCodeBack(1).CanBePartOfIdentifier()) {
                            withinAttributeName = false;
                        }
                    }
                }
            } else {
                if (currentChar == '\"' && !withinOutputElement) {
                    currentAttributeBlockSpan.EndLine = currentLine;
                    currentAttributeBlockSpan.EndIndex = currentIndex; // "
                    currentAttributeBlockSpan.AbsoluteCharLength = currentOffset - currentAttributeBlockSpan.AbsoluteCharOffset;

                    AttributeInfo nfo = new AttributeInfo() {
                        Name = attributeNameBuilder.ToString(),
                        Value = attributeValueBuilder.ToString().Substring(1), // "
                        IsMarkedWithUnlocalizableComment = false,
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

        private int CountBack(string text, char c, int i) {
            i--;
            int count=0;
            while (i >= 0 && text[i] == c) {
                count++;
                i--;
            }
            return count;
        }

        private void HitStart(ref BlockSpan span, int correction) {
            if (span == null) span = new BlockSpan();

            span.StartLine = currentLine;
            span.StartIndex = currentIndex + correction;
            span.AbsoluteCharOffset = currentOffset + correction;
        }

        private void HitEnd(ref BlockSpan span, int correction) {
            span.EndLine = currentLine;
            span.EndIndex = currentIndex + correction;
            span.AbsoluteCharLength = currentOffset - span.AbsoluteCharOffset + correction;
        }  

        private char GetCodeBack(int relative) {
            if (currentOffset - relative < 0) return '?';
            return text[currentOffset - relative];
        }

        private char GetCodeForth(int relative) {
            if (currentOffset + relative >= text.Length) return '?';
            return text[currentOffset + relative];
        }

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
