using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VisualLocalizer.Components;
using EnvDTE;
using EnvDTE80;
using VisualLocalizer.Commands;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell;
using VisualLocalizer.Components.Code;
using VisualLocalizer.Commands.Inline;

namespace VLUnitTests.VLTests {

    /// <summary>
    /// Tests for C# batch inline command.
    /// </summary>
    [TestClass()]
    public class CSharpBatchInlineTest : BatchTestsBase {

        private static Dictionary<string, List<AbstractResultItem>> validResults = new Dictionary<string, List<AbstractResultItem>>();

        public static List<AbstractResultItem> GetExpectedResultsFor(string file) {
            if (!validResults.ContainsKey(file)) {
                if (file == Agent.CSharpReferencesTestFile1) {
                    GenerateValidResultsForReferences1();
                } else if (file == Agent.CSharpReferencesTestFile2) {
                    GenerateValidResultsForReferences2();
                } else throw new Exception("Cannot resolve test file name.");
            }
            return validResults[file];
        }

        [TestMethod()]
        public void ProcessSelectionTest() {
            Agent.EnsureSolutionOpen();

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.CSharpReferencesTestFile1, true, true);
            IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(Agent.CSharpReferencesTestFile1, false);

            GenericSelectionTest(Agent.BatchInlineCommand_Accessor, Agent.CSharpReferencesTestFile1, view, lines, GetExpectedResultsFor);
        }

        [TestMethod()]
        public void ProcessEmptySelectionTest() {
            Agent.EnsureSolutionOpen();

            DTE2 DTE = Agent.GetDTE();
            BatchInlineCommand_Accessor target = Agent.BatchInlineCommand_Accessor;
            Window window = DTE.OpenFile(null, Agent.CSharpStringsTestFile1);

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.CSharpReferencesTestFile1, true, true);
            view.SetSelection(35, 42, 45, 22);

            List<AbstractResultItem> emptyList = new List<AbstractResultItem>();
            target.ProcessSelection(true);
            ValidateResults(emptyList, target.Results);
            Assert.IsFalse(VLDocumentViewsManager.IsFileLocked(Agent.CSharpReferencesTestFile1));

