using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Components;
using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.Shell;
using VisualLocalizer.Commands;

namespace VLUnitTests.VLTests {
    public class RunCommandsTestsBase {

        protected List<CodeReferenceResultItem> BatchInlineLookup(string[] files) {
            UIHierarchyItem[] selectedItems = new UIHierarchyItem[files.Length];
            int i = 0;
            foreach (var key in files) {
                selectedItems[i] = Agent.FindUIHierarchyItem(Agent.GetUIHierarchy().UIHierarchyItems, key);
                Assert.IsNotNull(selectedItems[i]);
                i++;
            }

            Agent.BatchInlineCommand.Results = null;
            Agent.BatchInlineCommand.Process(selectedItems, true);
            VLDocumentViewsManager.ReleaseLocks();

            List<CodeReferenceResultItem> list = new List<CodeReferenceResultItem>();
            foreach (CodeReferenceResultItem item in Agent.BatchInlineCommand.Results) {
                list.Add(item);
            }

            return list;
        }

        protected List<CodeStringResultItem> BatchMoveLookup(string[] testFiles) {
            List<CodeStringResultItem> list = new List<CodeStringResultItem>();

            UIHierarchyItem[] selectedItems = new UIHierarchyItem[testFiles.Length];
            for (int i = 0; i < testFiles.Length; i++) {
                selectedItems[i] = Agent.FindUIHierarchyItem(Agent.GetUIHierarchy().UIHierarchyItems, testFiles[i]);
                Assert.IsNotNull(selectedItems[i]);
            }

            Agent.BatchMoveCommand.Results = null;
            Agent.BatchMoveCommand.Process(selectedItems, true);
            
            foreach (CodeStringResultItem item in Agent.BatchMoveCommand.Results) {
                list.Add(item);
            }

            VLDocumentViewsManager.ReleaseLocks();

            return list;
        }

        protected Dictionary<string, string> CreateBackupsOf(string[] files) {
            Dictionary<string, string> backups = new Dictionary<string, string>();
            foreach (string sourcePath in files) {
                if (!backups.ContainsKey(sourcePath)) {
                    string copyPath = CreateBackup(sourcePath);
                    backups.Add(sourcePath, copyPath);
                }
            }
            return backups;
        }

        protected void RestoreBackups(Dictionary<string, string> backups) {
            foreach (var pair in backups) {
                if (RDTManager.IsFileOpen(pair.Key)) {
                    RDTManager.SetIgnoreFileChanges(pair.Key, true);
                    File.Copy(pair.Value, pair.Key, true);
                    File.Delete(pair.Value);
                    RDTManager.SilentlyReloadFile(pair.Key);
                    RDTManager.SetIgnoreFileChanges(pair.Key, false);
                } else {
                    File.Copy(pair.Value, pair.Key, true);
                    File.Delete(pair.Value);
                }
            }
        }

        protected string CreateBackup(string sourcePath) {
            string dest = Path.GetTempFileName();
            File.Copy(sourcePath, dest, true);
            return dest;
        }

        protected void SetFilesOpened(string[] testFiles, bool shouldBeOpened) {
            foreach (string sourcePath in testFiles) {
                if (!shouldBeOpened && RDTManager.IsFileOpen(sourcePath)) {
                    var win = VsShellUtilities.GetWindowObject(VLDocumentViewsManager.GetWindowFrameForFile(sourcePath, false));                                        
                    
                    win.Detach();
                    win.Close(vsSaveChanges.vsSaveChangesNo);
                }
                if (shouldBeOpened) {
                    Window win = null;
                    if (!RDTManager.IsFileOpen(sourcePath)) {
                        win = Agent.GetDTE().OpenFile(null, sourcePath);
                    } else {
                        win = VsShellUtilities.GetWindowObject(VLDocumentViewsManager.GetWindowFrameForFile(sourcePath, true));
                    }
                    Assert.IsNotNull(win, "Window cannot be opened " + sourcePath);
                    win.Activate();
                    win.Visible = true;                    
                }                
            }
        }
    }
}
