using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;
using System.ComponentModel.Design;
using System.Reflection;
using System.Drawing;
using System.IO;

namespace VisualLocalizer.Library {

    /// <summary>
    /// Container for extension methods working with ResXDataNode objects. 
    /// </summary>
    public static class ResXDataNodeEx {

        /// <summary>
        /// Returns true if given node contains value of type T.
        /// </summary>        
        public static bool HasValue<T>(this ResXDataNode node) {
            if (node == null) throw new ArgumentNullException("node");

            string type = node.GetValueTypeName((ITypeResolutionService)null);
            bool hasType = !string.IsNullOrEmpty(type) && Type.GetType(type) == typeof(T);

            return hasType && (typeof(T) != typeof(string) || node.FileRef == null);
        }

        /// <summary>
        /// Obtains value of type T from given node.
        /// </summary>        
        public static T GetValue<T>(this ResXDataNode node) where T:class {
            if (node == null) throw new ArgumentNullException("node");

            string type = node.GetValueTypeName((ITypeResolutionService)null);
            bool exists = node.FileRef == null || File.Exists(node.FileRef.FileName);
            return (string.IsNullOrEmpty(type) || !exists) ? null : (T)node.GetValue((ITypeResolutionService)null);
        }
    }
}
