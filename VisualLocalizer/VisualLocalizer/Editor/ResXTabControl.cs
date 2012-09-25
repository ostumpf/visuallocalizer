using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Windows.Forms;

namespace VisualLocalizer.Editor {
    internal sealed class ResXTabControl : TabControl {

        private int MouseOverTabIndex = -1;

        public ResXTabControl() {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);

            int oldIndex = MouseOverTabIndex;
            MouseOverTabIndex = -1;
            
            for (int i = 0; i < TabCount; i++) {
                Rectangle tabRect=GetTabRect(i);
                if (tabRect.Contains(e.Location)) {
                    MouseOverTabIndex = i;
                    if (MouseOverTabIndex != oldIndex) {
                        Invalidate(tabRect);                        
                    }                    
                }
            }
            if (oldIndex != -1) {
                Invalidate(GetTabRect(oldIndex));
            }
        }

        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);

            if (MouseOverTabIndex != -1) {
                Invalidate(GetTabRect(MouseOverTabIndex));
            }
            MouseOverTabIndex = -1;
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float borderWidth = 1.8f;
            Color borderColor = Color.FromArgb(166, 202, 255);
            Color terminalGradientColor = Color.FromArgb(160, 160, 160);
            float cornerRadius = 7f;
            GraphicsPath selectedTabPath = null;

            for (int i = 0; i < this.TabCount; i++) {
                string text = this.TabPages[i].Text;
                Rectangle itemRect = this.GetTabRect(i);
                float startX = borderWidth + itemRect.Width;

                GraphicsPath path = new GraphicsPath();
                path.AddLine(itemRect.X + cornerRadius, itemRect.Y, itemRect.X + itemRect.Width, itemRect.Y);
                path.AddLine(itemRect.X + itemRect.Width, itemRect.Y, itemRect.X + itemRect.Width, itemRect.Y + itemRect.Height);
                path.AddLine(itemRect.X + itemRect.Width, itemRect.Y + itemRect.Height, itemRect.X + cornerRadius, itemRect.Y + itemRect.Height);
                path.AddArc(itemRect.X, itemRect.Y + itemRect.Height - 2 * cornerRadius, cornerRadius * 2, cornerRadius * 2, 90, 90);
                path.AddLine(itemRect.X, itemRect.Y + itemRect.Height - cornerRadius, itemRect.X, itemRect.Y + cornerRadius);
                path.AddArc(itemRect.X, itemRect.Y, cornerRadius * 2, cornerRadius * 2, 180, 90);

                LinearGradientBrush lgb = new LinearGradientBrush(itemRect, Color.White, terminalGradientColor, LinearGradientMode.Horizontal);
                g.FillPath(lgb, path);
                if (i != SelectedIndex) {
                    g.DrawPath(new Pen(Color.FromArgb(187, 187, 187), borderWidth), path);
                } else {
                    selectedTabPath = path;
                }

                SizeF textSize = g.MeasureString(text, this.Font);
                Brush fontBrush = i == MouseOverTabIndex ? Brushes.Orange : Brushes.Black;
                g.DrawString(text, this.Font, fontBrush, itemRect.X + (itemRect.Width - textSize.Width) / 2, itemRect.Y + (itemRect.Height - textSize.Height) / 2);
            }

            int itemWidth = ItemSize.Height;
            Pen borderPen = new Pen(borderColor, borderWidth);
            Rectangle selectedRect=GetTabRect(SelectedIndex);

            g.DrawLine(borderPen, itemWidth + borderWidth, borderWidth, Width - borderWidth, borderWidth);
            g.DrawLine(borderPen, Width - borderWidth, borderWidth, Width - borderWidth, Height - borderWidth);
            g.DrawLine(borderPen, Width - borderWidth, Height - borderWidth, itemWidth + borderWidth, Height - borderWidth);

            g.DrawLine(borderPen, itemWidth + 2 * borderWidth, Height - borderWidth, itemWidth + 2 * borderWidth, selectedRect.Y + selectedRect.Height);
            g.DrawLine(borderPen, itemWidth + 2 * borderWidth, selectedRect.Y, itemWidth + 2 * borderWidth, borderWidth);
            
            g.DrawPath(new Pen(borderColor, borderWidth), selectedTabPath);

            g.SmoothingMode = SmoothingMode.Default;
            g.DrawLine(new Pen(terminalGradientColor, borderWidth * 1.5f), itemWidth + borderWidth, selectedRect.Y + borderWidth, itemWidth + borderWidth, selectedRect.Y + selectedRect.Height - borderWidth);            
        }

    }
}
