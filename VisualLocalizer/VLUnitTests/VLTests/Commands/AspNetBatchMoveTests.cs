using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VisualLocalizer.Components;
using EnvDTE80;
using VisualLocalizer.Commands;
using EnvDTE;
using System.IO;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell;

namespace VLUnitTests.VLTests {

    [TestClass()]
    public class AspNetBatchMoveTests : BatchTestsBase {
        private static Dictionary<string, List<AbstractResultItem>> validResults = new Dictionary<string, List<AbstractResultItem>>();

        public static List<AbstractResultItem> GetExpectedResultsFor(string file) {
            if (!validResults.ContainsKey(file)) {
                if (file == Agent.AspNetStringsTestFile1) {
                    GenerateValidResultsForStrings1();
                } else if (file == Agent.AspNetStringsTestFile2) {
                    GenerateValidResultsForStrings2();
                } else if (file == Agent.AspNetStringsCustomAspxFile1) {
                    GenerateValidResultsForCustomFile1();
                } else if (file == Agent.AspNetStringsCustomAspxFile2) {
                    GenerateValidResultsForCustomFile2();
                } else if (file == Agent.AspNetStringsCustomCsFile1) {
                    GenerateValidResultsForCustomCsFile1();
                } else if (file == Agent.AspNetStringsCustomVbFile2) {
                    GenerateValidResultsForCustomVbFile2();
                } else throw new Exception("Cannot resolve test file name.");
            } 

            return validResults[file];
        }                                

        [TestMethod()]
        public void ProcessSelectedItemsTest1() {
            string[] itemsToSelect = { Agent.AspNetStringsTestFile1, Agent.AspNetStringsTestFolder1 };
            string[] expectedFiles = { Agent.AspNetStringsTestFile1, Agent.AspNetStringsCustomAspxFile1, Agent.AspNetStringsCustomCsFile1, Agent.AspNetStringsCustomAspxFile2, Agent.AspNetStringsCustomVbFile2 };

            GenericTest(Agent.BatchMoveCommand, itemsToSelect, expectedFiles, GetExpectedResultsFor);
        }

        [TestMethod()]
        public void ProcessSelectedItemsTest2() {
            string[] itemsToSelect = { Agent.AspNetStringsTestFile2, Agent.AspNetStringsCustomCsFile1, Agent.AspNetStringsCustomVbFile2 };
            string[] expectedFiles = { Agent.AspNetStringsTestFile2, Agent.AspNetStringsCustomCsFile1, Agent.AspNetStringsCustomVbFile2 };

            GenericTest(Agent.BatchMoveCommand, itemsToSelect, expectedFiles, GetExpectedResultsFor);
        }

        [TestMethod()]
        public void ProcessSelectedItemsTest3() {
            string[] itemsToSelect = { Agent.AspNetStringsTestFolder1, Agent.AspNetStringsCustomAspxFile2 };
            string[] expectedFiles = { Agent.AspNetStringsCustomAspxFile1, Agent.AspNetStringsCustomCsFile1, Agent.AspNetStringsCustomAspxFile2, Agent.AspNetStringsCustomVbFile2 };

            GenericTest(Agent.BatchMoveCommand, itemsToSelect, expectedFiles, GetExpectedResultsFor);
        }

        [TestMethod()]
        public void ProcessTest() {
            Agent.EnsureSolutionOpen();

            DTE2 DTE = Agent.GetDTE();
            BatchMoveCommand target = Agent.BatchMoveCommand;

            var window = DTE.OpenFile(null, Agent.AspNetStringsTestFile1);
            window.Activate();

            target.Process(true);

            Assert.IsTrue(VLDocumentViewsManager.IsFileLocked(Agent.AspNetStringsTestFile1));

            ValidateResults(GetExpectedResultsFor(Agent.AspNetStringsTestFile1), target.Results);

            window.Close(EnvDTE.vsSaveChanges.vsSaveChangesNo);
            Assert.IsTrue(VLDocumentViewsManager.IsFileLocked(Agent.AspNetStringsTestFile1));

            VLDocumentViewsManager.ReleaseLocks();
            Assert.IsFalse(VLDocumentViewsManager.IsFileLocked(Agent.AspNetStringsTestFile1));
        }

        [TestMethod()]
        public void ProcessSelectionTest() {
            Agent.EnsureSolutionOpen();

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.AspNetStringsTestFile1, true, true);
            IVsTextLines lines = VLDocumentViewsManager.GetTextLinesForFile(Agent.AspNetStringsTestFile1, false);

            GenericSelectionTest(Agent.BatchMoveCommand_Accessor, Agent.AspNetStringsTestFile1, view, lines, GetExpectedResultsFor);
        }

