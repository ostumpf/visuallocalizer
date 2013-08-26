using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using VisualLocalizer.Components;
using System.Windows.Forms;
using Microsoft.VisualStudio.TextManager.Interop;
using VisualLocalizer.Library;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using EnvDTE80;
using VisualLocalizer.Components.Code;
using VisualLocalizer.Library.Components;
using VisualLocalizer.Library.Extensions;

namespace VisualLocalizer.Gui {

    /// <summary>
    /// Implementations of this interface can issue the "HighlightRequired" event, which causes block of text in the code window to be selected.
    /// </summary>
    internal interface IHighlightRequestSource {
        event EventHandler<CodeResultItemEventArgs> HighlightRequired;
    }

    /// <summary>
    /// Argument for the "HighlightRequired" event
    /// </summary>
    internal sealed class CodeResultItemEventArgs : EventArgs {
        /// <summary>
        /// Code result item whose text should be selected in the code window
        /// </summary>
        public AbstractResultItem Item { get; set; }
    }

    /// <summary>
    /// Implements IVsWindowFrameNotify3 to receive information about tool window events and provides WindowHidden event, issued
    /// when the toolwindow is closed.
    /// </summary>
    internal sealed class ToolWindowEvents : IVsWindowFrameNotify3 {

        public event EventHandler WindowHidden;
        
        public int OnClose(ref uint pgrfSaveOptions) {
            return VSConstants.S_OK;
        }

        public int OnDockableChange(int fDockable, int x, int y, int w, int h) {
            return VSConstants.S_OK;
        }

        public int OnMove(int x, int y, int w, int h) {
            return VSConstants.S_OK;
        }

        public int OnSize(int x, int y, int w, int h) {
            return VSConstants.S_OK;
        }
       
        public int OnShow(int fShow) {         
            if (fShow == (int)__FRAMESHOW2.FRAMESHOW_BeforeWinHidden) {
                if (WindowHidden != null) WindowHidden(this, null);
            }            
            
            return VSConstants.S_OK;
        }

    }

    /// <summary>
    /// Base class for "batch" tool windows. Provides functionality for receiving the "window closed" event and handles the "HighlightRequired" method.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class AbstractCodeToolWindow<T> : ToolWindowPane where T : Control,IHighlightRequestSource, new() {

        /// <summary>
        /// Content of the toolwindow
        /// </summary>
        protected T panel;

        /// <summary>
        /// Instance of ToolWindowEvents, receiving events from VS
        /// </summary>
        protected ToolWindowEvents windowEvents;

        public AbstractCodeToolWindow():base(null) {
            this.panel = new T(); // creates new tool window content
            this.panel.HighlightRequired+=new EventHandler<CodeResultItemEventArgs>(Panel_HighlightRequired);

            windowEvents = new ToolWindowEvents();
            windowEvents.WindowHidden += new EventHandler(OnWindowHidden);

            try {
                var events = VisualLocalizerPackage.Instance.DTE.Events as Events2;
                events.SolutionEvents.BeforeClosing += new EnvDTE._dispSolutionEvents_BeforeClosingEventHandler(OnSolutionClosing);
            } catch { }

            AddEventsListener();
        }

        /// <summary>
        /// Called when current solution is being closed
        /// </summary>
        protected virtual void OnSolutionClosing() {            
        }

        protected override void Initialize() {
            base.Initialize();

            AddEventsListener();
        }

        public override void OnToolWindowCreated() {
            base.OnToolWindowCreated();

            AddEventsListener();
        }
        
        protected override void OnCreate() {
            base.OnCreate();

            AddEventsListener();
        }

        /// <summary>
        /// Sets ToolWindowEvents instance as a listener for the VS events
        /// </summary>
        public void AddEventsListener() {            
            IVsWindowFrame frame = (IVsWindowFrame)Frame;
            if (frame != null) 
                frame.SetProperty((int)__VSFPROPID.VSFPROPID_ViewHelper, windowEvents);
        }

        /// <summary>
        /// Called when the toolwindow is closed
        /// </summary>        
        protected virtual void OnWindowHidden(object sender, EventArgs e) {            
        }

        /// <summary>
        /// Highlights given block of text in the code window
        /// </summary>        
        protected void Panel_HighlightRequired(object sender, CodeResultItemEventArgs e) {
            try {
                // obtains IVsTextView instance, opening the file if necessary
                IVsTextView view = DocumentViewsManager.GetTextViewForFile(e.Item.SourceItem.GetFullPath(), true, true);
                if (view == null) throw new Exception("Cannot open document.");

                TextSpan span = e.Item.ReplaceSpan; // get text span of the result item
                int hr = view.SetSelection(span.iStartLine, span.iStartIndex, span.iEndLine, span.iEndIndex);
                Marshal.ThrowExceptionForHR(hr);

                hr = view.EnsureSpanVisible(span); // scroll down to ensure selection visible
                Marshal.ThrowExceptionForHR(hr);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.Components.MessageBox.ShowException(ex);
            }
        }

        /// <summary>
        /// Returns control handling client portion of the window
        /// </summary>
        public override IWin32Window Window {
            get { return panel; }
        }
    }
}
