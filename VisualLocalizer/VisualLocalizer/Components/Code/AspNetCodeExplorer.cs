﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Commands;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;
using System.IO;
using VisualLocalizer.Library.AspX;
using System.Reflection;
using System.ComponentModel;
using System.Collections;
using VisualLocalizer.Extensions;
using VisualLocalizer.Library.Components;
using VisualLocalizer.Library.Extensions;
using VisualLocalizer.Commands.Move;
using VisualLocalizer.Commands.Inline;

namespace VisualLocalizer.Components.Code {

    /// <summary>
    /// Implementation of IAspxHandler, providing functionality for exploring ASP .NET files.
    /// </summary>
    internal sealed class AspNetCodeExplorer : IAspxHandler {

        /// <summary>
        /// Command whose methods LookupInAspCSharp() and LookupInAspVB() are called when code block are found
        /// </summary>
        private AbstractBatchCommand parentCommand;

        /// <summary>
        /// Namespaces imported in the file
        /// </summary>
        private NamespacesList declaredNamespaces = new NamespacesList();
        
        /// <summary>
        /// File name
        /// </summary>
        private string ClassFileName = null;
        
        /// <summary>
        /// Text of the file
        /// </summary>
        private string fileText;

        /// <summary>
        /// Full path to the file
        /// </summary>
        private string fullPath;

        /// <summary>
        /// Instance of WebConfig, handling attribute's types
        /// </summary>
        private WebConfig webConfig;

        /// <summary>
        /// Project item corresponding to the file
        /// </summary>
        private ProjectItem projectItem;

        /// <summary>
        /// Language of the file, initialized by Page directive
        /// </summary>
        private FILETYPE fileLanguage;

        /// <summary>
        /// Synchronization object
        /// </summary>
        private object syncObject = new object();

        /// <summary>
        /// Stack of elements, used to determine in which element plain text belongs
        /// </summary>
        private Stack<ElementContext> openedElements = new Stack<ElementContext>();


        private AspNetCodeExplorer() { }

        private static AspNetCodeExplorer instance;

        /// <summary>
        /// Returns instance of AspNetCodeExplorer
        /// </summary>
        public static AspNetCodeExplorer Instance {
            get {                
                if (instance == null) instance = new AspNetCodeExplorer();
                return instance;
            }
        }

        /// <summary>
        /// Explores given file, using parent batch command's methods as callbacks
        /// </summary>
        /// <param name="parentCommand"></param>
        /// <param name="projectItem"></param>
        /// <param name="maxLine">Line where parser should stop</param>
        /// <param name="maxIndex">Column where parser should stop</param>
        public void Explore(AbstractBatchCommand parentCommand, ProjectItem projectItem, int maxLine, int maxIndex) {
            if (parentCommand == null) throw new ArgumentNullException("parentCommand");
            if (projectItem == null) throw new ArgumentNullException("projectItem");

            lock (syncObject) {
                fullPath = projectItem.GetFullPath();
                if (string.IsNullOrEmpty(fullPath)) throw new Exception("Cannot process item " + projectItem.Name);

                this.parentCommand = parentCommand;
                this.declaredNamespaces.Clear();
                this.ClassFileName = Path.GetFileNameWithoutExtension(fullPath);
                this.projectItem = projectItem;
                this.openedElements.Clear();

                // initialize type resolver
                if (parentCommand is BatchMoveCommand) {
                    webConfig = WebConfig.Get(projectItem, VisualLocalizerPackage.Instance.DTE.Solution);
                } else {
                    webConfig = null;
                }
                fileText = null;

                if (RDTManager.IsFileOpen(fullPath)) { // file is open
                    var textLines = VLDocumentViewsManager.GetTextLinesForFile(fullPath, false); // get text buffer
                    if (textLines == null) return;

                    int lastLine, lastLineIndex;
                    int hr = textLines.GetLastLineIndex(out lastLine, out lastLineIndex);
                    Marshal.ThrowExceptionForHR(hr);

                    hr = textLines.GetLineText(0, 0, lastLine, lastLineIndex, out fileText); // get plain text
                    Marshal.ThrowExceptionForHR(hr);
                } else { // file is closed - read it from disk
                    fileText = File.ReadAllText(fullPath);
                }

                Parser parser = new Parser(fileText, this, maxLine, maxIndex); // run ASP .NET parser
                parser.Process();
            }
        }

