using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VisualLocalizer.Components;
using VisualLocalizer.Gui;
using VisualLocalizer.Library;
using System.Windows.Forms;
using System.IO;
using Microsoft.VisualStudio.OLE.Interop;

namespace VLUnitTests.VLTests {
    
    [TestClass()]
    public class InlinerTest : RunCommandsTestsBase {

        [TestMethod()]
        public void CSharpInlineTest1() {
            Agent.EnsureSolutionOpen();

            string[] files = { Agent.CSharpReferencesTestFile1 };

            InternalFileTest(true, files, 2);
            InternalFileTest(false, files, 2);
        }

        [TestMethod()]
        public void CSharpInlineTest2() {
            Agent.EnsureSolutionOpen();

            string[] files = { Agent.CSharpReferencesTestFile1, Agent.CSharpReferencesTestFile2 };

            InternalFileTest(true, files, 2);
            InternalFileTest(false, files, 2);
        }

        [TestMethod()]
        public void VBInlineTest1() {
            Agent.EnsureSolutionOpen();

            string[] files = { Agent.VBReferencesTestFile1 };

            InternalFileTest(true, files, 1);
            InternalFileTest(false, files, 1);
        }

        [TestMethod()]
        public void AspNetInlineTest1() {
            Agent.EnsureSolutionOpen();

            string[] files = { Agent.AspNetReferencesTestFile1 };
            
            InternalFileTest(true, files, 18);
            InternalFileTest(false, files, 18);
        }

        [TestMethod()]
        public void AspNetInlineTest2() {
            Agent.EnsureSolutionOpen();

            string[] files = { Agent.AspNetReferencesTestFile1, Agent.AspNetReferencesTestFile2 };

            InternalFileTest(true, files, 18 + 13);
            InternalFileTest(false, files, 18 + 13);
        }

        [TestMethod()]
        public void MixedInlineTest1() {
            Agent.EnsureSolutionOpen();

            string[] files = { Agent.AspNetReferencesTestFile1, Agent.CSharpReferencesTestFile1, Agent.VBReferencesTestFile1 };

            InternalFileTest(true, files, 18 + 2 + 1);
            InternalFileTest(false, files, 18 + 2 + 1);
        }

        private void InternalFileTest(bool fileOpened, string[] referenceFiles, int correction) {
            Dictionary<string, string> backups = CreateBackupsOf(referenceFiles);
            SetFilesOpened(referenceFiles, fileOpened);

            List<CodeReferenceResultItem> inlineList = BatchInlineLookup(referenceFiles);

            int checkedCount;
            BatchInlineToolWindow_Accessor window = InitBatchWindow(inlineList, out checkedCount);

            try {
                window.RunClick(null, null);
                File.Copy(referenceFiles[0], @"C:\Users\Ondra\Desktop\out.txt", true);
                List<CodeStringResultItem> moveList = BatchMoveLookup(referenceFiles);

                Assert.AreEqual(checkedCount, moveList.Count - correction);

                int i = 0, j = 0;
                for (; i < checkedCount;) {
                    while (!moveList[j].Value.StartsWith("value")) j++;
                    while (!inlineList[i].MoveThisItem) i++;

                    Assert.AreEqual(inlineList[i].Value, moveList[j].Value);
                    i++;
                    j++;
                }

                if (fileOpened) {
                    foreach (string file in referenceFiles) {
                        IOleUndoManager undoManager;
                        VLDocumentViewsManager.GetTextLinesForFile(file, false).GetUndoManager(out undoManager);
                        
                        List<IOleUndoUnit> units = undoManager.RemoveTopFromUndoStack(checkedCount);
                        foreach (AbstractUndoUnit unit in units)
                            unit.Undo();

                        Assert.AreEqual(File.ReadAllText(backups[file]), File.ReadAllText(file));
                    }
                }
            } finally {
                if (fileOpened) SetFilesOpened(referenceFiles, false);
                RestoreBackups(backups);
            }
        }

        private BatchInlineToolWindow_Accessor InitBatchWindow(List<CodeReferenceResultItem> inlineList, out int checkedCount) {
            BatchInlineToolWindow_Accessor window = new BatchInlineToolWindow_Accessor(new PrivateObject(new BatchInlineToolWindow()));
            window.SetData(inlineList);

            BatchInlineToolPanel grid = ((BatchInlineToolPanel)window.panel.Target);
            Random rnd = new Random();
            checkedCount = 0;

            foreach (DataGridViewRow row in grid.Rows) {
                bool check = rnd.Next(2) == 0;
                
                row.Cells[grid.CheckBoxColumnName].Value = check;
                if (check) checkedCount++;
            }                        

            grid.Sort(grid.Columns[rnd.Next(grid.Columns.Count)], rnd.Next(2) == 0 ? System.ComponentModel.ListSortDirection.Ascending : System.ComponentModel.ListSortDirection.Descending);

            return window;
        }
    }
}
