using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

namespace VisualLocalizer.Library.Components {

    /// <summary>
    /// Provides functionality for adding entries to VS log.
    /// </summary>
    public class ActivityLogger {

        /// <summary>
        /// Instance of the IVsActivityLog service
        /// </summary>
        protected static IVsActivityLog logService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityLogger"/> class.
        /// </summary>
        protected ActivityLogger() { }

        /// <summary>
        /// Initializes the services
        /// </summary>
        static ActivityLogger() {
            logService = (IVsActivityLog)Package.GetGlobalService(typeof(SVsActivityLog));
            if (logService == null) throw new Exception("Cannot consume SVsActivityLog service.");
        }

        /// <summary>
        /// Listed in column "Source" in log file
        /// </summary>
        public static string Source {
            get;
            set;
        }

        /// <summary>
        /// Creates a new log entry
        /// </summary>
        /// <param name="type">Category of the entry</param>
        /// <param name="message">Message</param>
        public static void Log(EntryType type, string message) {
            if (message == null) throw new ArgumentNullException("message");
            if (Source == null) throw new InvalidOperationException("ActivityLogger is not sufficiently initialized.");

            int hr = logService.LogEntry((uint)type, Source, message);
            Marshal.ThrowExceptionForHR(hr);
        }

        /// <summary>
        /// Creates a new log entry
        /// </summary>
        /// <param name="type">Category of the entry</param>
        /// <param name="message">Message</param>
        /// <param name="path">File path that will be written to the log</param>
        public static void Log(EntryType type, string message,string path) {
            if (message == null) throw new ArgumentNullException("message");
            if (path == null) throw new ArgumentNullException("path");
            if (Source == null) throw new InvalidOperationException("ActivityLogger is not sufficiently initialized.");

            int hr = logService.LogEntryPath((uint)type, Source, message, path);
            Marshal.ThrowExceptionForHR(hr);
        }

        /// <summary>
        /// Creates a new log entry
        /// </summary>
        /// <param name="type">Category of the entry</param>
        /// <param name="message">Message</param>
        /// <param name="hr">HResult that caused the error</param>
        public static void Log(EntryType type, string message,int hr) {
            if (message == null) throw new ArgumentNullException("message");
            if (Source == null) throw new InvalidOperationException("ActivityLogger is not sufficiently initialized.");

            int returnHr = logService.LogEntryHr((uint)type, Source, message, hr);
            Marshal.ThrowExceptionForHR(returnHr);
        }

        /// <summary>
        /// Creates a new log entry
        /// </summary>
        /// <param name="type">Category of the entry</param>
        /// <param name="message">Message</param>
        /// <param name="guid">GUID of the object that caused the error</param>
        public static void Log(EntryType type, string message,Guid guid) {
            if (message == null) throw new ArgumentNullException("message");
            if (guid == null) throw new ArgumentNullException("guid");
            if (Source == null) throw new InvalidOperationException("ActivityLogger is not sufficiently initialized.");

            int hr = logService.LogEntryGuid((uint)type, Source, message, guid);
            Marshal.ThrowExceptionForHR(hr);
        }

        /// <summary>
        /// Creates a new log entry
        /// </summary>
        /// <param name="type">Category of the entry</param>
        /// <param name="message">Message</param>
        /// <param name="hr">HResult that caused the error</param>
        /// <param name="path">File path that will be written to the log</param>
        public static void Log(EntryType type, string message,int hr, string path) {
            if (message == null) throw new ArgumentNullException("message");
            if (path == null) throw new ArgumentNullException("path");
            if (Source == null) throw new InvalidOperationException("ActivityLogger is not sufficiently initialized.");

            int returnHr = logService.LogEntryHrPath((uint)type, Source, message, hr, path);
            Marshal.ThrowExceptionForHR(returnHr);
        }

        /// <summary>
        /// Creates a new log entry
        /// </summary>
        /// <param name="type">Category of the entry</param>
        /// <param name="message">Message</param>
        /// <param name="guid">GUID of the object that caused the error</param>
        /// <param name="path">File path that will be written to the log</param>
        public static void Log(EntryType type, string message, Guid guid, string path) {
            if (message == null) throw new ArgumentNullException("message");
            if (path == null) throw new ArgumentNullException("path");
            if (guid == null) throw new ArgumentNullException("guid");
            if (Source == null) throw new InvalidOperationException("ActivityLogger is not sufficiently initialized.");

            int hr = logService.LogEntryGuidPath((uint)type, Source, message, guid, path);
            Marshal.ThrowExceptionForHR(hr);
        }

        /// <summary>
        /// Creates a new log entry
        /// </summary>
        /// <param name="type">Category of the entry</param>
        /// <param name="message">Message</param>
        /// <param name="guid">GUID of the object that caused the error</param>
        /// <param name="hr">HResult that caused the error</param>        
        public static void Log(EntryType type, string message, Guid guid, int hr) {
            if (message == null) throw new ArgumentNullException("message");
            if (guid == null) throw new ArgumentNullException("guid");
            if (Source == null) throw new InvalidOperationException("ActivityLogger is not sufficiently initialized.");

            int returnHr = logService.LogEntryGuidHr((uint)type, Source, message, guid, hr);
            Marshal.ThrowExceptionForHR(returnHr);
        }

        /// <summary>
        /// Creates a new log entry
        /// </summary>
        /// <param name="type">Category of the entry</param>
        /// <param name="message">Message</param>
        /// <param name="guid">GUID of the object that caused the error</param>
        /// <param name="hr">HResult that caused the error</param>        
        /// <param name="path">File path that will be written to the log</param>
        public static void Log(EntryType type, string message, Guid guid, int hr,string path) {
            if (message == null) throw new ArgumentNullException("message");
            if (path == null) throw new ArgumentNullException("path");
            if (guid == null) throw new ArgumentNullException("guid");
            if (Source == null) throw new InvalidOperationException("ActivityLogger is not sufficiently initialized.");

            int returnHr = logService.LogEntryGuidHrPath((uint)type, Source, message, guid, hr, path);
            Marshal.ThrowExceptionForHR(returnHr);
        }
    }

    /// <summary>
    /// Categories of the log entries
    /// </summary>
    public enum EntryType : uint { 
        /// <summary>
        /// Information
        /// </summary>
        INFORMATION=__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, 

        /// <summary>
        /// Warning
        /// </summary>
        WARNING=__ACTIVITYLOG_ENTRYTYPE.ALE_WARNING, 

        /// <summary>
        /// Error
        /// </summary>
        ERROR=__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR };
}
