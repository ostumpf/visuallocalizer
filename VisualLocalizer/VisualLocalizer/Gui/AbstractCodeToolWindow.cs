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

namespace VisualLocalizer.Gui {

    internal interface IHighlightRequestSource {
        event EventHandler<CodeResultItemEventArgs> HighlightRequired;
    }

    internal sealed class CodeResultItemEventArgs : EventArgs {
        public AbstractResultItem Item { get; set; }
    }

    internal sealed class AbstractCodeToolWindowEvents : IVsWindowFrameNotify3 {

        public event EventHandler WindowHidden;
        private int prevfShow = -1;

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
            /*string current="?";
            if (Enum.IsDefined(typeof(__FRAMESHOW), fShow)) current = ((__FRAMESHOW)fShow).ToString();
            if (Enum.IsDefined(typeof(__FRAMESHOW2), fShow)) current = ((__FRAMESHOW2)fShow).ToString();
            if (Enum.IsDefined(typeof(__FRAMESHOW3), fShow)) current = ((__FRAMESHOW3)fShow).ToString();

            string prev = "?";
            if (Enum.IsDefined(typeof(__FRAMESHOW), prevfShow)) prev = ((__FRAMESHOW)prevfShow).ToString();
            if (Enum.IsDefined(typeof(__FRAMESHOW2), prevfShow)) prev = ((__FRAMESHOW2)prevfShow).ToString();
            if (Enum.IsDefined(typeof(__FRAMESHOW3), prevfShow)) prev = ((__FRAMESHOW3)prevfShow).ToString(); 

            VisualLocalizer.Library.MessageBox.Show(VisualLocalizerPackage.VisualStudioVersion.ToString() +
               " " + prev +
               " -> " + current);
            */
         
            if (fShow == (int)__FRAMESHOW2.FRAMESHOW_BeforeWinHidden) {
                if (WindowHidden != null) WindowHidden(this, null);
            }            

            prevfShow = fShow;
            return VSConstants.S_OK;
        }

    }

    internal abstract class AbstractCodeToolWindow<T> : ToolWindowPane where T : Control,IHighlightRequestSource, new() {

        protected T panel;
        protected AbstractCodeToolWindowEvents windowEvents;

        public AbstractCodeToolWindow():base(null) {
            this.panel = new T();
            this.panel.HighlightRequired+=new EventHandler<CodeResultItemEventArgs>(panel_HighlightRequired);

            windowEvents = new AbstractCodeToolWindowEvents();
            windowEvents.WindowHidden += new EventHandler(OnWindowHidden);

            AddEventsListener();
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

        public void AddEventsListener() {            
            IVsWindowFrame frame = (IVsWindowFrame)Frame;
            if (frame != null) 
                frame.SetProperty((int)__VSFPROPID.VSFPROPID_ViewHelper, windowEvents);
        }

        protected virtual void OnWindowHidden(object sender, EventArgs e) {            
        }

        protected void panel_HighlightRequired(object sender, CodeResultItemEventArgs e) {
            try {
                IVsTextView view = DocumentViewsManager.GetTextViewForFile(e.Item.SourceItem.GetFullPath(), true, true);
                if (view == null) throw new Exception("Cannot open document.");

                TextSpan span = e.Item.ReplaceSpan;
                int hr = view.SetSelection(span.iStartLine, span.iStartIndex, span.iEndLine, span.iEndIndex);
                Marshal.ThrowExceptionForHR(hr);

                hr = view.EnsureSpanVisible(span);
                Marshal.ThrowExceptionForHR(hr);
            } catch (Exception ex) {
                VLOutputWindow.VisualLocalizerPane.WriteException(ex);
                VisualLocalizer.Library.MessageBox.ShowException(ex);
            }
        }

        public override IWin32Window Window {
            get { return panel; }
        }
    }
}
