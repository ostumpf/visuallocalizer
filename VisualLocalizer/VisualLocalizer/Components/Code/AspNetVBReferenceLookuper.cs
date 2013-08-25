using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Components.Code {

    /// <summary>
    /// Represents lookuper of references to resources in ASP .NET VB code blocks
    /// </summary>
    internal sealed class AspNetVBReferenceLookuper : VBLookuper<AspNetCodeReferenceResultItem> {
        private static AspNetVBReferenceLookuper instance;

        private AspNetVBReferenceLookuper() { }

        public static AspNetVBReferenceLookuper Instance {
            get {
                if (instance == null) instance = new AspNetVBReferenceLookuper();
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
            var result = base.AddReferenceResult(list, referenceText, trieElementInfos);
            result.Language = VisualLocalizer.Library.Extensions.LANGUAGE.VB;
            return result;
        }
    }
}
