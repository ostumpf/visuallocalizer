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
            if (pane.Activate() != VSConstants.S_OK)
                throw new Exception("Unexpected return code.");
        }

        public void Clear() {
            if (pane.Clear() != VSConstants.S_OK)
                throw new Exception("Unexpected return code.");
        }

        public void FlushToTaskList() {
            if (pane.FlushToTaskList() != VSConstants.S_OK)
                throw new Exception("Unexpected return code.");
        }

        public void Hide() {
            if (pane.Hide() != VSConstants.S_OK)
                throw new Exception("Unexpected return code.");
        }

        public string Name {
            get {
                string name = string.Empty;
                int hr=pane.GetName(ref name);
                if (hr != VSConstants.S_OK)
                    throw new Exception("Unexpected return code.");
                return name;
            }
            set {
                int hr = pane.SetName(value);
                if (hr != VSConstants.S_OK)
                    throw new Exception("Unexpected return code.");
            }
        }

        public void Write(string formatString,params object[] args) {
            pane.OutputString(string.Format(formatString,args));
        }

        public void WriteLine(string formatString, params object[] args) {
            Write(formatString, args);
            Write(Environment.NewLine);            
        }

        public void WriteTaskItem(string text,VSTASKPRIORITY priority,VSTASKCATEGORY category,string subcategory,
            int bitmap,string filename,uint linenum,string taskItemText) {
            pane.OutputTaskItemString(text, priority, category, subcategory, bitmap, filename, linenum, taskItemText);            
        }

        public void WriteTaskItem(string text, VSTASKPRIORITY priority, VSTASKCATEGORY category, string subcategory,
                    int bitmap, string filename, uint linenum, string taskItemText,string lookupKwd) {
            pane.OutputTaskItemStringEx(text, priority, category, subcategory, bitmap, filename, linenum, taskItemText,lookupKwd);
        }
    }
}
