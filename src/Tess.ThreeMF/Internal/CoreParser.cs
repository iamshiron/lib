using System.Xml;

namespace Shiron.Lib.Tess.ThreeMF.Internal;

internal static class CoreParser {
    public const string CoreNamespace = "http://schemas.microsoft.com/3dmanufacturing/core/2015/02";
    public const string ModelRelationshipType = "http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel";
    public const string ModelContentType = "application/vnd.ms-package.3dmanufacturing-3dmodel+xml";

    static readonly char[] Whitespace = [' ', '\t', '\r', '\n'];

    static readonly XmlReaderSettings Settings = new() {
        DtdProcessing = DtdProcessing.Ignore,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreWhitespace = true,
        CloseInput = true,
    };

    public static Core Parse(ThreeMFPackage package, ThreeMFValidationMode validationMode = ThreeMFValidationMode.Standard) {
        ArgumentNullException.ThrowIfNull(package);

        var (path, xml) = SelectModelPart(package);

        return ParseModel(path, xml, validationMode);
    }

    static (string Path, ReadOnlyMemory<byte> Xml) SelectModelPart(ThreeMFPackage package) {
        var target = package.Relationships
            .FirstOrDefault(r => r.Type == ModelRelationshipType)
            ?.Target;

        if (target is not null) {
            var path = NormalizePartPath(target);

            if (package.TryGet(path, out var xml))
                return (path, xml);

            throw new ThreeMFCoreException(
                $"The 3D model relationship targets the part '{target}', which is not present in the package."
            );
        }

        foreach (var contentType in package.ContentTypes) {
            if (!contentType.IsOverride || contentType.MediaType != ModelContentType)
                continue;

            var path = NormalizePartPath(contentType.PartName!);

            if (package.TryGet(path, out var xml))
                return (path, xml);
        }

        throw new ThreeMFCoreException(
            "The package contains no readable 3D model part: no 3D model relationship or "
            + "model content type declaration resolved to an existing part."
        );
    }

    static string NormalizePartPath(string target) {
        if (string.IsNullOrWhiteSpace(target))
            throw new ThreeMFCoreException("The 3D model part path is empty.");

        if (target.Contains("://", StringComparison.Ordinal))
            throw new ThreeMFCoreException(
                $"The 3D model part target '{target}' is an external URI and cannot reference a package part."
            );

        return target.TrimStart('/');
    }

    static Core ParseModel(string path, ReadOnlyMemory<byte> xml, ThreeMFValidationMode validationMode) {
        try {
            using var stream = new MemoryStream(xml.ToArray(), writable: false);
            using var reader = XmlReader.Create(stream, Settings);

            reader.MoveToContent();

            if (reader.NodeType is not XmlNodeType.Element
                || reader.LocalName != "model"
                || reader.NamespaceURI != CoreNamespace) {
                throw new ThreeMFCoreException(
                    $"The model part '{path}' must have a <model> root element in the 3MF core namespace."
                );
            }

            var model = ReadModel(reader, validationMode);

            if (validationMode is ThreeMFValidationMode.Standard)
                ValidateReferences(model);

            return new Core { PartPath = path, Models = [model], MainModel = model };
        } catch (XmlException e) {
            throw new ThreeMFCoreException($"The model part '{path}' is not well-formed XML.", e);
        }
    }

    static Model ReadModel(XmlReader reader, ThreeMFValidationMode validationMode) {
        var unit = ParseUnit(reader.GetAttribute("unit"));
        var language = reader.GetAttribute("xml:lang");

        var metadata = new List<Metadata>();
        Resources? resources = null;
        var build = new Build();

        ReadChildren(reader, "model", child => {
            switch (child.LocalName) {
                case "metadata":
                    metadata.Add(ReadMetadata(child));
                    break;
                case "resources":
                    resources = ReadResources(child, validationMode);
                    break;
                case "build":
                    build = ReadBuild(child);
                    break;
                default:
                    throw UnknownElement(child, "model");
            }
        });

        return new Model {
            Unit = unit,
            Language = language,
            Metadata = metadata,
            Resources = resources
                ?? throw new ThreeMFCoreException("The <model> element is missing the required <resources> element."),
            Build = build,
        };
    }

    static Metadata ReadMetadata(XmlReader reader) {
        var name = RequireAttribute(reader, "name", "metadata");

        return new Metadata {
            Name = name,
            Value = reader.ReadElementContentAsString(),
        };
    }

    static Resources ReadResources(XmlReader reader, ThreeMFValidationMode validationMode) {
        var objects = new List<Object>();

        ReadChildren(reader, "resources", child => {
            if (child.LocalName != "object")
                throw UnknownElement(child, "resources");

            objects.Add(ReadObject(child, validationMode));
        });

        return new Resources { Objects = objects };
    }

