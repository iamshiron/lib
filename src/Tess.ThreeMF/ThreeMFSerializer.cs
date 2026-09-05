using System.IO.Compression;
using Shiron.Lib.Tess.ThreeMF.Exceptions;
using Shiron.Lib.Tess.ThreeMF.Internal;

namespace Shiron.Lib.Tess.ThreeMF;

public static class ThreeMFSerializer {
    /// <summary>
    /// The part path of the OPC content-types stream within a 3MF package.
    /// </summary>
    public const string ContentTypesPath = "[Content_Types].xml";

    /// <summary>
    /// The part path of the package-level relationships part within a 3MF package.
    /// </summary>
    public const string RelationshipsPath = "_rels/.rels";

    /// <summary>
    /// Writes a 3MF document to a ZIP archive on the given stream. The writer is
    /// selected from the document's actual runtime type, preferring the most specific
    /// writer, e.g. <see cref="BambuGCodeThreeMFDocument"/> over
    /// <see cref="BambuThreeMFDocument"/> over <see cref="ThreeMFDocument"/>; writers
    /// registered through <see cref="ThreeMFExtensions"/> add further candidates for
    /// derived document types. Writers preserve the package's raw files by default.
    /// </summary>
    /// <param name="stream">The stream to write to; it is left open.</param>
    /// <param name="document">The document to write.</param>
    /// <param name="options">The serialization options; <see langword="null"/> uses <see cref="ThreeMFSerializerOptions.Default"/>.</param>
    public static void Serialize(Stream stream, ThreeMFDocument document, ThreeMFSerializerOptions? options = null) {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(document);

        var effectiveOptions = options ?? ThreeMFSerializerOptions.Default;

        DocumentWriterRegistry
            .Select(document.GetType(), effectiveOptions.Extensions.Writers)
            .Write(stream, document, effectiveOptions);
    }

    /// <summary>
    /// Writes every file of the package into a ZIP archive on the given stream.
    /// </summary>
    /// <param name="stream">The stream to write to; it is left open.</param>
    /// <param name="package">The package whose files are written.</param>
    /// <param name="options">The serialization options; <see langword="null"/> uses <see cref="ThreeMFSerializerOptions.Default"/>.</param>
    public static void SerializePackage(Stream stream, ThreeMFPackage package, ThreeMFSerializerOptions? options = null) {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(package);

        var effectiveOptions = options ?? ThreeMFSerializerOptions.Default;

        using var zip = new ZipArchive(stream, ZipArchiveMode.Create, true);
        foreach (var (file, bytes) in package.Files) {
            var entry = zip.CreateEntry(file, effectiveOptions.CompressionLevel);
            using var entryStream = entry.Open();
            entryStream.Write(bytes.Span);
        }
    }

    /// <summary>
    /// Reads a 3MF package from a ZIP archive on the given stream, including the OPC
    /// <c>[Content_Types].xml</c> and <c>_rels/.rels</c> metadata when present.
    /// </summary>
    /// <param name="stream">The stream to read from; it is left open.</param>
    /// <returns>The deserialized package.</returns>
    /// <exception cref="ThreeMFPackageException">
    /// Thrown when the stream is not a valid ZIP archive, contains duplicate file paths,
    /// or contains malformed OPC metadata.
    /// </exception>
    public static ThreeMFPackage DeserializePackage(Stream stream) {
        ArgumentNullException.ThrowIfNull(stream);

        var files = ReadFiles(stream);

        return new ThreeMFPackage {
            Files = files,
            ContentTypes = files.TryGetValue(ContentTypesPath, out var contentTypesXml)
                ? OpcParser.ParseContentTypes(contentTypesXml)
                : [],
            Relationships = files.TryGetValue(RelationshipsPath, out var relationshipsXml)
                ? OpcParser.ParseRelationships(relationshipsXml)
                : [],
        };
    }

