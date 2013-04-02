using VisualLocalizer.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.VisualStudio.Shell;
using VisualLocalizer.Library;
using VisualLocalizer.Components;
using System.Collections.Generic;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.TextManager.Interop;

namespace VLUnitTests.VLTests {

    [TestClass()]
    public class CSharpBatchMoveTest : BatchTestsBase {

        private static Dictionary<string, List<AbstractResultItem>> validResults = new Dictionary<string, List<AbstractResultItem>>();

        public static List<AbstractResultItem> GetExpectedResultsFor(string file) {
            if (!validResults.ContainsKey(file)) {
                if (file == Agent.CSharpStringsTestFile1) {
                    GenerateValidResultsForStrings1();
                } else if (file == Agent.CSharpStringsTestFile2) {
                    GenerateValidResultsForStrings2();
                } else if (file == Agent.CSharpStringsTestFile3) {
                    GenerateValidResultsForStrings3();
                } else if (file == Agent.CSharpStringsTestFormDesignerFile1) {
                    GenerateValidResultsForForm1();
                }  else throw new Exception("Cannot resolve test file name.");
            }
            return validResults[file];
        }

        [TestMethod()]
        public void ProcessSelectedItemsTest4() {
            string[] itemsToSelect = { Agent.CSharpStringsTestFolder1, Agent.CSharpStringsTestFolder2, Agent.CSharpStringsTestFormFile1 };
            string[] expectedFiles = { Agent.CSharpStringsTestFile3, Agent.CSharpStringsTestFile2, Agent.CSharpStringsTestFormDesignerFile1 };

            GenericTest(Agent.BatchMoveCommand, itemsToSelect, expectedFiles, GetExpectedResultsFor);            
        }

        [TestMethod()]
        public void ProcessSelectedItemsTest3() {
            string[] itemsToSelect = { };
            string[] expectedFiles = { };

            GenericTest(Agent.BatchMoveCommand, itemsToSelect, expectedFiles, GetExpectedResultsFor);            
        }

        [TestMethod()]
        public void ProcessSelectedItemsTest2() {
            string[] itemsToSelect = { Agent.CSharpStringsTestFormFile1 };
            string[] expectedFiles = { Agent.CSharpStringsTestFormDesignerFile1 };

            GenericTest(Agent.BatchMoveCommand, itemsToSelect, expectedFiles, GetExpectedResultsFor);            
        }

        [TestMethod()]
        public void ProcessSelectedItemsTest1() {
            string[] itemsToSelect = { Agent.CSharpStringsTestFile1, Agent.CSharpStringsTestFolder2 };
            string[] expectedFiles = { Agent.CSharpStringsTestFile1, Agent.CSharpStringsTestFile3, Agent.CSharpStringsTestFile2 };

            GenericTest(Agent.BatchMoveCommand, itemsToSelect, expectedFiles, GetExpectedResultsFor);            
        }

        [TestMethod()]
        public void ProcessDesignerItemsTest() {
            string[] itemsToSelect = { Agent.CSharpStringsTestFormFile1 };
            string[] expectedFiles = { Agent.CSharpStringsTestFormDesignerFile1 };
            GenericTest(Agent.BatchMoveCommand, itemsToSelect, expectedFiles, GetExpectedResultsFor);
        }

        [TestMethod()]
        public void ProcessTest() {
            Agent.EnsureSolutionOpen();

            DTE2 DTE = Agent.GetDTE();
            BatchMoveCommand target = Agent.BatchMoveCommand;

            var window = DTE.OpenFile(null, Agent.CSharpStringsTestFile1);
            window.Activate();

            target.Process(true);

            Assert.IsTrue(VLDocumentViewsManager.IsFileLocked(Agent.CSharpStringsTestFile1));

            ValidateResults(GetExpectedResultsFor(Agent.CSharpStringsTestFile1), target.Results);

            window.Close(EnvDTE.vsSaveChanges.vsSaveChangesNo);
            Assert.IsTrue(VLDocumentViewsManager.IsFileLocked(Agent.CSharpStringsTestFile1));

            VLDocumentViewsManager.SetFileReadonly(Agent.CSharpStringsTestFile1, false);
            Assert.IsFalse(VLDocumentViewsManager.IsFileLocked(Agent.CSharpStringsTestFile1));
        }

        [TestMethod()]
        public void ProcessSelectionTest() {
            Agent.EnsureSolutionOpen();                        
            
            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.CSharpStringsTestFile1, true, true);
            IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(Agent.CSharpStringsTestFile1, false);

            GenericSelectionTest(Agent.BatchMoveCommand_Accessor, Agent.CSharpStringsTestFile1, view, lines, GetExpectedResultsFor);
        }

        [TestMethod()]
        public void ProcessEmptySelectionTest() {
            Agent.EnsureSolutionOpen();

            DTE2 DTE = Agent.GetDTE();
            BatchMoveCommand_Accessor target = Agent.BatchMoveCommand_Accessor;

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.CSharpStringsTestFile1, true, true);
            view.SetSelection(133,33,138,35);

