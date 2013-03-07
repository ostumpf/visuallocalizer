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
    internal sealed class AspNetCodeExplorer : IAspxHandler {

        private AbstractBatchCommand parentCommand;
        private NamespacesList declaredNamespaces = new NamespacesList();
        private string ClassFileName = null;
        private Dictionary<string, Type> typesCache = new Dictionary<string, Type>();
        private string fileText, fullPath;
        private WebConfig webConfig;
        private ProjectItem projectItem;
        private FILETYPE fileLanguage;

        private AspNetCodeExplorer() { }

        private static AspNetCodeExplorer instance;
        public static AspNetCodeExplorer Instance {
            get {                
                if (instance == null) instance = new AspNetCodeExplorer();
                return instance;
            }
        }

        public void Explore(AbstractBatchCommand parentCommand, ProjectItem projectItem, int maxLine, int maxIndex) {
            fullPath = projectItem.GetFullPath();
            if (string.IsNullOrEmpty(fullPath)) throw new Exception("Cannot process item " + projectItem.Name);

            this.parentCommand = parentCommand;
            this.declaredNamespaces.Clear();
            this.ClassFileName = Path.GetFileNameWithoutExtension(fullPath);
            this.projectItem = projectItem;
            
            webConfig = new WebConfig(projectItem, VisualLocalizerPackage.Instance.DTE.Solution);
            fileText = null;

            if (RDTManager.IsFileOpen(fullPath)) {
                var textLines = VLDocumentViewsManager.GetTextLinesForFile(fullPath, false);
                if (textLines == null) return;

                int lastLine, lastLineIndex;
                int hr = textLines.GetLastLineIndex(out lastLine, out lastLineIndex);
                Marshal.ThrowExceptionForHR(hr);

                hr = textLines.GetLineText(0, 0, lastLine, lastLineIndex, out fileText);
                Marshal.ThrowExceptionForHR(hr);
            } else {
                fileText = File.ReadAllText(fullPath);
            }

            Parser parser = new Parser(fileText, this, maxLine, maxIndex);
            parser.Process();

            webConfig.ClearCache();
        }

        public void Explore(AbstractBatchCommand parentCommand, ProjectItem projectItem) {
            Explore(parentCommand, projectItem, int.MaxValue, int.MaxValue);
        }

        public bool StopRequested { get { return false;  } }

        public void OnCodeBlock(CodeBlockContext context) {
            context.InnerBlockSpan.Move(1, 1);

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

        public void OnPageDirective(DirectiveContext context) {
            if (context.DirectiveName == "Import" && context.Attributes.Exists((info) => { return info.Name == "Namespace"; })) {
                declaredNamespaces.Add(new UsedNamespaceItem(context.Attributes.Find((info) => { return info.Name == "Namespace"; }).Value, null));
            }
            if (context.DirectiveName == "Page") {
                string lang = null;
                string ext = null;
                foreach (var info in context.Attributes) {
                    if (info.Name == "Language") lang = info.Value;
                    if (info.Name == "CodeFile") {
                        int index = info.Value.LastIndexOf('.');
                        if (index != -1 && index + 1 < info.Value.Length) {
                            ext = info.Value.Substring(index + 1);
                        }
                    }                    
                }
                if (string.IsNullOrEmpty(lang)) {
                    if (!string.IsNullOrEmpty(ext)) {
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
                if (!string.IsNullOrEmpty(tagPrefix)) {
                    if (!string.IsNullOrEmpty(assembly) && !string.IsNullOrEmpty(nmspc)) {
                        webConfig.AddTagPrefixDefinition(new TagPrefixAssemblyDefinition(assembly, nmspc, tagPrefix));
                    } else if (!string.IsNullOrEmpty(tagName) && !string.IsNullOrEmpty(src)) {
                        webConfig.AddTagPrefixDefinition(new TagPrefixSourceDefinition(projectItem.ContainingProject, 
                            VisualLocalizerPackage.Instance.DTE.Solution, tagName, src, tagPrefix));
                    }
                }
            }

            if (parentCommand is BatchMoveCommand) {
                foreach (AttributeInfo info in context.Attributes) {
                    if (info.ContainsAspTags) continue;

                    AspNetStringResultItem item = AddResult(info, null, context.DirectiveName, context.WithinClientSideComment, false, false, true);
                    if (item != null) item.ComesFromDirective = true;
                }
            }
        }

        public void OnElementBegin(ElementContext context) {
            if (parentCommand is BatchMoveCommand) {
                foreach (var info in context.Attributes) {
                    if (info.ContainsAspTags) continue;
                    if (shouldIgnoreThisAttribute(context.ElementName, info.Name)) continue;

                    if (Settings.SettingsObject.Instance.UseReflectionInAsp) {
                        PropertyInfo propInfo;
                        bool? isString = webConfig.IsTypeof(context.Prefix, context.ElementName, info.Name, typeof(string), out propInfo);

                        AspNetStringResultItem newItem = null;
                        if (isString == null || isString.Value) {
                            newItem = AddResult(info, context, false);                            
                        }
                        if (newItem != null) newItem.LocalizabilityProved = isString.HasValue && isString.Value;
                    } else {
                        AddResult(info, context, false);
                    }                    
                }
            }
        }

        private bool shouldIgnoreThisAttribute(string elementName, string attributeName) {
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

        public void OnOutputElement(OutputElementContext context) {
            IList list = null;

            if (context.Kind == OutputElementKind.HTML_ESCAPED || context.Kind == OutputElementKind.PLAIN) {
                context.InnerBlockSpan.Move(1, 1);
                if (fileLanguage == FILETYPE.CSHARP) {
                    list = parentCommand.LookupInCSharpAspNet(context.InnerText, context.InnerBlockSpan, declaredNamespaces, ClassFileName);
                } else if (fileLanguage == FILETYPE.VB) {
                    list = parentCommand.LookupInVBAspNet(context.InnerText, context.InnerBlockSpan, declaredNamespaces, ClassFileName);
                }
            } else {
                if (parentCommand is BatchInlineCommand) {
                    list = ((BatchInlineCommand)parentCommand).ParseResourceExpression(context.InnerText, context.InnerBlockSpan);
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

        public void OnPlainText(PlainTextContext context) {
            if (parentCommand is BatchMoveCommand) {
                var newItem = AddResult(new AttributeInfo() { BlockSpan = context.BlockSpan, Name = context.Text, Value = context.Text },
                    null, null, context.WithinClientSideComment, false, false, false);
                if (newItem != null) {
                    newItem.ComesFromPlainText = true;
                    newItem.ComesFromElement = false;
                }
            }
        }

        public void OnElementEnd(EndElementContext context) { }

        private bool HasLocalizableFalse(PropertyInfo propInfo) {
            object[] objects = propInfo.GetCustomAttributes(typeof(LocalizableAttribute), true);
            if (objects != null && objects.Length > 0) {
                bool hasFalse = false;
                foreach (LocalizableAttribute attr in objects)
                    if (!attr.IsLocalizable) hasFalse = true;

                return hasFalse;
            } else return false;
        }

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
            resultItem.Value = info.Value;
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

        private void AddContextToItem(AbstractResultItem item) {
            if (!Settings.SettingsObject.Instance.ShowFilterContext) return;
            item.ContextRelativeLine = 0;

            int currentPos = item.AbsoluteCharOffset;
            string currentLine = GetLine(ref currentPos, 0);
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
