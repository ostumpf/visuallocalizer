using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using System.Drawing;
using System.Diagnostics;

namespace OndrejStumpf.VLTestingPackage {

    [Guid("CB8F5AD8-F629-44f4-B4C3-DC68C448DE1D")]
    class MarkerService : IVsTextMarkerTypeProvider {

        public FormatMarkerType FormatMarker;
        private static MarkerService instance;

        public MarkerService() {
            FormatMarker = new FormatMarkerType();
            instance = this;
        }

        public static MarkerService Instance {
            get {
                return instance;
            }
        }

        public int GetTextMarkerType(ref Guid pguidMarker, out IVsPackageDefinedTextMarkerType ppMarkerType) {
            ppMarkerType = null;
            if (pguidMarker.ToString("D") == GuidList.guidVLTestingPackageMarker.ToLower()) {
                Trace.WriteLine("get text marker type");
                ppMarkerType = FormatMarker;
                return VSConstants.S_OK;    
            }
            return VSConstants.E_FAIL;
        }

        
    }

    [Guid("77A57DBA-C461-423c-B54A-D3AB564C411C")]
    class FormatMarkerType : IVsPackageDefinedTextMarkerType,IVsTextMarkerClient {

        public int Id {
            get;
            set;
        }

         public int DrawGlyphWithColors(IntPtr hdc, RECT[] pRect, int iMarkerType, IVsTextMarkerColorSet pMarkerColors, uint dwGlyphDrawFlags, int iLineHeight) {
            Graphics g = Graphics.FromHdc(hdc);
            RECT r = pRect[0];
             
            g.DrawLine(new Pen(Color.Blue,5), r.left, r.bottom, r.right, r.bottom);
            return 0;
        }

        public int GetDefaultFontFlags(out uint pdwFontFlags) {
            pdwFontFlags = 0;
            return 0;
        }

        public int GetPriorityIndex(out int piPriorityIndex) {
            piPriorityIndex = 10000;
            return 0;
        }

        public int GetVisualStyle(out uint pdwVisualFlags) {
            pdwVisualFlags = (uint)MARKERVISUAL.MV_LINE | (uint)MARKERVISUAL.MV_TIP_FOR_BODY;

            return 0;
        }

        public int GetDefaultLineStyle(COLORINDEX[] piLineColor, LINESTYLE[] piLineIndex) {
            piLineColor[0] = COLORINDEX.CI_DARKBLUE;
            piLineIndex[0] = LINESTYLE.LI_SQUIGGLY;
            return 0;
        }

        public int GetBehaviorFlags(out uint pdwFlags) {
            // snap to current line
            pdwFlags = (uint)MARKERBEHAVIORFLAGS.MB_LEFTEDGE_LEFTTRACK
                | (uint)MARKERBEHAVIORFLAGS.MB_RIGHTEDGE_RIGHTTRACK;
            return 0;
        }

        public int GetDefaultColors(COLORINDEX[] piForeground, COLORINDEX[] piBackground) {
            piForeground[0] = COLORINDEX.CI_BLACK;
            piBackground[0] = COLORINDEX.CI_YELLOW;
            return 0;
        }

        public int ExecMarkerCommand(IVsTextMarker pMarker, int iItem) {
            return 0;
        }

        public int GetMarkerCommandInfo(IVsTextMarker pMarker, int iItem, string[] pbstrText, uint[] pcmdf) {
            return 0;
        }

        public int GetTipText(IVsTextMarker pMarker, string[] pbstrText) {
            
            return 0;
        }

        public void MarkerInvalidated() {
            
        }

        public int OnAfterMarkerChange(IVsTextMarker pMarker) {
            return 0;
        }

        public void OnAfterSpanReload() {
            
        }

        public void OnBeforeBufferClose() {
            
        }

        public void OnBufferSave(string pszFileName) {
            
        }
    }
}
