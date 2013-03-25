using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Windows.Forms;

namespace VisualLocalizer.Editor {

    /// <summary>
    /// Represents tab control used in ResX editor. Provides custom painting for tab items and handles mouseover events and effects.
    /// </summary>
    internal sealed class ResXTabControl : TabControl {

        /// <summary>
        /// On which tab the mouse currently hovers
        /// </summary>
        private int MouseOverTabIndex = -1;

        private float BorderWidth { get; set; }
        private Color BorderColor { get; set; }
        private Color TerminalTabGradientColor { get; set; }
        private Color StartTabGradientColor { get; set; }
        private Brush TabHoveredTextColor { get; set; }
        private Brush TabTextColor { get; set; }
        private float CornerRadius { get; set; }

        public ResXTabControl() {
            // to prevent flickering
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            BorderWidth = 1.8f;
            BorderColor = Color.FromArgb(166, 202, 255);
            TerminalTabGradientColor = Color.FromArgb(160, 160, 160);
            StartTabGradientColor = Color.White;
            TabHoveredTextColor = Brushes.Orange;
            TabTextColor = Brushes.Black;
            CornerRadius = 7f;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.MouseMove" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);

            int oldIndex = MouseOverTabIndex; 
            MouseOverTabIndex = -1;
            
            // find out the previous hovered tab and cancel its effects
            for (int i = 0; i < TabCount; i++) {
                Rectangle tabRect=GetTabRect(i);
                if (tabRect.Contains(e.Location)) {
                    MouseOverTabIndex = i;
                    if (MouseOverTabIndex != oldIndex) {
                        Invalidate(tabRect);                        
                    }                    
                }
            }

            // if mouse is over some tab, create the hovering effects
            if (oldIndex != -1) {
                Invalidate(GetTabRect(oldIndex));
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.MouseLeave" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);

            // remove the hovering effects from left tab
            if (MouseOverTabIndex != -1) {
                Invalidate(GetTabRect(MouseOverTabIndex));
            }
            MouseOverTabIndex = -1;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.Paint" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs" /> that contains the event data.</param>
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;            

            GraphicsPath selectedTabPath = null;

            // paint the tabs
            for (int i = 0; i < this.TabCount; i++) {
                string text = this.TabPages[i].Text;
                Rectangle itemRect = this.GetTabRect(i); 
                float startX = BorderWidth + itemRect.Width;

                // prepare graphics path for the tab
                GraphicsPath path = new GraphicsPath();
                path.AddLine(itemRect.X + CornerRadius, itemRect.Y, itemRect.X + itemRect.Width, itemRect.Y);
                path.AddLine(itemRect.X + itemRect.Width, itemRect.Y, itemRect.X + itemRect.Width, itemRect.Y + itemRect.Height);
                path.AddLine(itemRect.X + itemRect.Width, itemRect.Y + itemRect.Height, itemRect.X + CornerRadius, itemRect.Y + itemRect.Height);
                path.AddArc(itemRect.X, itemRect.Y + itemRect.Height - 2 * CornerRadius, CornerRadius * 2, CornerRadius * 2, 90, 90);
                path.AddLine(itemRect.X, itemRect.Y + itemRect.Height - CornerRadius, itemRect.X, itemRect.Y + CornerRadius);
                path.AddArc(itemRect.X, itemRect.Y, CornerRadius * 2, CornerRadius * 2, 180, 90);

                // paint tab's background
                LinearGradientBrush lgb = new LinearGradientBrush(itemRect, StartTabGradientColor, TerminalTabGradientColor, LinearGradientMode.Horizontal);
                g.FillPath(lgb, path);
                if (i != SelectedIndex) { // not painting selected tab
                    g.DrawPath(new Pen(Color.FromArgb(187, 187, 187), BorderWidth), path);
                } else { // painting selected tab
                    selectedTabPath = path;
                }

                SizeF textSize = g.MeasureString(text, this.Font);
                Brush fontBrush = i == MouseOverTabIndex ? TabHoveredTextColor : TabTextColor;

                // paint text of the tab
                g.DrawString(text, this.Font, fontBrush, itemRect.X + (itemRect.Width - textSize.Width) / 2, itemRect.Y + (itemRect.Height - textSize.Height) / 2);
            }

            int itemWidth = ItemSize.Height;
            Pen borderPen = new Pen(BorderColor, BorderWidth);
            Rectangle selectedRect=GetTabRect(SelectedIndex);

            // paint highlighting borders around selected tab
            g.DrawLine(borderPen, itemWidth + BorderWidth, BorderWidth, Width - BorderWidth, BorderWidth);
            g.DrawLine(borderPen, Width - BorderWidth, BorderWidth, Width - BorderWidth, Height - BorderWidth);
            g.DrawLine(borderPen, Width - BorderWidth, Height - BorderWidth, itemWidth + BorderWidth, Height - BorderWidth);

            g.DrawLine(borderPen, itemWidth + 2 * BorderWidth, Height - BorderWidth, itemWidth + 2 * BorderWidth, selectedRect.Y + selectedRect.Height);
            g.DrawLine(borderPen, itemWidth + 2 * BorderWidth, selectedRect.Y, itemWidth + 2 * BorderWidth, BorderWidth);
            
            g.DrawPath(new Pen(BorderColor, BorderWidth), selectedTabPath);

            g.SmoothingMode = SmoothingMode.Default;
            g.DrawLine(new Pen(TerminalTabGradientColor, BorderWidth * 1.5f), itemWidth + BorderWidth, selectedRect.Y + BorderWidth, itemWidth + BorderWidth, selectedRect.Y + selectedRect.Height - BorderWidth);            
        }

    }
}