        [TestMethod()]
        public void ProcessEmptySelectionTest() {
            Agent.EnsureSolutionOpen();

            BatchMoveCommand_Accessor target = Agent.BatchMoveCommand_Accessor;

            IVsTextView view = VLDocumentViewsManager.GetTextViewForFile(Agent.AspNetStringsTestFile1, true, true);
            view.SetSelection(14, 60, 16, 25);

            List<CodeStringResultItem> emptyList = new List<CodeStringResultItem>();
            target.ProcessSelection(true);
            ValidateResults(emptyList, target.Results);
            Assert.IsFalse(VLDocumentViewsManager.IsFileLocked(Agent.AspNetStringsTestFile1));

            VsShellUtilities.GetWindowObject(VLDocumentViewsManager.GetWindowFrameForFile(Agent.CSharpStringsTestFile1, false)).Close(vsSaveChanges.vsSaveChangesNo);
        }



        private static void GenerateValidResultsForStrings1() {
            List<AbstractResultItem> list = new List<AbstractResultItem>();
            
            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(Agent.AspNetStringsTestFile1);
            string className = Path.GetFileNameWithoutExtension(projectItem.Name);
    
            list.Add(new TestAspNetStringResultItem() {
                Value = "True",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = true,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Page",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(0, 40, 0, 44)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "strings1.aspx.cs",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = true,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Page",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(0, 56, 0, 72)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "strings1",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = true,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Page",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(0, 84, 0, 92)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "-//W3C//DTD XHTML 1.0 Transitional//EN",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "!DOCTYPE",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(2, 23, 2, 61)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "!DOCTYPE",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(2, 64, 2, 119)
            });



            list.Add(new TestAspNetStringResultItem() {
                Value = "http://www.w3.org/1999/xhtml",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "html",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(4, 13, 4, 41)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "server",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "head",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(5, 13, 5, 19)
            });

            list.Add(new TestAspNetStringResultItem() {
                Value = "Untitled Page",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = true,
                ElementName = null,
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(6, 11, 6, 24)
            });
            
            list.Add(new TestAspNetStringResultItem() {
                Value = "server",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "form",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(9, 28, 9, 34)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "server",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "MyElement1",
                ElementPrefix = "my",
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(10, 30, 10, 36)
            });
            
            list.Add(new TestAspNetStringResultItem() {
                Value = "test1value",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "MyElement1",
                ElementPrefix = "my",
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = true,
                ReplaceSpan = CreateTextSpan(10, 57, 10, 67)
            });
            
            list.Add(new TestAspNetStringResultItem() {
                Value = "test3value",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = true,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "MyElement1",
                ElementPrefix = "my",
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = true,
                ReplaceSpan = CreateTextSpan(10, 86, 10, 96)
            });



            list.Add(new TestAspNetStringResultItem() {
                Value = "server",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "MyElement2",
                ElementPrefix = "my",
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(11, 30, 11, 36)
            });
            
            list.Add(new TestAspNetStringResultItem() {
                Value = "test1value",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "MyElement2",
                ElementPrefix = "my",
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = true,
                ReplaceSpan = CreateTextSpan(11, 57, 11, 67)
            });
            
            list.Add(new TestAspNetStringResultItem() {
                Value = "test3value",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = true,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "MyElement2",
                ElementPrefix = "my",
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = true,
                ReplaceSpan = CreateTextSpan(11, 86, 11, 96)
            });


            list.Add(new TestAspNetStringResultItem() {
                Value = "server",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Button",
                ElementPrefix = "asp",
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(13, 27, 13, 33)
            });
        
            list.Add(new TestAspNetStringResultItem() {
                Value = "button1_text",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Button",
                ElementPrefix = "asp",
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = true,
                ReplaceSpan = CreateTextSpan(13, 54, 13, 66)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "17",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Button",
                ElementPrefix = "asp",
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(13, 111, 13, 113)
            });


            list.Add(new TestAspNetStringResultItem() {
                Value = "server",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Button",
                ElementPrefix = "asp",
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(14, 27, 14, 33)
            });
        
            list.Add(new TestAspNetStringResultItem() {
                Value = "<",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Button",
                ElementPrefix = "asp",
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = true,
                ReplaceSpan = CreateTextSpan(14, 54, 14, 58)
            });


            list.Add(new TestAspNetStringResultItem() {
                Value = "asp1",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = true,
                ComesFromDirective = false,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = null,
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(16, 26, 16, 32)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = @"asp2""asp2",
                WasVerbatim = true,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = true,
                ComesFromDirective = false,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = null,
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(17, 26, 17, 39)
            });


            list.Add(new TestAspNetStringResultItem() {
                Value = "lit_1",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = false,
                ComesFromInlineExpression = true,
                ComesFromPlainText = false,
                ElementName = null,
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(19, 32, 19, 39)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "\"",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Literal",
                ElementPrefix = "asp",
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = true,
                ReplaceSpan = CreateTextSpan(20, 28, 20, 34)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "t\"a\"v",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Literal",
                ElementPrefix = "asp",
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = true,
                ReplaceSpan = CreateTextSpan(22, 28, 22, 33)
            });

            list.Add(new TestAspNetStringResultItem() {
                Value = @"    
         
         PlainText
         ",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = true,
                ElementName = null,
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(22, 49, 25, 9)
            });

            list.Add(new TestAspNetStringResultItem() {
                Value = "<!--",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = false,
                ComesFromInlineExpression = true,
                ComesFromPlainText = false,
                ElementName = null,
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(25, 13, 25, 19)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "<!--",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Literal",
                ElementPrefix = "asp",
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = true,
                ReplaceSpan = CreateTextSpan(26, 28, 26, 32)
            });


            list.Add(new TestAspNetStringResultItem() {
                Value = "PlainText2",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = true,
                ElementName = null,
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(26, 48, 26, 58)
            });

            list.Add(new TestAspNetStringResultItem() {
                Value = "@",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Literal",
                ElementPrefix = "asp",
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = true,
                ReplaceSpan = CreateTextSpan(26, 77, 26, 78)
            });
            
            list.Add(new TestAspNetStringResultItem() {
                Value = "scr",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = true,
                ComesFromDirective = false,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = null,
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(27, 43, 27, 48)
            });

            
            list.Add(new TestAspNetStringResultItem() {               
                Value = "output",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = false,
                ComesFromInlineExpression = true,
                ComesFromPlainText = false,
                ElementName = null,
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(29, 13, 29, 21)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "test2",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = true,
                ComesFromDirective = false,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = null,
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(31, 24, 31, 31)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "aaa",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = true,
                ComesFromDirective = false,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = null,
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(33, 23, 33, 28)
            });            


            foreach (var item in list) {
                CalculateAbsolutePosition(item);                
            }

            validResults.Add(Agent.AspNetStringsTestFile1, list);
        }

