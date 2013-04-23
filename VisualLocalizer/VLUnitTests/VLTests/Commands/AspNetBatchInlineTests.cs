using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VisualLocalizer.Components;
using System.IO;
using EnvDTE;
using VisualLocalizer.Library.AspxParser;
using EnvDTE80;
using VisualLocalizer.Commands;
using Microsoft.VisualStudio.TextManager.Interop;

namespace VLUnitTests.VLTests {

    /// <summary>
    /// Tests for ASP .NET batch inline command.
    /// </summary>
    [TestClass()]
    public class AspNetBatchInlineTests : BatchTestsBase {

        private static Dictionary<string, List<AbstractResultItem>> validResults = new Dictionary<string, List<AbstractResultItem>>();

        public static List<AbstractResultItem> GetExpectedResultsFor(string file) {
            if (!validResults.ContainsKey(file)) {
                if (file == Agent.AspNetReferencesTestFile1) {
                    GenerateValidResultsForReferences1();
                } else if (file == Agent.AspNetReferencesTestFile2) {
                    GenerateValidResultsForReferences2();
                } else throw new Exception("Cannot resolve test file name.");
            }

            return validResults[file];
        }

        [TestMethod()]
        public void ProcessSelectionTest1() {
            Agent.EnsureSolutionOpen();

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.AspNetReferencesTestFile1, true, true);
            IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(Agent.AspNetReferencesTestFile1, false);

            GenericSelectionTest(Agent.BatchInlineCommand_Accessor, Agent.AspNetReferencesTestFile1, view, lines, GetExpectedResultsFor);
        }

        [TestMethod()]
        public void ProcessSelectionTest2() {
            Agent.EnsureSolutionOpen();

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.AspNetReferencesTestFile2, true, true);
            IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(Agent.AspNetReferencesTestFile2, false);

