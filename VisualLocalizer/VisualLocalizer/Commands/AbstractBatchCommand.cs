using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using VisualLocalizer.Library;
using EnvDTE80;
using VisualLocalizer.Components;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Collections;
using VisualLocalizer.Extensions;
using VisualLocalizer.Components.AspxParser;

namespace VisualLocalizer.Commands {
    internal abstract class AbstractBatchCommand {

        protected ProjectItem currentlyProcessedItem;
        protected VirtualPoint selectionTopPoint, selectionBotPoint;
        protected HashSet<ProjectItem> searchedProjectItems = new HashSet<ProjectItem>();
        protected Dictionary<Project, WebConfig> configurations = new Dictionary<Project, WebConfig>();

        public abstract IList LookupInCSharp(string functionText, TextPoint startPoint, CodeNamespace parentNamespace,
            CodeElement2 codeClassOrStruct, string codeFunctionName, string codeVariableName, bool isWithinLocFalse);
        public abstract IList LookupInAspNet(string functionText, BlockSpan blockSpan, NamespacesList declaredNamespaces, string className);

        public virtual void Process(bool verbose) {
            searchedProjectItems.Clear();
            configurations.Clear();

            Document currentDocument = VisualLocalizerPackage.Instance.DTE.ActiveDocument;
            if (currentDocument == null)
                throw new Exception("No selected document");
            if (currentDocument.ProjectItem == null)
                throw new Exception("Selected document has no corresponding Project Item.");
            if (currentDocument.ProjectItem.ContainingProject == null)
                throw new Exception("Selected document is not a part of any Project.");                        
            if (currentDocument.ReadOnly)
                throw new Exception("Cannot perform this operation - active document is readonly");
            currentlyProcessedItem = currentDocument.ProjectItem;
        }

        public virtual void Process(Array selectedItems, bool verbose) {
            searchedProjectItems.Clear();
            configurations.Clear();

            if (selectedItems == null) throw new ArgumentException("No selected items");

            foreach (UIHierarchyItem o in selectedItems) {
                if (o.Object is ProjectItem) {
                    ProjectItem item = (ProjectItem)o.Object;
                    Process(item, verbose);
                } else if (o.Object is Project) {
                    Project proj = (Project)o.Object;
                    Process(proj, verbose);
                } else throw new Exception("Unexpected project item type: " + o.Object.GetVisualBasicType());
            }            
        }

        public virtual void ProcessSelection(bool verbose) {
            searchedProjectItems.Clear();
            configurations.Clear();

            Document currentDocument = VisualLocalizerPackage.Instance.DTE.ActiveDocument;
            if (currentDocument == null)
                throw new Exception("No selected document");
            if (currentDocument.ProjectItem == null)
                throw new Exception("Selected document has no corresponding Project Item.");
            if (currentDocument.ProjectItem.ContainingProject == null)
                throw new Exception("Selected document is not a part of any Project.");
            if (currentDocument.ReadOnly)
                throw new Exception("Cannot perform this operation - active document is readonly");            
            currentlyProcessedItem = currentDocument.ProjectItem;

            TextSelection currentSelection = currentDocument.Selection as TextSelection;
            if (currentSelection == null || currentSelection.IsEmpty)
                throw new Exception("Cannot perform this operation on an empty selection.");
                       
            selectionTopPoint = currentSelection.BottomPoint;
            selectionBotPoint = currentSelection.TopPoint;            
        }       

        protected virtual void Process(Project project, bool verbose) {
            Process(project.ProjectItems, verbose);
        }

        protected virtual void Process(ProjectItems items, bool verbose) {
            if (items == null) return;            

            foreach (ProjectItem item in items) {
                if (VLDocumentViewsManager.IsFileLocked(item.Properties.Item("FullPath").Value.ToString())) {
                    if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("\tSkipping {0} - document is readonly", item.Name);
                    continue;
                }                
                
                if (item.CanShowCodeContextMenu()) Process(item, verbose);                
            }
        }

        protected virtual void Process(ProjectItem projectItem, bool verbose) {
            Process(projectItem, (e) => { return true; }, verbose);
            if (projectItem.ProjectItems != null) {
                foreach (ProjectItem item in projectItem.ProjectItems)
                    if (item.CanShowCodeContextMenu()) Process(item, verbose);    
            }
        }

        protected virtual void Process(ProjectItem projectItem, Predicate<CodeElement> exploreable, bool verbose) {
            if (searchedProjectItems.Contains(projectItem)) return;

            string path = (string)projectItem.Properties.Item("FullPath").Value;
            if (string.IsNullOrEmpty(path)) {
                if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Skipping {0} - null file path", projectItem.Name);
                return;
            }
            searchedProjectItems.Add(projectItem);

            switch (projectItem.GetFileType()) {
                case FILETYPE.CSHARP: ProcessCSharp(projectItem, exploreable, verbose); break;
                case FILETYPE.ASPX: ProcessAspNet(projectItem, verbose); break;
                case FILETYPE.RAZOR: break; // TODO
            }
        }

        protected void ProcessCSharp(ProjectItem projectItem, Predicate<CodeElement> exploreable, bool verbose) {
            FileCodeModel2 codeModel = projectItem.FileCodeModel as FileCodeModel2;
            if (codeModel == null) {
                if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("\tCannot process {0}, file code model does not exist.", projectItem.Name);
                return;
            }            
            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("\tProcessing {0}", projectItem.Name);

            currentlyProcessedItem = projectItem;
            
            CSharpCodeExplorer.Instance.Explore(this, exploreable, codeModel);            
            
            currentlyProcessedItem = null;
        }

        protected void ProcessAspNet(ProjectItem projectItem, bool verbose) {
            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("\tProcessing {0}", projectItem.Name);
            if (!configurations.ContainsKey(projectItem.ContainingProject)) {
                configurations.Add(projectItem.ContainingProject, WebConfig.Load(projectItem.ContainingProject));
            }

            currentlyProcessedItem = projectItem;

            AspNetCodeExplorer.Instance.Explore(this, projectItem, configurations[projectItem.ContainingProject]);

            currentlyProcessedItem = null;
        }

        protected virtual bool IntersectsWithSelection(CodeElement codeElement) {
            if (selectionBotPoint.GreaterThan(codeElement.EndPoint) && selectionTopPoint.LessThan(codeElement.StartPoint)) return true;
            if (selectionBotPoint.LessThan(codeElement.EndPoint) && selectionTopPoint.GreaterThan(codeElement.StartPoint)) return true;

            return false;
        }

        protected virtual bool IsItemOutsideSelection(AbstractResultItem item) {
            int startOffset = item.AbsoluteCharOffset - item.ReplaceSpan.iStartLine + 2;
            int endOffset = item.AbsoluteCharOffset + item.AbsoluteCharLength - item.ReplaceSpan.iEndLine + 2;

            int bottom = Math.Max(selectionBotPoint.AbsoluteCharOffset, selectionTopPoint.AbsoluteCharOffset);
            int top = Math.Min(selectionBotPoint.AbsoluteCharOffset, selectionTopPoint.AbsoluteCharOffset);

            return (startOffset > bottom) || (endOffset <= top);
        }
    }
}
