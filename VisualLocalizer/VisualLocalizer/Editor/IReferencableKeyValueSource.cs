using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using VisualLocalizer.Components;

namespace VisualLocalizer.Editor {
    internal interface IReferencableKeyValueSource : IKeyValueSource {
        List<CodeReferenceResultItem> CodeReferences { get; set; }
        void UpdateReferenceCount(bool determinated);
    }
}
