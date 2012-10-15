using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace VisualLocalizer.Editor {
    internal class VsColorTable : ProfessionalColorTable {

        private Color beginColor, middleColor, endColor;

        public VsColorTable() {
            switch (VisualLocalizerPackage.VisualStudioVersion) {
                case VS_VERSION.VS2008:
                    beginColor = ColorTranslator.FromHtml("#FAFAFD");
                    middleColor = ColorTranslator.FromHtml("#E9ECFA");
                    endColor = ColorTranslator.FromHtml("#C1C8D9");
                    break;
                case VS_VERSION.VS2010:
                    beginColor = ColorTranslator.FromHtml("#BCC7D8");
                    middleColor = ColorTranslator.FromHtml("#BCC7D8");
                    endColor = ColorTranslator.FromHtml("#BCC7D8");
                    break;
                case VS_VERSION.VS2011:
                    beginColor = ColorTranslator.FromHtml("#D0D2D3");
                    middleColor = ColorTranslator.FromHtml("#D0D2D3");
                    endColor = ColorTranslator.FromHtml("#D0D2D3");
                    break;
                case VS_VERSION.UNKNOWN:
                    beginColor = ToolStripGradientBegin;
                    middleColor = ToolStripGradientMiddle;
                    endColor = ToolStripGradientEnd;
                    break;

            }
        }

        public override Color ToolStripGradientBegin { get { return beginColor; } }
        public override Color ToolStripGradientMiddle { get { return middleColor; } }
        public override Color ToolStripGradientEnd { get { return endColor; } }
    }
}
