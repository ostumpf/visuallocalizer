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
using VisualLocalizer.Library.AspxParser;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;

namespace VisualLocalizer.Commands {

    /// <summary>
    /// Base class for batch commands that process given set of source data and use toolwindows to display it to the user, like BatchMove or BatchInline commands.
    /// These commands are invoked either from code context menu or Solution Explorer's context menu.
    /// </summary>
    public abstract class AbstractBatchCommand {

        /// <summary>
        /// ProjectItem currently being parsed
        /// </summary>
        protected ProjectItem currentlyProcessedItem;

        /// <summary>
        /// Used when processing a selection - marks selection scope
        /// </summary>
        protected VirtualPoint selectionTopPoint, selectionBotPoint;

        /// <summary>
        /// ProjectItems that were already searched in this instance (cleared before each Process command)
        /// </summary>
        protected HashSet<ProjectItem> searchedProjectItems = new HashSet<ProjectItem>();

        /// <summary>
        /// Cache for storing information about ProjectItems
        /// </summary>
        protected Dictionary<ProjectItem, bool> generatedProjectItems = new Dictionary<ProjectItem, bool>();

        /// <summary>
        /// Searches given C# code and returns list of result items
        /// </summary>
        /// <param name="functionText">Text to search</param>
        /// <param name="startPoint">Information about position of the text (line, column...)</param>
        /// <param name="parentNamespace">Namespace where this code belongs (can be null)</param>
        /// <param name="codeClassOrStruct">Class or struct where this code belongs (cannot be null)</param>
        /// <param name="codeFunctionName">Name of the function, where this code belongs (can be null)</param>
        /// <param name="codeVariableName">Name of the variable that is initialized by this code (can be null)</param>
        /// <param name="isWithinLocFalse">True if [Localizable(false)] was set</param>
        /// <returns>List of result items</returns>
        public abstract IList LookupInCSharp(string functionText, TextPoint startPoint, CodeNamespace parentNamespace,
            CodeElement2 codeClassOrStruct, string codeFunctionName, string codeVariableName, bool isWithinLocFalse);

        /// <summary>
        /// Searches given Visual Basic code and returns list of result items
        /// </summary>
        /// <param name="functionText">Text to search</param>
        /// <param name="startPoint">Information about position of the text (line, column...)</param>
        /// <param name="parentNamespace">Namespace where this code belongs (can be null)</param>
        /// <param name="codeClassOrStruct">Class, struct or module where this code belongs (cannot be null)</param>
        /// <param name="codeFunctionName">Name of the function, where this code belongs (can be null)</param>
        /// <param name="codeVariableName">Name of the variable that is initialized by this code (can be null)</param>
        /// <param name="isWithinLocFalse">True if [Localizable(false)] was set</param>
        /// <returns>List of result items</returns>
        public abstract IList LookupInVB(string functionText, TextPoint startPoint, CodeNamespace parentNamespace,
            CodeElement2 codeClassOrStruct, string codeFunctionName, string codeVariableName, bool isWithinLocFalse);

        /// <summary>
        /// Searches given C# code block located in an ASP .NET document
        /// </summary>
        /// <param name="functionText">Text to search</param>
        /// <param name="blockSpan">Information about position of the block (line, column...</param>
        /// <param name="declaredNamespaces">Namespaces imported in the document</param>
        /// <param name="fileName">Name of the ASP .NET document</param>
        /// <returns>List of result items</returns>
        public abstract IList LookupInCSharpAspNet(string functionText, BlockSpan blockSpan, NamespacesList declaredNamespaces, string fileName);

        /// <summary>
        /// Searches given VB code block located in an ASP .NET document
        /// </summary>
        /// <param name="functionText">Text to search</param>
        /// <param name="blockSpan">Information about position of the block (line, column...</param>
        /// <param name="declaredNamespaces">Namespaces imported in the document</param>
        /// <param name="fileName">Name of the ASP .NET document</param>
        /// <returns>List of result items</returns>
        public abstract IList LookupInVBAspNet(string functionText, BlockSpan blockSpan, NamespacesList declaredNamespaces, string fileName);

