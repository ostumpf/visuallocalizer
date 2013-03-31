using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VisualLocalizer.Commands;
using EnvDTE80;
using VisualLocalizer.Components;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Diagnostics;

namespace VLUnitTests.VLTests {
    
    [TestClass()]
    public class MoveTest {

        [TestMethod()]
        [DeploymentItem("VisualLocalizer.dll")]
        public void AspNetMoveTest1() {
            Agent.EnsureSolutionOpen();

            AspNetMoveToResourcesCommand_Accessor target = new AspNetMoveToResourcesCommand_Accessor();

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.AspNetStringsTestFile1, true, true);
            IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(Agent.AspNetStringsTestFile1, false);
            var expected = AspNetBatchMoveTests.GetExpectedResultsFor(Agent.AspNetStringsTestFile1);

            RunTest(target, view, lines, expected);
        }

        [TestMethod()]
        [DeploymentItem("VisualLocalizer.dll")]
        public void AspNetMoveTest2() {
            Agent.EnsureSolutionOpen();

            AspNetMoveToResourcesCommand_Accessor target = new AspNetMoveToResourcesCommand_Accessor();

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.AspNetStringsTestFile2, true, true);
            IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(Agent.AspNetStringsTestFile2, false);
            var expected = AspNetBatchMoveTests.GetExpectedResultsFor(Agent.AspNetStringsTestFile2);

            RunTest(target, view, lines, expected);
        }

        [TestMethod()]
        [DeploymentItem("VisualLocalizer.dll")]        
        public void CSharpMoveTest() {
            Agent.EnsureSolutionOpen();
            
            CSharpMoveToResourcesCommand_Accessor target = new CSharpMoveToResourcesCommand_Accessor();                         

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.CSharpStringsTestFile1, true, true);            
            IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(Agent.CSharpStringsTestFile1, false);
            var expected = CSharpBatchMoveTest.GetExpectedResultsFor(Agent.CSharpStringsTestFile1);

            RunTest(target, view, lines, expected);
        }

        [TestMethod()]
        [DeploymentItem("VisualLocalizer.dll")]
        public void VBMoveTest() {
            Agent.EnsureSolutionOpen();

            VBMoveToResourcesCommand_Accessor target = new VBMoveToResourcesCommand_Accessor();

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.VBStringsTestFile1, true, true);
            IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(Agent.VBStringsTestFile1, false);
            var expected = VBBatchMoveTest.GetExpectedResultsFor(Agent.VBStringsTestFile1);

            RunTest(target, view, lines, expected);
        }

        protected void RunTest<T>(MoveToResourcesCommand_Accessor<T> target, IVsTextView view, IVsTextLines lines, List<AbstractResultItem> expectedList) where T : CodeStringResultItem,new() {
            Random rnd = new Random();
            target.InitializeVariables();

            foreach (AbstractResultItem expectedItem in expectedList) {
                Assert.IsTrue(expectedItem.ReplaceSpan.iStartLine >= 0);
                Assert.IsTrue(expectedItem.ReplaceSpan.iEndLine >= 0);

                for (int line = expectedItem.ReplaceSpan.iStartLine; line <= expectedItem.ReplaceSpan.iEndLine; line++) {
                    int begin;
                    int end;

                    if (line == expectedItem.ReplaceSpan.iStartLine) {
                        begin = expectedItem.ReplaceSpan.iStartIndex;
                    } else {
                        begin = 0;
                    }

                    if (line == expectedItem.ReplaceSpan.iEndLine) {
                        end = expectedItem.ReplaceSpan.iEndIndex;
                    } else {
                        lines.GetLengthOfLine(line, out end);
                    }

                    for (int column = begin; column <= end; column++) {
                        view.SetSelection(line, column, line, column);
                        var actualItem = target.GetReplaceStringItem();

                        Assert.IsNotNull(actualItem, "Actual item cannot be null");
                        actualItem.IsWithinLocalizableFalse = expectedItem.IsWithinLocalizableFalse; // can be ignored

                        BatchTestsBase.ValidateItems(expectedItem, actualItem);
                    }

                    for (int i = 0; i < 5; i++) {
                        int b = rnd.Next(begin, end + 1);
                        int e = rnd.Next(b, end + 1);
                        view.SetSelection(line, b, line, e);
                        var actualItem = target.GetReplaceStringItem();

                        actualItem.IsWithinLocalizableFalse = expectedItem.IsWithinLocalizableFalse; // can be ignored

                        BatchTestsBase.ValidateItems(expectedItem, actualItem);
                    }
                }

                if (expectedItem.ReplaceSpan.iStartIndex - 1 >= 0) {
                    view.SetSelection(expectedItem.ReplaceSpan.iStartLine, expectedItem.ReplaceSpan.iStartIndex - 1, expectedItem.ReplaceSpan.iStartLine, expectedItem.ReplaceSpan.iStartIndex - 1);
                    Assert.IsNull(target.GetReplaceStringItem(), "For item " + expectedItem.Value);
                }
                
                int lineLength;
                lines.GetLengthOfLine(expectedItem.ReplaceSpan.iEndLine, out lineLength);

                if (expectedItem.ReplaceSpan.iEndIndex + 1 <= lineLength) {
                    view.SetSelection(expectedItem.ReplaceSpan.iEndLine, expectedItem.ReplaceSpan.iEndIndex + 1, expectedItem.ReplaceSpan.iEndLine, expectedItem.ReplaceSpan.iEndIndex + 1);
                    Assert.IsNull(target.GetReplaceStringItem(), "For item " + expectedItem.Value);
                }
            }            
        }

    }
}
