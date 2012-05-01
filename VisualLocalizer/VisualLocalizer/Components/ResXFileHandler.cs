using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;
using EnvDTE;
using System.Collections;
using VSLangProj;
using System.Diagnostics;

namespace VisualLocalizer.Editor {
    internal static class ResXFileHandler {

        public static void AddString(string key, string value, ResXProjectItem item) {
            VLOutputWindow.VisualLocalizerPane.WriteLine("Adding \"{0}\":\"{1}\" to \"{2}\"", key, value, item.DisplayName);
         /*   foreach (Property prop in item.ProjectItem.Properties)
                VLOutputWindow.VisualLocalizerPane.WriteLine(prop.Name+":"+prop.Value);*/

            string path = item.ProjectItem.Properties.Item("FullPath").Value.ToString();

            ResXResourceReader reader = new ResXResourceReader(path);
            Hashtable content = new Hashtable();
            foreach (DictionaryEntry entry in reader) {
                content.Add(entry.Key, entry.Value);
            }
            reader.Close();

            ResXResourceWriter writer = new ResXResourceWriter(path);
            foreach (DictionaryEntry entry in content) {
                writer.AddResource(entry.Key.ToString(), entry.Value);
            }            
            writer.AddResource(key, value);
            writer.Close();

            (item.ProjectItem.Object as VSProjectItem).RunCustomTool();
        }

        public static bool ContainsKey(string key, ResXProjectItem item) {
            string path = item.ProjectItem.Properties.Item("FullPath").Value.ToString();
            ResXResourceReader reader = new ResXResourceReader(path);
            bool found = false;
            try {
                foreach (DictionaryEntry entry in reader) {
                    if (entry.Key.ToString() == key) {
                        found = true;
                        break;
                    }
                }
                reader.Close();
            } catch (Exception ex) {
                Trace.WriteLine(ex.Message);
            }
            return found;
        }

        public static List<string> GetKeys(ResXProjectItem item) {
            List<string> list = new List<string>();
            string path = item.ProjectItem.Properties.Item("FullPath").Value.ToString();
           
            ResXResourceReader reader = new ResXResourceReader(path);
            foreach (DictionaryEntry entry in reader) {
                list.Add(entry.Key.ToString());
            }
            reader.Close();

            return list;
        }
    }
}
