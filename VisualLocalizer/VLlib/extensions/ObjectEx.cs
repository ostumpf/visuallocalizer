using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Library {
    public static class ObjectEx {

        public static string GetVisualBasicType(this object o) {
            if (o == null)
                throw new ArgumentNullException("o");
            return Microsoft.VisualBasic.Information.TypeName(o);
        }

    }
}
