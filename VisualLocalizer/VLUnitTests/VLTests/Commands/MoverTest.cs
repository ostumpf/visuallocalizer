using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using VisualLocalizer.Commands;
using System.Collections;
using VisualLocalizer.Gui;
using VisualLocalizer;
using EnvDTE;
using System.IO;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using VisualLocalizer.Extensions;
using Microsoft.VisualStudio.OLE.Interop;
using VisualLocalizer.Components.Code;
using VisualLocalizer.Library.Extensions;
using VisualLocalizer.Library.Components;
using VisualLocalizer.Library.Gui;
using VisualLocalizer.Commands.Move;
using VisualLocalizer.Commands.Inline;

namespace VLUnitTests.VLTests {

    /// <summary>
    /// Tests execution of the "move to resources" command
    /// </summary>
    [TestClass()]
    public class MoverTest : RunCommandsTestsBase {

        [TestMethod()]
        public void CSharpMoveTest1() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.CSharpStringsTestFile1 };
            string[] targetFiles = new string[] { Agent.CSharpResourceFile1, Agent.CSharpResourceFileLib };

            InternalFileMoveTest(true, false, false, testFiles, targetFiles);
            InternalFileMoveTest(false, false, false, testFiles, targetFiles);        
        }

        [TestMethod()]
        public void CSharpMoveTest2() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.CSharpStringsTestFile1 };
            string[] targetFiles = new string[] { Agent.CSharpResourceFile1, Agent.CSharpResourceFileLib };

            InternalFileMoveTest(true, true, false, testFiles, targetFiles);
            InternalFileMoveTest(false, true, false, testFiles, targetFiles);          
        }

        [TestMethod()]
        public void CSharpMoveTest3() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.CSharpStringsTestFile1, Agent.CSharpStringsTestFile2, Agent.CSharpStringsTestFile3 };
            string[] targetFiles = new string[] { Agent.CSharpResourceFile1, Agent.CSharpResourceFileLib };

            InternalFileMoveTest(true, false, false, testFiles, targetFiles);
            InternalFileMoveTest(false, false, false, testFiles, targetFiles);                
        }


        [TestMethod()]
        public void VBMoveTest1() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.VBStringsTestFile1 };
            string[] targetFiles = new string[] { Agent.VBResourceFile1, Agent.VBResourceFileLib, Agent.CSharpResourceFileLib };

            InternalFileMoveTest(true, false, false, testFiles, targetFiles);
            InternalFileMoveTest(false, false, false, testFiles, targetFiles);        
        }

        [TestMethod()]
        public void VBMoveTest2() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.VBStringsTestFile1 };
            string[] targetFiles = new string[] { Agent.VBResourceFile1, Agent.VBResourceFileLib, Agent.CSharpResourceFileLib };

            InternalFileMoveTest(true, true, false, testFiles, targetFiles);
            InternalFileMoveTest(false, true, false, testFiles, targetFiles);            
        }

        [TestMethod()]
        public void VBMoveTest3() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.VBStringsTestFile1, Agent.VBStringsTestFile2, Agent.VBStringsTestFile3 };
            string[] targetFiles = new string[] { Agent.VBResourceFile1, Agent.VBResourceFileLib, Agent.CSharpResourceFileLib };

            InternalFileMoveTest(true, false, false, testFiles, targetFiles);
            InternalFileMoveTest(false, false, false, testFiles, targetFiles);            
        }

        [TestMethod()]
        public void AspNetMoveTest1() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.AspNetStringsTestFile1 };
            string[] targetFiles = new string[] { Agent.AspNetResourceFile };

            InternalFileMoveTest(true, false, false, testFiles, targetFiles);
            InternalFileMoveTest(false, false, false, testFiles, targetFiles);        
        }

        [TestMethod()]
        public void AspNetMoveTest2() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.AspNetStringsTestFile1 };
            string[] targetFiles = new string[] { Agent.AspNetResourceFile };

            InternalFileMoveTest(true, true, false, testFiles, targetFiles);
            InternalFileMoveTest(false, true, false, testFiles, targetFiles);        
        }

        [TestMethod()]
        public void AspNetMoveTest3() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.AspNetStringsTestFile1, Agent.AspNetStringsTestFile2 };
            string[] targetFiles = new string[] { Agent.AspNetResourceFile };

            InternalFileMoveTest(true, false, false, testFiles, targetFiles);
            InternalFileMoveTest(false, false, false, testFiles, targetFiles);        
        }

        [TestMethod()]
        public void MixedTest1() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.CSharpStringsTestFile1, Agent.VBStringsTestFile1 };
            string[] targetFiles = new string[] { Agent.CSharpResourceFileLib };

            InternalFileMoveTest(true, false, false, testFiles, targetFiles);
            InternalFileMoveTest(false, false, false, testFiles, targetFiles);        
        }

        [TestMethod()]
        public void MarkTest1() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.CSharpStringsTestFile1 };
            string[] targetFiles = new string[] { Agent.CSharpResourceFileLib, Agent.CSharpResourceFile1 };

            InternalFileMoveTest(true, false, true, testFiles, targetFiles);
            InternalFileMoveTest(false, false, true, testFiles, targetFiles);    
        }

        [TestMethod()]
        public void MarkTest2() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.VBStringsTestFile1 };
            string[] targetFiles = new string[] { Agent.VBResourceFileLib, Agent.VBResourceFile1 };

            InternalFileMoveTest(true, false, true, testFiles, targetFiles);
            InternalFileMoveTest(false, false, true, testFiles, targetFiles);        
        }

        [TestMethod()]
        public void MarkTest3() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.AspNetStringsTestFile1 };
            string[] targetFiles = new string[] { Agent.AspNetResourceFile};

            InternalFileMoveTest(true, false, false, testFiles, targetFiles);
            InternalFileMoveTest(false, false, false, testFiles, targetFiles);        
        }

        /// <summary>
        /// Generic test method
        /// </summary>
        /// <param name="openFiles">True if the files should be opened first</param>
        /// <param name="fullName">True if the "Use full names" policy should be applied</param>
        /// <param name="mark">True if the "Mark with VL_NO_LOC policy should be applied"</param>
        /// <param name="testFiles">Files to test</param>
        /// <param name="targetFiles">ResX files where resources can be moved</param>
        private void InternalFileMoveTest(bool openFiles, bool fullName, bool mark, string[] testFiles, string[] targetFiles) {
            // backup the files
            Dictionary<string, string> backups = CreateBackupsOf(testFiles);            

            // run the "batch move" command to get result items
            List<CodeStringResultItem> items = BatchMoveLookup(testFiles);
            
            // open the necessary files
            SetFilesOpened(testFiles, openFiles);
          
            // initialize ResX destination files
            Dictionary<string, ResXProjectItem> resxItems = InitResxItems(targetFiles, Agent.GetDTE().Solution.FindProjectItem(testFiles[0]).ContainingProject);
            
            // initialize the "batch move" tool window and grid
            Dictionary<ResXProjectItem, int> resxCounts;
            Dictionary<ProjectItem, int> sourceItemCounts;
            int expectedToBeMarked;
            BatchMoveToResourcesToolWindow_Accessor window = InitBatchToolWindow(resxItems.Values.ToList(), items, fullName, mark, out resxCounts, out sourceItemCounts, out expectedToBeMarked);

            try {
                // execute
                window.RunClick(null, null);                               
                VLDocumentViewsManager.CloseInvisibleWindows(typeof(BatchMoveCommand), false);
                
                // verify every ResX file contains the correct number of resources
                foreach (ResXProjectItem target in resxItems.Values) {
                    Assert.AreEqual(resxCounts.ContainsKey(target) ? resxCounts[target] : 0, GetResourcesCountIn(target));
                }

                // run the "inline" command to verify all references are valid
                int checkedCount = resxCounts.Sum((pair) => { return pair.Value; });
                List<CodeReferenceResultItem> inlineResults = BatchInlineLookup(testFiles);
                VLDocumentViewsManager.CloseInvisibleWindows(typeof(BatchInlineCommand), false);
                Assert.AreEqual(checkedCount, inlineResults.Count);
                 
                // test if every unchecked string result item was marked 
                if (mark) {
                    int markedAfter = BatchMoveLookup(testFiles).Count((item) => { return item.IsMarkedWithUnlocalizableComment; });
                    VLDocumentViewsManager.CloseInvisibleWindows(typeof(BatchMoveCommand), false);
                    Assert.AreEqual(expectedToBeMarked, markedAfter, "mark");
                }

                // test if qualified name was really used
                if (fullName) {
                    foreach (var result in inlineResults) {
                        AspNetCodeReferenceResultItem citem = result as AspNetCodeReferenceResultItem;
                        if (citem == null || !citem.ComesFromWebSiteResourceReference) {
                            Assert.AreEqual(result.OriginalReferenceText, result.FullReferenceText);
                        }
                    }
                }

                // check if no import statement has been added twice
                foreach (string path in testFiles) {
                    CheckForDuplicateUsings(path);
                }

                if (openFiles) {
                    // use undo manager to revert the changes
                    foreach (string file in testFiles) {
                        var win = VsShellUtilities.GetWindowObject(VLDocumentViewsManager.GetWindowFrameForFile(file, false));
                        ProjectItem pitem = Agent.GetDTE().Solution.FindProjectItem(file);
                        int count = sourceItemCounts.ContainsKey(pitem) ? sourceItemCounts[pitem] : 0;

                        IOleUndoManager manager;
                        VLDocumentViewsManager.GetTextLinesForFile(file, false).GetUndoManager(out manager);
                        List<IOleUndoUnit> list = manager.RemoveTopFromUndoStack(count);
                        foreach (AbstractUndoUnit unit in list)
                            unit.Undo();

                        Assert.AreEqual(File.ReadAllText(backups[file]), File.ReadAllText(file));
                    }

                    // check that all changes were fully reverted
                    int sum = resxItems.Values.Sum((item) => { return GetResourcesCountIn(item); });
                    Assert.AreEqual(0, sum);                
                }
                              
            } finally {
                // close the files
                SetFilesOpened(testFiles, false);

                // restore the backups
                RestoreBackups(backups);

                // clear ResX files
                foreach (ResXProjectItem target in resxItems.Values) {
                    ClearFile(target);                    
                }
                VLDocumentViewsManager.CloseInvisibleWindows(typeof(BatchMoveCommand), false);
            }
        }
        
        /// <summary>
        /// Check the given file for multiply added import statements
        /// </summary>        
        private void CheckForDuplicateUsings(string path) {            
            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(path);
            if (projectItem.GetFileType() == FILETYPE.ASPX) return;

            HashSet<string> namespaces = new HashSet<string>();
            bool fileOpened;

            foreach (CodeElement codeElement in projectItem.GetCodeModel(true, true, out fileOpened).CodeElements) {
                if (codeElement.Kind != vsCMElement.vsCMElementImportStmt) continue;
                CodeImport import=(CodeImport)codeElement;
                if (namespaces.Contains(import.Namespace)) Assert.Fail();

                namespaces.Add(import.Namespace);
            }

            if (fileOpened) DocumentViewsManager.CloseFile(path);
        }

        /// <summary>
        /// Initializes list of ResX items
        /// </summary>       
        private Dictionary<string, ResXProjectItem> InitResxItems(string[] targetFiles, Project containingProject) {
            Dictionary<string, ResXProjectItem> d = new Dictionary<string, ResXProjectItem>();

            for (int i = 0; i < targetFiles.Length; i++) {
                ProjectItem pitem = Agent.GetDTE().Solution.FindProjectItem(targetFiles[i]);
                d.Add(targetFiles[i], ResXProjectItem.ConvertToResXItem(pitem, containingProject));
            }            

            return d;
        }

        /// <summary>
        /// Initializes "batch move to resources" tool window and grid
        /// </summary>
        /// <param name="resxItems">List of possible destination items</param>
        /// <param name="items">Result items</param>
        /// <param name="fullName">True if "use full name" policy should be applied</param>
        /// <param name="mark">True if "mark with VL_NO_LOC" policy should be applied</param>
        /// <param name="resxCounts">Number of resources determined to be moved to each ResX file</param>
        /// <param name="sourceItemCounts">Number of resource items for each source code file</param>
        /// <param name="expectedToBeMarked">Number of resources that are expected to be marked with VL_NO_LOC</param>
        /// <returns></returns>
        private BatchMoveToResourcesToolWindow_Accessor InitBatchToolWindow(List<ResXProjectItem> resxItems, List<CodeStringResultItem> items, bool fullName, bool mark, out Dictionary<ResXProjectItem, int> resxCounts, out Dictionary<ProjectItem, int> sourceItemCounts, out int expectedToBeMarked) {
            DTE2 dte = Agent.GetDTE();
            expectedToBeMarked = 0;
            sourceItemCounts = new Dictionary<ProjectItem, int>();

            // init window
            BatchMoveToResourcesToolWindow_Accessor  window = new BatchMoveToResourcesToolWindow_Accessor(new PrivateObject(new BatchMoveToResourcesToolWindow()));            
            window.SetData(items);

            // init the policies
            window.currentNamespacePolicy = window.NAMESPACE_POLICY_ITEMS[fullName ? 1 : 0];
            window.currentRememberOption = window.REMEMBER_OPTIONS[mark ? 1 : 0];

            BatchMoveToResourcesToolGrid grid= ((BatchMoveToResourcesToolGrid)window.panel.ToolGrid.Target);
            int x = 0;
                        
            Random rnd = new Random();
            resxCounts = new Dictionary<ResXProjectItem, int>();
            
            // check/uncheck random rows
            foreach (DataGridViewKeyValueRow<CodeStringResultItem> row in grid.Rows) {
                bool check = rnd.Next(2) == 0;
                if (check) {
                    // set unique key
                    row.Cells[grid.KeyColumnName].Value = string.Format("xx{0}", x);

                    // select random destination item
                    ResXProjectItem destResX = resxItems[rnd.Next(resxItems.Count)];
                    row.Cells[grid.DestinationColumnName].Value = destResX.ToString();

                    if (!resxCounts.ContainsKey(destResX)) resxCounts.Add(destResX, 0);
                    resxCounts[destResX]++;
                    if (!sourceItemCounts.ContainsKey(row.DataSourceItem.SourceItem)) sourceItemCounts.Add(row.DataSourceItem.SourceItem, 0);
                    sourceItemCounts[row.DataSourceItem.SourceItem]++;
                } else {
                    AspNetStringResultItem aitem = row.DataSourceItem as AspNetStringResultItem;
                    if (((row.DataSourceItem is CSharpStringResultItem) || (aitem != null && aitem.ComesFromCodeBlock && aitem.Language == LANGUAGE.CSHARP))) {
                        if (mark && !row.DataSourceItem.IsMarkedWithUnlocalizableComment) {
                            if (!sourceItemCounts.ContainsKey(row.DataSourceItem.SourceItem)) sourceItemCounts.Add(row.DataSourceItem.SourceItem, 0);
                            sourceItemCounts[row.DataSourceItem.SourceItem]++;                            
                        }
                        expectedToBeMarked++;
                    }
                }
                
                row.Cells[grid.CheckBoxColumnName].Value = check;
                window.panel.ToolGrid.Validate(row);
                if (check) Assert.IsTrue(string.IsNullOrEmpty(row.ErrorText), row.ErrorText);
                x++;                               
            }

            // randomly sort the grid
            grid.Sort(grid.Columns[rnd.Next(grid.Columns.Count)], rnd.Next(2) == 0 ? System.ComponentModel.ListSortDirection.Ascending : System.ComponentModel.ListSortDirection.Descending);

            return window;
        }

        /// <summary>
        /// Removes all data from specified ResX file
        /// </summary>        
        private void ClearFile(ResXProjectItem target) {
            try {
                target.Load();
                target.Data.Clear();
                target.Flush();
                target.Unload();
            } catch (Exception) { }
        }

        /// <summary>
        /// Returns number of string resources in specified ResX file
        /// </summary>        
        private int GetResourcesCountIn(ResXProjectItem item) {
            item.Load();
            int result = item.GetAllStringReferences(false).Count;
            item.Unload();

            return result;
        }
    }
}
