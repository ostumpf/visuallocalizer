using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EnvDTE80;
using VisualLocalizer.Commands;
using VisualLocalizer.Components;
using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell;

namespace VLUnitTests.VLTests {
    
    [TestClass()]
    public class VBBatchMoveTest : BatchTestsBase {

        private static Dictionary<string, List<AbstractResultItem>> validResults = new Dictionary<string, List<AbstractResultItem>>();

        public static List<AbstractResultItem> GetExpectedResultsFor(string file) {
            if (!validResults.ContainsKey(file)) {
                if (file == Agent.VBStringsTestFile1) {
                    GenerateValidResultsForStrings1();
                } else if (file == Agent.VBStringsTestFile2) {
                    GenerateValidResultsForStrings2();
                } else if (file == Agent.VBStringsTestFile3) {
                    GenerateValidResultsForStrings3();
                } else if (file == Agent.VBStringsTestFormDesigner1) {
                    GenerateValidResultsForFormDesigner1();
                } else throw new Exception("Cannot resolve test file name.");
            }
            return validResults[file];
        }        

        [TestMethod()]
        public void ProcessTest1() {
            Agent.EnsureSolutionOpen();

            DTE2 DTE = Agent.GetDTE();
            BatchMoveCommand target = Agent.BatchMoveCommand;

            for (int i = 0; i < 3; i++) {
                var window = DTE.OpenFile(null, Agent.VBStringsTestFile1);
                window.Activate();

                target.Process(true);

                Assert.IsTrue(VLDocumentViewsManager.IsFileLocked(Agent.VBStringsTestFile1));

                ValidateResults(GetExpectedResultsFor(Agent.VBStringsTestFile1), target.Results);

                window.Close(EnvDTE.vsSaveChanges.vsSaveChangesNo);
                Assert.IsTrue(VLDocumentViewsManager.IsFileLocked(Agent.VBStringsTestFile1));

                VLDocumentViewsManager.ReleaseLocks();
                Assert.IsFalse(VLDocumentViewsManager.IsFileLocked(Agent.VBStringsTestFile1));
            }
        }

        [TestMethod()]
        public void ProcessSelectedItemsTest1() {
            string[] itemsToSelect = { Agent.VBStringsTestFile1, Agent.VBStringsTestFolder1 };
            string[] expectedFiles = { Agent.VBStringsTestFile1, Agent.VBStringsTestFile3, Agent.VBStringsTestFile2 };

            GenericTest(Agent.BatchMoveCommand, itemsToSelect, expectedFiles, GetExpectedResultsFor);
        }

        [TestMethod()]
        public void ProcessSelectedItemsTest2() {
            string[] itemsToSelect = { Agent.VBStringsTestFolder1, Agent.VBStringsTestForm1 };
            string[] expectedFiles = { Agent.VBStringsTestFile3, Agent.VBStringsTestFile2, Agent.VBStringsTestFormDesigner1 };

            GenericTest(Agent.BatchMoveCommand, itemsToSelect, expectedFiles, GetExpectedResultsFor);
        }

        [TestMethod()]
        public void ProcessSelectionTest() {
            Agent.EnsureSolutionOpen();

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.VBStringsTestFile1, true, true);
            IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(Agent.VBStringsTestFile1, false);

            GenericSelectionTest(Agent.BatchMoveCommand_Accessor, Agent.VBStringsTestFile1, view, lines, GetExpectedResultsFor);
        }

        [TestMethod()]
        public void ProcessEmptySelectionTest() {
            Agent.EnsureSolutionOpen();

            BatchMoveCommand_Accessor target = Agent.BatchMoveCommand_Accessor;

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.VBStringsTestFile1, true, true);
            view.SetSelection(40, 17, 44, 26);

            List<CodeStringResultItem> emptyList = new List<CodeStringResultItem>();
            target.ProcessSelection(true);
            ValidateResults(emptyList, target.Results);
            Assert.IsFalse(VLDocumentViewsManager.IsFileLocked(Agent.VBStringsTestFile1));

