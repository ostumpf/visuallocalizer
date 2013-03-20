using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Components {
    internal sealed class AspNetCSharpReferenceLookuper : CSharpLookuper<AspNetCodeReferenceResultItem> {
        private static AspNetCSharpReferenceLookuper instance;

        private AspNetCSharpReferenceLookuper() { }

        public static AspNetCSharpReferenceLookuper Instance {
            get {
                if (instance == null) instance = new AspNetCSharpReferenceLookuper();
                return instance;
            }
        }

        protected override AspNetCodeReferenceResultItem AddReferenceResult(List<AspNetCodeReferenceResultItem> list, string referenceText, List<CodeReferenceInfo> trieElementInfos) {
            var item = base.AddReferenceResult(list, referenceText, trieElementInfos);
            item.Language = VisualLocalizer.Library.LANGUAGE.CSHARP;
            return item;
        }
    }
}
