using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VisualLocalizer.Components;
using VisualLocalizer.Commands;
using EnvDTE80;
using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell;

namespace VLUnitTests.VLTests {
    
    [TestClass()]
    public class VBBatchInlineTest : BatchTestsBase {

        private static Dictionary<string, List<AbstractResultItem>> validResults = new Dictionary<string, List<AbstractResultItem>>();

        public static List<AbstractResultItem> GetExpectedResultsFor(string file) {
            if (!validResults.ContainsKey(file)) {
                if (file == Agent.VBReferencesTestFile1) {
                    GenerateValidResultsForReferences1();
                } else throw new Exception("Cannot resolve test file name.");
            }
            return validResults[file];
        }

        [TestMethod()]
        public void ProcessSelectionTest() {
            Agent.EnsureSolutionOpen();

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.VBReferencesTestFile1, true, true);
            IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(Agent.VBReferencesTestFile1, false);

            GenericSelectionTest(Agent.BatchInlineCommand_Accessor, Agent.VBReferencesTestFile1, view, lines, GetExpectedResultsFor);
        }

        [TestMethod()]
        public void ProcessEmptySelectionTest() {
            Agent.EnsureSolutionOpen();

            DTE2 DTE = Agent.GetDTE();
            BatchInlineCommand_Accessor target = Agent.BatchInlineCommand_Accessor;

            Window window = DTE.OpenFile(null, Agent.VBReferencesTestFile1);
            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.VBReferencesTestFile1, false, true);
            view.SetSelection(28, 4, 35, 31);

            List<AbstractResultItem> emptyList = new List<AbstractResultItem>();
            target.ProcessSelection(true);
            ValidateResults(emptyList, target.Results);
            Assert.IsFalse(VLDocumentViewsManager.IsFileLocked(Agent.VBReferencesTestFile1));

