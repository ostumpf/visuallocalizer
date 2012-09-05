using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using EnvDTE80;

namespace VisualLocalizer.Library {
    public static class DocumentEx {

        public static CodeImport AddUsingBlock(this EnvDTE.Document document, string newNamespace) {
            FileCodeModel2 model = document.ProjectItem.FileCodeModel as FileCodeModel2;
            if (model == null)
                throw new Exception("Current document has no CodeModel.");

            return model.AddImport(newNamespace, 0, string.Empty);
        }
        
    }
}