        /// <summary>
        /// Explores given file, using parent batch command's methods as callbacks. No scope limitation are passed to the parser.
        /// </summary>
        public void Explore(AbstractBatchCommand parentCommand, ProjectItem projectItem) {
            Explore(parentCommand, projectItem, int.MaxValue, int.MaxValue);
        }

        /// <summary>
        /// Should return true, when parsing should be stopped. Currently processed block/element is first finished,
        /// after that parser exits.
        /// </summary>
        public bool StopRequested { get { return false;  } }

        /// <summary>
        /// Called after code block &lt;% %&gt;
        /// </summary> 
        public void OnCodeBlock(CodeBlockContext context) {
            context.InnerBlockSpan.Move(1, 1); // fix numbering

            // run parent command methods, adding found result items to results
            IList list = null;
            try {
                if (fileLanguage == FILETYPE.CSHARP) {
                    list = parentCommand.LookupInCSharpAspNet(context.BlockText, context.InnerBlockSpan, declaredNamespaces, ClassFileName);
                } else if (fileLanguage == FILETYPE.VB) {
                    list = parentCommand.LookupInVBAspNet(context.BlockText, context.InnerBlockSpan, declaredNamespaces, ClassFileName);
                }
            } catch (Exception ex) {
                if (!(parentCommand is ReferenceLister)) {
                    VLOutputWindow.VisualLocalizerPane.WriteLine("\tException occured while processing " + projectItem.Name);
                    VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                }
            }

            if (list != null) {
                foreach (AbstractResultItem item in list) {
                    AspNetStringResultItem aitem = item as AspNetStringResultItem;
                    if (aitem != null) {
                        aitem.ComesFromClientComment = context.WithinClientSideComment;
                        aitem.ComesFromCodeBlock = true;
                        aitem.ClassOrStructElementName = ClassFileName;
                    }
                    
                }

                AddContextToItems((IEnumerable)list);
            }
        }

        /// <summary>
        /// Called after page directive &lt;%@ %&gt;
        /// </summary> 
        public void OnPageDirective(DirectiveContext context) {
            // add new imported namespace
            if (context.DirectiveName == "Import" && context.Attributes.Exists((info) => { return info.Name == "Namespace"; })) {
                declaredNamespaces.Add(new UsedNamespaceItem(context.Attributes.Find((info) => { return info.Name == "Namespace"; }).Value, null, true));
            }
            if (context.DirectiveName == "Page" || context.DirectiveName == "Control") {
                string lang = null; // value of Language attribute
                string ext = null; // extension of the code-behind file
                foreach (var info in context.Attributes) {
                    if (info.Name == "Language") lang = info.Value; // get file language
                    if (info.Name == "CodeFile") { // get code-behind file
                        int index = info.Value.LastIndexOf('.');
                        if (index != -1 && index + 1 < info.Value.Length) {
                            ext = info.Value.Substring(index + 1);
                        }
                    }                    
                }
                if (string.IsNullOrEmpty(lang)) {
                    if (!string.IsNullOrEmpty(ext)) { // infer file language from the extension
                        fileLanguage = StringConstants.CsExtensions.Contains(ext.ToLower()) ? FILETYPE.CSHARP : FILETYPE.VB;
                    }
                } else {
                    fileLanguage = lang == "C#" ? FILETYPE.CSHARP : FILETYPE.VB;
                }
            }
            if (context.DirectiveName == "Register") {
                string assembly = null, nmspc = null, src = null, tagName = null, tagPrefix = null;
                foreach (AttributeInfo info in context.Attributes) {
                    if (info.Name == "Assembly") assembly = info.Value;
                    if (info.Name == "Namespace") nmspc = info.Value;
                    if (info.Name == "Src") src = info.Value;
                    if (info.Name == "TagName") tagName = info.Value;
                    if (info.Name == "TagPrefix") tagPrefix = info.Value;
                }
                // add definitions of elements for future type resolution
                if (!string.IsNullOrEmpty(tagPrefix)) {
                    if (!string.IsNullOrEmpty(assembly) && !string.IsNullOrEmpty(nmspc)) {
                        if (webConfig != null) webConfig.AddTagPrefixDefinition(new TagPrefixAssemblyDefinition(assembly, nmspc, tagPrefix));
                    } else if (!string.IsNullOrEmpty(tagName) && !string.IsNullOrEmpty(src)) {
                        if (webConfig != null) webConfig.AddTagPrefixDefinition(new TagPrefixSourceDefinition(projectItem, 
                            VisualLocalizerPackage.Instance.DTE.Solution, tagName, src, tagPrefix));
                    }
                }
            }

            if (parentCommand is BatchMoveCommand) { // no resource references can be directly in the attributes, safe to look only for string literals
                foreach (AttributeInfo info in context.Attributes) {
                    if (info.ContainsAspTags) continue; // attribute's value contains &lt;%= - like tags - localization is not desirable
                    if (info.Name.ToLower() == "language") continue;

                    AspNetStringResultItem item = AddResult(info, null, context.DirectiveName, context.WithinClientSideComment, false, false, true);
                    if (item != null) {
                        item.ComesFromDirective = true;
                        item.AttributeName = info.Name;
                    }
                }
            }
        }