    static Object ReadObject(XmlReader reader, ThreeMFValidationMode validationMode) {
        var id = ParseResourceID(RequireAttribute(reader, "id", "object"));
        var type = ParseObjectType(reader.GetAttribute("type"));
        var name = reader.GetAttribute("name");
        var partNumber = reader.GetAttribute("partnumber");

        Mesh? mesh = null;
        var components = new List<Component>();

        ReadChildren(reader, "object", child => {
            switch (child.LocalName) {
                case "mesh":
                    mesh = ReadMesh(child, id, validationMode);
                    break;
                case "components":
                    components.AddRange(ReadComponents(child));
                    break;
                default:
                    throw UnknownElement(child, "object");
            }
        });

        if (mesh is not null && components.Count > 0)
            throw new ThreeMFCoreException(
                $"The object {id} contains both a mesh and components; only one of them is allowed."
            );

        if (mesh is null && components.Count == 0)
            throw new ThreeMFCoreException(
                $"The object {id} must contain either a mesh or components."
            );

        return new Object {
            Id = id,
            Type = type,
            Name = name,
            PartNumber = partNumber,
            Mesh = mesh,
            Components = components,
        };
    }

    static Mesh ReadMesh(XmlReader reader, int objectId, ThreeMFValidationMode validationMode) {
        var vertices = new List<Vertex>();
        var triangles = new List<Triangle>();

        ReadChildren(reader, "mesh", child => {
            switch (child.LocalName) {
                case "vertices":
                    ReadVertices(child, vertices);
                    break;
                case "triangles":
                    ReadTriangles(child, triangles);
                    break;
                default:
                    throw UnknownElement(child, "mesh");
            }
        });

        if (validationMode is ThreeMFValidationMode.Standard) {
            for (var i = 0; i < triangles.Count; i++) {
                ValidateVertexIndex(triangles[i].V1, i, vertices.Count, objectId);
                ValidateVertexIndex(triangles[i].V2, i, vertices.Count, objectId);
                ValidateVertexIndex(triangles[i].V3, i, vertices.Count, objectId);
            }
        }

        return new Mesh { Vertices = vertices, Triangles = triangles };
    }

    static void ValidateVertexIndex(int index, int triangle, int vertexCount, int objectId) {
        if (index < 0 || index >= vertexCount)
            throw new ThreeMFCoreException(
                $"Triangle {triangle} of object {objectId} references vertex {index}, "
                + $"but the mesh has {vertexCount} vertices."
            );
    }

    static void ReadVertices(XmlReader reader, List<Vertex> vertices) {
        ReadChildren(reader, "vertices", child => {
            if (child.LocalName != "vertex")
                throw UnknownElement(child, "vertices");

            vertices.Add(new Vertex(
                ParseDouble(RequireAttribute(child, "x", "vertex"), "vertex 'x' coordinate"),
                ParseDouble(RequireAttribute(child, "y", "vertex"), "vertex 'y' coordinate"),
                ParseDouble(RequireAttribute(child, "z", "vertex"), "vertex 'z' coordinate")
            ));

            child.Skip();
        });
    }

    static void ReadTriangles(XmlReader reader, List<Triangle> triangles) {
        ReadChildren(reader, "triangles", child => {
            if (child.LocalName != "triangle")
                throw UnknownElement(child, "triangles");

            triangles.Add(new Triangle(
                ParseIndex(RequireAttribute(child, "v1", "triangle"), "triangle 'v1' index"),
                ParseIndex(RequireAttribute(child, "v2", "triangle"), "triangle 'v2' index"),
                ParseIndex(RequireAttribute(child, "v3", "triangle"), "triangle 'v3' index")
            ));

            child.Skip();
        });
    }

    static List<Component> ReadComponents(XmlReader reader) {
        var components = new List<Component>();

        ReadChildren(reader, "components", child => {
            if (child.LocalName != "component")
                throw UnknownElement(child, "components");

            components.Add(new Component {
                ObjectId = ParseResourceID(RequireAttribute(child, "objectid", "component")),
                Transform = ParseTransform(child.GetAttribute("transform")),
            });

            child.Skip();
        });

        return components;
    }

    static Build ReadBuild(XmlReader reader) {
        var items = new List<BuildItem>();

        ReadChildren(reader, "build", child => {
            if (child.LocalName != "item")
                throw UnknownElement(child, "build");

            items.Add(new BuildItem {
                ObjectId = ParseResourceID(RequireAttribute(child, "objectid", "item")),
                Transform = ParseTransform(child.GetAttribute("transform")),
                PartNumber = child.GetAttribute("partnumber"),
            });

            child.Skip();
        });

        return new Build { Items = items };
    }

