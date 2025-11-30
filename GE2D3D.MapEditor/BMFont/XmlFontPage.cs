using System.Xml.Serialization;

namespace GE2D3D.MapEditor.BMFont
{
    /// <summary>
    /// XML mapping for the &lt;page&gt; node in a BMFont .fnt file.
    /// </summary>
    public class XmlFontPage
    {
        /// <summary>
        /// Page index in the BMFont file (&lt;page id="0" ... /&gt;).
        /// </summary>
        [XmlAttribute("id")]
        public int ID { get; set; }

        /// <summary>
        /// Texture filename for this page (&lt;page file="myfont_0.png" /&gt;).
        /// </summary>
        [XmlAttribute("file")]
        public string File { get; set; } = string.Empty;
    }
}