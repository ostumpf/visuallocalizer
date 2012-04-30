using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace VisualLocalizer.Library {
    public class ActivityLogger {

        protected static IVsActivityLog logService;

        protected ActivityLogger() { }

        static ActivityLogger() {
            logService=(IVsActivityLog)Package.GetGlobalService(typeof(SVsActivityLog));
            Source = "VSPackage";
            if (logService == null)  throw new Exception("Cannot consume SVsActivityLog service.");
        }

        public static string Source {
            get;
            set;
        }

       
        public static void Log(EntryType type, string message) {
            logService.LogEntry((uint)type, Source, message);
        }

        public static void Log(EntryType type, string message,string path) {
            logService.LogEntryPath((uint)type, Source, message,path);
        }

        public static void Log(EntryType type, string message,int hr) {
            logService.LogEntryHr((uint)type, Source, message,hr);
        }

        public static void Log(EntryType type, string message,Guid guid) {
            logService.LogEntryGuid((uint)type, Source, message,guid);
        }

        public static void Log(EntryType type, string message,int hr, string path) {
            logService.LogEntryHrPath((uint)type, Source, message, hr, path);
        }

        public static void Log(EntryType type, string message, Guid guid, string path) {
            logService.LogEntryGuidPath((uint)type, Source, message, guid, path);
        }

        public static void Log(EntryType type, string message, Guid guid, int hr) {
            logService.LogEntryGuidHr((uint)type, Source, message, guid,hr);
        }

        public static void Log(EntryType type, string message, Guid guid, int hr,string path) {
            logService.LogEntryGuidHrPath((uint)type, Source, message, guid, hr,path);
        }
    }

    public enum EntryType : uint { 
        INFORMATION=__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, 
        WARNING=__ACTIVITYLOG_ENTRYTYPE.ALE_WARNING, 
        ERROR=__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR };
}
