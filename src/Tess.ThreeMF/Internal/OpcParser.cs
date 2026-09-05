using System.Xml;

namespace Shiron.Lib.Tess.ThreeMF.Internal;

internal static class OpcParser {
    static readonly XmlReaderSettings Settings = new() {
        DtdProcessing = DtdProcessing.Ignore,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreWhitespace = true,
        CloseInput = true,
    };

    public static IReadOnlyList<ThreeMFContentType> ParseContentTypes(ReadOnlyMemory<byte> xml) {
        try {
            using var reader = CreateReader(xml);

            MoveToRoot(reader, "Types", ThreeMFSerializer.ContentTypesPath);

            var contentTypes = new List<ThreeMFContentType>();

            while (reader.Read()) {
                if (reader.NodeType is not XmlNodeType.Element || reader.Depth != 1)
                    continue;

                switch (reader.LocalName) {
                    case "Default":
                        contentTypes.Add(new ThreeMFContentType {
                            Extension = RequireAttribute(reader, "Extension", "Default"),
                            ContentType = RequireAttribute(reader, "ContentType", "Default"),
                        });
                        break;
                    case "Override":
                        contentTypes.Add(new ThreeMFContentType {
                            PartName = RequireAttribute(reader, "PartName", "Override"),
                            ContentType = RequireAttribute(reader, "ContentType", "Override"),
                        });
                        break;
                }
            }

            return contentTypes;
        } catch (XmlException e) {
            throw new ThreeMFPackageException(
                $"'{ThreeMFSerializer.ContentTypesPath}' is not well-formed XML.", e
            );
        }
    }

    public static IReadOnlyList<ThreeMFRelationship> ParseRelationships(ReadOnlyMemory<byte> xml) {
        try {
            using var reader = CreateReader(xml);

            MoveToRoot(reader, "Relationships", ThreeMFSerializer.RelationshipsPath);

            var relationships = new List<ThreeMFRelationship>();

            while (reader.Read()) {
                if (reader.NodeType is not XmlNodeType.Element || reader.Depth != 1)
                    continue;

                if (reader.LocalName != "Relationship")
                    continue;

                relationships.Add(new ThreeMFRelationship {
                    Id = RequireAttribute(reader, "Id", "Relationship"),
                    Type = RequireAttribute(reader, "Type", "Relationship"),
                    Target = RequireAttribute(reader, "Target", "Relationship"),
                });
            }

            return relationships;
        } catch (XmlException e) {
            throw new ThreeMFPackageException(
                $"'{ThreeMFSerializer.RelationshipsPath}' is not well-formed XML.", e
            );
        }
    }

    static XmlReader CreateReader(ReadOnlyMemory<byte> xml) {
        var stream = new MemoryStream(xml.ToArray(), writable: false);
        return XmlReader.Create(stream, Settings);
    }

    static void MoveToRoot(XmlReader reader, string rootName, string partPath) {
        reader.MoveToContent();

        if (reader.NodeType is not XmlNodeType.Element || reader.LocalName != rootName)
            throw new ThreeMFPackageException(
                $"'{partPath}' must have a <{rootName}> root element."
            );
    }

    static string RequireAttribute(XmlReader reader, string name, string element) {
        var value = reader.GetAttribute(name);

        if (string.IsNullOrWhiteSpace(value))
            throw new ThreeMFPackageException(
                $"'<{element}>' in OPC metadata is missing a required '{name}' attribute."
            );

        return value;
    }
}
