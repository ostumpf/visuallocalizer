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

        protected override AspNetCodeReferenceResultItem AddStringResult(List<AspNetCodeReferenceResultItem> list, string originalValue, bool isVerbatimString, bool isUnlocalizableCommented) {
            var result = base.AddStringResult(list, originalValue, isVerbatimString, isUnlocalizableCommented);
            result.Language = VisualLocalizer.Library.LANGUAGE.VB;
            return result;
        }
    }
}
