using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace VisualLocalizer.Editor {

    /// <summary>
    /// Contains Visual Studio version-specific color themes, used to paint ResX editor's toolstrip's background. Contains 
    /// three colors, creating a vertical gradient.
    /// </summary>
    internal sealed class VsColorTable : ProfessionalColorTable {

        private Color beginColor, middleColor, endColor;

        public VsColorTable() {
            // initialize the colors based on current VS version
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
                case VS_VERSION.VS2012:
                    beginColor = ColorTranslator.FromHtml("#D0D2D3");
                    middleColor = ColorTranslator.FromHtml("#D0D2D3");
                    endColor = ColorTranslator.FromHtml("#D0D2D3");
                    break;
                case VS_VERSION.VS2013:
                    beginColor = ColorTranslator.FromHtml("#CFD6E5");
                    middleColor = ColorTranslator.FromHtml("#CFD6E5");
                    endColor = ColorTranslator.FromHtml("#CFD6E5");
                    break;
                case VS_VERSION.UNKNOWN:
                    beginColor = ToolStripGradientBegin;
                    middleColor = ToolStripGradientMiddle;
                    endColor = ToolStripGradientEnd;
                    break;

            }
        }

        /// <summary>
        /// Upper gradient color
        /// </summary>
        public override Color ToolStripGradientBegin { get { return beginColor; } }

        /// <summary>
        /// Middle gradient color
        /// </summary>
        public override Color ToolStripGradientMiddle { get { return middleColor; } }

        /// <summary>
        /// Bottom gradient color
        /// </summary>
        public override Color ToolStripGradientEnd { get { return endColor; } }
    }
}
