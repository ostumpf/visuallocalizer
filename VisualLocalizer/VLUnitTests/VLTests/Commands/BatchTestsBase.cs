using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VisualLocalizer.Components;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE;
using VisualLocalizer.Library;
using VisualLocalizer.Extensions;
using VisualLocalizer.Commands;
using VisualLocalizer.Gui;
using Microsoft.VisualStudio.Shell;
using VisualLocalizer.Library.AspxParser;

namespace VLUnitTests.VLTests {
    public abstract class BatchTestsBase {

        internal static void ValidateItems(AbstractResultItem a, AbstractResultItem b) {
            if (b is NetStringResultItem) {
                NetStringResultItem an = a as NetStringResultItem;
                NetStringResultItem bn = b as NetStringResultItem;
                Assert.AreEqual(an.VariableElementName, bn.VariableElementName, "Variable names are not equal, " + a.Value);
                Assert.AreEqual(an.MethodElementName, bn.MethodElementName, "Method names are not equal, " + a.Value);
            }
            if (b is CSharpStringResultItem) {
                TestCSharpStringResultItem an = a as TestCSharpStringResultItem;
                CSharpStringResultItem bn = b as CSharpStringResultItem;
                if (an.NamespaceElementName == null) {
                    Assert.IsNull(bn.NamespaceElement, "Namespace null, " + a.Value);
                } else {
                    Assert.AreEqual(an.NamespaceElementName, bn.NamespaceElement.FullName, "Namespace names are not equal, " + a.Value);
                }
            }
            if (b is VBStringResultItem) {
                TestVBStringResultItem an = a as TestVBStringResultItem;
                VBStringResultItem bn = b as VBStringResultItem;
                if (an.NamespaceElementName == null) {
                    Assert.IsNull(bn.NamespaceElement, "Namespace null, " + a.Value);
                } else {
                    Assert.AreEqual(an.NamespaceElementName, bn.NamespaceElement.FullName, "Namespace names are not equal, " + a.Value);
                }
            }
            if (b is AspNetStringResultItem) {
                TestAspNetStringResultItem an = a as TestAspNetStringResultItem;
                AspNetStringResultItem bn = b as AspNetStringResultItem;

                Assert.AreEqual(an.ComesFromClientComment, bn.ComesFromClientComment, "ComesFromClientComment are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
                Assert.AreEqual(an.ComesFromCodeBlock, bn.ComesFromCodeBlock, "ComesFromCodeBlock are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
                Assert.AreEqual(an.ComesFromDirective, bn.ComesFromDirective, "ComesFromDirective are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
                Assert.AreEqual(an.ComesFromElement, bn.ComesFromElement, "ComesFromElement are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
                Assert.AreEqual(an.ComesFromInlineExpression, bn.ComesFromInlineExpression, "ComesFromInlineExpression are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
                Assert.AreEqual(an.ComesFromPlainText, bn.ComesFromPlainText, "ComesFromPlainText are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
                Assert.AreEqual(an.ElementName, bn.ElementName, "ElementName are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
                Assert.AreEqual(an.ElementPrefix, bn.ElementPrefix, "ElementPrefix are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
                Assert.AreEqual(an.LocalizabilityProved, bn.LocalizabilityProved, "LocalizabilityProved are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
                Assert.AreEqual(an.Language, bn.Language, "Language are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
            }
            if (b is CodeStringResultItem) {
                CodeStringResultItem an = a as CodeStringResultItem;
                CodeStringResultItem bn = b as CodeStringResultItem;
                Assert.AreEqual(an.WasVerbatim, bn.WasVerbatim, "Verbatim options are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
                Assert.AreEqual(an.ClassOrStructElementName, bn.ClassOrStructElementName, "Class names are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);            
            }


            if (b is CodeReferenceResultItem) {
                CodeReferenceResultItem an = a as CodeReferenceResultItem;
                CodeReferenceResultItem bn = b as CodeReferenceResultItem;

                Assert.AreEqual(an.FullReferenceText, bn.FullReferenceText, "FullReferenceText are not equal" + " on line " + a.ReplaceSpan.iStartLine);
                Assert.AreEqual(an.OriginalReferenceText, bn.OriginalReferenceText, "OriginalReferenceText are not equal" + " on line " + a.ReplaceSpan.iStartLine);                
            }
            if (b is AspNetCodeReferenceResultItem) {
                AspNetCodeReferenceResultItem an = a as AspNetCodeReferenceResultItem;
                AspNetCodeReferenceResultItem bn = b as AspNetCodeReferenceResultItem;

                Assert.AreEqual(an.ComesFromInlineExpression, bn.ComesFromInlineExpression, "ComesFromInlineExpression are not equal" + " on line " + a.ReplaceSpan.iStartLine);
                Assert.AreEqual(an.ComesFromWebSiteResourceReference, bn.ComesFromWebSiteResourceReference, "ComesFromWebSiteResourceReference are not equal" + " on line " + a.ReplaceSpan.iStartLine);
                Assert.AreEqual(an.Language, bn.Language, "Language are not equal" + " on line " + a.ReplaceSpan.iStartLine);

                if (an.ComesFromInlineExpression) {
                    Assert.IsNotNull(an.InlineReplaceSpan);
                    Assert.IsNotNull(bn.InlineReplaceSpan);
                    Assert.AreEqual(an.InlineReplaceSpan.StartLine, bn.InlineReplaceSpan.StartLine, "StartLine are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
                    Assert.AreEqual(an.InlineReplaceSpan.StartIndex, bn.InlineReplaceSpan.StartIndex, "StartIndex are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
                    Assert.AreEqual(an.InlineReplaceSpan.EndLine, bn.InlineReplaceSpan.EndLine, "EndLine are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
                    Assert.AreEqual(an.InlineReplaceSpan.EndIndex, bn.InlineReplaceSpan.EndIndex, "EndIndex are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);

                    Assert.AreEqual(an.InlineReplaceSpan.AbsoluteCharOffset, bn.InlineReplaceSpan.AbsoluteCharOffset, "Offsets are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
                    Assert.AreEqual(an.InlineReplaceSpan.AbsoluteCharLength, bn.InlineReplaceSpan.AbsoluteCharLength, "AbsoluteLengths are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
                } else {
                    Assert.IsNull(an.InlineReplaceSpan);
                    Assert.IsNull(bn.InlineReplaceSpan);
                }
            }

            Assert.AreEqual(a.Value, b.Value, "Values are not equal on line " + a.ReplaceSpan.iStartLine);
            Assert.AreEqual(a.SourceItem, b.SourceItem, "Source items are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
            Assert.AreEqual(a.IsWithinLocalizableFalse, b.IsWithinLocalizableFalse, "[Localizable(false)] options are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
            Assert.AreEqual(a.IsMarkedWithUnlocalizableComment, b.IsMarkedWithUnlocalizableComment, "/*VL_NO_LOC*/ options are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
            Assert.AreEqual(a.ComesFromDesignerFile, b.ComesFromDesignerFile, "Designer file options are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
            
            Assert.IsNotNull(a.ReplaceSpan);
            Assert.IsNotNull(b.ReplaceSpan);
            Assert.AreEqual(a.ReplaceSpan.iStartLine, b.ReplaceSpan.iStartLine, "iStartLine are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
            Assert.AreEqual(a.ReplaceSpan.iStartIndex, b.ReplaceSpan.iStartIndex, "iStartIndex are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
            Assert.AreEqual(a.ReplaceSpan.iEndLine, b.ReplaceSpan.iEndLine, "iEndLine are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
            Assert.AreEqual(a.ReplaceSpan.iEndIndex, b.ReplaceSpan.iEndIndex, "iEndIndex are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);

            Assert.AreEqual(a.AbsoluteCharOffset, b.AbsoluteCharOffset, "Offsets are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);
            Assert.AreEqual(a.AbsoluteCharLength, b.AbsoluteCharLength, "AbsoluteLengths are not equal, " + a.Value + " on line " + a.ReplaceSpan.iStartLine);             
        }

        protected void ValidateResults<T1,T2>(List<T1> expected, List<T2> actual) 
            where T1 : AbstractResultItem
            where T2 : AbstractResultItem {
            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++) {
                ValidateItems(expected[i], actual[i]);
            }
        }

        protected static void CalculateAbsolutePosition(AbstractResultItem item) {
            IVsTextLines view = VLDocumentViewsManager.GetTextLinesForFile(item.SourceItem.GetFullPath(), true);
            object o;
            view.CreateTextPoint(item.ReplaceSpan.iStartLine, item.ReplaceSpan.iStartIndex, out o);
            TextPoint tp = (TextPoint)o;
            item.AbsoluteCharOffset = tp.AbsoluteCharOffset + item.ReplaceSpan.iStartLine - 1;

            view.CreateTextPoint(item.ReplaceSpan.iEndLine, item.ReplaceSpan.iEndIndex, out o);
            TextPoint tp2 = (TextPoint)o;
            item.AbsoluteCharLength = tp2.AbsoluteCharOffset + item.ReplaceSpan.iEndLine - 1 - item.AbsoluteCharOffset;            
        }

        protected static void CalculateAbsolutePosition(string path, BlockSpan span) {
            IVsTextLines view = VLDocumentViewsManager.GetTextLinesForFile(path, true);
            object o;
            view.CreateTextPoint(span.StartLine, span.StartIndex, out o);
            TextPoint tp = (TextPoint)o;
            span.AbsoluteCharOffset = tp.AbsoluteCharOffset + span.StartLine - 1;

            view.CreateTextPoint(span.EndLine, span.EndIndex, out o);
            TextPoint tp2 = (TextPoint)o;
            span.AbsoluteCharLength = tp2.AbsoluteCharOffset + span.EndLine - 1 - span.AbsoluteCharOffset;
        }

        protected static TextSpan CreateTextSpan(int startLine, int startColumn, int endLine, int endColumn) {
            TextSpan span = new TextSpan();
            span.iStartLine = startLine;
            span.iStartIndex = startColumn;
            span.iEndLine = endLine;
            span.iEndIndex = endColumn;
            return span;
        }

        protected static BlockSpan CreateBlockSpan(int startLine, int startColumn, int endLine, int endColumn) {
            BlockSpan span = new BlockSpan();            
            span.StartLine = startLine;
            span.StartIndex = startColumn;
            span.EndLine = endLine;
            span.EndIndex = endColumn;
            return span;
        }

        protected void GenericSelectionTest(AbstractBatchCommand_Accessor target, string file, IVsTextView view, IVsTextLines lines, Func<string, List<AbstractResultItem>> getExpected) {            
            int lineCount;
            lines.GetLineCount(out lineCount);
            Random rnd = new Random(1990);

            for (int i = 0; i < 20; i++) {
                int beginLine = rnd.Next(lineCount);
                int endLine = beginLine + rnd.Next(Math.Min(lineCount, beginLine + i) - beginLine);

                int beginLineLength, endLineLength;
                lines.GetLengthOfLine(beginLine, out beginLineLength);
                lines.GetLengthOfLine(endLine, out endLineLength);
                int beginColumn = rnd.Next(beginLineLength);
                int endColumn = beginLine == endLine ? beginColumn + (rnd.Next(Math.Min(endLineLength, beginColumn + i) - beginColumn)) : rnd.Next(endLineLength);
                if (beginLine == endLine && beginColumn == endColumn) endColumn++;

                view.SetSelection(beginLine, beginColumn, endLine, endColumn);
                target.InitializeSelection();

                List<AbstractResultItem> expectedList = new List<AbstractResultItem>();
                foreach (AbstractResultItem expected in getExpected(file)) {
                    if (!target.IsItemOutsideSelection(expected)) expectedList.Add(expected);
                }

                target.ProcessSelection(true);
                if (target is BatchMoveCommand_Accessor) {
                    ValidateResults(expectedList, (target as BatchMoveCommand_Accessor).Results);
                } else if (target is BatchInlineCommand_Accessor) {
                    ValidateResults(expectedList, (target as BatchInlineCommand_Accessor).Results);
                } else Assert.Fail("Unkown parent command type");

                Assert.IsTrue(expectedList.Count == 0 || VLDocumentViewsManager.IsFileLocked(file));

                VLDocumentViewsManager.ReleaseLocks();
                Assert.IsFalse(VLDocumentViewsManager.IsFileLocked(file));
            }

            VsShellUtilities.GetWindowObject(VLDocumentViewsManager.GetWindowFrameForFile(file, false)).Close(vsSaveChanges.vsSaveChangesNo);
        }

        protected void GenericTest(AbstractBatchCommand target, string[] itemsToSelect, string[] expectedFiles, Func<string, List<AbstractResultItem>> getExpected) {
            Agent.EnsureSolutionOpen();

            UIHierarchyItem[] selectedItems = new UIHierarchyItem[itemsToSelect.Length];
            for (int i = 0; i < itemsToSelect.Length; i++) {
                selectedItems[i] = Agent.FindUIHierarchyItem(Agent.GetUIHierarchy().UIHierarchyItems, itemsToSelect[i]);
                Assert.IsNotNull(selectedItems[i]);
            }
            
            target.Process(selectedItems, true);

            for (int i = 0; i < expectedFiles.Length; i++) {
                Assert.IsTrue(VLDocumentViewsManager.IsFileLocked(expectedFiles[i]));
            }

            List<AbstractResultItem> list = new List<AbstractResultItem>();
            for (int i = 0; i < expectedFiles.Length; i++) {
                list.AddRange(getExpected(expectedFiles[i]));
            }

            if (target is BatchMoveCommand) {
                ValidateResults(list, (target as BatchMoveCommand).Results);
            } else if (target is BatchInlineCommand) {
                ValidateResults(list, (target as BatchInlineCommand).Results);
            } else Assert.Fail("Unkown parent command type");

            VLDocumentViewsManager.ReleaseLocks();
            for (int i = 0; i < expectedFiles.Length; i++) {
                Assert.IsFalse(VLDocumentViewsManager.IsFileLocked(expectedFiles[i]));
            }
        }     

        internal class TestCSharpStringResultItem : CSharpStringResultItem {
            public string NamespaceElementName { get; set; }
        }

        internal class TestVBStringResultItem : CSharpStringResultItem {
            public string NamespaceElementName { get; set; }
        }

        internal class TestAspNetStringResultItem : AspNetStringResultItem {
            
        }
    }
}
