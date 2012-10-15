using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;

namespace VisualLocalizer.Components {
    internal abstract class AbstractResultItem {
        public AbstractResultItem() {
            MoveThisItem = true;
        }

        public bool MoveThisItem { get; set; }
        public bool IsWithinLocalizableFalse { get; set; }
        public bool IsMarkedWithUnlocalizableComment { get; set; }
        public ProjectItem SourceItem { get; set; }
        public ResXProjectItem DestinationItem { get; set; }
        public TextSpan ReplaceSpan { get; set; }        
        public int AbsoluteCharOffset { get; set; }
        public int AbsoluteCharLength { get; set; }
        public string Value { get; set; }
        public string Context { get; set; }
        public int ContextRelativeLine { get; set; }
    }

    internal sealed class CodeStringResultItem : AbstractResultItem {
        public CodeNamespace NamespaceElement { get; set; }
        public string MethodElementName { get; set; }
        public string VariableElementName { get; set; }
        public string ClassOrStructElementName { get; set; }
        public string Key { get; set; }
        public bool WasVerbatim { get; set; }
        public string ErrorText { get; set; }

        public override string ToString() {
            return string.Format("CodeStringResultItem: Key=\"{0}\", Value=\"{1}\", Source=\"{2}\", Target=\"{3}\"", Key, Value, (SourceItem == null ? "(null)" : SourceItem.Name), (DestinationItem == null ? "(null)" : DestinationItem.InternalProjectItem.Name));
        }
    }

    internal sealed class CodeReferenceResultItem : AbstractResultItem {
        public string ReferenceText { get; set; }

        public override string ToString() {
            return string.Format("CodeReferenceResultItem: ReferenceText=\"{0}\", Value=\"{1}\", Source=\"{2}\", Target=\"{3}\"", ReferenceText, Value, (SourceItem == null ? "(null)" : SourceItem.Name), (DestinationItem == null ? "(null)" : DestinationItem.InternalProjectItem.Name));
        }
    }
}
