using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using VisualLocalizer.Library;
using EnvDTE80;
using VisualLocalizer.Components;

namespace VisualLocalizer.Commands {
    internal abstract class AbstractBatchCommand {

        protected ProjectItem currentlyProcessedItem;
        protected abstract void Lookup(string functionText, TextPoint startPoint, CodeNamespace parentNamespace,
            CodeElement2 codeClassOrStruct, string codeFunctionName, string codeVariableName);

        public virtual void Process() {
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

        public virtual void Process(Array selectedItems) {
            if (selectedItems == null) throw new ArgumentException("No selected items");

            foreach (UIHierarchyItem o in selectedItems) {
                if (o.Object is ProjectItem) {
                    ProjectItem item = (ProjectItem)o.Object;
                    Process(item);
                } else if (o.Object is Project) {
                    Project proj = (Project)o.Object;
                    Process(proj);
                } else throw new Exception("Unexpected project item type: " + o.Object.GetVisualBasicType());
            }            
        }

        protected virtual void Process(Project project) {
            if (project.Kind != VSLangProj.PrjKind.prjKindCSharpProject)
                throw new InvalidOperationException("Selected project is not a C# project.");

            Process(project.ProjectItems);
        }

        protected virtual void Process(ProjectItems items) {
            if (items == null) return;            

            foreach (ProjectItem o in items) {
                if (VLDocumentViewsManager.IsFileLocked(o.Properties.Item("FullPath").Value.ToString())) {
                    VLOutputWindow.VisualLocalizerPane.WriteLine("\tSkipping {0} - document is readonly", o.Name);
                    continue;
                }
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

        protected virtual void Process(ProjectItem projectItem) {
            FileCodeModel2 codeModel = projectItem.FileCodeModel as FileCodeModel2;
            if (codeModel == null) {
                VLOutputWindow.VisualLocalizerPane.WriteLine("\tCannot process {0}, file code model does not exist.", projectItem.Name);
                return;
            }
            currentlyProcessedItem = projectItem;
            VLOutputWindow.VisualLocalizerPane.WriteLine("\tProcessing {0}", projectItem.Name);

            foreach (CodeElement2 codeElement in codeModel.CodeElements) {
                if (codeElement.Kind == vsCMElement.vsCMElementNamespace || codeElement.Kind == vsCMElement.vsCMElementClass ||
                    codeElement.Kind == vsCMElement.vsCMElementStruct) {
                    Explore(codeElement, null);
                }
            }
            currentlyProcessedItem = null;
        }

        protected virtual void Explore(CodeElement2 parentElement, CodeElement2 parentNamespace) {
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

        protected virtual void Explore(CodeProperty codeProperty, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct) {
            if (codeProperty.Getter != null) Explore(codeProperty.Getter as CodeFunction2, parentNamespace, codeClassOrStruct);
            if (codeProperty.Setter != null) Explore(codeProperty.Setter as CodeFunction2, parentNamespace, codeClassOrStruct);
        }

        protected virtual void Explore(CodeVariable2 codeVariable, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct) {
            if (codeVariable.ConstKind == vsCMConstKind.vsCMConstKindConst) return;
            if (codeVariable.Type.TypeKind != vsCMTypeRef.vsCMTypeRefString) return;
            if (codeVariable.InitExpression == null) return;
            if (codeClassOrStruct.Kind == vsCMElement.vsCMElementStruct) return;

            string initExpression = codeVariable.GetText();
            TextPoint startPoint = codeVariable.StartPoint;

            Lookup(initExpression, startPoint, parentNamespace, codeClassOrStruct, null, codeVariable.Name);
        }

        protected virtual void Explore(CodeFunction2 codeFunction, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct) {
            if (codeFunction.MustImplement) return;

            string functionText = codeFunction.GetText();
            TextPoint startPoint = codeFunction.GetStartPoint(vsCMPart.vsCMPartBody);

            Lookup(functionText, startPoint, parentNamespace, codeClassOrStruct, codeFunction.Name, null);
        }
    }
}