        private static void GenerateValidResultsForStrings2() {
            List<AbstractResultItem> list = new List<AbstractResultItem>();

            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(Agent.AspNetStringsTestFile2);
            string className = Path.GetFileNameWithoutExtension(projectItem.Name);

            list.Add(new TestAspNetStringResultItem() {
                Value = "false",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = true,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Page",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(0, 40, 0, 45)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "strings2.aspx.vb",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = true,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Page",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(0, 57, 0, 73)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "strings2",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = true,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Page",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(0, 85, 0, 93)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "-//W3C//DTD XHTML 1.0 Transitional//EN",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "!DOCTYPE",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(2, 23, 2, 61)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "!DOCTYPE",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(2, 64, 2, 119)
            });



            list.Add(new TestAspNetStringResultItem() {
                Value = "http://www.w3.org/1999/xhtml",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "html",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(4, 13, 4, 41)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "server",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "head",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(5, 13, 5, 19)
            });

            list.Add(new TestAspNetStringResultItem() {
                Value = "Untitled Page",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = true,
                ElementName = null,
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(6, 11, 6, 24)
            });

            list.Add(new TestAspNetStringResultItem() {
                Value = "server",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "form",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(9, 28, 9, 34)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = @"some""string""",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = false,
                ComesFromInlineExpression = true,
                ComesFromPlainText = false,
                ElementName = null,
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(10, 11, 10, 27)
            });


            list.Add(new TestAspNetStringResultItem() {
                Value = "test",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = true,
                ComesFromDirective = false,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = null,
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(12, 30, 12, 36)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "test\"test",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = true,
                ComesFromDirective = false,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = null,
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(13, 30, 13, 42)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "ee",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = true,
                ComesFromDirective = false,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = null,
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(17, 30, 17, 34)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "x23",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = true,
                ComesFromDirective = false,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = null,
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(20, 30, 20, 35)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "40",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = true,
                ComesFromDirective = false,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = null,
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(22, 28, 22, 32)
            });

            list.Add(new TestAspNetStringResultItem() {
                Value = "ttt",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = true,
                ComesFromDirective = false,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = null,
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(23, 50, 23, 55)
            });


            foreach (var item in list) CalculateAbsolutePosition(item);

            validResults.Add(Agent.AspNetStringsTestFile2, list);
        }

