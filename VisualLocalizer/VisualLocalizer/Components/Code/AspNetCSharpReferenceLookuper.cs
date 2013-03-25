using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Components {

    /// <summary>
    /// Represents lookuper of references to resources in ASP .NET C# code blocks
    /// </summary>
    internal sealed class AspNetCSharpReferenceLookuper : CSharpLookuper<AspNetCodeReferenceResultItem> {
        private static AspNetCSharpReferenceLookuper instance;

        private AspNetCSharpReferenceLookuper() { }

        public static AspNetCSharpReferenceLookuper Instance {
            get {
                if (instance == null) instance = new AspNetCSharpReferenceLookuper();
                return instance;
            }
        }

        /// <summary>
        /// Adds reference to a resource result item to the list
        /// </summary>
        /// <param name="list">Result list</param>
        /// <param name="referenceText">Full text of the reference</param>
        /// <param name="trieElementInfos">Info about reference, taken from terminal state of the trie</param>
        /// <returns>
        /// New result item
        /// </returns>
        protected override AspNetCodeReferenceResultItem AddReferenceResult(List<AspNetCodeReferenceResultItem> list, string referenceText, List<CodeReferenceInfo> trieElementInfos) {
            var item = base.AddReferenceResult(list, referenceText, trieElementInfos);
            item.Language = VisualLocalizer.Library.LANGUAGE.CSHARP;
            return item;
        }
    }
}
