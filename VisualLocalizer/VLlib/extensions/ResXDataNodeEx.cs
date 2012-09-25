using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;
using System.ComponentModel.Design;
using System.Reflection;

namespace VisualLocalizer.Library {
    public static class ResXDataNodeEx {

        public static bool HasStringValue(this ResXDataNode node) {
            string type = node.GetValueTypeName((ITypeResolutionService)null);
            return !string.IsNullOrEmpty(type) && Type.GetType(type) == typeof(string);
        }

        public static string GetStringValue(this ResXDataNode node) {
            string type = node.GetValueTypeName((ITypeResolutionService)null);
            return string.IsNullOrEmpty(type) ? null : (string)node.GetValue((ITypeResolutionService)null);
        }

    }
}
