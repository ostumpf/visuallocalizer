using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;

namespace VisualLocalizer.Components.AspxParser {
    internal sealed class WebConfig {
        public static WebConfig Load(Project project) {
            return new WebConfig();
        }

    }
}
