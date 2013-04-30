using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VisualLocalizer.Components;
using VisualLocalizer.Commands;
using System.IO;
using VisualLocalizer.Components.Code;

namespace VLUnitTests.VLTests {
    
    /// <summary>
    /// Tests for renaming resource keys and their references from ResX editor
    /// </summary>
    [TestClass()]
    public class RenamerTest : RunCommandsTestsBase {

        /// <summary>
        /// Tests C# files
        /// </summary>
        [TestMethod()]
        public void CSharpTest1() {
            Agent.EnsureSolutionOpen();

            string[] files = { Agent.CSharpReferencesTestFile1 };
            InternalTest(files);
        }

        /// <summary>
        /// Tests VB files
        /// </summary>
        [TestMethod()]
        public void VBTest1() {
            Agent.EnsureSolutionOpen();

            string[] files = { Agent.VBReferencesTestFile1 };
            InternalTest(files);
        }

        /// <summary>
        /// Tests ASP .NET (C# variant) files
        /// </summary>
        [TestMethod()]
        public void AspNetTest1() {
            Agent.EnsureSolutionOpen();

            string[] files = { Agent.AspNetReferencesTestFile1 };
            InternalTest(files);
        }

        /// <summary>
        /// Tests ASP .NET (VB variant) files
        /// </summary>
        [TestMethod()]
        public void AspNetTest2() {
            Agent.EnsureSolutionOpen();

            string[] files = { Agent.AspNetReferencesTestFile2 };
            InternalTest(files);
        }

        /// <summary>
        /// Generic test method
        /// </summary>
        /// <param name="files">List of files whose references should be renamed</param>
        public void InternalTest(string[] files) {
            // backup the files
            var backups = CreateBackupsOf(files);

            try {
                // get the result items
                List<CodeReferenceResultItem> list = BatchInlineLookup(files);
                VLDocumentViewsManager.CloseInvisibleWindows(typeof(BatchInlineCommand), false);
                int originalCount = list.Count;
                Assert.IsTrue(list.Count > 0);

                // rename each item by adding "XX". These resources must be present in the ResX files.
                list.ForEach((item) => {
                    item.Key = item.FullReferenceText.Substring(item.FullReferenceText.LastIndexOf('.') + 1);
                    item.KeyAfterRename = item.Key + "XX";
                });

                // run the replacer
                int errors = 0;
                BatchReferenceReplacer replacer = new BatchReferenceReplacer();
                replacer.Inline(list, true, ref errors);
                VLDocumentViewsManager.CloseInvisibleWindows(typeof(BatchInlineCommand), false);

                Assert.AreEqual(0, errors);

                // run the inline command again - the number of result items should be equal to the one before renaming
                int newCount = BatchInlineLookup(files).Count((item) => { return item.OriginalReferenceText.EndsWith("XX"); });
                VLDocumentViewsManager.CloseInvisibleWindows(typeof(BatchInlineCommand), false);
                Assert.AreEqual(originalCount, newCount);
            } finally {
                RestoreBackups(backups);
            }
        }
    }
}