        /// <summary>
        /// Called after beginnnig tag is read
        /// </summary>    
        public void OnElementBegin(ElementContext context) {
            if (!context.IsEnd) openedElements.Push(context);

            if (parentCommand is BatchMoveCommand) { // no resource references can be directly in the attributes, safe to look only for string literals
                foreach (var info in context.Attributes) {
                    if (info.ContainsAspTags) continue; // attribute's value contains &lt;%= - like tags - localization is not desirable
                    if (ShouldIgnoreThisAttribute(context.ElementName, info.Name)) continue; // attribute is not localizable

                    if (Settings.SettingsObject.Instance.UseReflectionInAsp) { // attempt to resolve type
                        bool isLocalizableFalse;
                        bool? isString = webConfig.IsTypeof(context.Prefix, context.ElementName, info.Name, typeof(string), out isLocalizableFalse);

                        AspNetStringResultItem newItem = null;
                        if (isString == null || isString.Value) { // add to results if resolution returned true or was not conclusive
                            newItem = AddResult(info, context, false);
                            newItem.IsWithinLocalizableFalse = isLocalizableFalse;
                            newItem.LocalizabilityProved = isString.HasValue && isString.Value;
                        }                        
                    } else { // type resolution not enabled
                        AddResult(info, context, false);
                    }                    
                }
            }
        }

        /// <summary>
        /// Returns true if given attribute of given element should be overlooked (ID, Name...)
        /// </summary>        
        private bool ShouldIgnoreThisAttribute(string elementName, string attributeName) {
            bool ignore = false;
            foreach (string token in StringConstants.AspNetIgnoredAttributes) {
                string[] t = token.Split(':');
                if (t.Length != 2) continue;

                if ((t[0] == "*" || t[0] == elementName) && (t[1] == "*" || t[1] == attributeName)) {
                    ignore = true;
                    break;
                }
            }
            return ignore;
        }

