using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GE2D3D.MapEditor.BMFont
{
    [XmlRoot("font")]
    public class XmlFontFile : IDisposable
    {
        // These are set by the XML serializer, so we use the null-forgiving operator.
        [XmlElement("info")]
        public XmlFontInfo Info { get; set; } = null!;

        [XmlElement("common")]
        public XmlFontCommon Common { get; set; } = null!;

        // These collections are created up front to avoid null checks.
        [XmlArray("pages")]
        [XmlArrayItem("page")]
        public List<XmlFontPage> Pages { get; set; } = new();

        [XmlArray("chars")]
        [XmlArrayItem("char")]
        public List<XmlFontChar> Chars { get; set; } = new();

        [XmlArray("kernings")]
        [XmlArrayItem("kerning")]
        public List<XmlFontKerning> Kernings { get; set; } = new();

        public void Dispose()
        {
            Pages.Clear();
            Chars.Clear();
            Kernings.Clear();
        }
    }
}