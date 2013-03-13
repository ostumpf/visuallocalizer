using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Library {

    /// <summary>
    /// Container for extension methods working with plain objects. 
    /// </summary>
    public static class ObjectEx {

        /// <summary>
        /// Returns type name (handles COM-like object types)
        /// </summary>        
        public static string GetVisualBasicType(this object o) {
            if (o == null) throw new ArgumentNullException("o");
            return Microsoft.VisualBasic.Information.TypeName(o);
        }

    }
}
