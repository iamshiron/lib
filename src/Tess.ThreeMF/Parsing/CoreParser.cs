using System.Xml;
using Shiron.Lib.Tess.ThreeMF.Exceptions;
using Shiron.Lib.Tess.ThreeMF.Opc;

namespace Shiron.Lib.Tess.ThreeMF.Parsing;

internal static class CoreParser {
    public const string CoreNamespace = "http://schemas.microsoft.com/3dmanufacturing/core/2015/02";
    public const string ProductionNamespace = "http://schemas.microsoft.com/3dmanufacturing/production/2015/06";
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
        var walker = new PartWalker(package, validationMode);

        var mainModel = walker.ParsePartTree(path, xml);

        if (validationMode is ThreeMFValidationMode.Standard)
            ValidateReferences(package, walker.Order);

        return new Core {
            PartPath = path,
            Models = [.. walker.Order.Select(part => part.Model)],
            MainModel = mainModel,
            Parts = walker.Parts,
        };
    }

    /// <summary>
    /// Builds the OPC part path of the relationships part of the given model part,
    /// e.g. <c>3D/3dmodel.model</c> -> <c>3D/_rels/3dmodel.model.rels</c>.
    /// </summary>
    /// <param name="partPath">The normalized part path of a model part.</param>
    /// <returns>The part path of its relationships part.</returns>
    public static string GetRelationshipsPartPath(string partPath) {
        var separator = partPath.LastIndexOf('/');

        if (separator < 0)
            return $"_rels/{partPath}.rels";

        var directory = partPath[..(separator + 1)];
        var fileName = partPath[(separator + 1)..];

        return $"{directory}_rels/{fileName}.rels";
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

    /// <summary>
    /// Parses one model part and, depth-first in relationship document order, every
    /// model part linked from it through a 3D model relationship of its part-level
    /// relationships part. Cycle-safe: a back-edge to a part currently being parsed
    /// throws, already-completed parts are parsed only once.
    /// </summary>
    sealed class PartWalker(ThreeMFPackage package, ThreeMFValidationMode validationMode) {
        readonly Dictionary<string, Model> parsed = new(StringComparer.Ordinal);
        readonly HashSet<string> active = new(StringComparer.Ordinal);

        public List<(string Path, Model Model)> Order { get; } = [];

        public IReadOnlyDictionary<string, Model> Parts => parsed;

        public Model ParsePartTree(string path, ReadOnlyMemory<byte> xml) {
            if (active.Contains(path))
                throw new ThreeMFCoreException(
                    $"The 3D model relationships of the package form a cycle at the part '{path}'."
                );

            if (parsed.TryGetValue(path, out var existing))
                return existing;

            active.Add(path);

            var model = ParseModelXml(path, xml, validationMode);

            Order.Add((path, model));
            parsed[path] = model;

            foreach (var target in SelectLinkedPartPaths(package, path)) {
                ParsePartTree(target, package.Get(target));
            }

            active.Remove(path);

            return model;
        }
    }

    /// <summary>
    /// Selects the normalized part paths targeted by the 3D model relationships of the
    /// given model part, validating its part-level relationships part: malformed
    /// relationships XML and duplicate relationship ids throw, as do targets that are
    /// external URIs or missing from the package. Relationships of other types are
    /// ignored.
    /// </summary>
    static List<string> SelectLinkedPartPaths(ThreeMFPackage package, string partPath) {
        var relsPath = GetRelationshipsPartPath(partPath);

        if (!package.TryGet(relsPath, out var relsXml))
            return [];

        var relationships = OpcParser.ParseRelationships(relsXml);
        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var relationship in relationships) {
            if (!seenIds.Add(relationship.Id))
                throw new ThreeMFPackageException(
                    $"The relationships part '{relsPath}' declares the duplicate relationship id '{relationship.Id}'."
                );
        }

        var linked = new List<string>();

        foreach (var relationship in relationships) {
            if (relationship.Type != ModelRelationshipType)
                continue;

            var target = ResolveRelationshipTarget(relsPath, partPath, relationship.Target);

            if (!package.TryGet(target, out _))
                throw new ThreeMFCoreException(
                    $"The 3D model relationship '{relationship.Id}' declared in '{relsPath}' "
                    + $"targets the part '{target}', which is not present in the package."
                );

            linked.Add(target);
        }

        return linked;
    }

    /// <summary>
    /// Resolves a relationship target to a normalized package part path: absolute
    /// targets (leading <c>/</c>) name package parts directly, relative targets are
    /// resolved against the directory of the source part.
    /// </summary>
    static string ResolveRelationshipTarget(string relsPath, string sourcePartPath, string target) {
        if (string.IsNullOrWhiteSpace(target))
            throw new ThreeMFPackageException(
                $"A relationship declared in '{relsPath}' has an empty target."
            );

        if (target.Contains("://", StringComparison.Ordinal))
            throw new ThreeMFPackageException(
                $"The relationship target '{target}' declared in '{relsPath}' is an external URI "
                + "and cannot reference a package part."
            );

        var segments = new List<string>();

        if (target[0] != '/') {
            var separator = sourcePartPath.LastIndexOf('/');

            if (separator > 0)
                segments.AddRange(sourcePartPath[..separator].Split('/'));
        }

        foreach (var segment in target.Split('/')) {
            switch (segment) {
                case "" or ".":
                    continue;
                case "..":
                    if (segments.Count == 0)
                        throw new ThreeMFPackageException(
                            $"The relationship target '{target}' declared in '{relsPath}' escapes the package root."
                        );

                    segments.RemoveAt(segments.Count - 1);
                    break;
                default:
                    segments.Add(segment);
                    break;
            }
        }

        return string.Join("/", segments);
    }

    static Model ParseModelXml(string path, ReadOnlyMemory<byte> xml, ThreeMFValidationMode validationMode) {
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

            return ReadModel(reader, validationMode);
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
                PartPath = NormalizeComponentPath(child.GetAttribute("path", ProductionNamespace)),
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

    static void ValidateReferences(ThreeMFPackage package, IReadOnlyList<(string Path, Model Model)> parts) {
        var modelsByPath = new Dictionary<string, Model>(StringComparer.Ordinal);

        foreach (var (path, model) in parts)
            modelsByPath[path] = model;

        foreach (var (_, model) in parts) {
            var objects = new Dictionary<int, Object>();

            foreach (var obj in model.Resources.Objects) {
                if (!objects.TryAdd(obj.Id, obj))
                    throw new ThreeMFCoreException($"Duplicate object resource id {obj.Id}.");
            }

            foreach (var obj in objects.Values) {
                foreach (var component in obj.Components) {
                    var referenced = ResolveComponentReference(package, modelsByPath, objects, obj, component);

                    if (referenced.Type is not ObjectType.Other)
                        continue;

                    throw new ThreeMFCoreException(
                        component.PartPath is null
                            ? $"A component of object {obj.Id} references object {referenced.Id}, "
                              + "which is of type 'other' and cannot be referenced."
                            : $"A component of object {obj.Id} references object {referenced.Id} in the model "
                              + $"part '{component.PartPath}', which is of type 'other' and cannot be referenced."
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
    }

    /// <summary>
    /// Resolves a component reference: against the model of the part named by the
    /// production <c>p:path</c> attribute when present, against the local model
    /// otherwise.
    /// </summary>
    static Object ResolveComponentReference(
        ThreeMFPackage package,
        IReadOnlyDictionary<string, Model> modelsByPath,
        Dictionary<int, Object> localObjects,
        Object obj,
        Component component
    ) {
        if (component.PartPath is null) {
            if (!localObjects.TryGetValue(component.ObjectId, out var local))
                throw new ThreeMFCoreException(
                    $"A component of object {obj.Id} references the unknown object {component.ObjectId}."
                );

            return local;
        }

        if (!modelsByPath.TryGetValue(component.PartPath, out var targetModel)) {
            throw package.Contains(component.PartPath)
                ? new ThreeMFCoreException(
                    $"A component of object {obj.Id} references the model part '{component.PartPath}', "
                    + "which is not linked by a 3D model relationship."
                )
                : new ThreeMFCoreException(
                    $"A component of object {obj.Id} references the model part '{component.PartPath}', "
                    + "which is not present in the package."
                );
        }

        if (!targetModel.Resources.TryGetObject(component.ObjectId, out var referenced))
            throw new ThreeMFCoreException(
                $"A component of object {obj.Id} references the unknown object {component.ObjectId} "
                + $"in the model part '{component.PartPath}'."
            );

        return referenced;
    }

    static string? NormalizeComponentPath(string? value) {
        return string.IsNullOrWhiteSpace(value) ? null : value.TrimStart('/');
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
