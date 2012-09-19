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

    internal sealed class AbstractCodeToolWindowEvents : IVsWindowFrameNotify {

        public event EventHandler WindowHidden;
        private int prevfShow=-1;

        public int OnDockableChange(int fDockable) {
            return VSConstants.S_OK;
        }

        public int OnMove() {
            return VSConstants.S_OK;
        }

        public int OnShow(int fShow) {
            if (fShow == (int)__FRAMESHOW.FRAMESHOW_Hidden && prevfShow!=(int)__FRAMESHOW.FRAMESHOW_TabDeactivated) {
                if (WindowHidden != null) WindowHidden(this, null);
            }
            prevfShow = fShow;
            return VSConstants.S_OK;
        }

        public int OnSize() {
            throw new NotImplementedException();
        }
    }

    internal abstract class AbstractCodeToolWindow<T> : ToolWindowPane where T : DataGridView,IHighlightRequestSource, new() {

        protected T panel;
        
        public AbstractCodeToolWindow():base(null) {
            this.panel = new T();
            this.panel.HighlightRequired+=new EventHandler<CodeResultItemEventArgs>(panel_HighlightRequired);            
        }

        public override void OnToolWindowCreated() {
            base.OnToolWindowCreated();

            IVsWindowFrame frame = (IVsWindowFrame)Frame;
            var events = new AbstractCodeToolWindowEvents();
            events.WindowHidden += new EventHandler(OnWindowHidden);
            frame.SetProperty((int)__VSFPROPID.VSFPROPID_ViewHelper, events); 
        }        

        protected virtual void OnWindowHidden(object sender, EventArgs e) {            
        }

        protected void panel_HighlightRequired(object sender, CodeResultItemEventArgs e) {
            try {
                IVsTextView view = DocumentViewsManager.GetTextViewForFile(e.Item.SourceItem.Properties.Item("FullPath").Value.ToString(), true, true);
                if (view == null) throw new Exception("Cannot open document.");

                TextSpan span = e.Item.ReplaceSpan;
                int hr = view.SetSelection(span.iStartLine, span.iStartIndex, span.iEndLine, span.iEndIndex);
                Marshal.ThrowExceptionForHR(hr);

                hr = view.EnsureSpanVisible(span);
                Marshal.ThrowExceptionForHR(hr);
            } catch (Exception ex) {
                string text = string.Format("{0} while processing command: {1}", ex.GetType().Name, ex.Message);

                VLOutputWindow.VisualLocalizerPane.WriteLine(text);
                VisualLocalizer.Library.MessageBox.ShowError(text);
            }
        }

        public override IWin32Window Window {
            get { return panel; }
        }
    }
}