        private static void GenerateValidResultsForCustomFile1() {
            List<AbstractResultItem> list = new List<AbstractResultItem>();

            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(Agent.AspNetStringsCustomAspxFile1);
            string className = Path.GetFileNameWithoutExtension(projectItem.Name);
            
            list.Add(new TestAspNetStringResultItem() {
                Value = "true",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = true,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Control",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(0, 43, 0, 47)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "Custom1.ascx.cs",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = true,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Control",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(0, 59, 0, 74)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "Custom1",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = true,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Control",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(0, 86, 0, 93)
            });


            list.Add(new TestAspNetStringResultItem() {
                Value = "server",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Button",
                ElementPrefix = "asp",
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(1, 19, 1, 25)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "buttontext",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Button",
                ElementPrefix = "asp",
                Language = VisualLocalizer.Library.LANGUAGE.CSHARP,
                LocalizabilityProved = true,
                ReplaceSpan = CreateTextSpan(1, 46, 1, 56)
            });


            foreach (var item in list) CalculateAbsolutePosition(item);

            validResults.Add(Agent.AspNetStringsCustomAspxFile1, list);
        }

        private static void GenerateValidResultsForCustomFile2() {
            List<AbstractResultItem> list = new List<AbstractResultItem>();

            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(Agent.AspNetStringsCustomAspxFile2);
            string className = Path.GetFileNameWithoutExtension(projectItem.Name);

            list.Add(new TestAspNetStringResultItem() {
                Value = "false",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = true,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Control",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(0, 43, 0, 48)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "Custom2.ascx.vb",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = true,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Control",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(0, 60, 0, 75)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "Controls_Custom2",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = true,
                ComesFromElement = false,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Control",
                ElementPrefix = null,
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(0, 87, 0, 103)
            });


            list.Add(new TestAspNetStringResultItem() {
                Value = "server",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Button",
                ElementPrefix = "asp",
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = false,
                ReplaceSpan = CreateTextSpan(1, 19, 1, 25)
            });
            list.Add(new TestAspNetStringResultItem() {
                Value = "VB .NET",
                WasVerbatim = false,
                SourceItem = projectItem,
                ClassOrStructElementName = className,
                IsWithinLocalizableFalse = false,
                ComesFromDesignerFile = false,
                ComesFromClientComment = false,
                ComesFromCodeBlock = false,
                ComesFromDirective = false,
                ComesFromElement = true,
                ComesFromInlineExpression = false,
                ComesFromPlainText = false,
                ElementName = "Button",
                ElementPrefix = "asp",
                Language = VisualLocalizer.Library.LANGUAGE.VB,
                LocalizabilityProved = true,
                ReplaceSpan = CreateTextSpan(1, 49, 1, 56)
            });

            foreach (var item in list) CalculateAbsolutePosition(item);

            validResults.Add(Agent.AspNetStringsCustomAspxFile2, list);
        }

        private static void GenerateValidResultsForCustomCsFile1() {
            List<AbstractResultItem> list = new List<AbstractResultItem>();

            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(Agent.AspNetStringsCustomCsFile1);
            string className = Path.GetFileNameWithoutExtension(projectItem.Name);

            list.Add(new TestCSharpStringResultItem() {
                Value = "custom1",
                WasVerbatim = true,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "Custom1",
                MethodElementName = "Page_Load",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = true,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(17, 19, 17, 29)
            });
            list.Add(new TestCSharpStringResultItem() {
                Value = "cus\"t\"om1",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "Custom1",
                MethodElementName = "Page_Load",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = true,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(18, 19, 18, 32)
            });

            foreach (var item in list) CalculateAbsolutePosition(item);

            validResults.Add(Agent.AspNetStringsCustomCsFile1, list);
        }

        private static void GenerateValidResultsForCustomVbFile2() {
            List<AbstractResultItem> list = new List<AbstractResultItem>();

            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(Agent.AspNetStringsCustomVbFile2);
            string className = Path.GetFileNameWithoutExtension(projectItem.Name);

            list.Add(new TestVBStringResultItem() {
                Value = "test1",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "Controls_Custom2",
                MethodElementName = "Test1",
                IsWithinLocalizableFalse = false,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = true,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(7, 19, 7, 26)
            });
            list.Add(new TestVBStringResultItem() {
                Value = "test3",
                WasVerbatim = false,
                SourceItem = projectItem,
                VariableElementName = null,
                ClassOrStructElementName = "Controls_Custom2",
                MethodElementName = "Test3",
                IsWithinLocalizableFalse = true,
                IsMarkedWithUnlocalizableComment = false,
                ComesFromDesignerFile = true,
                NamespaceElementName = null,
                ReplaceSpan = CreateTextSpan(26, 19, 26, 26)
            });

            foreach (var item in list) CalculateAbsolutePosition(item);

            validResults.Add(Agent.AspNetStringsCustomVbFile2, list);
        }
    }
}