        /// <summary>
        /// Called after output element &lt;%= %&gt;, &lt;%$ %&gt; or &lt;%: %&gt;
        /// </summary> 
        public void OnOutputElement(OutputElementContext context) {
            IList list = null;

            if (context.Kind == OutputElementKind.HTML_ESCAPED || context.Kind == OutputElementKind.PLAIN) {
                context.InnerBlockSpan.Move(1, 1); // fix numbering
                // use code lookupers to explore the code
                if (fileLanguage == FILETYPE.CSHARP) {
                    list = parentCommand.LookupInCSharpAspNet(context.InnerText, context.InnerBlockSpan, declaredNamespaces, ClassFileName);
                } else if (fileLanguage == FILETYPE.VB) {
                    list = parentCommand.LookupInVBAspNet(context.InnerText, context.InnerBlockSpan, declaredNamespaces, ClassFileName);
                }
            } else if (context.Kind == OutputElementKind.EXPRESSION) {
                if (parentCommand is BatchInlineCommand) {
                    list = ((BatchInlineCommand)parentCommand).ParseResourceExpression(context.InnerText, context.InnerBlockSpan, fileLanguage == FILETYPE.CSHARP ? LANGUAGE.CSHARP : LANGUAGE.VB);
                }
            }

            if (list != null) {
                foreach (AbstractResultItem item in list) {
                    item.ComesFromClientComment = context.WithinClientSideComment;
                    if (item is AspNetStringResultItem) {
                        ((AspNetStringResultItem)item).ComesFromInlineExpression = true;
                        ((AspNetStringResultItem)item).ClassOrStructElementName = ClassFileName;
                    }
                    if (item is AspNetCodeReferenceResultItem) {
                        ((AspNetCodeReferenceResultItem)item).ComesFromInlineExpression = true;
                        ((AspNetCodeReferenceResultItem)item).InlineReplaceSpan = context.OuterBlockSpan;
                    }
                }
                AddContextToItems((IEnumerable)list);
            }
        }

        /// <summary>
        /// Called after plain text (between elements) is read
        /// </summary>
        public void OnPlainText(PlainTextContext context) {
            if (parentCommand is BatchMoveCommand) { // no resource references can be directly in the attributes, safe to look only for string literals
                // add whole text to results
                var newItem = AddResult(new AttributeInfo() { BlockSpan = context.BlockSpan, Name = context.Text, Value = context.Text },
                    null, null, context.WithinClientSideComment, false, false, false);
                if (newItem != null) {
                    newItem.ComesFromPlainText = true;
                    newItem.ComesFromElement = false;

                    if (openedElements.Count > 0) {
                        ElementContext element = openedElements.Peek();
                        newItem.ElementName = element.ElementName;
                        newItem.ElementPrefix = element.Prefix;
                    }
                }
            }
        }

        /// <summary>
        /// Called after end tag is read
        /// </summary>
        public void OnElementEnd(EndElementContext context) {
            if (openedElements.Count > 0 && openedElements.Peek().ElementName == context.ElementName && openedElements.Peek().Prefix == context.Prefix)
                openedElements.Pop();
        }        

        /// <summary>
        /// Adds new result item to the list of results
        /// </summary>
        private AspNetStringResultItem AddResult(AttributeInfo info, string elementPrefix,string elementName, bool comesFromClientComment,
            bool propertyLocalizableFalse, bool comesFromElement, bool stripApos) {
            if (!(parentCommand is BatchMoveCommand)) return null;

            BatchMoveCommand bCmd = (BatchMoveCommand)parentCommand;
            if (stripApos) info.BlockSpan.Move(1, 0);

            TextSpan span = new TextSpan();
            span.iStartLine = info.BlockSpan.StartLine;
            span.iStartIndex = info.BlockSpan.StartIndex + (stripApos ? 1 : 0);
            span.iEndLine = info.BlockSpan.EndLine;
            span.iEndIndex = info.BlockSpan.EndIndex - (stripApos ? 1 : 0);

            AspNetStringResultItem resultItem = new AspNetStringResultItem();
            resultItem.Value = info.Value.ConvertAspNetEscapeSequences().Trim();
            resultItem.ReplaceSpan = span;
            resultItem.AbsoluteCharOffset = info.BlockSpan.AbsoluteCharOffset + (stripApos ? 2 : 0);
            resultItem.AbsoluteCharLength = info.Value.Length;
            resultItem.WasVerbatim = false;
            resultItem.IsWithinLocalizableFalse = propertyLocalizableFalse;
            resultItem.IsMarkedWithUnlocalizableComment = false;
            resultItem.ClassOrStructElementName = ClassFileName;
            resultItem.DeclaredNamespaces = declaredNamespaces;
            resultItem.ComesFromElement = comesFromElement;
            resultItem.ComesFromClientComment = comesFromClientComment;
            resultItem.ElementPrefix = elementPrefix;
            resultItem.ElementName = elementName;
            resultItem.Language = fileLanguage == FILETYPE.CSHARP ? LANGUAGE.CSHARP : LANGUAGE.VB;
            resultItem.AttributeName = info.Name;

            AddContextToItem(resultItem);

            bCmd.AddToResults(resultItem);

            return resultItem;
        }

