using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Components {
    internal sealed class AspNetVBReferenceLookuper : VBLookuper<AspNetCodeReferenceResultItem> {
        private static AspNetVBReferenceLookuper instance;

        private AspNetVBReferenceLookuper() { }

        public static AspNetVBReferenceLookuper Instance {
            get {
                if (instance == null) instance = new AspNetVBReferenceLookuper();
                return instance;
            }
        }

        protected override AspNetCodeReferenceResultItem AddReferenceResult(List<AspNetCodeReferenceResultItem> list, string referenceText, List<CodeReferenceInfo> trieElementInfos) {
            var result = base.AddReferenceResult(list, referenceText, trieElementInfos);
            result.Language = VisualLocalizer.Library.LANGUAGE.VB;
            return result;
        }
    }
}