        /// <summary>
        /// Signature of the windows opened by this command in the background
        /// </summary>
        protected object invisibleWindowsAuthor;

        /// <summary>
        /// Sets currently processed item and cleares all cache
        /// </summary>        
        public void ReinitializeWith(ProjectItem projectItem) {
            if (projectItem == null) throw new ArgumentNullException("projectItem");

            currentlyProcessedItem = projectItem;
            searchedProjectItems.Clear();
            generatedProjectItems.Clear();
        }

        /// <summary>
        /// Called from context menu of a code file, processes current document
        /// </summary>
        /// <param name="verbose">True if processing info should be printed to the output</param>
        public virtual void Process(bool verbose) {
            searchedProjectItems.Clear();
            generatedProjectItems.Clear();
            
            CheckActiveDocument();

            Document currentDocument = VisualLocalizerPackage.Instance.DTE.ActiveDocument;
            currentlyProcessedItem = currentDocument.ProjectItem;
        }

        /// <summary>
        /// Called from context menu of Solution Explorer, processes given list of ProjectItems
        /// </summary>
        /// <param name="selectedItems">Items selected in Solution Explorer - to be searched</param>
        /// <param name="verbose">True if processing info should be printed to the output</param>
        public virtual void Process(Array selectedItems, bool verbose) {
            if (selectedItems == null) throw new ArgumentNullException("selectedItems");

            searchedProjectItems.Clear();
            generatedProjectItems.Clear();

            foreach (UIHierarchyItem o in selectedItems) {
                if (o.Object is ProjectItem) {
                    ProjectItem item = (ProjectItem)o.Object;
                    Process(item, verbose);
                } else if (o.Object is Project) {
                    Project proj = (Project)o.Object;
                    Process(proj, verbose);
                } else if (o.Object is Solution) {
                    Solution s = (Solution)o.Object;
                    Process(s.Projects, verbose);
                } else throw new Exception("Unexpected project item type: " + o.Object.GetVisualBasicType());
            }            
        }

        /// <summary>
        /// Called from context menu of a code file, processes selected block of code
        /// </summary>
        /// <param name="verbose">True if processing info should be printed to the output</param>
        public virtual void ProcessSelection(bool verbose) {
            searchedProjectItems.Clear();
            generatedProjectItems.Clear();

            CheckActiveDocument();

            InitializeSelection();
        }

        protected void InitializeSelection() {
            Document currentDocument = VisualLocalizerPackage.Instance.DTE.ActiveDocument;
            currentlyProcessedItem = currentDocument.ProjectItem;

            TextSelection currentSelection = currentDocument.Selection as TextSelection;
            if (currentSelection == null || currentSelection.IsEmpty)
                throw new Exception("Cannot perform this operation on an empty selection.");

            selectionTopPoint = currentSelection.BottomPoint;
            selectionBotPoint = currentSelection.TopPoint;            
        }

        protected virtual void Process(Projects projects, bool verbose) {
            if (projects == null) return;

            foreach (Project proj in projects) {
                Process(proj, verbose);
            }
        }

        protected virtual void Process(Project project, bool verbose) {
            if (project == null) return;
            if (!project.IsKnownProjectType()) return;

            Process(project.ProjectItems, verbose);
        }

        protected virtual void Process(ProjectItems items, bool verbose) {
            if (items == null) return;            

            foreach (ProjectItem item in items) {                
                Process(item, verbose);                
            }
        }

        /// <summary>
        /// Search given ProjectItem and its dependant items
        /// </summary>        
        protected virtual void Process(ProjectItem projectItem, bool verbose) {
            if (projectItem.CanShowCodeContextMenu()) Process(projectItem, (e) => { return true; }, verbose);
            
            if (projectItem.ProjectItems != null) {
                foreach (ProjectItem item in projectItem.ProjectItems)
                    Process(item, verbose);
            }

            // in ASP .NET projects, ProjectItems returns null even though there are child items
            if (projectItem.GetFileType() == FILETYPE.ASPX) {
                foreach (string ext in StringConstants.CodeExtensions) { // try adding .vb and .cs extensions and search for the file
                    string path = projectItem.GetFullPath() + ext;
                    ProjectItem item = VisualLocalizerPackage.Instance.DTE.Solution.FindProjectItem(path);
                    if (item != null && item != projectItem) Process(item, verbose);
                }
            }
        }

