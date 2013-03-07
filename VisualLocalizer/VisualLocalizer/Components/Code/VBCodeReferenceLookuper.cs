using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Components {
    internal class VBCodeReferenceLookuper : VBLookuper<VBCodeReferenceResultItem> {

        private static VBCodeReferenceLookuper instance;

        private VBCodeReferenceLookuper() { }

        public static VBCodeReferenceLookuper Instance {
            get {
                if (instance == null) instance = new VBCodeReferenceLookuper();
                return instance;
            }
        }

    }
}
