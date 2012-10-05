using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;
using System.ComponentModel.Design;
using System.Reflection;
using System.Drawing;

namespace VisualLocalizer.Library {
    public static class ResXDataNodeEx {

        public static bool HasStringValue(this ResXDataNode node) {
            string type = node.GetValueTypeName((ITypeResolutionService)null);
            return !string.IsNullOrEmpty(type) && Type.GetType(type) == typeof(string);
        }

        public static bool HasImageValue(this ResXDataNode node) {
            string type = node.GetValueTypeName((ITypeResolutionService)null);
            return !string.IsNullOrEmpty(type) && Type.GetType(type) == typeof(Bitmap);
        }

        public static string GetStringValue(this ResXDataNode node) {
            string type = node.GetValueTypeName((ITypeResolutionService)null);
            return string.IsNullOrEmpty(type) ? null : (string)node.GetValue((ITypeResolutionService)null);
        }

        public static Bitmap GetImageValue(this ResXDataNode node) {
            string type = node.GetValueTypeName((ITypeResolutionService)null);
            return string.IsNullOrEmpty(type) ? null : (Bitmap)node.GetValue((ITypeResolutionService)null);
        }
    }
}
