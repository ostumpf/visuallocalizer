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
    }
}