        /// <summary>
        /// Search given ProjectItem, using predicate to determine whether a code element should be explored (used when processing selection)
        /// </summary>
        /// <param name="projectItem">Item to search</param>
        /// <param name="exploreable">Predicate returning true, if given code element should be searched for result items</param>
        /// <param name="verbose"></param>
        protected virtual void Process(ProjectItem projectItem, Predicate<CodeElement> exploreable, bool verbose) {
            if (searchedProjectItems.Contains(projectItem)) return;
            searchedProjectItems.Add(projectItem);

            invisibleWindowsAuthor = GetType();

            if (VLDocumentViewsManager.IsFileLocked(projectItem.GetFullPath()) || RDTManager.IsFileReadonly(projectItem.GetFullPath())) {
                if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("\tSkipping {0} - document is readonly", projectItem.Name);
            } else {                
                switch (projectItem.GetFileType()) {
                    case FILETYPE.CSHARP: ProcessCSharp(projectItem, exploreable, verbose); break;
                    case FILETYPE.ASPX: ProcessAspNet(projectItem, verbose); break;
                    case FILETYPE.VB: ProcessVB(projectItem, exploreable, verbose); break;
                    default: break; // do nothing if file type is not known
                }
            }
        }

        /// <summary>
        /// Treats given ProjectItem as a C# code file, using CSharpCodeExplorer to examine the file. LookInCSharp method is called as a callback,
        /// given plain methods text.
        /// </summary>        
        protected virtual void ProcessCSharp(ProjectItem projectItem, Predicate<CodeElement> exploreable, bool verbose) {
            bool fileOpened;
            FileCodeModel2 codeModel = projectItem.GetCodeModel(false, true, out fileOpened);
            if (fileOpened) {
                VLDocumentViewsManager.AddInvisibleWindow(projectItem.GetFullPath(), invisibleWindowsAuthor);
                VLOutputWindow.VisualLocalizerPane.WriteLine("\tForce opening {0} in background in order to obtain code model", projectItem.Name);
            }

            if (codeModel == null) {
                if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("\tCannot process {0}, file code model does not exist.", projectItem.Name);
                return;
            }
            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("\tProcessing {0}", projectItem.Name);

            currentlyProcessedItem = projectItem;

            try {
                CSharpCodeExplorer.Instance.Explore(this, exploreable, codeModel);               
            } catch (COMException ex) {
                if (ex.ErrorCode == -2147483638) {
                    VLOutputWindow.VisualLocalizerPane.WriteLine("\tError occured during processing {0} - the file is not yet compiled.", projectItem.Name);
                } else {
                    throw;
                }
            }

            currentlyProcessedItem = null;
        }

        /// <summary>
        /// Treats given ProjectItem as a VB code file, using VBCodeExplorer to examine the file. LookInVB method is called as a callback,
        /// given plain methods text.
        /// </summary>    
        protected virtual void ProcessVB(ProjectItem projectItem, Predicate<CodeElement> exploreable, bool verbose) {
            bool fileOpened;
            FileCodeModel2 codeModel = projectItem.GetCodeModel(false, true, out fileOpened);
            if (fileOpened) {
                VLDocumentViewsManager.AddInvisibleWindow(projectItem.GetFullPath(), invisibleWindowsAuthor);
                VLOutputWindow.VisualLocalizerPane.WriteLine("\tForce opening {0} in background in order to obtain code model", projectItem.Name);
            }
                        
            if (codeModel == null) {
                if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("\tCannot process {0}, file code model does not exist.", projectItem.Name);
                return;
            }
            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("\tProcessing {0}", projectItem.Name);

            currentlyProcessedItem = projectItem;

            try {
                VBCodeExplorer.Instance.Explore(this, exploreable, codeModel);                
            } catch (COMException ex) {
                if (ex.ErrorCode == -2147483638) {
                    VLOutputWindow.VisualLocalizerPane.WriteLine("\tError occured during processing {0} - the file is not yet compiled.", projectItem.Name);
                } else {
                    throw;
                }
            }

            currentlyProcessedItem = null;
        }
        
