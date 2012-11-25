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
using VisualLocalizer.Components.AspxParser;
using System.Reflection;
using System.ComponentModel;
using System.Collections;

namespace VisualLocalizer.Components {
    internal sealed class AspNetCodeExplorer : IAspxHandler {

        private AbstractBatchCommand parentCommand;
        private NamespacesList declaredNamespaces = new NamespacesList();
        private string ClassFileName = null;
        private Dictionary<string, Type> typesCache = new Dictionary<string, Type>();
        private string fileText, fullPath;        

        private AspNetCodeExplorer() { }

        private static AspNetCodeExplorer instance;
        public static AspNetCodeExplorer Instance {
            get {
                if (instance == null) instance = new AspNetCodeExplorer();
                return instance;
            }
        }

        public void Explore(AbstractBatchCommand parentCommand, ProjectItem projectItem, WebConfig webConfig, int maxLine, int maxIndex) {
            fullPath = (string)projectItem.Properties.Item("FullPath").Value;
            if (string.IsNullOrEmpty(fullPath)) throw new Exception("Cannot process item " + projectItem.Name);

            this.parentCommand = parentCommand;
            this.declaredNamespaces.Clear();
            this.ClassFileName = Path.GetFileNameWithoutExtension(fullPath);

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
        }

        public void Explore(AbstractBatchCommand parentCommand, ProjectItem projectItem, WebConfig webConfig) {
            Explore(parentCommand, projectItem, webConfig, int.MaxValue, int.MaxValue);
        }

        public void OnCodeBlock(CodeBlockContext context) {
            context.InnerBlockSpan.Move(1, 1);
            
            IList list = parentCommand.LookupInAspNet(context.BlockText, context.InnerBlockSpan, declaredNamespaces, ClassFileName);
            
            foreach (AbstractResultItem item in list)
                item.ComesFromClientComment = context.WithinClientSideComment;

            AddContextToItems((IEnumerable)list);
        }

        public void OnPageDirective(DirectiveContext context) {
            if (context.DirectiveName == "Import" && context.Attributes.Exists((info) => { return info.Name == "Namespace"; })) {
                declaredNamespaces.Add(new UsedNamespaceItem(context.Attributes.Find((info) => { return info.Name == "Namespace"; }).Value, null));
            }

            foreach (AttributeInfo info in context.Attributes) {
                if (info.ContainsAspTags) continue;

                AddResult(info, context.BlockSpan, context.WithinClientSideComment, false);
            }
        }

        public void OnElementBegin(ElementContext context) {
            if (parentCommand is BatchMoveCommand) {
                if (context.Prefix == "asp") {
                    if (!typesCache.ContainsKey(context.ElementName)) {
                        Type type = Type.GetType(string.Format(StringConstants.AspAssemblyQualifiedNameFormat, context.ElementName), false, true);
                        typesCache.Add(context.ElementName, type);
                    }
                    Type elementType = typesCache[context.ElementName];

                    if (elementType != null) {
                        foreach (var info in context.Attributes) {
                            if (info.ContainsAspTags) continue;
                            if (StringConstants.AspUnlocalizableAttributes.Contains(info.Name, StringIgnoreCaseComparer.Instance)) continue;

                            PropertyInfo propInfo = elementType.GetProperty(info.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                            if (propInfo != null && propInfo.PropertyType == typeof(string)) {
                                AddResult(info, context.BlockSpan, context.WithinClientSideComment, HasLocalizableFalse(propInfo));
                            }
                        }
                    }
                } else {
                    foreach (var info in context.Attributes) {
                        if (info.ContainsAspTags) continue;
                        
                        AddResult(info, context.BlockSpan, context.WithinClientSideComment, false);                        
                    }
                }
            }
        }

        public void OnOutputElement(OutputElementContext context) {
            IList list = null;

            switch (context.Kind) {
                case OutputElementKind.PLAIN:
                    context.InnerBlockSpan.Move(1, 1);
                    list = parentCommand.LookupInAspNet(context.InnerText, context.InnerBlockSpan, declaredNamespaces, ClassFileName);
                    break;
                case OutputElementKind.HTML_ESCAPED:
                    context.InnerBlockSpan.Move(1, 1);
                    list = parentCommand.LookupInAspNet(context.InnerText, context.InnerBlockSpan, declaredNamespaces, ClassFileName);
                    break;
                case OutputElementKind.EXPRESSION:
                    if (parentCommand is BatchInlineCommand) {
                        list = ((BatchInlineCommand)parentCommand).ParseResourceExpression(context.InnerText, context.InnerBlockSpan);
                    }
                    break;
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
        }

        public void OnElementEnd(EndElementContext context) {
        }


        private bool HasLocalizableFalse(PropertyInfo propInfo) {
            object[] objects = propInfo.GetCustomAttributes(typeof(LocalizableAttribute), true);
            if (objects != null && objects.Length > 0) {
                bool hasFalse = false;
                foreach (LocalizableAttribute attr in objects)
                    if (!attr.IsLocalizable) hasFalse = true;

                return hasFalse;
            } else return false;
        }

        private void AddResult(AttributeInfo info, BlockSpan elementSpan, bool withinClientSideComment, bool propertyLocalizableFalse) {
            if (!(parentCommand is BatchMoveCommand)) return;

            BatchMoveCommand bCmd = (BatchMoveCommand)parentCommand;
            info.BlockSpan.Move(1, 0);

            TextSpan span = new TextSpan();            
            span.iStartLine = info.BlockSpan.StartLine;
            span.iStartIndex = info.BlockSpan.StartIndex + 1;
            span.iEndLine = info.BlockSpan.EndLine;
            span.iEndIndex = info.BlockSpan.EndIndex - 1;

            AspNetStringResultItem resultItem = new AspNetStringResultItem();
            resultItem.Value = info.Value;            
            resultItem.ReplaceSpan = span;
            resultItem.AbsoluteCharOffset = info.BlockSpan.AbsoluteCharOffset + 2;
            resultItem.AbsoluteCharLength = info.Value.Length;
            resultItem.WasVerbatim = false;
            resultItem.IsWithinLocalizableFalse = propertyLocalizableFalse;
            resultItem.IsMarkedWithUnlocalizableComment = info.IsMarkedWithUnlocalizableComment;
            resultItem.ClassOrStructElementName = ClassFileName;
            resultItem.DeclaredNamespaces = declaredNamespaces;
            resultItem.ComesFromElement = true;
            resultItem.ComesFromClientComment = withinClientSideComment;            

            AddContextToItem(resultItem);

            bCmd.AddToResults(resultItem);
        }
    
        public void AddContextToItems(IEnumerable items) {
            foreach (AbstractResultItem item in items) {
                AddContextToItem(item);
            }
        }

        private void AddContextToItem(AbstractResultItem item) {
            item.ContextRelativeLine = 0;

            int currentPos = item.AbsoluteCharOffset;
            string currentLine = GetLine(ref currentPos, 0);
            currentLine = currentLine.Substring(0, item.ReplaceSpan.iStartIndex) + StringConstants.ContextSubstituteText + currentLine.Substring(item.ReplaceSpan.iEndIndex);

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

            currentPos = item.AbsoluteCharOffset;

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
