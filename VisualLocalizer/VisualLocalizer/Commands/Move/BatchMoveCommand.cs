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
using System.Collections;
using VisualLocalizer.Library.AspxParser;

namespace VisualLocalizer.Commands {    
    internal sealed class BatchMoveCommand : AbstractBatchCommand {

        public List<CodeStringResultItem> Results {
            get;
            set;
        }

        public override void Process(bool verbose) {
            base.Process(verbose);

            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Move to Resources command started on active document... ");            

            Results = new List<CodeStringResultItem>();

            Process(currentlyProcessedItem, verbose);

            Results.RemoveAll((item) => { return item.Value.Trim().Length == 0; });            
            Results.ForEach((item) => {
                VLDocumentViewsManager.SetFileReadonly(item.SourceItem.GetFullPath(), true); 
            });

            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Found {0} items to be moved", Results.Count);
        }

        public override void ProcessSelection(bool verbose) {
            base.ProcessSelection(verbose);

            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Move to Resources command started on text selection of active document ");

            Results = new List<CodeStringResultItem>();

            Process(currentlyProcessedItem, IntersectsWithSelection, verbose);

            Results.RemoveAll((item) => {
                bool empty = item.Value.Trim().Length == 0;
                return empty || IsItemOutsideSelection(item);
            });
            Results.ForEach((item) => {
                VLDocumentViewsManager.SetFileReadonly(item.SourceItem.GetFullPath(), true);
            });

            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Found {0} items to be moved", Results.Count);
        }

        public override void Process(Array selectedItems, bool verbose) {
            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Move to Resources command started on selected items from Solution Explorer");
            Results = new List<CodeStringResultItem>();
            
            base.Process(selectedItems, verbose);

            Results.RemoveAll((item) => { return item.Value.Trim().Length == 0; });
            Results.ForEach((item) => {
                VLDocumentViewsManager.SetFileReadonly(item.SourceItem.GetFullPath(), true); 
            });

            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Move to Resources completed - found {0} items to be moved", Results.Count);
        }

        public override IList LookupInCSharp(string functionText, TextPoint startPoint, CodeNamespace parentNamespace,
            CodeElement2 codeClassOrStruct, string codeFunctionName, string codeVariableName, bool isWithinLocFalse) {

            if (!generatedProjectItems.ContainsKey(currentlyProcessedItem)) {
                generatedProjectItems.Add(currentlyProcessedItem, currentlyProcessedItem.IsGenerated());
            }

            var list = CSharpStringLookuper.Instance.Run(currentlyProcessedItem, generatedProjectItems[currentlyProcessedItem], functionText, startPoint, 
                parentNamespace, codeClassOrStruct.Name, codeFunctionName, codeVariableName, isWithinLocFalse);
     
            foreach (CSharpStringResultItem item in list) {
                Results.Add(item);
            }

            return list;
        }

        public override IList LookupInAspNet(string functionText, BlockSpan blockSpan, NamespacesList declaredNamespaces, string className) {
            if (!generatedProjectItems.ContainsKey(currentlyProcessedItem)) {
                generatedProjectItems.Add(currentlyProcessedItem, currentlyProcessedItem.IsGenerated());
            }

            var list = AspNetCodeStringLookuper.Instance.Run(currentlyProcessedItem, generatedProjectItems[currentlyProcessedItem],
                functionText, blockSpan, declaredNamespaces, className);

            foreach (AspNetStringResultItem item in list) {
                Results.Add(item);
            }

            return list;
        }
       
        public void AddToResults<T>(T resultItem) where T : CodeStringResultItem, new() {
            resultItem.SourceItem = currentlyProcessedItem;
            Results.Add(resultItem);
        }
    }

}
