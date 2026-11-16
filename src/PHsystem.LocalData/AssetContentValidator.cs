using System.Buffers.Binary;
using System.Xml;
using System.Xml.Linq;

namespace PHsystem.LocalData;

internal static class AssetContentValidator
{
    public static bool IsValid(string path, byte[] bytes)
    {
        var span = bytes.AsSpan();
        switch (Path.GetExtension(path).ToLowerInvariant())
        {
            case ".png":
                if (bytes.Length < 33 || !span[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
                    || !span.Slice(12, 4).SequenceEqual("IHDR"u8)) return false;
                var width = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(16, 4));
                var height = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(20, 4));
                return width is > 0 and <= 4096 && height is > 0 and <= 4096;
            case ".webp":
                return bytes.Length >= 30 && span[..4].SequenceEqual("RIFF"u8) && span.Slice(8, 4).SequenceEqual("WEBP"u8)
                    && BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(4, 4)) == bytes.Length - 8
                    && (span.Slice(12, 4).SequenceEqual("VP8 "u8) || span.Slice(12, 4).SequenceEqual("VP8L"u8) || span.Slice(12, 4).SequenceEqual("VP8X"u8));
            case ".wav":
                return bytes.Length >= 44 && span[..4].SequenceEqual("RIFF"u8) && span.Slice(8, 4).SequenceEqual("WAVE"u8)
                    && BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(4, 4)) == bytes.Length - 8;
            case ".svg": return ValidSvg(bytes);
            default: return false;
        }
    }

    private static bool ValidSvg(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null, MaxCharactersInDocument = 4 * 1024 * 1024 });
            var doc = XDocument.Load(reader);
            if (doc.Root?.Name != XName.Get("svg", "http://www.w3.org/2000/svg") || doc.DescendantNodes().Count() > 4096) return false;
            string[] elements = ["svg", "g", "path", "rect", "circle", "ellipse", "line", "polyline", "polygon", "title", "desc"];
            string[] attributes = ["viewBox", "width", "height", "x", "y", "x1", "y1", "x2", "y2", "cx", "cy", "r", "rx", "ry", "d", "points", "fill", "stroke", "stroke-width", "stroke-linecap", "stroke-linejoin", "fill-rule", "opacity", "fill-opacity", "stroke-opacity", "transform"];
            return doc.Descendants().All(element => element.Name.NamespaceName == "http://www.w3.org/2000/svg"
                && elements.Contains(element.Name.LocalName, StringComparer.Ordinal)
                && element.Attributes().All(attribute => attribute.IsNamespaceDeclaration
                    ? attribute.Name.LocalName == "xmlns" && attribute.Value == "http://www.w3.org/2000/svg"
                    : attribute.Name.NamespaceName.Length == 0 && attributes.Contains(attribute.Name.LocalName, StringComparer.Ordinal)
                        && attribute.Value.Length <= 8192 && !attribute.Value.Contains("url(", StringComparison.OrdinalIgnoreCase)))
                && !doc.DescendantNodes().Any(node => node is XProcessingInstruction or XDocumentType);
        }
        catch (XmlException) { return false; }
    }
}
