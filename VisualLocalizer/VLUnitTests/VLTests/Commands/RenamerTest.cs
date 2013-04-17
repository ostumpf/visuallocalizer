using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VisualLocalizer.Components;
using VisualLocalizer.Commands;
using System.IO;

namespace VLUnitTests.VLTests {
    
    [TestClass()]
    public class RenamerTest : RunCommandsTestsBase {

        [TestMethod()]
        public void CSharpTest1() {
            Agent.EnsureSolutionOpen();

            string[] files = { Agent.CSharpReferencesTestFile1 };
            InternalTest(files);
        }

        [TestMethod()]
        public void VBTest1() {
            Agent.EnsureSolutionOpen();

            string[] files = { Agent.VBReferencesTestFile1 };
            InternalTest(files);
        }

        [TestMethod()]
        public void AspNetTest1() {
            Agent.EnsureSolutionOpen();

            string[] files = { Agent.AspNetReferencesTestFile1 };
            InternalTest(files);
        }

        [TestMethod()]
        public void AspNetTest2() {
            Agent.EnsureSolutionOpen();

            string[] files = { Agent.AspNetReferencesTestFile2 };
            InternalTest(files);
        }

        public void InternalTest(string[] files) {
            var backups = CreateBackupsOf(files);
            try {
                List<CodeReferenceResultItem> list = BatchInlineLookup(files);
                VLDocumentViewsManager.CloseInvisibleWindows(typeof(BatchInlineCommand), false);
                int originalCount = list.Count;
                Assert.IsTrue(list.Count > 0);

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

                int newCount = BatchInlineLookup(files).Count((item) => { return item.OriginalReferenceText.EndsWith("XX"); });
                VLDocumentViewsManager.CloseInvisibleWindows(typeof(BatchInlineCommand), false);
                Assert.AreEqual(originalCount, newCount);
            } finally {
                RestoreBackups(backups);
            }
        }
    }
}