    /// <summary>
    /// Reads a 3MF document from a ZIP archive on the given stream. The runtime
    /// document type is detected from the package contents via the registered document
    /// handlers; the built-in handlers cover standard and Bambu documents, extensions
    /// registered through <see cref="ThreeMFExtensions"/> add further candidates, and
    /// a valid generic 3MF yields exactly <see cref="ThreeMFDocument"/>.
    /// </summary>
    /// <param name="stream">The stream to read from; it is left open.</param>
    /// <param name="options">The deserialization options; <see langword="null"/> uses <see cref="ThreeMFSerializerOptions.Default"/>.</param>
    /// <returns>The deserialized document, whose runtime type is the detected document type.</returns>
    /// <exception cref="ThreeMFPackageException">
    /// Thrown when the stream is not a valid ZIP archive, contains duplicate file paths,
    /// or contains malformed OPC metadata.
    /// </exception>
    /// <exception cref="ThreeMFDocumentDetectionException">
    /// Thrown when no registered handler recognizes the package as a 3MF document, or
    /// when unrelated handlers claim it with equal authority
    /// (<see cref="ThreeMFAmbiguousDocumentTypeException"/>).
    /// </exception>
    /// <exception cref="ThreeMFCoreException">
    /// Thrown when the 3D model part is malformed or violates validation rules.
    /// </exception>
    public static ThreeMFDocument Deserialize(Stream stream, ThreeMFSerializerOptions? options = null) {
        ArgumentNullException.ThrowIfNull(stream);

        var effectiveOptions = options ?? ThreeMFSerializerOptions.Default;

        var files = ReadFiles(stream);

        var probeContext = new ThreeMFProbeContext {
            Files = files,
            Options = effectiveOptions,
        };

        var parseContext = new ThreeMFParseContext {
            Files = files,
            Options = effectiveOptions,
        };

        var handler = DocumentDetector.Select(BuildHandlers(effectiveOptions), probeContext);

        return handler.Parse(parseContext);
    }

    /// <summary>
    /// Reads a 3MF document from a ZIP archive on the given stream and requires the
    /// detected runtime document type to be assignable to <typeparamref name="TDocument"/>.
    /// Requesting a base type preserves the detected runtime type.
    /// </summary>
    /// <typeparam name="TDocument">The document type to require of the result.</typeparam>
    /// <param name="stream">The stream to read from; it is left open.</param>
    /// <param name="options">The deserialization options; <see langword="null"/> uses <see cref="ThreeMFSerializerOptions.Default"/>.</param>
    /// <returns>The deserialized document.</returns>
    /// <exception cref="ThreeMFDocumentTypeMismatchException">
    /// Thrown when the detected document type is not assignable to
    /// <typeparamref name="TDocument"/>.
    /// </exception>
    /// <inheritdoc cref="Deserialize(Stream, ThreeMFSerializerOptions?)"/>
    public static TDocument Deserialize<TDocument>(Stream stream, ThreeMFSerializerOptions? options = null)
        where TDocument : ThreeMFDocument {
        var document = Deserialize(stream, options);

        if (document is not TDocument matched)
            throw new ThreeMFDocumentTypeMismatchException(typeof(TDocument), document.GetType());

        return matched;
    }

    static IReadOnlyList<IThreeMFDocumentHandler> BuildHandlers(ThreeMFSerializerOptions options) {
        return [
            new StandardDocumentHandler(),
            new BambuDocumentHandler(),
            new BambuGCodeDocumentHandler(),
            .. options.Extensions.Handlers,
        ];
    }

    static Dictionary<string, ReadOnlyMemory<byte>> ReadFiles(Stream stream) {
        using var zip = OpenArchive(stream);

        var files = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);

        foreach (var entry in zip.Entries) {
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            var bytes = ReadEntry(entry);

            if (!files.TryAdd(entry.FullName, bytes))
                throw new ThreeMFPackageException(
                    $"The package contains a duplicate file path '{entry.FullName}'."
                );
        }

        return files;
    }

    static ZipArchive OpenArchive(Stream stream) {
        try {
            return new ZipArchive(stream, ZipArchiveMode.Read, true);
        } catch (InvalidDataException e) {
            throw new ThreeMFPackageException(
                "The stream is not a valid ZIP archive and cannot be a 3MF package.", e
            );
        }
    }

    static ReadOnlyMemory<byte> ReadEntry(ZipArchiveEntry entry) {
        try {
            using var entryStream = entry.Open();
            using var buffer = new MemoryStream();

            entryStream.CopyTo(buffer);

            return buffer.ToArray();
        } catch (InvalidDataException e) {
            throw new ThreeMFPackageException(
                $"Failed to read the file '{entry.FullName}' from the package: the entry data is corrupt.", e
            );
        }
    }
}
