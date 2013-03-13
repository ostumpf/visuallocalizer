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

        protected override AspNetCodeReferenceResultItem AddStringResult(List<AspNetCodeReferenceResultItem> list, string originalValue, bool isVerbatimString, bool isUnlocalizableCommented) {
            var result = base.AddStringResult(list, originalValue, isVerbatimString, isUnlocalizableCommented);
            result.Language = VisualLocalizer.Library.LANGUAGE.CSHARP;
            return result;
        }
    }
}