            GenericSelectionTest(Agent.BatchInlineCommand_Accessor, Agent.AspNetReferencesTestFile2, view, lines, GetExpectedResultsFor);
        }

        [TestMethod()]
        public void ProcessSelectedItemsTest1() {
            string[] itemsToSelect = { Agent.AspNetReferencesTestFile2 }; 
            string[] expectedFiles = { Agent.AspNetReferencesTestFile2 }; 

            GenericTest(Agent.BatchInlineCommand, itemsToSelect, expectedFiles, GetExpectedResultsFor);
        }

        [TestMethod()]
        public void ProcessSelectedItemsTest2() {
            string[] itemsToSelect = { Agent.AspNetReferencesTestFile1, Agent.AspNetReferencesTestFile2 };
            string[] expectedFiles = { Agent.AspNetReferencesTestFile1, Agent.AspNetReferencesTestFile2 };

            GenericTest(Agent.BatchInlineCommand, itemsToSelect, expectedFiles, GetExpectedResultsFor);
        }

        [TestMethod()]
        public void ProcessTest() {
            Agent.EnsureSolutionOpen();

            DTE2 DTE = Agent.GetDTE();
            BatchInlineCommand target = Agent.BatchInlineCommand;

            var window = DTE.OpenFile(null, Agent.AspNetReferencesTestFile1);
            window.Activate();

            target.Process(true);

            Assert.IsTrue(VLDocumentViewsManager.IsFileLocked(Agent.AspNetReferencesTestFile1));

            ValidateResults(GetExpectedResultsFor(Agent.AspNetReferencesTestFile1), target.Results);
            
            Assert.IsTrue(VLDocumentViewsManager.IsFileLocked(Agent.AspNetReferencesTestFile1));

            VLDocumentViewsManager.ReleaseLocks();
            Assert.IsFalse(VLDocumentViewsManager.IsFileLocked(Agent.AspNetReferencesTestFile1));

            window.Detach();
            window.Close(EnvDTE.vsSaveChanges.vsSaveChangesNo);
        }

        private static void GenerateValidResultsForReferences1() {
            List<AbstractResultItem> list = new List<AbstractResultItem>();

            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(Agent.AspNetReferencesTestFile1);
            
            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value1",                
                SourceItem = projectItem,                                
                ComesFromClientComment = false,                
                ComesFromInlineExpression = true,                
                ComesFromWebSiteResourceReference = true,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                FullReferenceText = "Resources.Resource1.Key1",
                OriginalReferenceText = "Resources:Resource1,Key1",
                ReplaceSpan = CreateTextSpan(11, 58, 11, 82),
                InlineReplaceSpan = CreateBlockSpan(11, 54, 11, 85)
            });
            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = true,
                ComesFromWebSiteResourceReference = true,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                FullReferenceText = "Resources.Resource1.Key1",
                OriginalReferenceText = "Resources:Resource1,Key1",
                ReplaceSpan = CreateTextSpan(12, 58, 16, 4),
                InlineReplaceSpan = CreateBlockSpan(12, 54, 16, 7)
            });
            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value11",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = true,
                ComesFromWebSiteResourceReference = true,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                FullReferenceText = "Resources.Resource1.Key11",
                OriginalReferenceText = "Resources:Resource1,Key11",
                ReplaceSpan = CreateTextSpan(17, 58, 19, 5),
                InlineReplaceSpan = CreateBlockSpan(17, 54, 19, 8)
            });
            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value11",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = true,
                ComesFromWebSiteResourceReference = true,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                FullReferenceText = "Resources.Resource1.Key11",
                OriginalReferenceText = "Resources:Resource1,Key11",
                ReplaceSpan = CreateTextSpan(20, 58, 21, 5),
                InlineReplaceSpan = CreateBlockSpan(20, 54, 21, 8)
            });
            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = true,
                ComesFromWebSiteResourceReference = true,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                FullReferenceText = "Resources.Resource1.Key1",
                OriginalReferenceText = "Resources:Resource1,Key1",
                ReplaceSpan = CreateTextSpan(23, 0, 23, 24),
                InlineReplaceSpan = CreateBlockSpan(22, 54, 24, 10)
            });



            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = true,
                ComesFromWebSiteResourceReference = false,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                FullReferenceText = "Resources.Resource1.Key1",
                OriginalReferenceText = "Resources.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(26, 12, 26, 36),
                InlineReplaceSpan = CreateBlockSpan(26, 8, 26, 39)
            });
            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = true,
                ComesFromWebSiteResourceReference = false,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                FullReferenceText = "Resources.Resource1.Key1",
                OriginalReferenceText = "Resources.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(27, 12, 32, 40),
                InlineReplaceSpan = CreateBlockSpan(27, 8, 32, 43)
            });
            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = true,
                ComesFromWebSiteResourceReference = false,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                FullReferenceText = "Resources.Resource1.Key1",
                OriginalReferenceText = "Resources.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(34, 12, 34, 36),
                InlineReplaceSpan = CreateBlockSpan(33, 8, 35, 14)
            });
            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = false,
                ComesFromWebSiteResourceReference = false,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                FullReferenceText = "Resources.Resource1.Key1",
                OriginalReferenceText = "Resources.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(36, 24, 36, 48),
                InlineReplaceSpan = null
            });

            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = false,
                ComesFromWebSiteResourceReference = false,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                FullReferenceText = "Resources.Resource1.Key1",
                OriginalReferenceText = "Resources.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(38, 25, 38, 49),
                InlineReplaceSpan = null
            });
            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value11",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = false,
                ComesFromWebSiteResourceReference = false,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                FullReferenceText = "Resources.Resource1.Key11",
                OriginalReferenceText = "Resources.Resource1.Key11",
                ReplaceSpan = CreateTextSpan(39, 25, 39, 50),
                InlineReplaceSpan = null
            });
            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value2",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = false,
                ComesFromWebSiteResourceReference = false,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                FullReferenceText = "Resources.Resource1.Key2",
                OriginalReferenceText = "Resources.Resource1.Key2",
                ReplaceSpan = CreateTextSpan(40, 25, 40, 49),
                InlineReplaceSpan = null
            });
            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = false,
                ComesFromWebSiteResourceReference = false,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                FullReferenceText = "Resources.Resource1.Key1",
                OriginalReferenceText = "Resources.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(41, 25, 41, 49),
                InlineReplaceSpan = null
            });

            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = false,
                ComesFromWebSiteResourceReference = false,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                FullReferenceText = "Resources.Resource1.Key1",
                OriginalReferenceText = "Resources.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(43, 16, 47, 4),
                InlineReplaceSpan = null
            });
            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "valueInner1",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = false,
                ComesFromWebSiteResourceReference = false,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                FullReferenceText = "Resources.Resource2.Key1",
                OriginalReferenceText = "Resources.Resource2.Key1",
                ReplaceSpan = CreateTextSpan(48, 25, 48, 49),
                InlineReplaceSpan = null
            });


            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value11",
                SourceItem = projectItem,
                ComesFromClientComment = true,
                ComesFromInlineExpression = true,
                ComesFromWebSiteResourceReference = true,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                FullReferenceText = "Resources.Resource1.Key11",
                OriginalReferenceText = "Resources:Resource1,Key11",
                ReplaceSpan = CreateTextSpan(52, 63, 52, 88),
                InlineReplaceSpan = CreateBlockSpan(52, 59, 52, 91)
            });
            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value11",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = true,
                ComesFromWebSiteResourceReference = true,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                FullReferenceText = "Resources.Resource1.Key11",
                OriginalReferenceText = "Resources:Resource1,Key11",
                ReplaceSpan = CreateTextSpan(53, 58, 53, 83),
                InlineReplaceSpan = CreateBlockSpan(53, 54, 53, 86)
            });
            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "valueInner1",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = true,
                ComesFromWebSiteResourceReference = true,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                FullReferenceText = "Resources.Resource2.Key1",
                OriginalReferenceText = "Resources:Resource2,Key1",
                ReplaceSpan = CreateTextSpan(54, 58, 54, 82),
                InlineReplaceSpan = CreateBlockSpan(54, 54, 54, 85)
            });



            foreach (var item in list) {
                CalculateAbsolutePosition(item);
                if (((AspNetCodeReferenceResultItem)item).InlineReplaceSpan != null)
                    CalculateAbsolutePosition(Agent.AspNetReferencesTestFile1, ((AspNetCodeReferenceResultItem)item).InlineReplaceSpan);
            }

            validResults.Add(Agent.AspNetReferencesTestFile1, list);
        }

        private static void GenerateValidResultsForReferences2() {
            List<AbstractResultItem> list = new List<AbstractResultItem>();

            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(Agent.AspNetReferencesTestFile2);

            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = true,
                ComesFromWebSiteResourceReference = true,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                FullReferenceText = "Resources.Resource1.Key1",
                OriginalReferenceText = "Resources:Resource1,Key1",
                ReplaceSpan = CreateTextSpan(13, 58, 13, 82),
                InlineReplaceSpan = CreateBlockSpan(13, 54, 13, 85)
            });
            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = true,
                ComesFromWebSiteResourceReference = true,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                FullReferenceText = "Resources.Resource1.Key1",
                OriginalReferenceText = "Resources:Resource1,Key1",
                ReplaceSpan = CreateTextSpan(16, 8, 20, 4),
                InlineReplaceSpan = CreateBlockSpan(15, 4, 21, 8)
            });
            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = true,
                ComesFromWebSiteResourceReference = false,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                FullReferenceText = "Resources.Resource1.Key1",
                OriginalReferenceText = "Resource1.Key1",
                ReplaceSpan = CreateTextSpan(22, 10, 22, 24),
                InlineReplaceSpan = CreateBlockSpan(22, 6, 22, 27)
            });


            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = true,
                ComesFromWebSiteResourceReference = false,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                FullReferenceText = "Resources.Resource1.Key1",
                OriginalReferenceText = "Resources.Resource1.Key1",
                ReplaceSpan = CreateTextSpan(23, 10, 23, 34),
                InlineReplaceSpan = CreateBlockSpan(23, 6, 23, 37)
            });
            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "valueInner1",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = true,
                ComesFromWebSiteResourceReference = false,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                FullReferenceText = "Resources.Resource2.Key1",
                OriginalReferenceText = "Resource2.Key1",
                ReplaceSpan = CreateTextSpan(24, 10, 25, 10),
                InlineReplaceSpan = CreateBlockSpan(24, 6, 25, 13)
            });

            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = true,
                ComesFromWebSiteResourceReference = false,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                FullReferenceText = "Resources.Resource1.Key1",
                OriginalReferenceText = "Resource1.Key1",
                ReplaceSpan = CreateTextSpan(26, 10, 28, 10),
                InlineReplaceSpan = CreateBlockSpan(26, 6, 29, 12)
            });


            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "value1",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = false,
                ComesFromWebSiteResourceReference = false,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                FullReferenceText = "Resources.Resource1.Key1",
                OriginalReferenceText = "Resource1.Key1",
                ReplaceSpan = CreateTextSpan(31, 27, 33, 10),
                InlineReplaceSpan = null
            });
            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "valueInner1",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = false,
                ComesFromWebSiteResourceReference = false,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                FullReferenceText = "Resources.Resource2.Key1",
                OriginalReferenceText = "Resource2.Key1",
                ReplaceSpan = CreateTextSpan(35, 27, 37, 10),
                InlineReplaceSpan = null
            });
            list.Add(new AspNetCodeReferenceResultItem() {
                Value = "valueInner1",
                SourceItem = projectItem,
                ComesFromClientComment = false,
                ComesFromInlineExpression = false,
                ComesFromWebSiteResourceReference = false,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                FullReferenceText = "Resources.Resource2.Key1",
                OriginalReferenceText = "Resources.Resource2.Key1",
                ReplaceSpan = CreateTextSpan(40, 9, 43, 10),
                InlineReplaceSpan = null
            });

            foreach (var item in list) {
                CalculateAbsolutePosition(item);
                if (((AspNetCodeReferenceResultItem)item).InlineReplaceSpan != null)
                    CalculateAbsolutePosition(Agent.AspNetReferencesTestFile2, ((AspNetCodeReferenceResultItem)item).InlineReplaceSpan);
            }

            validResults.Add(Agent.AspNetReferencesTestFile2, list);
        }
    }
}