        /// <summary>
        /// Adds new result item to the list of results
        /// </summary>
        private AspNetStringResultItem AddResult(AttributeInfo info, ElementContext elementContext, bool propertyLocalizableFalse) {
            return AddResult(info, elementContext.Prefix, elementContext.ElementName, elementContext.WithinClientSideComment, propertyLocalizableFalse, true, true);
        }
    
        /// <summary>
        /// Adds context to given list of result items
        /// </summary>
        /// <param name="items"></param>
        public void AddContextToItems(IEnumerable items) {
            foreach (AbstractResultItem item in items) {
                AddContextToItem(item);
            }
        }

        /// <summary>
        /// Adds few lines of code as a context to the result item
        /// </summary>        
        private void AddContextToItem(AbstractResultItem item) {
            item.ContextRelativeLine = 0;

            int currentPos = item.AbsoluteCharOffset;
            string currentLine = GetLine(ref currentPos, 0); // current line's text
            currentLine = currentLine.Substring(0, Math.Min(item.ReplaceSpan.iStartIndex, currentLine.Length)) + StringConstants.ContextSubstituteText;

            StringBuilder context = new StringBuilder();
            context.Append(currentLine);

            int topLines = 0;
            int botLines = 0;

            while ((currentLine = GetLine(ref currentPos, -1)) != null && topLines < NumericConstants.ContextLineRadius) {
                string lineText = currentLine.ToString();

                if (lineText.Trim().Length > 0) {
                    context.Insert(0, lineText + Environment.NewLine);
                    item.ContextRelativeLine++;
                    if (lineText.Trim().Length > 1) topLines++;
                }
            }

            currentPos = item.AbsoluteCharOffset+item.AbsoluteCharLength;
            currentLine = GetLine(ref currentPos, 0);
            context.Append(currentLine.Substring(Math.Min(item.ReplaceSpan.iEndIndex, currentLine.Length - 1)));

            while ((currentLine = GetLine(ref currentPos, +1)) != null && botLines < NumericConstants.ContextLineRadius) {
                string lineText = currentLine.ToString();

                if (lineText.Trim().Length > 0) {
                    context.Append(Environment.NewLine + lineText);
                    if (lineText.Trim().Length > 1) botLines++;
                }
            }

            item.Context = context.ToString();
        }

        /// <summary>
        /// Returns text of the line after or before the given absolute index
        /// </summary>
        /// <param name="currentPos"></param>
        /// <param name="delta">+1 for next line, -1 for previous line</param>        
        private string GetLine(ref int currentPos, int delta) {
            if (delta == 0) {
                return GetTextOnLine(currentPos);
            } else {                
                while (currentPos >= 0 && currentPos < fileText.Length && fileText[currentPos] != '\n') {
                    currentPos += delta;
                }

                bool newline = false;
                if (currentPos >= 0 && currentPos < fileText.Length && fileText[currentPos] == '\n') newline = true;

                if (!newline) {
                    return null;
                } else {
                    currentPos += delta;
                    return GetTextOnLine(currentPos);
                }
            }
        }

        /// <summary>
        /// Returns line text on given absolute offset
        /// </summary>        
        private string GetTextOnLine(int currentPos) {
            int startIndex = -1;
            int endIndex = -1;
            int i = currentPos;

            while (i >= 0 && i < fileText.Length && fileText[i] != '\r') {
                i++;
            }
            endIndex = Math.Max(0, Math.Min(i, fileText.Length));

            i = currentPos;
            while (i < fileText.Length && i >= 0 && fileText[i] != '\n') {
                i--;
            }
            i++;

            startIndex = Math.Min(fileText.Length, Math.Max(i, 0));

            return fileText.Substring(startIndex, endIndex - startIndex);
        }


      
    }
}
