using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE;
using VisualLocalizer.Library.AspxParser;

namespace VisualLocalizer.Components {

    /// <summary>
    /// Represents lookuper of references in C# code
    /// </summary>
    internal sealed class CSharpReferenceLookuper : CSharpLookuper<CSharpCodeReferenceResultItem> {

        private static CSharpReferenceLookuper instance;        

        private CSharpReferenceLookuper() { }

        public static CSharpReferenceLookuper Instance {
            get {
                if (instance == null) instance = new CSharpReferenceLookuper();
                return instance;
            }
        }
        
    }
}
