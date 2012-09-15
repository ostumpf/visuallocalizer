using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using VisualLocalizer.Library;
using VisualLocalizer.Components;
using EnvDTE80;
using System.ComponentModel;
using Microsoft.VisualStudio.TextManager.Interop;

namespace VisualLocalizer.Commands {    
    internal sealed class BatchMoveCommand {

        private ProjectItem currentlyProcessedItem;
      
        public List<CodeStringResultItem> Results {
            get;
            private set;
        }

        public void Process() {
            Document currentDocument = VisualLocalizerPackage.Instance.DTE.ActiveDocument;
            if (currentDocument == null)
                throw new Exception("No selected document");
            if (currentDocument.ProjectItem == null)
                throw new Exception("Selected document has no corresponding Project Item.");
            if (currentDocument.ProjectItem.ContainingProject == null)
                throw new Exception("Selected document is not a part of any Project.");

            VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Move to Resources command started on active document... ");

            Results = new List<CodeStringResultItem>();

            Process(currentDocument.ProjectItem);

            Results.RemoveAll((item) => { return item.Value.Trim().Length == 0; });
            Results.ForEach((item) => { RDTManager.SetFileReadonly(item.SourceItem.Properties.Item("FullPath").Value.ToString(), true); });

            VLOutputWindow.VisualLocalizerPane.WriteLine("Found {0} items to be moved", Results.Count);
        }        

        public void Process(Array selectedItems) {
            VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Move to Resources command started on selection");

            Results = new List<CodeStringResultItem>();

            foreach (UIHierarchyItem o in selectedItems) {
                if (o.Object is ProjectItem) {
                    ProjectItem item = (ProjectItem)o.Object;
                    Process(item);
                } else if (o.Object is Project) {
                    Project proj = (Project)o.Object;
                    Process(proj);           
                } else throw new Exception("Unexpected project item type: " + o.Object.GetVisualBasicType());
            }

            Results.RemoveAll((item) => { return item.Value.Trim().Length == 0; });
            Results.ForEach((item) => { RDTManager.SetFileReadonly(item.SourceItem.Properties.Item("FullPath").Value.ToString(), true); });

            VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Move to Resources completed - found {0} items to be moved", Results.Count);            
        }   

        private void Process(Project project) {
            if (project.Kind != VSLangProj.PrjKind.prjKindCSharpProject)
                throw new InvalidOperationException("Selected project is not a C# project.");

            Process(project.ProjectItems);
        }

        private void Process(ProjectItems items) {
            foreach (ProjectItem o in items) {
                bool ok = true;
                for (short i = 0; i < o.FileCount; i++) {
                    ok = ok && o.get_FileNames(i).ToLowerInvariant().EndsWith(StringConstants.CsExtension);
                    ok = ok && o.ContainingProject.Kind == VSLangProj.PrjKind.prjKindCSharpProject;
                }
                if (ok) {
                    Process(o);
                    Process(o.ProjectItems);
                }
            }
        }

        private void Process(ProjectItem projectItem) {            
            FileCodeModel2 codeModel = projectItem.FileCodeModel as FileCodeModel2;
            if (codeModel == null) {
                VLOutputWindow.VisualLocalizerPane.WriteLine("\tCannot process {0}, file code model does not exist.", projectItem.Name);
                return;
            }
            currentlyProcessedItem = projectItem;
            VLOutputWindow.VisualLocalizerPane.WriteLine("\tProcessing {0}", projectItem.Name);
            
            foreach (CodeElement2 codeElement in codeModel.CodeElements) {
                if (codeElement.Kind == vsCMElement.vsCMElementNamespace || codeElement.Kind==vsCMElement.vsCMElementClass || 
                    codeElement.Kind==vsCMElement.vsCMElementStruct) {
                    Explore(codeElement, null);
                }               
            }
            currentlyProcessedItem = null;
        }

        private void Explore(CodeElement2 parentElement, CodeElement2 parentNamespace) {
            foreach (CodeElement2 codeElement in parentElement.Children) {
                if (codeElement.Kind == vsCMElement.vsCMElementClass) {
                    Explore(codeElement, parentElement.Kind == vsCMElement.vsCMElementNamespace ? parentElement : parentNamespace);
                }
                if (codeElement.Kind == vsCMElement.vsCMElementNamespace) {
                    Explore(codeElement, parentElement);
                }
                if (codeElement.Kind == vsCMElement.vsCMElementVariable) {
                    Explore(codeElement as CodeVariable2, (CodeNamespace)parentNamespace, parentElement);
                }
                if (codeElement.Kind == vsCMElement.vsCMElementFunction) {
                    Explore(codeElement as CodeFunction2, (CodeNamespace)parentNamespace, parentElement);
                }
                if (codeElement.Kind == vsCMElement.vsCMElementProperty) {
                    Explore(codeElement as CodeProperty, (CodeNamespace)parentNamespace, parentElement);
                }
                if (codeElement.Kind == vsCMElement.vsCMElementStruct) {
                    Explore(codeElement, parentElement.Kind == vsCMElement.vsCMElementNamespace ? parentElement : parentNamespace);
                }
            }
        }

        private void Explore(CodeProperty codeProperty, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct) {
            if (codeProperty.Getter != null) Explore(codeProperty.Getter as CodeFunction2, parentNamespace, codeClassOrStruct);
            if (codeProperty.Setter != null) Explore(codeProperty.Setter as CodeFunction2, parentNamespace, codeClassOrStruct);
        }

        private void Explore(CodeVariable2 codeVariable, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct) {
            if (codeVariable.ConstKind == vsCMConstKind.vsCMConstKindConst) return;
            if (codeVariable.Type.TypeKind != vsCMTypeRef.vsCMTypeRefString) return;
            if (codeVariable.InitExpression == null) return;
            if (codeClassOrStruct.Kind == vsCMElement.vsCMElementStruct) return;

            string initExpression = codeVariable.GetText();
            TextPoint startPoint = codeVariable.StartPoint;

            var lookuper = new CodeStringLookuper(initExpression, startPoint.Line, startPoint.LineCharOffset, startPoint.AbsoluteCharOffset + startPoint.Line - 2,
                parentNamespace, codeClassOrStruct.Name, null, codeVariable.Name);
            lookuper.SourceItem = currentlyProcessedItem;
            Results.AddRange(lookuper.LookForStrings());                 
        }

        private void Explore(CodeFunction2 codeFunction, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct) {
            if (codeFunction.MustImplement) return;

            string functionText = codeFunction.GetText();
            TextPoint startPoint = codeFunction.GetStartPoint(vsCMPart.vsCMPartBody);

            var lookuper = new CodeStringLookuper(functionText, startPoint.Line, startPoint.LineCharOffset,
                startPoint.AbsoluteCharOffset+startPoint.Line - 2,
                parentNamespace, codeClassOrStruct.Name, codeFunction.Name, null);
            lookuper.SourceItem = currentlyProcessedItem;
            Results.AddRange(lookuper.LookForStrings());            
        }
 
    }

   

   
}
