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
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using VisualLocalizer.Components.Code;
using VisualLocalizer.Commands.Move;

namespace VLUnitTests.VLTests {
    
    /// <summary>
    /// Tests for ad-hoc version of the "move to resources" command.
    /// </summary>
    [TestClass()]
    public class MoveTest {

        /// <summary>
        /// Tests ASP .NET (C# variant) files.
        /// </summary>
        [TestMethod()]
        [DeploymentItem("VisualLocalizer.dll")]
        public void AspNetMoveTest1() {
            Agent.EnsureSolutionOpen();

            AspNetMoveToResourcesCommand_Accessor target = new AspNetMoveToResourcesCommand_Accessor();
            Window window = Agent.GetDTE().OpenFile(null, Agent.AspNetStringsTestFile1);

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.AspNetStringsTestFile1, true, true);
            IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(Agent.AspNetStringsTestFile1, false);
            var expected = AspNetBatchMoveTests.GetExpectedResultsFor(Agent.AspNetStringsTestFile1);

            RunTest(target, view, lines, expected);

            window.Detach();
            window.Close(vsSaveChanges.vsSaveChangesNo);
        }

        /// <summary>
        /// Tests ASP .NET (VB variant) files.
        /// </summary>
        [TestMethod()]
        [DeploymentItem("VisualLocalizer.dll")]
        public void AspNetMoveTest2() {
            Agent.EnsureSolutionOpen();

            AspNetMoveToResourcesCommand_Accessor target = new AspNetMoveToResourcesCommand_Accessor();
            Window window = Agent.GetDTE().OpenFile(null, Agent.AspNetStringsTestFile2);

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.AspNetStringsTestFile2, true, true);
            IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(Agent.AspNetStringsTestFile2, false);
            var expected = AspNetBatchMoveTests.GetExpectedResultsFor(Agent.AspNetStringsTestFile2);

            RunTest(target, view, lines, expected);

            window.Detach();
            window.Close(vsSaveChanges.vsSaveChangesNo);
        }

        /// <summary>
        /// Tests C# files.
        /// </summary>
        [TestMethod()]
        [DeploymentItem("VisualLocalizer.dll")]        
        public void CSharpMoveTest() {
            Agent.EnsureSolutionOpen();
            
            CSharpMoveToResourcesCommand_Accessor target = new CSharpMoveToResourcesCommand_Accessor();
            Window window = Agent.GetDTE().OpenFile(null, Agent.CSharpStringsTestFile1);

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.CSharpStringsTestFile1, true, true);            
            IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(Agent.CSharpStringsTestFile1, false);
            var expected = CSharpBatchMoveTest.GetExpectedResultsFor(Agent.CSharpStringsTestFile1);

            RunTest(target, view, lines, expected);

            window.Detach();
            window.Close(vsSaveChanges.vsSaveChangesNo);
        }

        /// <summary>
        /// Tests VB files.
        /// </summary>
        [TestMethod()]
        [DeploymentItem("VisualLocalizer.dll")]
        public void VBMoveTest() {
            Agent.EnsureSolutionOpen();

            VBMoveToResourcesCommand_Accessor target = new VBMoveToResourcesCommand_Accessor();
            Window window = Agent.GetDTE().OpenFile(null, Agent.VBStringsTestFile1);

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.VBStringsTestFile1, true, true);
            IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(Agent.VBStringsTestFile1, false);
            var expected = VBBatchMoveTest.GetExpectedResultsFor(Agent.VBStringsTestFile1);

            RunTest(target, view, lines, expected);

            window.Detach();
            window.Close(vsSaveChanges.vsSaveChangesNo);
        }

        /// <summary>
        /// Generic test for the ad-hoc move commands.
        /// </summary>
        /// <typeparam name="T">Type of expected result item</typeparam>
        /// <param name="target">Target command</param>
        /// <param name="view"></param>
        /// <param name="lines"></param>
        /// <param name="expectedList">List of expected results</param>
        protected void RunTest<T>(MoveToResourcesCommand_Accessor<T> target, IVsTextView view, IVsTextLines lines, List<AbstractResultItem> expectedList) where T : AbstractResultItem,new() {
            Random rnd = new Random();
            target.InitializeVariables();

            // simulate right-click around each of expected result items and verify that move command reacts
            foreach (AbstractResultItem expectedItem in expectedList) {
                Assert.IsTrue(expectedItem.ReplaceSpan.iStartLine >= 0);
                Assert.IsTrue(expectedItem.ReplaceSpan.iEndLine >= 0);

                // each result item will be clicked at every its characted
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
                        // perform the click
                        view.SetSelection(line, column, line, column);

                        // execute the command
                        var actualItem = target.GetReplaceStringItem();

                        Assert.IsNotNull(actualItem, "Actual item cannot be null");
                        actualItem.IsWithinLocalizableFalse = expectedItem.IsWithinLocalizableFalse; // can be ignored

                        // compare results
                        BatchTestsBase.ValidateItems(expectedItem, actualItem);
                    }

                    // try selecting random block of code within the item
                    for (int i = 0; i < 5; i++) {
                        int b = rnd.Next(begin, end + 1);
                        int e = rnd.Next(b, end + 1);
                        view.SetSelection(line, b, line, e);
                        var actualItem = target.GetReplaceStringItem();

                        actualItem.IsWithinLocalizableFalse = expectedItem.IsWithinLocalizableFalse; // can be ignored

                        BatchTestsBase.ValidateItems(expectedItem, actualItem);
                    }
                }

                // simulate clicks out of the result item and verify null results
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
