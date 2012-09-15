using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;

namespace VisualLocalizer.Library {
    public static class DocumentEx {

        public static CodeImport AddUsingBlock(this EnvDTE.Document document, string newNamespace) {
            FileCodeModel2 model = document.ProjectItem.FileCodeModel as FileCodeModel2;
            if (model == null)
                throw new Exception("Current document has no CodeModel.");

            return model.AddImport(newNamespace, 0, string.Empty);
        }

        public static bool Contains(this TextSpan parent, TextSpan child) {
            bool before = parent.iStartLine < child.iStartLine || (parent.iStartLine == child.iStartLine && parent.iStartIndex <= child.iStartIndex);
            bool after = parent.iEndLine > child.iEndLine || (parent.iEndLine == child.iEndLine && parent.iEndIndex >= child.iEndIndex);
            return before && after;
        }

       
    }
}