            window.Detach();
            window.Close(vsSaveChanges.vsSaveChangesNo);
        }

        [TestMethod()]
        public void ProcessSelectedItemsTest1() {
            string[] itemsToSelect = { Agent.CSharpReferencesTestFile1, Agent.CSharpReferencesTestFile2 };
            string[] expectedFiles = { Agent.CSharpReferencesTestFile1, Agent.CSharpReferencesTestFile2 };

            GenericTest(Agent.BatchInlineCommand, itemsToSelect, expectedFiles, GetExpectedResultsFor);
        }

        [TestMethod()]
        public void ProcessSelectedItemsTest2() {
            string[] itemsToSelect = { Agent.CSharpStringsTestProject };
            string[] expectedFiles = { Agent.CSharpReferencesTestFile1, Agent.CSharpReferencesTestFile2 };

            GenericTest(Agent.BatchInlineCommand, itemsToSelect, expectedFiles, GetExpectedResultsFor);
        }

        [TestMethod()]
        public void ProcessTest() {
            Agent.EnsureSolutionOpen();

            DTE2 DTE = Agent.GetDTE();
            BatchInlineCommand target = Agent.BatchInlineCommand;

            var window = DTE.OpenFile(null, Agent.CSharpReferencesTestFile1);
            window.Activate();

            target.Process(true);

            Assert.IsTrue(VLDocumentViewsManager.IsFileLocked(Agent.CSharpReferencesTestFile1));

            ValidateResults(GetExpectedResultsFor(Agent.CSharpReferencesTestFile1), target.Results);
            
            Assert.IsTrue(VLDocumentViewsManager.IsFileLocked(Agent.CSharpReferencesTestFile1));

            VLDocumentViewsManager.SetFileReadonly(Agent.CSharpReferencesTestFile1, false);
            Assert.IsFalse(VLDocumentViewsManager.IsFileLocked(Agent.CSharpReferencesTestFile1));

            window.Detach();
            window.Close(EnvDTE.vsSaveChanges.vsSaveChangesNo);
        }

        private static void GenerateValidResultsForReferences1() {
            List<AbstractResultItem> list = new List<AbstractResultItem>();
            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(Agent.CSharpReferencesTestFile1);

            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value3",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key1",
                OriginalReferenceText = "Resource1.Key1",
                ReplaceSpan = CreateTextSpan(7, 29, 7, 43)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value3",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key1",
                OriginalReferenceText = "Resource1.Key1",
                ReplaceSpan = CreateTextSpan(8, 29, 8, 61)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value3",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key1",
                OriginalReferenceText = "Resource1.Key1",
                ReplaceSpan = CreateTextSpan(11, 12, 15, 16)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value3",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key1",
                OriginalReferenceText = "Resource1.Key1",
                ReplaceSpan = CreateTextSpan(19, 0, 21, 4)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value3",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key1",
                OriginalReferenceText = "Resource1.Key1",
                ReplaceSpan = CreateTextSpan(23, 28, 23, 42)
            });


            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value11",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key11",
                OriginalReferenceText = "Resource1.Key11",
                ReplaceSpan = CreateTextSpan(24, 28, 24, 43)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value11",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key11",
                OriginalReferenceText = "Resource1.Key11",
                ReplaceSpan = CreateTextSpan(27, 25, 27, 40)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value3",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key1",
                OriginalReferenceText = "Resource1.Key1",
                ReplaceSpan = CreateTextSpan(29, 16, 31, 20)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value3",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key1",
                OriginalReferenceText = "Resource1.Key1",
                ReplaceSpan = CreateTextSpan(33, 43, 33, 57)
            });


            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value11",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key11",
                OriginalReferenceText = "Resource1.Key11",
                ReplaceSpan = CreateTextSpan(35, 26, 35, 41)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value3",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key1",
                OriginalReferenceText = "CSharpTests.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(45, 23, 45, 54)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value3",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key1",
                OriginalReferenceText = "Resource1.Key1",
                ReplaceSpan = CreateTextSpan(48, 34, 48, 48)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value11",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key11",
                OriginalReferenceText = "Resource1.Key11",
                ReplaceSpan = CreateTextSpan(49, 34, 49, 49)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value11",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key11",
                OriginalReferenceText = "Resource1.Key11",
                ReplaceSpan = CreateTextSpan(50, 34, 52, 25)
            });

            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value3",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key1",
                OriginalReferenceText = "Resource1.Key1",
                ReplaceSpan = CreateTextSpan(59, 0, 59, 70)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value3",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key1",
                OriginalReferenceText = "CSharpTests.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(65, 34, 65, 60)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value3",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key1",
                OriginalReferenceText = "CSharpTests.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(67, 0, 72, 4)
            });


            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value2",
                SourceItem = projectItem,
                FullReferenceText = "CSharpLib.Resource1.Key1",
                OriginalReferenceText = "CSharpLib.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(82, 25, 82, 49)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value3",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key1",
                OriginalReferenceText = "CSharpTests.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(83, 25, 83, 51)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Properties.Resources.Key1",
                OriginalReferenceText = "CSharpTests.Properties.Resources.Key1",
                ReplaceSpan = CreateTextSpan(84, 25, 84, 62)
            });


            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value2",
                SourceItem = projectItem,
                FullReferenceText = "CSharpLib.Resource1.Key1",
                OriginalReferenceText = "CSharpLib.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(90, 29, 90, 53)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value2",
                SourceItem = projectItem,
                FullReferenceText = "CSharpLib.Resource1.Key1",
                OriginalReferenceText = "Resource1.Key1",
                ReplaceSpan = CreateTextSpan(91, 29, 91, 43)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Properties.Resources.Key1",
                OriginalReferenceText = "CSharpTests.Properties.Resources.Key1",
                ReplaceSpan = CreateTextSpan(92, 29, 92, 66)
            });


            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Properties.Resources.Key1",
                OriginalReferenceText = "Properties.Resources.Key1",
                ReplaceSpan = CreateTextSpan(99, 29, 99, 54)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value2",
                SourceItem = projectItem,
                FullReferenceText = "CSharpLib.Resource1.Key1",
                OriginalReferenceText = "CSharpLib.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(100, 29, 100, 53)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value2",
                SourceItem = projectItem,
                FullReferenceText = "CSharpLib.Resource1.Key1",
                OriginalReferenceText = "al.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(101, 29, 101, 46)
            });


            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "a",
                SourceItem = projectItem,
                FullReferenceText = "CustomNamespace.Resource2.KeyA",
                OriginalReferenceText = "CustomNamespace.Resource2.KeyA",
                ReplaceSpan = CreateTextSpan(103, 29, 103, 59)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "a",
                SourceItem = projectItem,
                FullReferenceText = "CustomNamespace.Resource2.KeyA",
                OriginalReferenceText = "Resource2.KeyA",
                ReplaceSpan = CreateTextSpan(109, 29, 109, 43)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "a",
                SourceItem = projectItem,
                FullReferenceText = "CustomNamespace.Resource2.KeyA",
                OriginalReferenceText = "CustomNamespace.Resource2.KeyA",
                ReplaceSpan = CreateTextSpan(110, 29, 110, 59)
            });


            foreach (var item in list) CalculateAbsolutePosition(item);
            validResults.Add(Agent.CSharpReferencesTestFile1, list);
        }

        private static void GenerateValidResultsForReferences2() {
            List<AbstractResultItem> list = new List<AbstractResultItem>();
            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(Agent.CSharpReferencesTestFile2);

            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value3",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key1",
                OriginalReferenceText = "Resource1.Key1",
                ReplaceSpan = CreateTextSpan(9, 24, 9, 38)
            });

            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value11",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key11",
                OriginalReferenceText = "Resource1.Key11",
                ReplaceSpan = CreateTextSpan(10, 24, 10, 39)
            });

            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value3",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key1",
                OriginalReferenceText = "test.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(12, 24, 12, 43)
            });

            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value11",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key11",
                OriginalReferenceText = "test.Resource1.Key11",
                ReplaceSpan = CreateTextSpan(13, 24, 13, 44)
            });


            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value3",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key1",
                OriginalReferenceText = "CSharpTests.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(15, 24, 15, 50)
            });


            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value3",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key1",
                OriginalReferenceText = "Resource1.Key1",
                ReplaceSpan = CreateTextSpan(25, 28, 25, 42)
            });

            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value3",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key1",
                OriginalReferenceText = "test.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(26, 28, 26, 47)
            });

            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value11",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Resource1.Key11",
                OriginalReferenceText = "test.Resource1.Key11",
                ReplaceSpan = CreateTextSpan(27, 28, 27, 48)
            });


            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value2",
                SourceItem = projectItem,
                FullReferenceText = "CSharpLib.Resource1.Key1",
                OriginalReferenceText = "CSharpLib.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(29, 28, 29, 52)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Properties.Resources.Key1",
                OriginalReferenceText = "Properties.Resources.Key1",
                ReplaceSpan = CreateTextSpan(30, 29, 30, 54)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "a" + Environment.NewLine + "b" + Environment.NewLine + "c" + Environment.NewLine + "d",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Properties.Resources.Key2",
                OriginalReferenceText = "Properties.Resources.Key2",
                ReplaceSpan = CreateTextSpan(31, 29, 31, 54)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "a" + Environment.NewLine + "b" + Environment.NewLine + "c" + Environment.NewLine + "d",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Properties.Resources.Key2",
                OriginalReferenceText = "Properties.Resources.Key2",
                ReplaceSpan = CreateTextSpan(32, 29, 32, 54)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "test\"test\"\"",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Properties.Resources.Key4",
                OriginalReferenceText = "Properties.Resources.Key4",
                ReplaceSpan = CreateTextSpan(33, 29, 33, 54)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "test\\ntest",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Properties.Resources.Key5",
                OriginalReferenceText = "Properties.Resources.Key5",
                ReplaceSpan = CreateTextSpan(34, 29, 34, 54)
            });
            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "\\x0012",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Properties.Resources.Key6",
                OriginalReferenceText = "Properties.Resources.Key6",
                ReplaceSpan = CreateTextSpan(35, 29, 35, 54)
            });


            list.Add(new CSharpCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                FullReferenceText = "CSharpTests.Properties.Resources.Key1",
                OriginalReferenceText = "CSharpTests.Properties.Resources.Key1",
                ReplaceSpan = CreateTextSpan(37, 29, 37, 66)
            });

            foreach (var item in list) CalculateAbsolutePosition(item);
            validResults.Add(Agent.CSharpReferencesTestFile2, list);
        }

    }
}
