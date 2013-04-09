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

namespace VLUnitTests.VLTests {

    [TestClass()]
    public class MoverTest : RunCommandsTestsBase {

        [TestMethod()]
        public void CSharpMoveTest1() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.CSharpStringsTestFile1 };
            string[] targetFiles = new string[] { Agent.CSharpResourceFile1, Agent.CSharpResourceFileLib };

            InternalOpenedFileMoveTest(false, false, testFiles, targetFiles);
            InternalClosedFileMoveTest(false, false, testFiles, targetFiles);        
        }

        [TestMethod()]
        public void CSharpMoveTest2() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.CSharpStringsTestFile1 };
            string[] targetFiles = new string[] { Agent.CSharpResourceFile1, Agent.CSharpResourceFileLib };

            InternalOpenedFileMoveTest(true, false, testFiles, targetFiles);
            InternalClosedFileMoveTest(true, false, testFiles, targetFiles);          
        }

        [TestMethod()]
        public void CSharpMoveTest3() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.CSharpStringsTestFile1, Agent.CSharpStringsTestFile2, Agent.CSharpStringsTestFile3 };
            string[] targetFiles = new string[] { Agent.CSharpResourceFile1, Agent.CSharpResourceFileLib };

            InternalOpenedFileMoveTest(false, false, testFiles, targetFiles);
            InternalClosedFileMoveTest(false, false, testFiles, targetFiles);            
        }


        [TestMethod()]
        public void VBMoveTest1() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.VBStringsTestFile1 };
            string[] targetFiles = new string[] { Agent.VBResourceFile1, Agent.VBResourceFileLib, Agent.CSharpResourceFileLib };

            InternalOpenedFileMoveTest(false, false, testFiles, targetFiles);
            InternalClosedFileMoveTest(false, false, testFiles, targetFiles);            
        }

        [TestMethod()]
        public void VBMoveTest2() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.VBStringsTestFile1 };
            string[] targetFiles = new string[] { Agent.VBResourceFile1, Agent.VBResourceFileLib, Agent.CSharpResourceFileLib };

            InternalOpenedFileMoveTest(true, false, testFiles, targetFiles);
            InternalClosedFileMoveTest(true, false, testFiles, targetFiles);            
        }

        [TestMethod()]
        public void VBMoveTest3() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.VBStringsTestFile1, Agent.VBStringsTestFile2, Agent.VBStringsTestFile3 };
            string[] targetFiles = new string[] { Agent.VBResourceFile1, Agent.VBResourceFileLib };

            InternalOpenedFileMoveTest(false, false, testFiles, targetFiles);
            InternalClosedFileMoveTest(false, false, testFiles, targetFiles);            
        }

        [TestMethod()]
        public void AspNetMoveTest1() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.AspNetStringsTestFile1 };
            string[] targetFiles = new string[] { Agent.AspNetResourceFile };

            InternalOpenedFileMoveTest(false, false, testFiles, targetFiles);
            InternalClosedFileMoveTest(false, false, testFiles, targetFiles);
        }

        [TestMethod()]
        public void AspNetMoveTest2() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.AspNetStringsTestFile1 };
            string[] targetFiles = new string[] { Agent.AspNetResourceFile };

            InternalOpenedFileMoveTest(true, false, testFiles, targetFiles);
            InternalClosedFileMoveTest(true, false, testFiles, targetFiles);
        }

        [TestMethod()]
        public void AspNetMoveTest3() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.AspNetStringsTestFile1, Agent.AspNetStringsTestFile2 };
            string[] targetFiles = new string[] { Agent.AspNetResourceFile };

            InternalOpenedFileMoveTest(false, false, testFiles, targetFiles);
            InternalClosedFileMoveTest(false, false, testFiles, targetFiles);
        }

        [TestMethod()]
        public void MixedTest1() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.CSharpStringsTestFile1, Agent.VBStringsTestFile1 };
            string[] targetFiles = new string[] { Agent.CSharpResourceFileLib };

            InternalOpenedFileMoveTest(false, false, testFiles, targetFiles);
            InternalClosedFileMoveTest(false, false, testFiles, targetFiles);
        }

        [TestMethod()]
        public void MarkTest1() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.CSharpStringsTestFile1 };
            string[] targetFiles = new string[] { Agent.CSharpResourceFileLib, Agent.CSharpResourceFile1 };

            InternalOpenedFileMoveTest(false, true, testFiles, targetFiles);
            InternalClosedFileMoveTest(false, true, testFiles, targetFiles);
        }

        [TestMethod()]
        public void MarkTest2() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.VBStringsTestFile1 };
            string[] targetFiles = new string[] { Agent.VBResourceFileLib, Agent.VBResourceFile1 };

            InternalOpenedFileMoveTest(false, true, testFiles, targetFiles);
            InternalClosedFileMoveTest(false, true, testFiles, targetFiles);
        }

        [TestMethod()]
        public void MarkTest3() {
            Agent.EnsureSolutionOpen();

            string[] testFiles = new string[] { Agent.AspNetStringsTestFile1 };
            string[] targetFiles = new string[] { Agent.AspNetResourceFile};

            InternalOpenedFileMoveTest(false, true, testFiles, targetFiles);
            InternalClosedFileMoveTest(false, true, testFiles, targetFiles);
        }


        private void InternalOpenedFileMoveTest(bool fullName, bool mark, string[] testFiles, string[] targetFiles) {
            Dictionary<string, string> backups = CreateBackupsOf(testFiles);            
            List<CodeStringResultItem> items = BatchMoveLookup(testFiles);
            
            SetFilesOpened(testFiles, true);
            
            Dictionary<string, ResXProjectItem> resxItems = InitResxItems(targetFiles, Agent.GetDTE().Solution.FindProjectItem(testFiles[0]).ContainingProject);
            
            Dictionary<ResXProjectItem, int> resxCounts;
            Dictionary<ProjectItem, int> sourceItemCounts;
            int expectedToBeMarked;
            BatchMoveToResourcesToolWindow_Accessor window = InitBatchToolWindow(resxItems.Values.ToList(), items, fullName, mark, out resxCounts, out sourceItemCounts, out expectedToBeMarked);

            try {
                window.RunClick(null, null);                

                foreach (ResXProjectItem target in resxItems.Values) {
                    Assert.AreEqual(resxCounts.ContainsKey(target) ? resxCounts[target] : 0, GetResourcesCountIn(target));
                }

                int checkedCount = resxCounts.Sum((pair) => { return pair.Value; });
                List<CodeReferenceResultItem> inlineResults = BatchInlineLookup(testFiles);
                Assert.AreEqual(checkedCount, inlineResults.Count);
                 
                if (mark) {
                    int markedAfter = BatchMoveLookup(testFiles).Count((item) => { return item.IsMarkedWithUnlocalizableComment; });
                    Assert.AreEqual(expectedToBeMarked, markedAfter, "mark");
                }

                if (fullName) {
                    foreach (var result in inlineResults) {
                        AspNetCodeReferenceResultItem citem = result as AspNetCodeReferenceResultItem;
                        if (citem == null || !citem.ComesFromWebSiteResourceReference) {
                            Assert.AreEqual(result.OriginalReferenceText, result.FullReferenceText);
                        }
                    }
                }

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
                
                int sum = resxItems.Values.Sum((item) => { return GetResourcesCountIn(item); });
                Assert.AreEqual(0, sum);

                foreach (string path in testFiles) {
                    CheckForDuplicateUsings(path);
                }
            } finally {
                SetFilesOpened(testFiles, false);
                RestoreBackups(backups);
                foreach (ResXProjectItem target in resxItems.Values) {
                    ClearFile(target);                    
                }
            }
        }

        private void InternalClosedFileMoveTest(bool fullName, bool mark, string[] testFiles, string[] targetFiles) {
            Dictionary<string, string> backups = CreateBackupsOf(testFiles);
            List<CodeStringResultItem> items = BatchMoveLookup(testFiles);            

            SetFilesOpened(testFiles, false);

            Dictionary<string, ResXProjectItem> resxItems = InitResxItems(targetFiles, Agent.GetDTE().Solution.FindProjectItem(testFiles[0]).ContainingProject);
            
            Dictionary<ResXProjectItem, int> resxCounts;
            Dictionary<ProjectItem, int> itemCounts;
            int expectedToBeMarked;
            BatchMoveToResourcesToolWindow_Accessor window = InitBatchToolWindow(resxItems.Values.ToList(), items, fullName, mark, out resxCounts,out itemCounts, out expectedToBeMarked);
            

            try {
                window.RunClick(null, null);
                
                foreach (ResXProjectItem target in resxItems.Values) {
                    Assert.AreEqual(resxCounts.ContainsKey(target) ? resxCounts[target] : 0, GetResourcesCountIn(target));
                }

                int checkedCount = resxCounts.Sum((pair) => { return pair.Value; });
                List<CodeReferenceResultItem> inlineResults = BatchInlineLookup(testFiles);
                if (checkedCount != inlineResults.Count) File.Copy(testFiles[0], @"C:\Users\Ondra\Desktop\out.txt", true);
                Assert.AreEqual(checkedCount, inlineResults.Count);

                if (mark) {
                    int markedAfter = BatchMoveLookup(testFiles).Count((item) => { return item.IsMarkedWithUnlocalizableComment; });
                    Assert.AreEqual(expectedToBeMarked, markedAfter, "mark");
                }

                if (fullName) {
                    foreach (var result in inlineResults) {
                        AspNetCodeReferenceResultItem citem = result as AspNetCodeReferenceResultItem;
                        if (citem == null || !citem.ComesFromWebSiteResourceReference) {
                            Assert.AreEqual(result.OriginalReferenceText, result.FullReferenceText);
                        }
                    }
                }

                foreach (string path in testFiles) {
                    CheckForDuplicateUsings(path);
                }
            } finally {
                RestoreBackups(backups);
                foreach (ResXProjectItem target in resxItems.Values) {
                    ClearFile(target);                    
                }
            }
        }        

        private void CheckForDuplicateUsings(string path) {            
            ProjectItem projectItem = Agent.GetDTE().Solution.FindProjectItem(path);
            if (projectItem.GetFileType() == FILETYPE.ASPX) return;

            HashSet<string> namespaces = new HashSet<string>();

            foreach (CodeElement codeElement in projectItem.GetCodeModel().CodeElements) {
                if (codeElement.Kind != vsCMElement.vsCMElementImportStmt) continue;
                CodeImport import=(CodeImport)codeElement;
                if (namespaces.Contains(import.Namespace)) Assert.Fail();

                namespaces.Add(import.Namespace);
            }
        }

        private Dictionary<string, ResXProjectItem> InitResxItems(string[] targetFiles, Project containingProject) {
            Dictionary<string, ResXProjectItem> d = new Dictionary<string, ResXProjectItem>();

            for (int i = 0; i < targetFiles.Length; i++) {
                ProjectItem pitem = Agent.GetDTE().Solution.FindProjectItem(targetFiles[i]);
                d.Add(targetFiles[i], ResXProjectItem.ConvertToResXItem(pitem, containingProject));
            }            

            return d;
        }

        private BatchMoveToResourcesToolWindow_Accessor InitBatchToolWindow(List<ResXProjectItem> resxItems, List<CodeStringResultItem> items, bool fullName, bool mark, out Dictionary<ResXProjectItem, int> resxCounts, out Dictionary<ProjectItem, int> sourceItemCounts, out int expectedToBeMarked) {
            DTE2 dte = Agent.GetDTE();
            expectedToBeMarked = 0;
            sourceItemCounts = new Dictionary<ProjectItem, int>();

            BatchMoveToResourcesToolWindow_Accessor  window = new BatchMoveToResourcesToolWindow_Accessor(new PrivateObject(new BatchMoveToResourcesToolWindow()));            
            window.SetData(items);
            window.currentNamespacePolicy = window.NAMESPACE_POLICY_ITEMS[fullName ? 1 : 0];
            window.currentRememberOption = window.REMEMBER_OPTIONS[mark ? 1 : 0];

            BatchMoveToResourcesToolGrid grid= ((BatchMoveToResourcesToolGrid)window.panel.ToolGrid.Target);
            int x = 0;
                        
            Random rnd = new Random();
            resxCounts = new Dictionary<ResXProjectItem, int>();
            
            foreach (DataGridViewKeyValueRow<CodeStringResultItem> row in grid.Rows) {
                bool check = rnd.Next(2) == 0 && !row.DataSourceItem.IsMarkedWithUnlocalizableComment;
                if (check) {
                    row.Cells[grid.KeyColumnName].Value = string.Format("xx{0}", x);

                    ResXProjectItem destResX = resxItems[rnd.Next(resxItems.Count)];
                    row.Cells[grid.DestinationColumnName].Value = destResX.ToString();

                    if (!resxCounts.ContainsKey(destResX)) resxCounts.Add(destResX, 0);
                    resxCounts[destResX]++;
                    if (!sourceItemCounts.ContainsKey(row.DataSourceItem.SourceItem)) sourceItemCounts.Add(row.DataSourceItem.SourceItem, 0);
                    sourceItemCounts[row.DataSourceItem.SourceItem]++;
                } else {
                    AspNetStringResultItem aitem = row.DataSourceItem as AspNetStringResultItem;
                    if (((row.DataSourceItem is CSharpStringResultItem) || (aitem != null && aitem.ComesFromCodeBlock && aitem.Language == LANGUAGE.CSHARP))) {
                        if (mark) {
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
            grid.Sort(grid.Columns[rnd.Next(grid.Columns.Count)], rnd.Next(2) == 0 ? System.ComponentModel.ListSortDirection.Ascending : System.ComponentModel.ListSortDirection.Descending);

            return window;
        }

        private void ClearFile(ResXProjectItem target) {
            try {
                target.Load();
                target.Data.Clear();
                target.Flush();
                target.Unload();
            } catch (Exception) { }
        }

        private int GetResourcesCountIn(ResXProjectItem item) {
            item.Load();
            int result = item.GetAllStringReferences(false).Count;
            item.Unload();

            return result;
        }
    }
}
