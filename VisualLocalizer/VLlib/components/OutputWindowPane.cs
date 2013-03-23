using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;

namespace VisualLocalizer.Library {

    /// <summary>
    /// Wrapper for standard VS IVsOutputWindowPane interface
    /// </summary>
    public class OutputWindowPane {

        protected IVsOutputWindowPane pane;

        public OutputWindowPane(IVsOutputWindowPane pane) {
            this.pane = pane;            
        }
        
        public void Activate() {
            if (pane == null) return;
            int hr = pane.Activate();
            Marshal.ThrowExceptionForHR(hr);
        }

        public void Clear() {
            if (pane == null) return;
            int hr = pane.Clear();
            Marshal.ThrowExceptionForHR(hr);
        }

        public void FlushToTaskList() {
            if (pane == null) return;
            int hr = pane.FlushToTaskList();
            Marshal.ThrowExceptionForHR(hr);
        }

        public void Hide() {
            if (pane == null) return;
            int hr = pane.Hide();
            Marshal.ThrowExceptionForHR(hr);
        }

        public string Name {
            get {
                if (pane == null) return "BLACKHOLE";

                string name = string.Empty;
                int hr = pane.GetName(ref name); 
                Marshal.ThrowExceptionForHR(hr);

                return name;
            }
            set {
                if (pane == null) return;

                int hr = pane.SetName(value);                
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        /// <summary>
        /// Writes formatted string to the output pane
        /// </summary>        
        public void Write(string formatString,params object[] args) {
            if (pane == null) return;
            int hr;
            if (formatString == null) {
                hr = pane.OutputString("(null)");
            } else {
                if (args == null || args.Length == 0) {
                    hr = pane.OutputString(formatString);
                } else {
                    hr = pane.OutputString(string.Format(formatString, args));
                }
            }            
            Marshal.ThrowExceptionForHR(hr);
        }

        /// <summary>
        /// Writes formatted string to the output pane and terminates the line
        /// </summary>  
        public void WriteLine(string formatString, params object[] args) {
            if (pane == null) return;

            Write(formatString, args);
            Write(Environment.NewLine);            
        }

        public void WriteException(Exception ex) {
            WriteLine("{0} occurred while processing command.\nMessage: {1}\n{2}", ex.GetType().Name, ex.Message, ex.StackTrace);
        }

        public void WriteException(Exception ex, Dictionary<string,string> moreData) {
            WriteLine("{0} occurred while processing command.\nMessage: {1}\n{2}", ex.GetType().Name, ex.Message, ex.StackTrace);
            if (moreData != null) {
                foreach (var pair in moreData) {
                    WriteLine("{0}:\n{1}", pair.Key, pair.Value);
                }
            }
        }

        public void WriteTaskItem(string text,VSTASKPRIORITY priority,VSTASKCATEGORY category,string subcategory,
            int bitmap,string filename,uint linenum,string taskItemText) {
            if (pane == null) return;

            int hr = pane.OutputTaskItemString(text, priority, category, subcategory, bitmap, filename, linenum, taskItemText);
            Marshal.ThrowExceptionForHR(hr);
        }

        public void WriteTaskItem(string text, VSTASKPRIORITY priority, VSTASKCATEGORY category, string subcategory,
                    int bitmap, string filename, uint linenum, string taskItemText,string lookupKwd) {
            if (pane == null) return;

            int hr = pane.OutputTaskItemStringEx(text, priority, category, subcategory, bitmap, filename, linenum, taskItemText, lookupKwd);
            Marshal.ThrowExceptionForHR(hr);
        }
    }
}
