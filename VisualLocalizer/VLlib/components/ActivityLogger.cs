using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

namespace VisualLocalizer.Library {
    public class ActivityLogger {

        protected static IVsActivityLog logService;

        protected ActivityLogger() { }

        static ActivityLogger() {
            logService=(IVsActivityLog)Package.GetGlobalService(typeof(SVsActivityLog));
            if (logService == null)  throw new Exception("Cannot consume SVsActivityLog service.");
        }

        public static string Source {
            get;
            set;
        }

       
        public static void Log(EntryType type, string message) {
            if (message == null)
                throw new ArgumentNullException("message");
            if (Source == null)
                throw new InvalidOperationException("ActivityLogger is not sufficiently initialized.");

            int hr = logService.LogEntry((uint)type, Source, message);
            Marshal.ThrowExceptionForHR(hr);
        }

        public static void Log(EntryType type, string message,string path) {
            if (message == null)
                throw new ArgumentNullException("message");
            if (path == null)
                throw new ArgumentNullException("path");
            if (Source == null)
                throw new InvalidOperationException("ActivityLogger is not sufficiently initialized.");

            int hr = logService.LogEntryPath((uint)type, Source, message, path);
            Marshal.ThrowExceptionForHR(hr);
        }

        public static void Log(EntryType type, string message,int hr) {
            if (message == null)
                throw new ArgumentNullException("message");
            if (Source == null)
                throw new InvalidOperationException("ActivityLogger is not sufficiently initialized.");

            int returnHr = logService.LogEntryHr((uint)type, Source, message, hr);
            Marshal.ThrowExceptionForHR(returnHr);
        }

        public static void Log(EntryType type, string message,Guid guid) {
            if (message == null)
                throw new ArgumentNullException("message");
            if (guid == null)
                throw new ArgumentNullException("guid");
            if (Source == null)
                throw new InvalidOperationException("ActivityLogger is not sufficiently initialized.");

            int hr = logService.LogEntryGuid((uint)type, Source, message, guid);
            Marshal.ThrowExceptionForHR(hr);
        }

        public static void Log(EntryType type, string message,int hr, string path) {
            if (message == null)
                throw new ArgumentNullException("message");
            if (path == null)
                throw new ArgumentNullException("path");
            if (Source == null)
                throw new InvalidOperationException("ActivityLogger is not sufficiently initialized.");

            int returnHr = logService.LogEntryHrPath((uint)type, Source, message, hr, path);
            Marshal.ThrowExceptionForHR(returnHr);
        }

        public static void Log(EntryType type, string message, Guid guid, string path) {
            if (message == null)
                throw new ArgumentNullException("message");
            if (path == null)
                throw new ArgumentNullException("path");
            if (guid == null)
                throw new ArgumentNullException("guid");
            if (Source == null)
                throw new InvalidOperationException("ActivityLogger is not sufficiently initialized.");

            int hr = logService.LogEntryGuidPath((uint)type, Source, message, guid, path);
            Marshal.ThrowExceptionForHR(hr);
        }

        public static void Log(EntryType type, string message, Guid guid, int hr) {
            if (message == null)
                throw new ArgumentNullException("message");
            if (guid == null)
                throw new ArgumentNullException("guid");
            if (Source == null)
                throw new InvalidOperationException("ActivityLogger is not sufficiently initialized.");

            int returnHr = logService.LogEntryGuidHr((uint)type, Source, message, guid, hr);
            Marshal.ThrowExceptionForHR(returnHr);
        }

        public static void Log(EntryType type, string message, Guid guid, int hr,string path) {
            if (message == null)
                throw new ArgumentNullException("message");
            if (path == null)
                throw new ArgumentNullException("path");
            if (guid == null)
                throw new ArgumentNullException("guid");
            if (Source == null)
                throw new InvalidOperationException("ActivityLogger is not sufficiently initialized.");

            int returnHr = logService.LogEntryGuidHrPath((uint)type, Source, message, guid, hr, path);
            Marshal.ThrowExceptionForHR(returnHr);
        }
    }

    public enum EntryType : uint { 
        INFORMATION=__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, 
        WARNING=__ACTIVITYLOG_ENTRYTYPE.ALE_WARNING, 
        ERROR=__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR };
}
