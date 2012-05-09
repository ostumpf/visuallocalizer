using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;

namespace VisualLocalizer.Library {
    public class OutputWindowPane {

        protected IVsOutputWindowPane pane;

        public OutputWindowPane(IVsOutputWindowPane pane) {
            this.pane = pane;            
        }
        
        public void Activate() {
            if (pane == null) return;
            if (pane.Activate() != VSConstants.S_OK)
                throw new Exception("Unexpected return code.");
        }

        public void Clear() {
            if (pane == null) return;
            if (pane.Clear() != VSConstants.S_OK)
                throw new Exception("Unexpected return code.");
        }

        public void FlushToTaskList() {
            if (pane == null) return;
            if (pane.FlushToTaskList() != VSConstants.S_OK)
                throw new Exception("Unexpected return code.");
        }

        public void Hide() {
            if (pane == null) return;
            if (pane.Hide() != VSConstants.S_OK)
                throw new Exception("Unexpected return code.");
        }

        public string Name {
            get {
                if (pane == null) 
                    return "BLACKHOLE";

                string name = string.Empty;
                int hr=pane.GetName(ref name);
                if (hr != VSConstants.S_OK)
                    throw new Exception("Unexpected return code.");
                return name;
            }
            set {
                if (pane == null) return;

                int hr = pane.SetName(value);
                if (hr != VSConstants.S_OK)
                    throw new Exception("Unexpected return code.");
            }
        }

        public void Write(string formatString,params object[] args) {
            if (pane == null) return;
            if (formatString == null) {
                pane.OutputString("(null)");
            } else {
                pane.OutputString(string.Format(formatString, args));
            }
        }

        public void WriteLine(string formatString, params object[] args) {
            if (pane == null) return;

            Write(formatString, args);
            Write(Environment.NewLine);            
        }

        public void WriteTaskItem(string text,VSTASKPRIORITY priority,VSTASKCATEGORY category,string subcategory,
            int bitmap,string filename,uint linenum,string taskItemText) {
            if (pane == null) return;

            pane.OutputTaskItemString(text, priority, category, subcategory, bitmap, filename, linenum, taskItemText);            
        }

        public void WriteTaskItem(string text, VSTASKPRIORITY priority, VSTASKCATEGORY category, string subcategory,
                    int bitmap, string filename, uint linenum, string taskItemText,string lookupKwd) {
            if (pane == null) return;

            pane.OutputTaskItemStringEx(text, priority, category, subcategory, bitmap, filename, linenum, taskItemText,lookupKwd);
        }
    }
}
