using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;

namespace VisualLocalizer.Library.Gui {

    /// <summary>
    /// Represents a context menu item with an image on the left side
    /// </summary>
    public class ImageMenuItem : MenuItem {

        /// <summary>
        /// Constructs new menu item with given text
        /// </summary>        
        public ImageMenuItem(string text) : base(text) {
            this.OwnerDraw = true;
            this.Font = SystemInformation.MenuFont;
            this.Margin = new Padding(5, 0, 20, 0);
            this.ImageMargin = new Padding(1, 2, 1, 2);
            this.DrawItem += new DrawItemEventHandler(ImageMenuItem_DrawItem);
            this.MeasureItem+=new MeasureItemEventHandler(ImageMenuItem_MeasureItem);            
        }

        /// <summary>
        /// Draws the menu item
        /// </summary>        
        private void ImageMenuItem_DrawItem(object sender, DrawItemEventArgs e) {
            MenuItem mi = (MenuItem)sender;
            SolidBrush menuBrush = null;
          
            if (mi.Enabled == false) {
                menuBrush = new SolidBrush(SystemColors.GrayText);
            } else {
                if ((e.State & DrawItemState.Selected) != 0) {
                    // Text color when selected (highlighted)
                    menuBrush = new SolidBrush(SystemColors.HighlightText);
                } else {
                    // Text color during normal drawing
                    menuBrush = new SolidBrush(SystemColors.MenuText);
                }
            }
            
            if ((e.State & DrawItemState.Selected) != 0) {                
                e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);
            } else {
                e.Graphics.FillRectangle(SystemBrushes.Menu, e.Bounds);
            }

            // Rectanble for text portion
            Rectangle rectText = e.Bounds;

            // Rectangle for image portion
            Rectangle rectImage = e.Bounds;

            // Set image rectangle same dimensions as image
            if (Image == null) {
                rectImage.Width = 18;
                rectImage.Height = 18;
            } else {
                rectImage.Width = Image.Width;
                rectImage.Height = Image.Height;                
            }
            rectImage.X += ImageMargin.Left;
            rectImage.Y += ImageMargin.Top;

            // set width to x value of text portion
            rectText.X = rectImage.Right + ImageMargin.Right + Margin.Left;
            rectText.Y = e.Bounds.Y + Margin.Top + ((e.Bounds.Height - Font.Height) / 2);
            
            if (mi.Enabled && Image!=null) {
                e.Graphics.DrawImage(Image, rectImage);
            } else if (!mi.Enabled && DisabledImage != null) {
                e.Graphics.DrawImage(DisabledImage, rectImage);
            }
            
            e.Graphics.DrawString(mi.Text, Font, menuBrush, rectText.X, rectText.Y);

            if (ShowShortcut && Shortcut != Shortcut.None) {
                string shortcutText = GetTextFor(mi.Shortcut);
                SizeF shortcutSize = e.Graphics.MeasureString(shortcutText, Font);
                e.Graphics.DrawString(shortcutText, Font, menuBrush, e.Bounds.Right - shortcutSize.Width - Margin.Right, rectText.Y + Margin.Top);
            }
        }

        /// <summary>
        /// Returns dimensions of this menu item
        /// </summary>        
        private void ImageMenuItem_MeasureItem(object sender, MeasureItemEventArgs e) {
            MenuItem mi = (MenuItem)sender;

            SizeF sizef = e.Graphics.MeasureString(mi.Text + GetTextFor(mi.Shortcut), Font);

            if (Image == null) {
                e.ItemWidth = (int)(Math.Ceiling(sizef.Width)) + Margin.Left + Margin.Right + 10;
                e.ItemHeight = (int)Math.Ceiling(sizef.Height) + Margin.Top + Margin.Bottom;
            } else {
                e.ItemWidth = (int)(Math.Ceiling(sizef.Width)) + Image.Width + 10 + ImageMargin.Right + ImageMargin.Left + Margin.Left + Margin.Right;
                e.ItemHeight = (int)Math.Max(Math.Ceiling(sizef.Height), Image.Height) + Math.Max(ImageMargin.Top + ImageMargin.Bottom, Margin.Top + Margin.Bottom);
            }
        }

        /// <summary>
        /// Returns human-readeble form of the shortcut
        /// </summary>  
        private string GetTextFor(Shortcut shortcut) {
            StringBuilder b = new StringBuilder(System.Enum.GetName(typeof(Shortcut), shortcut));
            for (int i = 0; i < b.Length; i++) {
                char prev = i > 0 ? b[i - 1] : '?';
                
                if (i>0 && (char.IsUpper(b[i]) || (char.IsDigit(b[i]) && !char.IsDigit(prev) && prev!='F'))) {
                    b.Insert(i, "+");
                    i += 1;
                }
            }

            return b.ToString();
        }

        private Image _Image;

        /// <summary>
        /// Image to be displayed in the menu item
        /// </summary>
        public Image Image {
            get { return _Image; }
            set {
                _Image = value;
                if (value == null) {
                    DisabledImage = null;
                } else {                    
                    Bitmap bmp = new Bitmap(value);
                    for (int i = 0; i < bmp.Width; i++) {
                        for (int x = 0; x < bmp.Height; x++) {
                            Color oc = bmp.GetPixel(i, x);
                            int grayScale = (int)((oc.R * 0.3) + (oc.G * 0.59) + (oc.B * 0.11));
                            Color nc = Color.FromArgb(oc.A, grayScale, grayScale, grayScale);
                            bmp.SetPixel(i, x, nc);
                        }
                    }
                    DisabledImage = bmp;
                }
            }
        }

        /// <summary>
        /// The greyscale version of the image
        /// </summary>
        private Image DisabledImage {
            get;
            set;
        }

        /// <summary>
        /// Font for menu text
        /// </summary>
        public Font Font {
            get;
            set;
        }

        /// <summary>
        /// Margin of the image
        /// </summary>
        public Padding ImageMargin {
            get;
            set;
        }

        /// <summary>
        /// Margin of the text
        /// </summary>
        public Padding Margin {
            get;
            set;
        }
    }
}