        /// <summary>
        /// Treats given ProjectItem as a ASP .NET code file, using AspNetCodeExplorer to examine the file. LookupInCSharpAspNet and LookupInVBAspNet methods are called as a callbacks, depending on file language,
        /// given plain methods text.
        /// </summary>   
        protected void ProcessAspNet(ProjectItem projectItem, bool verbose) {
            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("\tProcessing {0}", projectItem.Name);
           
            currentlyProcessedItem = projectItem;

            AspNetCodeExplorer.Instance.Explore(this, projectItem);

            currentlyProcessedItem = null;
        }

        /// <summary>
        /// Returns true if given code element has non-empty intersection with selected part of document
        /// </summary>        
        protected bool IntersectsWithSelection(CodeElement codeElement) {
            if (codeElement == null) throw new ArgumentNullException("codeElement");
            if (selectionBotPoint == null) throw new ArgumentNullException("selectionBotPoint");
            if (selectionTopPoint == null) throw new ArgumentNullException("selectionTopPoint");

            if (selectionBotPoint.GreaterThan(codeElement.EndPoint) && selectionTopPoint.LessThan(codeElement.StartPoint)) return true;
            if (selectionBotPoint.LessThan(codeElement.EndPoint) && selectionTopPoint.GreaterThan(codeElement.StartPoint)) return true;

            return false;
        }

        /// <summary>
        /// Returns true if given result item lies outside the selection
        /// </summary>        
        protected bool IsItemOutsideSelection(AbstractResultItem item) {
            if (item == null) throw new ArgumentNullException("item");
            if (selectionBotPoint == null) throw new ArgumentNullException("selectionBotPoint");
            if (selectionTopPoint == null) throw new ArgumentNullException("selectionTopPoint");

            int startOffset = item.AbsoluteCharOffset - item.ReplaceSpan.iStartLine + 2;
            int endOffset = item.AbsoluteCharOffset + item.AbsoluteCharLength - item.ReplaceSpan.iEndLine + 2;

            int bottom = Math.Max(selectionBotPoint.AbsoluteCharOffset, selectionTopPoint.AbsoluteCharOffset);
            int top = Math.Min(selectionBotPoint.AbsoluteCharOffset, selectionTopPoint.AbsoluteCharOffset);

            return (startOffset > bottom) || (endOffset <= top);
        }

        /// <summary>
        /// Checks active document whether it can be searched
        /// </summary>
        protected void CheckActiveDocument() {
            Document currentDocument = VisualLocalizerPackage.Instance.DTE.ActiveDocument;
            if (currentDocument == null)
                throw new Exception("No selected document");
            if (currentDocument.ProjectItem == null)
                throw new Exception("Selected document has no corresponding Project Item.");
            if (currentDocument.ProjectItem.ContainingProject == null)
                throw new Exception("Selected document is not a part of any Project.");
            if (RDTManager.IsFileReadonly(currentDocument.FullName) || VLDocumentViewsManager.IsFileLocked(currentDocument.FullName))
                throw new Exception("Cannot perform this operation - active document is readonly");
            if (VisualLocalizerPackage.Instance.DTE.Solution.FindProjectItem(currentDocument.FullName) == null)
                throw new Exception("Selected document is not a part of an open Solution.");
        }
    }

    public class ResultItemsPositionCompararer<T> : IComparer<T> where T:AbstractResultItem {
        
        public int Compare(T x, T y) {
            return x.AbsoluteCharOffset.CompareTo(y.AbsoluteCharOffset);
        }
    }
}
