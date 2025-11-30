using System.Drawing;
using System.Xml.Serialization;

namespace GE2D3D.MapEditor.BMFont
{
    /// <summary>
    /// XML mapping for the <info> node in a BMFont .fnt file.
    /// This version uses System.Drawing.Point / Rectangle so it
    /// does not depend on XNA or MonoGame types.
    /// </summary>
    [XmlRoot("info")]
    public class XmlFontInfo
    {
        [XmlAttribute("face")]
        public string Face { get; set; } = string.Empty;

        [XmlAttribute("size")]
        public int Size { get; set; }

        // --- bold as int in XML, bool in code ------------------------

        [XmlAttribute("bold")]
        public int BoldInt { get; set; }

        [XmlIgnore]
        public bool Bold
        {
            get => BoldInt != 0;
            set => BoldInt = value ? 1 : 0;
        }

        // --- italic ---------------------------------------------------

        [XmlAttribute("italic")]
        public int ItalicInt { get; set; }

        [XmlIgnore]
        public bool Italic
        {
            get => ItalicInt != 0;
            set => ItalicInt = value ? 1 : 0;
        }

        [XmlAttribute("charset")]
        public string Charset { get; set; } = string.Empty;

        // --- unicode flag --------------------------------------------

        [XmlAttribute("unicode")]
        public int UnicodeInt { get; set; }

        [XmlIgnore]
        public bool Unicode
        {
            get => UnicodeInt != 0;
            set => UnicodeInt = value ? 1 : 0;
        }

        [XmlAttribute("stretchH")]
        public int StretchHeight { get; set; }

        // --- smooth flag ---------------------------------------------

        [XmlAttribute("smooth")]
        public int SmoothInt { get; set; }

        [XmlIgnore]
        public bool Smooth
        {
            get => SmoothInt != 0;
            set => SmoothInt = value ? 1 : 0;
        }

        /// <summary>
        /// Supersampling level (aa attribute in BMFont).
        /// </summary>
        [XmlAttribute("aa")]
        public int SuperSampling { get; set; }

        // --- padding: "left,top,right,bottom" ------------------------

        [XmlAttribute("padding")]
        public string PaddingString
        {
            get => $"{Padding.Left},{Padding.Top},{Padding.Right},{Padding.Bottom}";
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    Padding = Rectangle.Empty;
                    return;
                }

                var parts = value.Split(',');
                if (parts.Length == 4 &&
                    int.TryParse(parts[0], out var l) &&
                    int.TryParse(parts[1], out var t) &&
                    int.TryParse(parts[2], out var r) &&
                    int.TryParse(parts[3], out var b))
                {
                    // We store l/t in X/Y, r/b in Width/Height to keep all 4 ints
                    Padding = new Rectangle(l, t, r, b);
                }
                else
                {
                    Padding = Rectangle.Empty;
                }
            }
        }

        [XmlIgnore]
        public Rectangle Padding { get; set; }

        // --- spacing: "x,y" ------------------------------------------

        [XmlAttribute("spacing")]
        public string SpacingString
        {
            get => $"{Spacing.X},{Spacing.Y}";
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    Spacing = Point.Empty;
                    return;
                }

                var parts = value.Split(',');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out var x) &&
                    int.TryParse(parts[1], out var y))
                {
                    Spacing = new Point(x, y);
                }
                else
                {
                    Spacing = Point.Empty;
                }
            }
        }

        [XmlIgnore]
        public Point Spacing { get; set; }

        // Optional extra attributes ? include if your .fnt uses them:
        [XmlAttribute("outline")]
        public int Outline { get; set; }
    }
}