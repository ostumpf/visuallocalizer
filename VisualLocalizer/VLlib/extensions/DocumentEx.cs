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

    /// <summary>
    /// Container for extension methods working with Document-like objects. 
    /// </summary>
    public static class DocumentEx {

        /// <summary>
        /// Adds new namespace import into the given document.
        /// </summary>        
        public static CodeImport AddUsingBlock(this Document document, string newNamespace) {
            if (document == null || document.ProjectItem == null) throw new Exception("No document or project item.");
            if (string.IsNullOrEmpty(newNamespace)) throw new ArgumentNullException("newNamespace");

            bool fileOpened;
            FileCodeModel2 model = document.ProjectItem.GetCodeModel(true, false, out fileOpened);
            
            return model.AddImport(newNamespace, 0, string.Empty);
        }

        /// <summary>
        /// Returns true if child TextSpan is contained within parent TextSpan. That is, parent TextSpan begins earlier in the document
        /// and ends later.
        /// </summary>        
        public static bool Contains(this TextSpan parent, TextSpan child) {
            bool before = parent.iStartLine < child.iStartLine || (parent.iStartLine == child.iStartLine && parent.iStartIndex <= child.iStartIndex);
            bool after = parent.iEndLine > child.iEndLine || (parent.iEndLine == child.iEndLine && parent.iEndIndex >= child.iEndIndex);
            return before && after;
        }

       
    }
}