            VsShellUtilities.GetWindowObject(VLDocumentViewsManager.GetWindowFrameForFile(Agent.VBStringsTestFile1, false)).Close(vsSaveChanges.vsSaveChangesNo);
        }

        private static void GenerateValidResultsForStrings1() {
            List<AbstractResultItem> list = new List<AbstractResultItem>();
            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(Agent.VBStringsTestFile1);

            list.Add(new TestVBStringResultItem() {
                Value = "a_1",                
                SourceItem = projectItem,
                VariableElementName = "a_1",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,                
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(1, 28, 1, 33)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "\"a_3\"",
                SourceItem = projectItem,
                VariableElementName = "a_3",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(3, 28, 3, 37)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "\"",
                SourceItem = projectItem,
                VariableElementName = "a_4",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(4, 28, 4, 32)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "\"\"",
                SourceItem = projectItem,
                VariableElementName = "a_5",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(5, 28, 5, 34)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "\"\\\"",
                SourceItem = projectItem,
                VariableElementName = "a_6",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(6, 28, 6, 35)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "@",
                SourceItem = projectItem,
                VariableElementName = "a_7",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(7, 28, 7, 31)
            });


            list.Add(new TestVBStringResultItem() {
                Value = "a_9",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "s_1",
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(12, 28, 12, 33)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "a_10",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "s_1",
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(13, 29, 13, 35)
            });


            list.Add(new TestVBStringResultItem() {
                Value = "a_12",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "f_1",
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(18, 29, 18, 35)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "a_13",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "f_1",
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(20, 29, 20, 35)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "a_14",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "f_1",
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(22, 29, 22, 35)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "'",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "f_1",
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(23, 29, 23, 32)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "a_16",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "f_1",
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(24, 29, 24, 35)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "''",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "f_1",
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(25, 29, 25, 33)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "a_18",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "f_1",
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(26, 29, 26, 35)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "a_19",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "f_1",
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(29, 29, 29, 35)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "REM",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "f_1",
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(33, 29, 33, 34)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "a_21",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "f_1",
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(34, 32, 34, 38)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "a_22",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "f_1",
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(35, 30, 35, 36)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "a_23",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "f_1",
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(36, 30, 36, 36)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "a_24",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "f_1",
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(37, 29, 37, 35)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "a_25",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "f_1",
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(38, 29, 38, 35)
            });


            list.Add(new TestVBStringResultItem() {
                Value = "a_26",
                SourceItem = projectItem,
                VariableElementName = "a_26",
                ClassOrStructElementName = "A_c_inner",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(44, 29, 44, 35)
            });

            list.Add(new TestVBStringResultItem() {
                Value = "a_27",
                SourceItem = projectItem,
                VariableElementName = "a_27",
                ClassOrStructElementName = "A_s_inner",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(48, 32, 48, 38)
            });
            
            list.Add(new TestVBStringResultItem() {
                Value = "a_28",
                SourceItem = projectItem,
                VariableElementName = "a_28",
                ClassOrStructElementName = "R",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(56, 34, 56, 40)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "a_29",
                SourceItem = projectItem,
                VariableElementName = "a_29",
                ClassOrStructElementName = "R",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(57, 29, 57, 35)
            });


            list.Add(new TestVBStringResultItem() {
                Value = "a_31",
                SourceItem = projectItem,
                VariableElementName = "a_31",
                ClassOrStructElementName = "M",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(61, 25, 61, 31)
            });

            list.Add(new TestVBStringResultItem() {
                Value = "a_32",
                SourceItem = projectItem,
                VariableElementName = "a_32",
                ClassOrStructElementName = "M1",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "VBTests.N1",
                ReplaceSpan = CreateTextSpan(66, 29, 66, 35)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "a_33",
                SourceItem = projectItem,
                VariableElementName = "a_33",
                ClassOrStructElementName = "C_inner",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "VBTests.N1.N1_inner",
                ReplaceSpan = CreateTextSpan(71, 33, 71, 39)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "a_34",
                SourceItem = projectItem,
                VariableElementName = "a_34",
                ClassOrStructElementName = "C_inner_2",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "VBTests.N1.N1_inner",
                ReplaceSpan = CreateTextSpan(75, 33, 75, 39)
            });

            foreach (var item in list) CalculateAbsolutePosition(item);
            validResults.Add(Agent.VBStringsTestFile1, list);
        }

        private static void GenerateValidResultsForStrings2() {
            List<AbstractResultItem> list = new List<AbstractResultItem>();
            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(Agent.VBStringsTestFile2);

            list.Add(new TestVBStringResultItem() {
                Value = "b_1",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "strings2",
                MethodElementName = "f1",
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(4, 28, 4, 33)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "b_2",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "strings2",
                MethodElementName = "s4",
                IsWithinLocalizableFalse = true,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(9, 28, 9, 33)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "b_3",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "C1",
                MethodElementName = "C1_s",
                IsWithinLocalizableFalse = true,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(17, 32, 17, 37)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "b_4",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "strings2",
                MethodElementName = "s",
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(23, 28, 23, 33)
            });

            foreach (var item in list) CalculateAbsolutePosition(item);
            validResults.Add(Agent.VBStringsTestFile2, list);
        }

        private static void GenerateValidResultsForStrings3() {
            List<AbstractResultItem> list = new List<AbstractResultItem>();
            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(Agent.VBStringsTestFile3);

            list.Add(new TestVBStringResultItem() {
                Value = "c_1",
                SourceItem = projectItem,
                VariableElementName = "c_1",
                ClassOrStructElementName = "strings3",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(1, 24, 1, 29)
            });

            foreach (var item in list) CalculateAbsolutePosition(item);
            validResults.Add(Agent.VBStringsTestFile3, list);
        }

        private static void GenerateValidResultsForFormDesigner1() {
            List<AbstractResultItem> list = new List<AbstractResultItem>();
            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(Agent.VBStringsTestFormDesigner1);

            list.Add(new TestVBStringResultItem() {
                Value = "Form1",
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "Form1",
                MethodElementName = "InitializeComponent",
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = true,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(26, 18, 26, 25)
            });

            foreach (var item in list) CalculateAbsolutePosition(item);
            validResults.Add(Agent.VBStringsTestFormDesigner1, list);
        }
    }
}
