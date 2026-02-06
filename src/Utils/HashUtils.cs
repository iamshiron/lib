using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Shiron.Lib.Logging;
using Shiron.Lib.Profiling;

namespace Shiron.Lib.Utils;

/// <summary>Utility functions for hashing files and file sets.</summary>
public static class HashUtils {
    /// <summary>Computes the SHA256 hash of a file.</summary>
    /// <param name="file">The path to the file to hash.</param>
    /// <param name="profiler">Optional profiler for performance measurement.</param>
    /// <returns>The SHA256 hash as a lowercase hexadecimal string.</returns>
    public static string HashFile(string file, IProfiler? profiler = null) {
        IDisposable? disposable = null;
        if (profiler is not null) disposable = new ProfileScope(profiler, MethodBase.GetCurrentMethod()!);

        using var stream = File.OpenRead(file);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(stream);

        disposable?.Dispose();
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>Computes the SHA256 hash of a string.</summary>
    /// <param name="input">The string to hash.</param>
    /// <param name="profiler">Optional profiler for performance measurement.</param>
    /// <returns>The SHA256 hash as a lowercase hexadecimal string.</returns>
    public static string HashString(string input, IProfiler? profiler = null) {
        ProfileScope? disposable = null;
        if (profiler is not null) disposable = new ProfileScope(profiler, MethodBase.GetCurrentMethod()!);

        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(inputBytes);

        disposable?.Dispose();
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// Creates a combined SHA256 hash for a set of files.
    /// Each file's hash is combined with its relative path (if root is provided) to
    /// ensure uniqueness.
    /// </summary>
    /// <param name="filePaths">The collection of file paths to hash.</param>
    /// <param name="root">The root directory to use for relative paths. If null, absolute paths are used.</param>
    /// <returns>The combined SHA256 hash as a lowercase hexadecimal string.</returns>
    public static string CreateFileSetHash(IEnumerable<string> filePaths, string? root = null, IProfiler? profiler = null) {
        ProfileScope? disposable = null;
        if (profiler is not null) disposable = new ProfileScope(profiler, MethodBase.GetCurrentMethod()!);

        var sortedFiles = filePaths.OrderBy(p => p).ToList();
        var individualHashes = sortedFiles.Select(path => {
            var filePath = root is not null ? Path.GetRelativePath(root, path) : path;
            var fileHash = HashFile(path);
            var pathHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(filePath)));
            return CombineHashes([fileHash, pathHash]);
        }).ToList();
        var combinedHashes = string.Concat(individualHashes);
        var combinedBytes = Encoding.UTF8.GetBytes(combinedHashes);
        var finalHash = SHA256.HashData(combinedBytes);

        return Convert.ToHexStringLower(finalHash);
    }

    /// <summary>
    /// Creates a dictionary of file paths and their corresponding hashes.
    /// The keys are the file paths relative to the specified root, and the values are the
    /// SHA256 hashes of the files.
    /// </summary>
    /// <param name="files">The collection of file paths to hash.</param>
    /// <param name="root">The root directory to use for relative paths. If null, absolute paths are used.</param>
    /// <returns></returns>
    public static async Task<Dictionary<string, string>> CreateFileSetHashesAsync(IEnumerable<string> files, string? root = null, IProfiler? profiler = null) {
        ProfileScope? disposable = null;
        if (profiler is not null) disposable = new ProfileScope(profiler, MethodBase.GetCurrentMethod()!);

        var result = new ConcurrentDictionary<string, string>();
        await Parallel.ForEachAsync(files, async (file, token) => {
            var filePath = root is not null ? Path.GetRelativePath(root, file) : file;
            var fileHash = await Task.Run(() => HashFile(file), token);
            _ = result.TryAdd(filePath, fileHash);
        });

        disposable?.Dispose();
        return result.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Combines multiple SHA256 hashes into a single SHA256 hash.
    /// </summary>
    /// <param name="hashes">The collection of hashes to combine.</param>
    /// <param name="profiler">Optional profiler for performance measurement.</param>
    /// <returns>The combined SHA256 hash as a lowercase hexadecimal string.</returns>
    public static string CombineHashes(IEnumerable<string> hashes, IProfiler? profiler = null) {
        ProfileScope? disposable = null;
        if (profiler is not null) disposable = new ProfileScope(profiler, MethodBase.GetCurrentMethod()!);

        var combined = string.Concat(hashes.OrderBy(h => h));
        var combinedBytes = Encoding.UTF8.GetBytes(combined);
        var finalHash = SHA256.HashData(combinedBytes);

        disposable?.Dispose();
        return Convert.ToHexStringLower(finalHash);
    }

    /// <summary>
    /// Computes the SHA256 hash of an object by serializing it to JSON.
    /// </summary>
    /// <param name="obj">The object to hash. Must be serializable by System.Text.Json.</param>
    /// <param name="profiler">Optional profiler for performance measurement.</param>
    /// <returns>The SHA256 hash as a lowercase hexadecimal string.</returns>
    /// <remarks>
    /// <para>
    /// <b>Performance Warning:</b> This method relies on JSON serialization, which allocates memory and is CPU-intensive.
    /// It is NOT suitable for hot paths (e.g., inside game loops).
    /// </para>
    /// <para>
    /// <b>Optimization Tip:</b> Avoid using this as a primary equality check for large objects. 
    /// Instead, try to check a frequently changing field first (such as a <c>LastModified</c> timestamp, <c>Version</c> ID, or <c>Generation</c> counter).
    /// Only resort to full object hashing if those simple checks pass or are unavailable.
    /// </para>
    /// </remarks>
    public static string HashObject(object obj, IProfiler? profiler = null) {
        ProfileScope? disposable = null;
        if (profiler is not null) disposable = new ProfileScope(profiler, MethodBase.GetCurrentMethod()!);

        var json = JsonSerializer.Serialize(obj);
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var hash = SHA256.HashData(jsonBytes);

        disposable?.Dispose();
        return Convert.ToHexStringLower(hash);
    }
}
