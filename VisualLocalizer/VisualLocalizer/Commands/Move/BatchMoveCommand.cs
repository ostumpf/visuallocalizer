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
  
    /// <summary>
    /// Represents "Batch move to resources" command, invokeable either from code context menu or Solution Explorer's context menu. It scans
    /// given set of files, looking for string literals available for localization.
    /// </summary>
    internal sealed class BatchMoveCommand : AbstractBatchCommand {

        /// <summary>
        /// After processing this command, returns list of found result items (string literals)
        /// </summary>
        public List<CodeStringResultItem> Results {
            get;
            set;
        }
        
        public override void Process(bool verbose) {
            base.Process(verbose); // initialize class variables

            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Move to Resources command started on active document... ");            

            Results = new List<CodeStringResultItem>();

            Process(currentlyProcessedItem, verbose); // process active document

            Results.RemoveAll((item) => { return item.Value.Trim().Length == 0; }); // remove empty strings            
            
            // set each source file as readonly
            Results.ForEach((item) => {
                VLDocumentViewsManager.SetFileReadonly(item.SourceItem.GetFullPath(), true); 
            });

            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Found {0} items to be moved", Results.Count);
        }

        public override void ProcessSelection(bool verbose) {
            base.ProcessSelection(verbose); // initialize class variables

            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Move to Resources command started on text selection of active document ");

            Results = new List<CodeStringResultItem>();

            Process(currentlyProcessedItem, IntersectsWithSelection, verbose); // process active document, leaving only those items that have non-empty intersection with the selection

            // remove empty strings and result items laying outside the selection
            Results.RemoveAll((item) => {
                bool empty = item.Value.Trim().Length == 0;
                return empty || IsItemOutsideSelection(item);
            });

            // set each source file as readonly
            Results.ForEach((item) => {
                VLDocumentViewsManager.SetFileReadonly(item.SourceItem.GetFullPath(), true);
            });

            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Found {0} items to be moved", Results.Count);
        }

        public override void Process(Array selectedItems, bool verbose) {
            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Move to Resources command started on selected items from Solution Explorer");
            Results = new List<CodeStringResultItem>();
            
            base.Process(selectedItems, verbose);

            // remove empty strings
            Results.RemoveAll((item) => { return item.Value.Trim().Length == 0; });

            // set each source file as readonly
            Results.ForEach((item) => {
                VLDocumentViewsManager.SetFileReadonly(item.SourceItem.GetFullPath(), true); 
            });

            if (verbose) VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Move to Resources completed - found {0} items to be moved", Results.Count);
        }

        public override IList LookupInCSharp(string functionText, TextPoint startPoint, CodeNamespace parentNamespace,
            CodeElement2 codeClassOrStruct, string codeFunctionName, string codeVariableName, bool isWithinLocFalse) {
            
            if (codeClassOrStruct == null) throw new ArgumentNullException("codeClassOrStruct");
            if (functionText == null) throw new ArgumentNullException("functionText");
            if (startPoint == null) throw new ArgumentNullException("startPoint");

            // set information about processed item
            if (!generatedProjectItems.ContainsKey(currentlyProcessedItem)) {
                generatedProjectItems.Add(currentlyProcessedItem, currentlyProcessedItem.IsGenerated());
            }

            // run C# lookuper on code text, obtaining list of string literals
            var list = CSharpStringLookuper.Instance.LookForStrings(currentlyProcessedItem, generatedProjectItems[currentlyProcessedItem], functionText, startPoint, 
                parentNamespace, codeClassOrStruct.Name, codeFunctionName, codeVariableName, isWithinLocFalse);
     
            // add string literals to the results list
            foreach (CSharpStringResultItem item in list) {
                Results.Add(item);
            }

            return list;
        }

        public override IList LookupInVB(string functionText, TextPoint startPoint, CodeNamespace parentNamespace,
            CodeElement2 codeClassOrStruct, string codeFunctionName, string codeVariableName, bool isWithinLocFalse) {
            if (codeClassOrStruct == null) throw new ArgumentNullException("codeClassOrStruct");
            if (functionText == null) throw new ArgumentNullException("functionText");
            if (startPoint == null) throw new ArgumentNullException("startPoint");

            // set information about processed item
            if (!generatedProjectItems.ContainsKey(currentlyProcessedItem)) {
                generatedProjectItems.Add(currentlyProcessedItem, currentlyProcessedItem.IsGenerated());
            }

            // run VB lookuper on code text, obtaining list of string literals
            var list = VBStringLookuper.Instance.LookForStrings(currentlyProcessedItem, generatedProjectItems[currentlyProcessedItem], functionText, startPoint,
                parentNamespace, codeClassOrStruct.Name, codeFunctionName, codeVariableName, isWithinLocFalse);

            // add string literals to the results list
            foreach (VBStringResultItem item in list) {
                Results.Add(item);
            }

            return list;
        }

        public override IList LookupInCSharpAspNet(string functionText, BlockSpan blockSpan, NamespacesList declaredNamespaces, string className) {
            if (declaredNamespaces == null) throw new ArgumentNullException("declaredNamespaces");
            if (functionText == null) throw new ArgumentNullException("functionText");
            if (blockSpan == null) throw new ArgumentNullException("blockSpan");

            // set information about processed item
            if (!generatedProjectItems.ContainsKey(currentlyProcessedItem)) {
                generatedProjectItems.Add(currentlyProcessedItem, currentlyProcessedItem.IsGenerated());
            }

            // run ASP .NET C# lookuper on code text, obtaining list of string literals
            var list = AspNetCSharpStringLookuper.Instance.LookForStrings(currentlyProcessedItem, generatedProjectItems[currentlyProcessedItem],
                functionText, blockSpan, className, declaredNamespaces);

            foreach (AspNetStringResultItem item in list) {
                Results.Add(item);
            }

            return list;
        }

        public override IList LookupInVBAspNet(string functionText, BlockSpan blockSpan, NamespacesList declaredNamespaces, string className) {
            if (declaredNamespaces == null) throw new ArgumentNullException("declaredNamespaces");
            if (functionText == null) throw new ArgumentNullException("functionText");
            if (blockSpan == null) throw new ArgumentNullException("blockSpan");

            if (!generatedProjectItems.ContainsKey(currentlyProcessedItem)) {
                generatedProjectItems.Add(currentlyProcessedItem, currentlyProcessedItem.IsGenerated());
            }

            var list = AspNetVBStringLookuper.Instance.LookForStrings(currentlyProcessedItem, generatedProjectItems[currentlyProcessedItem],
                functionText, blockSpan, className, declaredNamespaces);

            foreach (AspNetStringResultItem item in list) {
                Results.Add(item);
            }

            return list;
        }

        /// <summary>
        /// Adds given result item to the result list
        /// </summary>        
        public void AddToResults<T>(T resultItem) where T : CodeStringResultItem, new() {
            if (resultItem == null) throw new ArgumentNullException("resultItem");

            resultItem.SourceItem = currentlyProcessedItem;
            Results.Add(resultItem);
        }
    }

}
