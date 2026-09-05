using System.Diagnostics.CodeAnalysis;

namespace Shiron.Lib.Tess.ThreeMF;

/// <summary>
/// Represents the <c>&lt;resources&gt;</c> section of a 3MF model.
/// </summary>
public sealed class Resources {
    /// <summary>
    /// Gets the object resources in document order.
    /// </summary>
    public required IReadOnlyList<Object> Objects { get; init; }

    /// <summary>
    /// Attempts to get the object resource with the given id.
    /// </summary>
    /// <param name="id">The object resource id to look for.</param>
    /// <param name="resource">The matching object, when found.</param>
    /// <returns><see langword="true"/> if an object with the id exists.</returns>
    public bool TryGetObject(int id, [NotNullWhen(true)] out Object? resource) {
        foreach (var obj in Objects) {
            if (obj.Id == id) {
                resource = obj;
                return true;
            }
        }

        resource = null;
        return false;
    }
}