            window.Detach();
            window.Close(vsSaveChanges.vsSaveChangesNo);
        }

        [TestMethod()]
        public void ProcessEmptySelectedItemsTest() {
            string[] itemsToSelect = { };
            string[] expectedFiles = { };

            GenericTest(Agent.BatchInlineCommand, itemsToSelect, expectedFiles, GetExpectedResultsFor);
        }

        [TestMethod()]
        public void ProcessSelectedItemsTest1() {
            string[] itemsToSelect = { Agent.VBReferencesTestFile1 }; 
            string[] expectedFiles = { Agent.VBReferencesTestFile1 }; 

            GenericTest(Agent.BatchInlineCommand, itemsToSelect, expectedFiles, GetExpectedResultsFor);
        }

        [TestMethod()]
        public void ProcessTest() {
            Agent.EnsureSolutionOpen();

            DTE2 DTE = Agent.GetDTE();
            BatchInlineCommand target = Agent.BatchInlineCommand;

            var window = DTE.OpenFile(null, Agent.VBReferencesTestFile1);
            window.Activate();

            target.Process(true);

            Assert.IsTrue(VLDocumentViewsManager.IsFileLocked(Agent.VBReferencesTestFile1));

            ValidateResults(GetExpectedResultsFor(Agent.VBReferencesTestFile1), target.Results);
            
            Assert.IsTrue(VLDocumentViewsManager.IsFileLocked(Agent.VBReferencesTestFile1));

            VLDocumentViewsManager.SetFileReadonly(Agent.VBReferencesTestFile1, false);
            Assert.IsFalse(VLDocumentViewsManager.IsFileLocked(Agent.VBReferencesTestFile1));

            window.Detach();
            window.Close(EnvDTE.vsSaveChanges.vsSaveChangesNo);
        }


        private static void GenerateValidResultsForReferences1() {
            List<AbstractResultItem> list = new List<AbstractResultItem>();
            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(Agent.VBReferencesTestFile1);

            list.Add(new VBCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                FullReferenceText = "VBTests.My.Resources.Resources.Key1",
                OriginalReferenceText = "VBTests.My.Resources.Resources.Key1",
                ReplaceSpan = CreateTextSpan(4, 28, 4, 63)
            });
            list.Add(new VBCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                FullReferenceText = "VBTests.My.Resources.Resources.Key1",
                OriginalReferenceText = "My.Resources.Key1",
                ReplaceSpan = CreateTextSpan(5, 28, 5, 45)
            });
            list.Add(new VBCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                FullReferenceText = "VBTests.My.Resources.Resources.Key1",
                OriginalReferenceText = "Resources.Key1",
                ReplaceSpan = CreateTextSpan(6, 28, 6, 42)
            });

            list.Add(new VBCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                FullReferenceText = "VBTests.My.Resources.Resources.Key1",
                OriginalReferenceText = "My.Resources.Key1",
                ReplaceSpan = CreateTextSpan(9, 28, 9, 70)
            });
            list.Add(new VBCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                FullReferenceText = "VBTests.My.Resources.Resources.Key1",
                OriginalReferenceText = "VBTests.My.Resources.Key1",
                ReplaceSpan = CreateTextSpan(10, 28, 10, 77)
            });
            list.Add(new VBCodeReferenceResultItem() {
                Value = "valueREM",
                SourceItem = projectItem,
                FullReferenceText = "VBTests.My.Resources.Resources._REM",
                OriginalReferenceText = "My.Resources._REM",
                ReplaceSpan = CreateTextSpan(13, 28, 13, 68)
            });
            list.Add(new VBCodeReferenceResultItem() {
                Value = "valueREM",
                SourceItem = projectItem,
                FullReferenceText = "VBTests.My.Resources.Resources._REM",
                OriginalReferenceText = "Resources._REM",
                ReplaceSpan = CreateTextSpan(14, 28, 14, 42)
            });
            list.Add(new VBCodeReferenceResultItem() {
                Value = "valueREM2",
                SourceItem = projectItem,
                FullReferenceText = "VBTests.My.Resources.Resources.REM_",
                OriginalReferenceText = "Resources.REM_",
                ReplaceSpan = CreateTextSpan(15, 28, 15, 42)
            });


            list.Add(new VBCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                FullReferenceText = "VBTests.My.Resources.Resources.Key1",
                OriginalReferenceText = "Resources.Key1",
                ReplaceSpan = CreateTextSpan(20, 28, 20, 42)
            });


            list.Add(new VBCodeReferenceResultItem() {
                Value = "value2",
                SourceItem = projectItem,
                FullReferenceText = "CSharpLib.Resource1.Key1",
                OriginalReferenceText = "CSharpLib.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(24, 28, 24, 52)
            });
            list.Add(new VBCodeReferenceResultItem() {
                Value = "valueLib",
                SourceItem = projectItem,
                FullReferenceText = "VBLib.My.Resources.Resource1.Key1",
                OriginalReferenceText = "VBLib.My.Resources.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(25, 28, 25, 61)
            });
            list.Add(new VBCodeReferenceResultItem() {
                Value = "valueVB",
                SourceItem = projectItem,
                FullReferenceText = "VBTests.My.Resources.Resource1.Key1",
                OriginalReferenceText = "My.Resources.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(26, 28, 26, 55)
            });
            list.Add(new VBCodeReferenceResultItem() {
                Value = "valueVB",
                SourceItem = projectItem,
                FullReferenceText = "VBTests.My.Resources.Resource1.Key1",
                OriginalReferenceText = "Resources.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(27, 28, 27, 52)
            });


            list.Add(new VBCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                FullReferenceText = "VBTests.My.Resources.Resources.Key1",
                OriginalReferenceText = "VBTests.My.Resources.Key1",
                ReplaceSpan = CreateTextSpan(35, 32, 35, 57)
            });
            list.Add(new VBCodeReferenceResultItem() {
                Value = "value2",
                SourceItem = projectItem,
                FullReferenceText = "CSharpLib.Resource1.Key1",
                OriginalReferenceText = "Resource1.Key1",
                ReplaceSpan = CreateTextSpan(36, 32, 36, 46)
            });
            list.Add(new VBCodeReferenceResultItem() {
                Value = "test\"test\"\"",
                SourceItem = projectItem,
                FullReferenceText = "VBTests.My.Resources.Resource1.Key2",
                OriginalReferenceText = "VBTests.My.Resources.Resource1.Key2",
                ReplaceSpan = CreateTextSpan(37, 32, 37, 67)
            });
            list.Add(new VBCodeReferenceResultItem() {
                Value = "a" + Environment.NewLine + "b" + Environment.NewLine + "c" + Environment.NewLine + "d",
                SourceItem = projectItem,
                FullReferenceText = "VBTests.My.Resources.Resource1.Key3",
                OriginalReferenceText = "VBTests.My.Resources.Resource1.Key3",
                ReplaceSpan = CreateTextSpan(38, 32, 38, 67)
            });


            list.Add(new VBCodeReferenceResultItem() {
                Value = "valueVB",
                SourceItem = projectItem,
                FullReferenceText = "VBTests.My.Resources.Resource1.Key1",
                OriginalReferenceText = "VBTests.My.Resources.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(40, 32, 42, 27)
            });
            list.Add(new VBCodeReferenceResultItem() {
                Value = "valueVB",
                SourceItem = projectItem,
                FullReferenceText = "VBTests.My.Resources.Resource1.Key1",
                OriginalReferenceText = "VBTests.My.Resources.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(45, 12, 49, 15)
            });
            list.Add(new VBCodeReferenceResultItem() {
                Value = "valueREM",
                SourceItem = projectItem,
                FullReferenceText = "VBTests.My.Resources.Resources._REM",
                OriginalReferenceText = "My.Resources._REM",
                ReplaceSpan = CreateTextSpan(51, 32, 52, 16)
            });

            list.Add(new VBCodeReferenceResultItem() {
                Value = "valueVB",
                SourceItem = projectItem,
                FullReferenceText = "VBTests.My.Resources.Resource1.Key1",
                OriginalReferenceText = "VBTests.My.Resources.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(54, 32, 57, 4)
            });

            foreach (var item in list) CalculateAbsolutePosition(item);
            validResults.Add(Agent.VBReferencesTestFile1, list);
        }
    }
}