            List<AbstractResultItem> emptyList = new List<AbstractResultItem>();
            target.ProcessSelection(true);
            ValidateResults(emptyList, target.Results);
            Assert.IsFalse(VLDocumentViewsManager.IsFileLocked(Agent.CSharpStringsTestFile1));

            VsShellUtilities.GetWindowObject(VLDocumentViewsManager.GetWindowFrameForFile(Agent.CSharpStringsTestFile1, false)).Close(vsSaveChanges.vsSaveChangesNo);
        }

        private static void GenerateValidResultsForStrings1() {
            List<AbstractResultItem> ValidResultsForStrings1 = new List<AbstractResultItem>();
            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(Agent.CSharpStringsTestFile1);

            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "@",
                WasVerbatim = true,
                SourceItem = projectItem,
                VariableElementName = "a_54",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(14, 30, 14, 34)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "@\"",
                WasVerbatim = true,
                SourceItem = projectItem,
                VariableElementName = "a_55",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(15, 30, 15, 36)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "\"@\"",
                WasVerbatim = true,
                SourceItem = projectItem,
                VariableElementName = "a_56",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(16, 30, 16, 38)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "\"\"",
                WasVerbatim = true,
                SourceItem = projectItem,
                VariableElementName = "a_57",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(17, 30, 17, 37)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "@\"@\"",
                WasVerbatim = true,
                SourceItem = projectItem,
                VariableElementName = "a_58",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(18, 30, 18, 39)
            });


            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a1",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = "a_1",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(20, 29, 20, 33)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = @"\a2\@",
                WasVerbatim = true,
                SourceItem = projectItem,
                VariableElementName = "a_2",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(21, 29, 21, 37)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = @"a3\",
                WasVerbatim = true,
                SourceItem = projectItem,
                VariableElementName = "a_3",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(22, 29, 22, 35)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "\\",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = "a_6",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(25, 29, 25, 33)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "\"",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = "a_7",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(26, 29, 26, 33)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "\"\"",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = "a_8",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(27, 29, 27, 35)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = @"""",
                WasVerbatim = true,
                SourceItem = projectItem,
                VariableElementName = "a_9",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(28, 29, 28, 34)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = @"""a10""",
                WasVerbatim = true,
                SourceItem = projectItem,
                VariableElementName = "a_10",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(29, 30, 29, 40)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = @"""""",
                WasVerbatim = true,
                SourceItem = projectItem,
                VariableElementName = "a_11",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(30, 30, 30, 37)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a\nb",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = "a_12",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(31, 30, 31, 36)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "readonly",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = "a_14",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(34, 39, 34, 49)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "static",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = "a_15",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(35, 37, 35, 45)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = @"a
b
c
d

e",
                WasVerbatim = true,
                SourceItem = projectItem,
                VariableElementName = "a_16",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(37, 30, 42, 2)
            });            
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = @"f
""
g
""
",
                WasVerbatim = true,
                SourceItem = projectItem,
                VariableElementName = "a_18",
                ClassOrStructElementName = "A",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(45, 30, 49, 1)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_20",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_2",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(54, 26, 54, 32)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_21",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_2",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(55, 12, 55, 18)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_22",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_2",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(56, 30, 56, 36)
            });


            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_23",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_3",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(61, 26, 61, 32)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_24",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_3",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(63, 26, 63, 32)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_25",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_3",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(65, 26, 65, 32)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_26",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_3",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(66, 26, 66, 32)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_29",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_3",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(69, 26, 69, 32)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "//",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_3",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(70, 26, 70, 30)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_31",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_3",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(70, 47, 70, 53)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "/*",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_3",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(71, 26, 71, 30)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_32",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_3",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(72, 26, 72, 32)
            });

            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "/*/",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_3",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(73, 26, 73, 31)
            });

            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_34",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_3",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(74, 26, 74, 32)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_36",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_3",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(81, 26, 81, 32)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_37",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_3",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(83, 26, 83, 32)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_38",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_3",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(84, 26, 84, 32)
            });

            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_39",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_4",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(89, 23, 89, 29)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_40",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_4",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(92, 30, 92, 36)
            });

            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_41",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_5",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(98, 23, 98, 29)
            });

            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_42",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "am_6",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(104, 30, 104, 36)
            });


            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_43",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = "a_43",
                ClassOrStructElementName = "A_c_inner",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(109, 26, 109, 32)
            });

            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_44",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = "a_44",
                ClassOrStructElementName = "A_s_inner",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(113, 33, 113, 39)
            });


            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_44",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = "a_44",
                ClassOrStructElementName = "A_c1_inner",
                MethodElementName = null,
                IsWithinLocalizableFalse = true,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(118, 26, 118, 32)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_45",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A_c1_inner",
                MethodElementName = "m",
                IsWithinLocalizableFalse = true,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(121, 30, 121, 36)
            });

            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_46",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "m",
                IsWithinLocalizableFalse = true,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(127, 26, 127, 32)
            });

            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_47",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "c",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(131, 26, 131, 32)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_48",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "c",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = true,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(132, 39, 132, 45)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_49",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "A",
                MethodElementName = "c",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(133, 26, 133, 32)
            });


            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "static",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = "b_1",
                ClassOrStructElementName = "B",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(138, 36, 138, 44)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_50",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "B",
                MethodElementName = "d",
                IsWithinLocalizableFalse = true,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(142, 26, 142, 32)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_51",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "B",
                MethodElementName = "d",
                IsWithinLocalizableFalse = true,
                IsMarkedWithUnlocalizableComment = true,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(143, 39, 143, 45)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_52",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "B",
                MethodElementName = "d",
                IsWithinLocalizableFalse = true,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(144, 26, 144, 32)
            });


            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_53",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = "a_53",
                ClassOrStructElementName = "X",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests.A_nmspc",
                ReplaceSpan = CreateTextSpan(150, 34, 150, 40)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = @"@",
                WasVerbatim = true,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "X",
                MethodElementName = "m",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests.A_nmspc",
                ReplaceSpan = CreateTextSpan(153, 30, 153, 34)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = @"""@",
                WasVerbatim = true,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "X",
                MethodElementName = "m",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests.A_nmspc",
                ReplaceSpan = CreateTextSpan(154, 30, 154, 36)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_61",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "X",
                MethodElementName = "m",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests.A_nmspc",
                ReplaceSpan = CreateTextSpan(155, 30, 155, 36)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_62",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = "a_62",
                ClassOrStructElementName = "Oc",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(162, 26, 162, 32)
            });
            ValidResultsForStrings1.Add(new TestCSharpStringResultItem() {
                Value = "a_63",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = "a_63",
                ClassOrStructElementName = "Os",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(166, 33, 166, 39)
            });
            foreach (var item in ValidResultsForStrings1) CalculateAbsolutePosition(item);

            validResults.Add(Agent.CSharpStringsTestFile1, ValidResultsForStrings1);                        
        }

        private static void GenerateValidResultsForStrings2() {
            List<AbstractResultItem> ValidResultsForStrings2 = new List<AbstractResultItem>();
            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(Agent.CSharpStringsTestFile2);

            ValidResultsForStrings2.Add(new TestCSharpStringResultItem() {
                Value = "test3",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = "test3",
                ClassOrStructElementName = "B",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests.Data",
                ReplaceSpan = CreateTextSpan(9, 36, 9, 43)
            });

            ValidResultsForStrings2.Add(new TestCSharpStringResultItem() {
                Value = "test4",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = "test4",
                ClassOrStructElementName = "B",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests.Data",
                ReplaceSpan = CreateTextSpan(10, 31, 10, 38)
            });

            foreach (var item in ValidResultsForStrings2) CalculateAbsolutePosition(item);
            validResults.Add(Agent.CSharpStringsTestFile2, ValidResultsForStrings2);                                    
        }

        private static void GenerateValidResultsForStrings3() {
            List<AbstractResultItem> ValidResultsForStrings3 = new List<AbstractResultItem>();
            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(Agent.CSharpStringsTestFile3);

            ValidResultsForStrings3.Add(new TestCSharpStringResultItem() {
                Value = "test1",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = "test1",
                ClassOrStructElementName = "strings3",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = true,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests.Data",
                ReplaceSpan = CreateTextSpan(7, 36, 7, 43)    
            });

            ValidResultsForStrings3.Add(new TestCSharpStringResultItem() {
                Value = "test2",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = "test2",
                ClassOrStructElementName = "strings3",
                MethodElementName = null,
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = false,
                NamespaceElementName = "CSharpTests.Data",
                ReplaceSpan = CreateTextSpan(8, 40, 8, 47)    
            });

            foreach (var item in ValidResultsForStrings3) CalculateAbsolutePosition(item);
            validResults.Add(Agent.CSharpStringsTestFile3, ValidResultsForStrings3);                        
        }

        private static void GenerateValidResultsForForm1() {
            List<AbstractResultItem> ValidResultsForForm1 = new List<AbstractResultItem>();
            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(Agent.CSharpStringsTestFormDesignerFile1);

            ValidResultsForForm1.Add(new TestCSharpStringResultItem() {
                Value = "Form1",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "Form1",
                MethodElementName = "InitializeComponent",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = true,
                NamespaceElementName = "CSharpTests",
                ReplaceSpan = CreateTextSpan(32, 24, 32, 31)    
            });

            foreach (var item in ValidResultsForForm1) CalculateAbsolutePosition(item);
            validResults.Add(Agent.CSharpStringsTestFormDesignerFile1, ValidResultsForForm1);            
        }

    }
}
