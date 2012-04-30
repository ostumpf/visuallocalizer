using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;
using EnvDTE;

namespace VisualLocalizer.Components {
    internal static class ResXFileHandler {

        public static void AddString(string key, string value, ResXProjectItem item) {
            VLOutputWindow.VisualLocalizerPane.WriteLine("Adding \"{0}\":\"{1}\" to \"{2}\"", key, value, item.DisplayName);
         /*   foreach (Property prop in item.ProjectItem.Properties)
                VLOutputWindow.VisualLocalizerPane.WriteLine(prop.Name+":"+prop.Value);*/

            string path = item.ProjectItem.Properties.Item("FullPath").Value.ToString();

            ResXResourceWriter writer = new ResXResourceWriter(path);
            writer.AddResource(key, value);
            writer.Close();
        }
    }
}
