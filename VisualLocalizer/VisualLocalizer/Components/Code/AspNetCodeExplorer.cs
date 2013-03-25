using System;
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
using VisualLocalizer.Library.AspxParser;
using System.Reflection;
using System.ComponentModel;
using System.Collections;
using VisualLocalizer.Extensions;

namespace VisualLocalizer.Components {

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

        private AspNetCodeExplorer() { }

        private static AspNetCodeExplorer instance;
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

            fullPath = projectItem.GetFullPath();
            if (string.IsNullOrEmpty(fullPath)) throw new Exception("Cannot process item " + projectItem.Name);

            this.parentCommand = parentCommand;
            this.declaredNamespaces.Clear();
            this.ClassFileName = Path.GetFileNameWithoutExtension(fullPath);
            this.projectItem = projectItem;
            
            // initialize type resolver
            webConfig = new WebConfig(projectItem, VisualLocalizerPackage.Instance.DTE.Solution);
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

            webConfig.ClearCache();
        }

        /// <summary>
        /// Explores given file, using parent batch command's methods as callbacks. No scope limitation are passed to the parser.
        /// </summary>
        public void Explore(AbstractBatchCommand parentCommand, ProjectItem projectItem) {
            Explore(parentCommand, projectItem, int.MaxValue, int.MaxValue);
        }

        public bool StopRequested { get { return false;  } }

        /// <summary>
        /// Called after code block &lt;% %&gt;
        /// </summary> 
        public void OnCodeBlock(CodeBlockContext context) {
            context.InnerBlockSpan.Move(1, 1); // fix numbering

            // run parent command methods, adding found result items to results
            IList list = null;
            if (fileLanguage == FILETYPE.CSHARP) {
                list = parentCommand.LookupInCSharpAspNet(context.BlockText, context.InnerBlockSpan, declaredNamespaces, ClassFileName);
            } else if (fileLanguage == FILETYPE.VB) {
                list = parentCommand.LookupInVBAspNet(context.BlockText, context.InnerBlockSpan, declaredNamespaces, ClassFileName);
            }

            if (list != null) {
                foreach (AbstractResultItem item in list) {
                    AspNetStringResultItem aitem = item as AspNetStringResultItem;
                    if (aitem != null) {
                        aitem.ComesFromClientComment = context.WithinClientSideComment;
                        aitem.ComesFromCodeBlock = true;
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
                declaredNamespaces.Add(new UsedNamespaceItem(context.Attributes.Find((info) => { return info.Name == "Namespace"; }).Value, null));
            }
            if (context.DirectiveName == "Page") {
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
                        webConfig.AddTagPrefixDefinition(new TagPrefixAssemblyDefinition(assembly, nmspc, tagPrefix));
                    } else if (!string.IsNullOrEmpty(tagName) && !string.IsNullOrEmpty(src)) {
                        webConfig.AddTagPrefixDefinition(new TagPrefixSourceDefinition(projectItem.ContainingProject, 
                            VisualLocalizerPackage.Instance.DTE.Solution, tagName, src, tagPrefix));
                    }
                }
            }

            if (parentCommand is BatchMoveCommand) { // no resource references can be directly in the attributes, safe to look only for string literals
                foreach (AttributeInfo info in context.Attributes) {
                    if (info.ContainsAspTags) continue; // attribute's value contains &lt;%= - like tags - localization is not desirable

                    AspNetStringResultItem item = AddResult(info, null, context.DirectiveName, context.WithinClientSideComment, false, false, true);
                    if (item != null) item.ComesFromDirective = true;
                }
            }
        }

        /// <summary>
        /// Called after beginnnig tag is read
        /// </summary>    
        public void OnElementBegin(ElementContext context) {
            if (parentCommand is BatchMoveCommand) { // no resource references can be directly in the attributes, safe to look only for string literals
                foreach (var info in context.Attributes) {
                    if (info.ContainsAspTags) continue; // attribute's value contains &lt;%= - like tags - localization is not desirable
                    if (ShouldIgnoreThisAttribute(context.ElementName, info.Name)) continue; // attribute is not localizable

                    if (Settings.SettingsObject.Instance.UseReflectionInAsp) { // attempt to resolve type
                        PropertyInfo propInfo;                        
                        bool? isString = webConfig.IsTypeof(context.Prefix, context.ElementName, info.Name, typeof(string), out propInfo);

                        AspNetStringResultItem newItem = null;
                        if (isString == null || (isString.Value && !HasLocalizableFalse(propInfo))) { // add to results if resolution returned true or was not conclusive
                            newItem = AddResult(info, context, false);                            
                        }
                        if (newItem != null) newItem.LocalizabilityProved = isString.HasValue && isString.Value;
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
                }
            }
        }

        /// <summary>
        /// Called after end tag is read
        /// </summary>
        public void OnElementEnd(EndElementContext context) { }

        /// <summary>
        /// Returns true if given property is decorated with Localizable(false)
        /// </summary>        
        private bool HasLocalizableFalse(PropertyInfo propInfo) {
            if (propInfo == null) return false;

            object[] objects = propInfo.GetCustomAttributes(typeof(LocalizableAttribute), true);
            if (objects != null && objects.Length > 0) {
                bool hasFalse = false;
                foreach (LocalizableAttribute attr in objects)
                    if (!attr.IsLocalizable) hasFalse = true;

                return hasFalse;
            } else return false;
        }

        /// <summary>
        /// Adds new result item
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
            resultItem.Value = info.Value.ConvertAspNetEscapeSequences();
            resultItem.ReplaceSpan = span;
            resultItem.AbsoluteCharOffset = info.BlockSpan.AbsoluteCharOffset + (stripApos ? 2 : 0);
            resultItem.AbsoluteCharLength = info.Value.Length;
            resultItem.WasVerbatim = false;
            resultItem.IsWithinLocalizableFalse = propertyLocalizableFalse;
            resultItem.IsMarkedWithUnlocalizableComment = info.IsMarkedWithUnlocalizableComment;
            resultItem.ClassOrStructElementName = ClassFileName;
            resultItem.DeclaredNamespaces = declaredNamespaces;
            resultItem.ComesFromElement = comesFromElement;
            resultItem.ComesFromClientComment = comesFromClientComment;
            resultItem.ElementPrefix = elementPrefix;
            resultItem.ElementName = elementName;

            AddContextToItem(resultItem);

            bCmd.AddToResults(resultItem);

            return resultItem;
        }

        private AspNetStringResultItem AddResult(AttributeInfo info, ElementContext elementContext, bool propertyLocalizableFalse) {
            return AddResult(info, elementContext.Prefix, elementContext.ElementName, elementContext.WithinClientSideComment, propertyLocalizableFalse, true, true);
        }
    
        public void AddContextToItems(IEnumerable items) {
            foreach (AbstractResultItem item in items) {
                AddContextToItem(item);
            }
        }

        /// <summary>
        /// Adds few lines of code as a context to the result item
        /// </summary>        
        private void AddContextToItem(AbstractResultItem item) {
            if (!Settings.SettingsObject.Instance.ShowContextColumn) return;
            item.ContextRelativeLine = 0;

            int currentPos = item.AbsoluteCharOffset;
            string currentLine = GetLine(ref currentPos, 0); // current line's text
            currentLine = currentLine.Substring(0, item.ReplaceSpan.iStartIndex) + StringConstants.ContextSubstituteText;

            StringBuilder context = new StringBuilder();
            context.Append(currentLine.Trim());

            int topLines = 0;
            int botLines = 0;

            while ((currentLine = GetLine(ref currentPos, -1)) != null && topLines < NumericConstants.ContextLineRadius) {
                string lineText = currentLine.ToString().Trim();

                if (lineText.Length > 0) {
                    context.Insert(0, lineText + Environment.NewLine);
                    item.ContextRelativeLine++;
                    if (lineText.Length > 1) topLines++;
                }
            }

            currentPos = item.AbsoluteCharOffset+item.AbsoluteCharLength;
            currentLine = GetLine(ref currentPos, 0);
            context.Append(currentLine.Substring(item.ReplaceSpan.iEndIndex));

            while ((currentLine = GetLine(ref currentPos, +1)) != null && botLines < NumericConstants.ContextLineRadius) {
                string lineText = currentLine.ToString().Trim();

                if (lineText.Length > 0) {
                    context.Append(Environment.NewLine + lineText);
                    if (lineText.Length > 1) botLines++;
                }
            }

            item.Context = context.ToString();
        }

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

        private string GetTextOnLine(int currentPos) {
            int startIndex = -1;
            int endIndex = -1;
            int i = currentPos;

            while (i < fileText.Length && fileText[i] != '\r') {
                i++;
            }
            endIndex = Math.Min(i, fileText.Length);

            i = currentPos;
            while (i >= 0 && fileText[i] != '\n') {
                i--;
            }
            i++;

            startIndex = Math.Max(i, 0);

            return fileText.Substring(startIndex, endIndex - startIndex);
        }


        
        private sealed class StringIgnoreCaseComparer : IEqualityComparer<string> {
            static StringIgnoreCaseComparer() {
                Instance = new StringIgnoreCaseComparer();
            }

            public static StringIgnoreCaseComparer Instance {
                get;
                private set;
            }

            public bool Equals(string x, string y) {
                return string.Compare(x, y, true) == 0;
            }

            public int GetHashCode(string obj) {
                return obj.GetHashCode();
            }
        }
    }
}
