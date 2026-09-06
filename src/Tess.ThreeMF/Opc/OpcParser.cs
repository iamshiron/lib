using System.Xml;
using Shiron.Lib.Tess.ThreeMF.Exceptions;

namespace Shiron.Lib.Tess.ThreeMF.Opc;

internal static class OpcParser {
    static readonly XmlReaderSettings Settings = new() {
        DtdProcessing = DtdProcessing.Ignore,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreWhitespace = true,
        CloseInput = true,
    };

    public static IReadOnlyList<ContentType> ParseContentTypes(ReadOnlyMemory<byte> xml) {
        try {
            using var reader = CreateReader(xml);

            MoveToRoot(reader, "Types", ThreeMFSerializer.ContentTypesPath);

            var contentTypes = new List<ContentType>();

            while (reader.Read()) {
                if (reader.NodeType is not XmlNodeType.Element || reader.Depth != 1)
                    continue;

                switch (reader.LocalName) {
                    case "Default":
                        contentTypes.Add(new ContentType {
                            Extension = RequireAttribute(reader, "Extension", "Default"),
                            MediaType = RequireAttribute(reader, "ContentType", "Default"),
                        });
                        break;
                    case "Override":
                        contentTypes.Add(new ContentType {
                            PartName = RequireAttribute(reader, "PartName", "Override"),
                            MediaType = RequireAttribute(reader, "ContentType", "Override"),
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

    public static IReadOnlyList<Relationship> ParseRelationships(ReadOnlyMemory<byte> xml) {
        try {
            using var reader = CreateReader(xml);

            MoveToRoot(reader, "Relationships", ThreeMFSerializer.RelationshipsPath);

            var relationships = new List<Relationship>();

            while (reader.Read()) {
                if (reader.NodeType is not XmlNodeType.Element || reader.Depth != 1)
                    continue;

                if (reader.LocalName != "Relationship")
                    continue;

                relationships.Add(new Relationship {
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
