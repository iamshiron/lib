using System.IO.Compression;
using System.Text.Json;
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
    /// Writes every file of the package into a ZIP archive on the given stream.
    /// </summary>
    /// <param name="stream">The stream to write to; it is left open.</param>
    /// <param name="package">The package whose files are written.</param>
    /// <param name="options">The serialization options.</param>
    public static void SerializePackage(Stream stream, ThreeMFPackage package, ThreeMFSerializerOptions options) {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(package);

        using var zip = new ZipArchive(stream, ZipArchiveMode.Create, true);
        foreach (var (file, bytes) in package.Files) {
            var entry = zip.CreateEntry(file, options.CompressionLevel);
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
