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
using EnvDTE80;
using EnvDTE;
using VisualLocalizer.Commands;

namespace VLUnitTests.VLTests {
    
    /// <summary>
    /// Tests for executing the "inline" command.
    /// </summary>
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
            
            InternalFileTest(true, files, 8);
            InternalFileTest(false, files, 8);
        }

        [TestMethod()]
        public void AspNetInlineTest2() {
            Agent.EnsureSolutionOpen();

            string[] files = { Agent.AspNetReferencesTestFile1, Agent.AspNetReferencesTestFile2 };

            InternalFileTest(true, files, 8 + 9);
            InternalFileTest(false, files, 8 + 9);
        }

        [TestMethod()]
        public void MixedInlineTest1() {
            Agent.EnsureSolutionOpen();

            string[] files = { Agent.AspNetReferencesTestFile1, Agent.CSharpReferencesTestFile1, Agent.VBReferencesTestFile1 };

            InternalFileTest(true, files, 8 + 2 + 1);
            InternalFileTest(false, files, 8 + 2 + 1);
        }

        /// <summary>
        /// Generic testing method
        /// </summary>
        /// <param name="fileOpened">True if files should be opened</param>
        /// <param name="referenceFiles">Files to test</param>
        /// <param name="correction">Number of string literals reported by the "batch move" command in the files</param>
        private void InternalFileTest(bool fileOpened, string[] referenceFiles, int correction) {
            // backup the files
            Dictionary<string, string> backups = CreateBackupsOf(referenceFiles);

            // open/close the files
            SetFilesOpened(referenceFiles, fileOpened);
           
            // run inline command in order to obtain result items
            List<CodeReferenceResultItem> inlineList = BatchInlineLookup(referenceFiles);

            // init the tool window and the grid
            int checkedCount;
            Dictionary<ProjectItem, int> sourceItemsCounts;
            BatchInlineToolWindow_Accessor window = InitBatchWindow(inlineList, out sourceItemsCounts, out checkedCount);

            try {
                // run the command
                window.RunClick(null, null);
                VLDocumentViewsManager.CloseInvisibleWindows(typeof(BatchInlineCommand), false);

                // run "batch move" command
                List<CodeStringResultItem> moveList = BatchMoveLookup(referenceFiles);
                VLDocumentViewsManager.CloseInvisibleWindows(typeof(BatchMoveCommand), false);

                // the number of string literals found by the "batch move" command, minus the string literals that were
                // already there should be equal to the number of inlined result items
                Assert.AreEqual(checkedCount, moveList.Count - correction);

                // check correct value was inlined
                int i = 0, j = 0;
                for (; i < checkedCount;) {
                    while (!moveList[j].Value.StartsWith("value")) j++;
                    while (!inlineList[i].MoveThisItem) i++;

                    Assert.AreEqual(inlineList[i].Value, moveList[j].Value);
                    i++;
                    j++;
                }

                // use the undo manager to revert the changes
                if (fileOpened) {
                    foreach (string file in referenceFiles) {
                        IOleUndoManager undoManager;
                        VLDocumentViewsManager.GetTextLinesForFile(file, false).GetUndoManager(out undoManager);

                        foreach (AbstractUndoUnit unit in undoManager.RemoveTopFromUndoStack(sourceItemsCounts[Agent.GetDTE().Solution.FindProjectItem(file)]))
                            unit.Undo();

                        Assert.AreEqual(File.ReadAllText(backups[file]), File.ReadAllText(file));
                    }
                }
            } finally {
                // close the files
                SetFilesOpened(referenceFiles, false);                                             
                VLDocumentViewsManager.CloseInvisibleWindows(typeof(BatchInlineCommand), false);

                // restore backups
                RestoreBackups(backups);   
            }
        }

        /// <summary>
        /// Initialize "inline" tool window and grid with specified list of result items
        /// </summary>        
        private BatchInlineToolWindow_Accessor InitBatchWindow(List<CodeReferenceResultItem> inlineList, out Dictionary<ProjectItem, int> sourceItemCounts, out int checkedCount) {
            BatchInlineToolWindow_Accessor window = new BatchInlineToolWindow_Accessor(new PrivateObject(new BatchInlineToolWindow()));
            window.SetData(inlineList);

            BatchInlineToolGrid grid = ((BatchInlineToolGrid)window.panel.Target);
            Random rnd = new Random();
            checkedCount = 0;
            sourceItemCounts = new Dictionary<ProjectItem, int>();

            foreach (DataGridViewCheckedRow<CodeReferenceResultItem> row in grid.Rows) {
                bool check = rnd.Next(2) == 0;
                
                row.Cells[grid.CheckBoxColumnName].Value = check;
                if (check) checkedCount++;

                if (!sourceItemCounts.ContainsKey(row.DataSourceItem.SourceItem)) sourceItemCounts.Add(row.DataSourceItem.SourceItem, 0);
                if (check) sourceItemCounts[row.DataSourceItem.SourceItem]++;
            }                        

            grid.Sort(grid.Columns[rnd.Next(grid.Columns.Count)], rnd.Next(2) == 0 ? System.ComponentModel.ListSortDirection.Ascending : System.ComponentModel.ListSortDirection.Descending);

            return window;
        }
    }
}