    static void ReadChildren(XmlReader reader, string container, Action<XmlReader> handler) {
        if (reader.IsEmptyElement) {
            reader.Skip();
            return;
        }

        reader.Read();

        while (reader.NodeType is not (XmlNodeType.EndElement or XmlNodeType.None)) {
            if (reader.NodeType is not XmlNodeType.Element) {
                reader.Read();
                continue;
            }

            if (reader.NamespaceURI != CoreNamespace) {
                reader.Skip();
                continue;
            }

            handler(reader);
        }

        if (reader.NodeType is XmlNodeType.EndElement)
            reader.Read();
    }

    static void ValidateReferences(Model model) {
        var objects = new Dictionary<int, Object>();

        foreach (var obj in model.Resources.Objects) {
            if (!objects.TryAdd(obj.Id, obj))
                throw new ThreeMFCoreException($"Duplicate object resource id {obj.Id}.");
        }

        foreach (var obj in objects.Values) {
            foreach (var component in obj.Components) {
                if (!objects.TryGetValue(component.ObjectId, out var referenced))
                    throw new ThreeMFCoreException(
                        $"A component of object {obj.Id} references the unknown object {component.ObjectId}."
                    );

                if (referenced.Type is ObjectType.Other)
                    throw new ThreeMFCoreException(
                        $"A component of object {obj.Id} references object {referenced.Id}, "
                        + "which is of type 'other' and cannot be referenced."
                    );
            }
        }

        foreach (var item in model.Build.Items) {
            if (!objects.TryGetValue(item.ObjectId, out var referenced))
                throw new ThreeMFCoreException(
                    $"A build item references the unknown object {item.ObjectId}."
                );

            if (referenced.Type is ObjectType.Other)
                throw new ThreeMFCoreException(
                    $"A build item references object {referenced.Id}, "
                    + "which is of type 'other' and cannot be built."
                );
        }
    }

    static Unit ParseUnit(string? value) {
        return value?.ToLowerInvariant() switch {
            null => Unit.Millimeter,
            "micron" => Unit.Micron,
            "millimeter" => Unit.Millimeter,
            "centimeter" => Unit.Centimeter,
            "inch" => Unit.Inch,
            "foot" => Unit.Foot,
            "meter" => Unit.Meter,
            _ => throw new ThreeMFCoreException($"'{value}' is not a valid model unit."),
        };
    }

    static ObjectType ParseObjectType(string? value) {
        return value?.ToLowerInvariant() switch {
            null => ObjectType.Model,
            "model" => ObjectType.Model,
            "support" => ObjectType.Support,
            "other" => ObjectType.Other,
            _ => throw new ThreeMFCoreException($"'{value}' is not a valid object type."),
        };
    }

    static Transform? ParseTransform(string? value) {
        if (value is null)
            return null;

        var parts = value.Split(Whitespace, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 12)
            throw new ThreeMFCoreException(
                $"A transform must consist of 12 whitespace-separated numbers, got {parts.Length}."
            );

        var values = new double[12];

        for (var i = 0; i < values.Length; i++)
            values[i] = ParseDouble(parts[i], "transform value");

        return new Transform(
            values[0], values[1], values[2],
            values[3], values[4], values[5],
            values[6], values[7], values[8],
            values[9], values[10], values[11]
        );
    }

    static int ParseResourceID(string value) {
        var id = ParseInt32(value, "resource id");

        if (id < 1)
            throw new ThreeMFCoreException($"Resource ids must be positive integers, got '{value}'.");

        return id;
    }

    static int ParseIndex(string value, string what) {
        var index = ParseInt32(value, what);

        if (index < 0)
            throw new ThreeMFCoreException($"Vertex indices must be non-negative integers, got '{value}'.");

        return index;
    }

    static int ParseInt32(string value, string what) {
        try {
            return XmlConvert.ToInt32(value);
        } catch (Exception e) when (e is FormatException or OverflowException or ArgumentException) {
            throw new ThreeMFCoreException($"'{value}' is not a valid {what}.");
        }
    }

    static double ParseDouble(string value, string what) {
        try {
            return XmlConvert.ToDouble(value);
        } catch (Exception e) when (e is FormatException or OverflowException or ArgumentException) {
            throw new ThreeMFCoreException($"'{value}' is not a valid {what}.");
        }
    }

    static string RequireAttribute(XmlReader reader, string name, string element) {
        var value = reader.GetAttribute(name);

        if (string.IsNullOrWhiteSpace(value))
            throw new ThreeMFCoreException(
                $"The <{element}> element is missing the required '{name}' attribute."
            );

        return value;
    }

    static ThreeMFCoreException UnknownElement(XmlReader reader, string container) {
        return new ThreeMFCoreException(
            $"Unexpected <{reader.LocalName}> element inside <{container}> in the 3MF core namespace."
        );
    }
}
