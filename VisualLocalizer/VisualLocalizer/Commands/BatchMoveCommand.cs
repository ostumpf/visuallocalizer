﻿using System;
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
    internal sealed class BatchMoveCommand : AbstractBatchCommand {

        public List<CodeStringResultItem> Results {
            get;
            private set;
        }

        public override void Process() {
            base.Process();

            VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Move to Resources command started on active document... ");            

            Results = new List<CodeStringResultItem>();

            Process(currentlyProcessedItem);

            Results.RemoveAll((item) => { return item.Value.Trim().Length == 0; });
            Results.ForEach((item) => { RDTManager.SetFileReadonly(item.SourceItem.Properties.Item("FullPath").Value.ToString(), true); });

            VLOutputWindow.VisualLocalizerPane.WriteLine("Found {0} items to be moved", Results.Count);
        }

        public override void Process(Array selectedItems) {
            VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Move to Resources command started on selection");
            Results = new List<CodeStringResultItem>();
            
            base.Process(selectedItems);

            Results.RemoveAll((item) => { return item.Value.Trim().Length == 0; });
            Results.ForEach((item) => { RDTManager.SetFileReadonly(item.SourceItem.Properties.Item("FullPath").Value.ToString(), true); });

            VLOutputWindow.VisualLocalizerPane.WriteLine("Batch Move to Resources completed - found {0} items to be moved", Results.Count);
        }

        protected override void Lookup(string functionText, TextPoint startPoint, CodeNamespace parentNamespace,
            CodeElement2 codeClassOrStruct, string codeFunctionName, string codeVariableName) {
            
            var lookuper = new CodeStringLookuper(functionText, startPoint.Line, startPoint.LineCharOffset,
                startPoint.AbsoluteCharOffset + startPoint.Line - 2,
                parentNamespace, codeClassOrStruct.Name, codeFunctionName, codeVariableName);
            lookuper.SourceItem = currentlyProcessedItem;
            Results.AddRange(lookuper.LookForStrings());
        }
 
    }

   

   
}
